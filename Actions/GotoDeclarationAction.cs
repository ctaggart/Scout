using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Text;
using JetBrains.ReSharper.Navigation;
using JetBrains.UI.PopupWindowManager;
using Microsoft.VisualStudio.Shell.Interop;

using JetBrains.ActionManagement;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

#if RS40
using JetBrains.DocumentModel;
using JetBrains.IDE;
using JetBrains.TextControl;
using JetBrains.VSIntegration.Shell;
using JetBrains.ReSharper.Psi.Caches;

#else
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Editor;
using JetBrains.ReSharper.EditorManager;
using JetBrains.ReSharper.TextControl;
using JetBrains.Shell;
using JetBrains.Shell.VSIntegration;
#endif

namespace ReSharper.Scout.Actions
{
	using DebugSymbols;

	[ActionHandler(ActionId, ActionId + "InContextMenu")]
	internal class GotoDeclarationAction : IActionHandler
	{
		public const string OverridenActionId = "GotoDeclaration";
		public const string ActionId = "Scout." + OverridenActionId;

		#region IActionHandler Members

		public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
		{
			return isAvailable(context) || nextUpdate();
		}

		public void Execute(IDataContext context, DelegateExecute nextExecute)
		{
			if (isAvailable(context))
			{
				execute(context);
			}
			else
			{
				nextExecute();
			}
		}

		#endregion

		#region Implementation

		private ITypeElement          _targetType;
		private IDeclaredElement      _targetElement;
		private ISolution             _solution;
		private ISymUnmanagedReader   _symbolReader;
		private long                  _moduleCookie;
		private readonly List<string> _parsedDocuments = new List<string>();
#if RS40
		private bool                  _gotSomeDocuments;
#endif

		private IAssembly assembly
		{
			get { return (IAssembly) _targetElement.Module; }
		}

		private string assemblyFilePath
		{
			get
			{
				foreach (IAssemblyFile file in assembly.GetFiles())
				{
					if (!file.IsMissing)
						return file.Location.FullPath;
				}

				return null;
			}
		}

		private static bool isAvailable(IDataContext context)
		{
			IDeclaredElement element = context.GetData(JetBrains.ReSharper.DataConstants.DECLARED_ELEMENT);

			if (element != null && element.Module != null &&
				element.Module.Name != null && !string.IsNullOrEmpty(element.XMLDocId))
				return true;

			IUpdatableAction action = ActionManager.Instance.GetAction(OverridenActionId);
			return action != null && action.Update(context);
		}

		private void execute(IDataContext context)
		{
			bool succeeded = false;
			_targetElement = context.GetData(JetBrains.ReSharper.DataConstants.DECLARED_ELEMENT);

			if (_targetElement != null)
			{
				if (_targetElement.Module is IAssembly)
				{
					List<INavigationResult> results = new List<INavigationResult>();

					_solution = _targetElement.GetManager().Solution;
#if RS40
					using (CommitCookie.Commit(_solution))
#else
					using (ReadLockCookie.Create())
#endif
					{
						Logger.LogMessage(LoggingLevel.VERBOSE, "Navigate to '{0}'", _targetElement.XMLDocId);

						_targetType = _targetElement is ITypeElement? (ITypeElement)_targetElement: _targetElement.GetContainingType();
						_symbolReader = getSymbolReader(assemblyFilePath);

						if (_symbolReader != null)
						{
							// The fast way first.
							//
							processSources(getPreferredTokens(), results, optimisticProcess);

							if (results.Count == 0)
							{
								List<uint> tokens = new List<uint>();
								getAllTypeElementTokens(_targetType, tokens);

								// The slow but more reliable way.
								//
								processSources(tokens, results, pessimisticProcess);
							}
						}
					}

					if (results.Count != 0)
					{
						string target = DeclaredElementPresenter.Format(_targetElement.Language,
							DeclaredElementPresenter.KIND_NAME_PRESENTER, _targetElement);
						Navigator.Navigate(true, _solution, PopupWindowContext.Empty.CreateLayouter(),
							PopupWindowContext.Empty, results, target);
						succeeded = true;
					}

					else
						succeeded = 
#if RS40
							_gotSomeDocuments ||
#endif
							executeAction(OpenWithReflectorAction.ActionId, context);
				}

				if (!succeeded)
					Navigator.Navigate(_targetElement, false, true);
			}

			cleanup();
		}

		private void cleanup()
		{
			_targetElement = null;
			_targetType    = null;
			_solution      = null;
			_symbolReader  = null;
			_moduleCookie  = 0L;
			_parsedDocuments.Clear();
#if RS40
			_gotSomeDocuments = false;
#endif
		}

		private delegate bool ProcessSourceDelegate(List<INavigationResult> results, string document, int line, int column);

		private void processSources(IEnumerable<uint> tokens, List<INavigationResult> results, ProcessSourceDelegate callback)
		{
			StringBuilder sb = new StringBuilder(2048);
			foreach (uint token in tokens)
			{
				ISymUnmanagedMethod um;
				if (_symbolReader.GetMethod(new SymbolToken((int) token), out um) < 0)
					continue;

				int count;
				um.GetSequencePointCount(out count);
				if (count > 0)
				{
					string                  documentUrl  = null;
					ISymUnmanagedDocument   prevDocument = null;
					ISymUnmanagedDocument[] documents    = new ISymUnmanagedDocument[count];
					int[] lines   = new int[count];
					int[] columns = new int[count];

					um.GetSequencePoints(count, out count, null, documents, lines, columns, null, null);

					for (int i = 0; i < count; ++i)
					{
						ISymUnmanagedDocument document = documents[i];

						if (document != prevDocument)
						{
							int urlLen;
							document.GetURL(sb.Capacity, out urlLen, sb);
							if (urlLen > 0)
								documentUrl = sb.ToString();

							prevDocument = document;
						}

						if (string.IsNullOrEmpty(documentUrl))
							continue;

						if (callback(results, documentUrl, lines[i], columns[i]))
							return;
					}
				}
			}
		}

		private bool optimisticProcess(List<INavigationResult> results, string sourceFilePath, int line, int column)
		{
			IProjectFile file;
			ITreeNode    rootNode;
			int offset = loadFile(sourceFilePath, line, column, out file, out rootNode);

			if (rootNode != null)
			{
				ITreeNode elementNode = rootNode.FindNextNode(delegate(ITreeNode n)
				{
					if (!n.GetTreeTextRange().Contains(offset))
						return TreeNodeActionType.IGNORE_SUBTREE;

					if (n is ITypeMemberDeclaration && !(n is ITypeDeclaration))
						return TreeNodeActionType.ACCEPT;

					return TreeNodeActionType.CONTINUE;
				});

				if (elementNode != null)
				{
					results.Add(getNavigationResult(file, elementNode));
					return true;
				}
			}

			return false;
		}

		private bool pessimisticProcess(List<INavigationResult> results, string sourceFilePath, int line, int column)
		{
			if (_parsedDocuments.Contains(sourceFilePath))
				return false;

			_parsedDocuments.Add(sourceFilePath);

			IProjectFile file;
			ITreeNode    rootNode;
			int offset = loadFile(sourceFilePath, line, column, out file, out rootNode);

			if (rootNode != null)
			{
				ITreeNode tn = rootNode.FindNextNode(delegate(ITreeNode n)
				{
					if (!n.GetTreeTextRange().Contains(offset))
						return TreeNodeActionType.IGNORE_SUBTREE;

					if (n is ITypeDeclaration)
					{
						ITypeDeclaration typeDecl = (ITypeDeclaration)n;

						if (typeDecl.DeclaredName == _targetType.ShortName)
							return TreeNodeActionType.ACCEPT;
					}
					else if (n is ITypeMemberDeclaration)
						return TreeNodeActionType.IGNORE_SUBTREE;

					return TreeNodeActionType.CONTINUE;
				});

				if (tn != null)
				{
					if (!_targetElement.Equals(_targetType))
					{
						// Loop through all type members
						//
						foreach (ITypeMemberDeclaration memDecl in ((ITypeDeclaration)tn).MemberDeclarations)
						{
							// TODO: There is still an issue with absract methods.
							// For fields\events\properties name check is enought.
							//
							if (memDecl.DeclaredName == _targetElement.ShortName)
							{
								tn = memDecl.ToTreeNode();
								break;
							}
						}
					}

					results.Add(getNavigationResult(file, tn));
				}
			}

			return false;
		}

#if RS40

		private IEnumerable<uint> getPreferredTokens()
		{
			if (_targetElement is IMetadataTokenOwner)
			{
				yield return ((IMetadataTokenOwner)_targetElement).Token;
			}
			
			if (_targetElement is IProperty)
			{
				IProperty prop = (IProperty) _targetElement;
				IMetadataTokenOwner getter = prop.Getter(true) as IMetadataTokenOwner;
				IMetadataTokenOwner setter = prop.Setter(true) as IMetadataTokenOwner;

				if (getter != null)
					yield return getter.Token;
				if (setter != null)
					yield return setter.Token;
			}
			else if (_targetElement is IEvent)
			{
				IEvent evt = (IEvent) _targetElement;
				IMetadataTokenOwner adder   = evt.Adder   as IMetadataTokenOwner;
				IMetadataTokenOwner remover = evt.Remover as IMetadataTokenOwner;
				IMetadataTokenOwner raiser  = evt.Raiser  as IMetadataTokenOwner;

				if (adder != null)
					yield return adder.Token;
				if (remover != null)
					yield return remover.Token;
				if (raiser != null)
					yield return raiser.Token;
			}
		}

		private static void getAllTypeElementTokens(ITypeElement typeElm, List<uint> tokens)
		{
			foreach (ITypeMember m in typeElm.GetMembers())
			{
				if (m is IMetadataTokenOwner)
					tokens.Add(((IMetadataTokenOwner)m).Token);

				// A nested type.
				//
				if (m is ITypeElement)
					getAllTypeElementTokens((ITypeElement)m, tokens);
			}
		}

#else

		private WeakReference<AssemblyLoader2> _loader;
		private IMetadataTypeInfo getMetaDataType(ITypeElement typeElm)
		{
			AssemblyLoader2 loader = _loader == null? null: _loader.Target;
			if (loader == null)
				_loader = new WeakReference<AssemblyLoader2>(loader = new AssemblyLoader2());

			IMetadataAssembly asm = loader.GetMdAssembly(assembly);
			if (asm == null)
			{
				loader.LoadAssembly(assembly);
				asm = loader.GetMdAssembly(assembly);
			}

			return asm == null? null: asm.GetTypeInfoFromQualifiedName(typeElm.CLRName, false);
		}

		private IEnumerable<uint> getPreferredTokens()
		{
			IMetadataTypeInfo typeInfo = getMetaDataType(_targetType);

			if (typeInfo != null)
			{
				if (_targetElement is IFunction)
				{
					IFunction f = (IFunction)_targetElement;

					foreach (IMetadataMethod mm in typeInfo.GetMethods())
					{
						if (mm.Name == _targetElement.ShortName && checkSignature(f, mm))
							yield return mm.Token.Value;
					}
				}
				else if (_targetElement is IProperty)
				{
					foreach (IMetadataProperty mp in typeInfo.GetProperties())
					{
						if (mp.Name != _targetElement.ShortName)
							continue;

						if (mp.Getter   != null)
							yield return mp.Getter.Token.Value;
						if (mp.Setter != null)
							yield return mp.Setter.Token.Value;
					}
				}
				else if (_targetElement is IEvent)
				{
					foreach (IMetadataEvent me in typeInfo.GetEvents())
					{
						if (me.Name != _targetElement.ShortName)
							continue;

						if (me.Adder   != null)
							yield return me.Adder.Token.Value;
						if (me.Remover != null)
							yield return me.Remover.Token.Value;
						if (me.Raiser  != null)
							yield return me.Raiser.Token.Value;
					}
				}
			}
		}

		private void getAllTypeElementTokens(ITypeElement typeElm, List<uint> tokens)
		{
			IMetadataTypeInfo typeInfo = getMetaDataType(typeElm);

			if (typeInfo == null)
				return;

			tokens.AddRange(Array.ConvertAll<IMetadataMethod, uint>(typeInfo.GetMethods(),
				delegate(IMetadataMethod method) { return method.Token.Value; }));

			foreach (ITypeElement nestedType in typeElm.NestedTypes)
				getAllTypeElementTokens(nestedType, tokens);
		}

		private static bool checkSignature(IParametersOwner po, IMetadataMethod mm)
		{
			if (po.Parameters.Count != mm.Parameters.Length)
				return false;

			for (int i = 0; i < mm.Parameters.Length; i++)
			{
				IParameter         pp = po.Parameters[i];
				IMetadataParameter mp = mm.Parameters[i];

				// C# compiler produces arrays with wrong lower bound value.
				// It must be -1..-1 instead of 0..-1, so fix them.
				//
				string mpName = mp.Type.PresentableName.Replace("0..", string.Empty);

				if (!mpName.Equals(pp.Type.ToString()))
					return false;
			}

			return true;
		}

#endif

		private INavigationResult getNavigationResult(IProjectFile projectFile, ITreeNode node)
		{
			TextRange range = (node is IDeclaration)? ((IDeclaration) node).GetNameRange(): node.GetTreeTextRange();
			return new TextControlNavigationResult(_solution, projectFile, range, range.StartOffset);
		}

		private static ISymUnmanagedReader getSymbolReader(string assemblyFilePath)
		{
			IVsSmartOpenScope vsScope = (IVsSmartOpenScope)
				VSShell.Instance.GetService(typeof (SVsSmartOpenScope));

			// Visual studio is broken?
			//
			if (vsScope == null)
			{
				Logger.LogMessage(LoggingLevel.NORMAL, "Failed to query VS for IVsSmartOpenScope");
				return null;
			}

			Guid iid = JetBrains.Metadata.Access.Constants.IID_IMetaDataImport;
			ISymUnmanagedBinder2 binder = (ISymUnmanagedBinder2) new JetBrains.Metadata.Access.CorSymBinder_SxSClass();
			object unkMetaDataImport;
			int hr = vsScope.OpenScope(assemblyFilePath, 0, ref iid, out unkMetaDataImport);

			if (hr < 0)
			{
				Logger.LogMessage(LoggingLevel.NORMAL, "IVsSmartOpenScope.OpenScope({0}) == {1:X}", assemblyFilePath, hr);
				return null;
			}

			string symbolPath = string.Join("*", new string[]
				{"srv", Options.SymbolCacheDir, Options.SymbolPath});

			ISymUnmanagedReader reader;
			return binder.GetReaderForFile2(unkMetaDataImport,
				assemblyFilePath, symbolPath, 0x0F, out reader) < 0? null: reader;
		}

		private string ensureSourceFile(string sourceFilePath)
		{
			// Download from the source server if enabled.
			//
			if (!File.Exists(sourceFilePath) && Options.UsePdbFiles && _symbolReader is ISymUnmanagedSourceServerModule)
			{
				if (_moduleCookie == 0L)
				{
					ISymUnmanagedSourceServerModule ssm = (ISymUnmanagedSourceServerModule)_symbolReader;
					_moduleCookie = SrcSrv.Instance.LoadModule(assemblyFilePath, ssm);
				}

				if (_moduleCookie != 0L)
				{
					string sourceFileUrl = SrcSrv.Instance.GetFileUrl(sourceFilePath, _moduleCookie);
					if (!string.IsNullOrEmpty(sourceFileUrl))
					{
						sourceFilePath = SymSrv.DownloadFile(sourceFileUrl, Options.SymbolCacheDir);
					}
				}
			}

			return sourceFilePath;
		}

		private static bool executeAction(string actionId, IDataContext context)
		{
			IExecutableAction action = (IExecutableAction)ActionManager.Instance.GetAction(actionId);
			bool available = action != null && action.Update(context);

			if (available)
				action.Execute(context);

			return available;
		}

		private int loadFile(string sourceFilePath, int line, int column, out IProjectFile file, out ITreeNode rootNode)
		{
			file     = null;
			rootNode = null;

			sourceFilePath = ensureSourceFile(sourceFilePath);

			if (!File.Exists(sourceFilePath))
				return -1;

			Logger.LogMessage(LoggingLevel.NORMAL, "Open {0}", sourceFilePath);

#if RS40
			// Open the file as read only.
			//
			ITextControl textControl = EditorManager.GetInstance(_solution).OpenFile(sourceFilePath, true, true);
#else
			// Open the file using ReSharper services.
			//
			ITextControl textControl = EditorManager.GetInstance(_solution)
				.OpenProjectFile(sourceFilePath, true);

			// Open the file again using EnvDTE services to make it ReadOnly.
			//
			VSShell.Instance.ApplicationObject.ItemOperations.OpenFile(
				sourceFilePath, EnvDTE.Constants.vsViewKindCode).Document.ReadOnly = true;
#endif

			if (textControl == null)
			{
#if RS40
				_gotSomeDocuments = VSShell.Instance.ApplicationObject.ItemOperations.OpenFile(
					sourceFilePath, EnvDTE.Constants.vsViewKindCode) != null;
#endif
				return -1;
			}

			IDocument document = textControl.Document;
			file = DocumentManager.GetInstance(_solution).GetProjectFile(document);

			if (file == null)
				return -1;

			PsiLanguageType languageType    = ProjectFileLanguageServiceManager.Instance.GetPsiLanguageType(file);
			LanguageService languageService = LanguageServiceManager.Instance.GetLanguageService(languageType);

			if (languageService == null)
				return -1;

			ILexer  lexer  = languageService.CreateCachingLexer(document.Buffer);
			IParser parser = languageService.CreateParser(lexer, _solution, null);

			rootNode = parser.ParseFile().ToTreeNode();

			// Convert line & row into a plain offset value.
			//
			return textControl.VisualToLogical(new VisualPosition(line, column)).Offset;
		}

		#endregion
	}
}