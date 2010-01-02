using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Text;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Parsing;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;
using Microsoft.VisualStudio.Shell.Interop;

#if RS30
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Editor;
using JetBrains.ReSharper.TextControl;
#else
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Psi.Caches;
using JetBrains.TextControl;
#endif

#if RS45 || RS50
using JetBrains.ReSharper.Feature.Services.Navigation;
#else
using INavigationPoint = JetBrains.ReSharper.Navigation.INavigationResult;
#endif

using JetBrains.Metadata.Access;
using Constants = JetBrains.Metadata.Access.Constants;

namespace ReSharper.Scout.DebugSymbols
{
	internal class ReferenceSource
	{
		private readonly List<string>        _parsedDocuments   = new List<string>();
		private readonly ISymUnmanagedReader _symbolReader;
		
		private readonly IDeclaredElement    _targetElement;
		private readonly ITypeElement        _targetType;
		private readonly ISolution           _solution;
		private readonly string              _assemblyFilePath;
		private          long                _moduleCookie;

		public ReferenceSource(IDeclaredElement targetElement)
		{
			if (targetElement == null)
				throw new ArgumentNullException("targetElement");

			_targetElement = targetElement;
			_targetType    = targetElement is ITypeElement ? (ITypeElement)targetElement : targetElement.GetContainingType();
			_solution      = _targetElement.GetManager().Solution;

			IAssembly assembly = ReSharper.GetAssembly(targetElement);
			if (assembly == null)
				throw new ArgumentException("targetElement");

			foreach (IAssemblyFile file in assembly.GetFiles())
			{
				if (file.IsMissing)
					continue;

				_symbolReader = GetSymbolReader(file.Location.FullPath);

				if (_symbolReader == null)
					continue;

				_assemblyFilePath = file.Location.FullPath;
				break;
			}
		}

		public List<INavigationPoint> GetNavigationPoints()
		{
			List<INavigationPoint> results = new List<INavigationPoint>();

			if (_symbolReader == null)
				return results;

			// The fast way first.
			//
			ProcessSources(GetPreferredTokens(), results, OptimisticProcess);

			if (results.Count == 0 && _targetType != null)
			{
				List<uint> tokens = new List<uint>();
				GetAllTypeElementTokens(_targetType, tokens);

				// The slow but more reliable way.
				//
				ProcessSources(tokens, results, PessimisticProcess);
			}

			return results;
		}

		private static ISymUnmanagedReader GetSymbolReader(string assemblyFilePath)
		{
			IVsSmartOpenScope vsScope = ReSharper.GetVsService<SVsSmartOpenScope, IVsSmartOpenScope>();

			Guid iid = Constants.IID_IMetaDataImport;
			ISymUnmanagedBinder2 binder = (ISymUnmanagedBinder2) new CorSymBinder_SxSClass();
			object unkMetaDataImport;
			int hr = vsScope.OpenScope(assemblyFilePath, 0, ref iid, out unkMetaDataImport);

			if (hr < 0)
			{
				Logger.LogMessage(LoggingLevel.NORMAL, "IVsSmartOpenScope.OpenScope({0}) == {1:X}", assemblyFilePath, hr);
				return null;
			}

            string cacheDir = Options.SymbolCacheDir;
            ISymUnmanagedReader reader = null;
            foreach (string path in Options.SymbolPath.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
		        string symbolPath = string.Join("*", new string[] {"srv", cacheDir, path});

                if (binder.GetReaderForFile2(unkMetaDataImport, assemblyFilePath,
                    symbolPath, 0x0F, out reader) >= 0)
                {
                    break;
                }
            }

		    return reader;
        }

		private delegate bool ProcessSourceDelegate(List<INavigationPoint> results, string document, int line, int column);

		private void ProcessSources(IEnumerable<uint> tokens, List<INavigationPoint> results, ProcessSourceDelegate callback)
		{
			StringBuilder sb = new StringBuilder(2048);
			foreach (uint token in tokens)
			{
				ISymUnmanagedMethod um;
				if (_symbolReader.GetMethod(new SymbolToken((int) token), out um) < 0)
					continue;

				int count;
				um.GetSequencePointCount(out count);
				if (count <= 0)
					continue;

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

		private bool OptimisticProcess(List<INavigationPoint> results, string sourceFilePath, int line, int column)
		{
			IProjectFile file;
			ITreeNode    rootNode;
			int offset = LoadFile(sourceFilePath, line, column, out file, out rootNode);

			if (rootNode != null)
			{
				ITreeNode elementNode = rootNode.FindNextNode(delegate(ITreeNode n)
				{
					if (!n.GetTreeTextRange().Contains(
#if RS50
                        new TreeOffset(offset)
#else
                        offset
#endif
                        ))
						return TreeNodeActionType.IGNORE_SUBTREE;

					if (n is ITypeMemberDeclaration && !(n is ITypeDeclaration))
						return TreeNodeActionType.ACCEPT;

					return TreeNodeActionType.CONTINUE;
				});

				if (elementNode != null)
				{
					results.Add(ReSharper.CreateSourceNavigationPoint(_solution, file, elementNode));
					return true;
				}
			}
#if RS40
			else if (offset >= 0)
			{
				// We got a file, but failed to find the declaration

				results.Add(null);
				return true;
			}
#endif
			return false;
		}

		private bool PessimisticProcess(List<INavigationPoint> results, string sourceFilePath, int line, int column)
		{
			if (_parsedDocuments.Contains(sourceFilePath))
				return false;

			_parsedDocuments.Add(sourceFilePath);

			IProjectFile file;
			ITreeNode    rootNode;
			int offset = LoadFile(sourceFilePath, line, column, out file, out rootNode);

			if (rootNode != null)
			{
				ITreeNode tn = rootNode.FindNextNode(delegate(ITreeNode n)
				{
					if (!n.GetTreeTextRange().Contains(
#if RS50
                        new TreeOffset(offset)
#else
                        offset
#endif
                        ))
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

					results.Add(ReSharper.CreateSourceNavigationPoint(_solution, file, tn));
				}
			}
#if RS40
			else if (offset >= 0)
			{
				// We got a file, but failed to find the declaration

				results.Add(null);
				return true;
			}
#endif
			return false;
		}

#if RS30

		private WeakReference<AssemblyLoader2> _loader;
		private IMetadataTypeInfo GetMetaDataType(ITypeElement typeElm)
		{
			if (typeElm == null)
				return null;

			AssemblyLoader2 loader = _loader == null? null: _loader.Target;
			if (loader == null)
				_loader = new WeakReference<AssemblyLoader2>(loader = new AssemblyLoader2());

			IAssembly assembly = (IAssembly)_targetElement.Module;
			IMetadataAssembly asm = loader.GetMdAssembly(assembly);
			if (asm == null)
			{
				loader.LoadAssembly(assembly);
				asm = loader.GetMdAssembly(assembly);
			}

			return asm == null? null: asm.GetTypeInfoFromQualifiedName(typeElm.CLRName, false);
		}

		private IEnumerable<uint> GetPreferredTokens()
		{
			IMetadataTypeInfo typeInfo = GetMetaDataType(_targetType);

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

		private void GetAllTypeElementTokens(ITypeElement typeElm, List<uint> tokens)
		{
			IMetadataTypeInfo typeInfo = GetMetaDataType(typeElm);

			if (typeInfo == null)
				return;

			tokens.AddRange(Array.ConvertAll<IMetadataMethod, uint>(typeInfo.GetMethods(),
				delegate(IMetadataMethod method) { return method.Token.Value; }));

			foreach (ITypeElement nestedType in typeElm.NestedTypes)
				GetAllTypeElementTokens(nestedType, tokens);
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
#else

		private IEnumerable<uint> GetPreferredTokens()
		{
			if (_targetElement is IMetadataTokenOwner)
			{
				yield return ReSharper.GetToken(((IMetadataTokenOwner)_targetElement).Token);
			}
			
			if (_targetElement is IProperty)
			{
				IProperty prop = (IProperty)_targetElement;
				IMetadataTokenOwner getter = prop.Getter as IMetadataTokenOwner;
				IMetadataTokenOwner setter = prop.Setter as IMetadataTokenOwner;

				if (getter != null)
					yield return ReSharper.GetToken(getter.Token);
				if (setter != null)
					yield return ReSharper.GetToken(setter.Token);
			}
			else if (_targetElement is IEvent)
			{
				IEvent evt = (IEvent)_targetElement;
				IMetadataTokenOwner adder   = evt.Adder   as IMetadataTokenOwner;
				IMetadataTokenOwner remover = evt.Remover as IMetadataTokenOwner;
				IMetadataTokenOwner raiser  = evt.Raiser  as IMetadataTokenOwner;

				if (adder != null)
					yield return ReSharper.GetToken(adder.Token);
				if (remover != null)
					yield return ReSharper.GetToken(remover.Token);
				if (raiser != null)
					yield return ReSharper.GetToken(raiser.Token);
			}
		}

		private static void GetAllTypeElementTokens(ITypeElement typeElm, List<uint> tokens)
		{
			foreach (ITypeMember m in typeElm.GetMembers())
			{
				if (m is IMetadataTokenOwner)
					tokens.Add(ReSharper.GetToken(((IMetadataTokenOwner)m).Token));

				// A nested type.
				//
				if (m is ITypeElement)
					GetAllTypeElementTokens((ITypeElement)m, tokens);
			}
		}

#endif

		private string EnsureSourceFile(string sourceFilePath)
		{
			// Download from the source server if enabled.
			//
			if (!File.Exists(sourceFilePath) && Options.UsePdbFiles && _symbolReader is ISymUnmanagedSourceServerModule)
			{
				if (_moduleCookie == 0L)
				{
					ISymUnmanagedSourceServerModule ssm = (ISymUnmanagedSourceServerModule)_symbolReader;
					_moduleCookie = SrcSrv.Instance.LoadModule(_assemblyFilePath, ssm);
				}

				if (_moduleCookie != 0L)
				{
					string sourceFileUrl = SrcSrv.Instance.GetFileUrl(sourceFilePath, _moduleCookie);
					if (!string.IsNullOrEmpty(sourceFileUrl))
					{
						sourceFilePath = File.Exists(sourceFileUrl)? sourceFileUrl:
							SymSrv.DownloadFile(sourceFileUrl, Options.SymbolCacheDir);
					}
				}
			}

			return sourceFilePath;
		}

		private int LoadFile(string sourceFilePath, int line, int column, out IProjectFile file, out ITreeNode rootNode)
		{
			file     = null;
			rootNode = null;

			sourceFilePath = EnsureSourceFile(sourceFilePath);

			if (!File.Exists(sourceFilePath))
				return -1;

			Logger.LogMessage(LoggingLevel.NORMAL, "Open {0}", sourceFilePath);

			ITextControl textControl = ReSharper.OpenSourceFile(sourceFilePath, _solution);

			if (textControl == null)
			{
#if RS40
				// There is a bug in ReSharper 4.0/4.1
				// Fixed in 4.5
				//
				if (ReSharper.VsShell.ApplicationObject.ItemOperations.OpenFile(
					sourceFilePath, EnvDTE.Constants.vsViewKindCode) != null)
				{
					return 0;
				}
#endif
				return -1;
			}

			IDocument document = textControl.Document;
			file = DocumentManager.GetInstance(_solution).GetProjectFile(document);

			if (file == null)
				return 0;

			PsiLanguageType languageType    = ProjectFileLanguageServiceManager.Instance.GetPsiLanguageType(file);
			LanguageService languageService = LanguageServiceManager.Instance.GetLanguageService(languageType);

			if (languageService == null)
				return 0;

			ILexer  lexer  = languageService.CreateCachingLexer(document.Buffer);
			IParser parser = languageService.CreateParser(lexer, _solution, null
#if RS50
				, file
#endif
				);

			// Convert line & row into a plain offset value.
			//
#if RS50
			rootNode = parser.ParseFile(true).ToTreeNode();
			TextControlLineColumn textcoords = new TextControlLineColumn(
				(Int32<TextControlLine>)(line - 1),
				(Int32<TextControlColumn>)(column - 1));

			return textControl.Coords.FromTextControlLineColumn(textcoords).ToDocOffset();
#else
			rootNode = parser.ParseFile().ToTreeNode();
			return textControl.VisualToLogical(new VisualPosition(line - 1, column - 1)).Offset;
#endif

		}
	}
}
