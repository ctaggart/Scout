using System;
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
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
using ProjectModelDataConstants=JetBrains.IDE.DataConstants;
#else
using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Editor;
using JetBrains.ReSharper.EditorManager;
using JetBrains.ReSharper.TextControl;
using JetBrains.Shell.VSIntegration;
using ProjectModelDataConstants=JetBrains.ReSharper.DataConstants;
#endif

namespace ReSharper.Scout
{
	using DebugSymbols;
	using Reflector;

	[ActionHandler("Scout.GotoDeclaration", "Scout.GotoDeclarationInContextMenu", "Scout.OpenWithReflector")]
	internal class GotoDeclarationAction : IActionHandler
	{
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

		private static bool isAvailable(IDataContext context)
		{
			IDeclaredElement element = context.GetData(JetBrains.ReSharper.DataConstants.DECLARED_ELEMENT);

			if (element != null && element.Module != null &&
				element.Module.Name != null && !string.IsNullOrEmpty(element.XMLDocId))
			{
				return true;
			}

			_DTE dte = VSShell.Instance.ApplicationObject;
			if (dte.ActiveWindow == null || !Options.UseReflector)
				return false;

			string  toolWindowKind  = dte.ActiveWindow.ObjectKind;
			Command requiredCommand = null;

			if (toolWindowKind == ToolWindowGuids80.SolutionExplorer)
			{
				IProjectModelElement projectModelElement = context.GetData(ProjectModelDataConstants.PROJECT_MODEL_ELEMENT);

				if (projectModelElement is IModuleReference)
				{
					IModuleReference moduleReference = (IModuleReference)projectModelElement;

					return moduleReference.ResolveReferencedModule() != null;
				}
			}
			else if (toolWindowKind == ToolWindowGuids80.Modules || toolWindowKind == ToolWindowGuids80.ObjectBrowser)
				requiredCommand = dte.Commands.Item("Edit.Copy", 0);
			else if (toolWindowKind == ToolWindowGuids80.CallStack)
				requiredCommand = dte.Commands.Item("DebuggerContextMenus.CallStackWindow.SwitchToFrame", 0);

			return requiredCommand != null && requiredCommand.IsAvailable;
		}

		private static void execute(IDataContext context)
		{
			IDeclaredElement element = context.GetData(JetBrains.ReSharper.DataConstants.DECLARED_ELEMENT);

			if (element != null)
			{
				Logger.LogMessage(LoggingLevel.VERBOSE, "Navigate to '{0}'", element.XMLDocId);

				if (!(element.Module is IProject))
				{
					if (Options.UsePdbFiles && element.Module is IAssembly)
					{
						string pathToAssemblyFile = getAssemblyFile((IAssembly) element.Module);
						ISymUnmanagedReader reader = getSymbolReader(pathToAssemblyFile);
						List<uint> tokens = getElementTokens(element);

						if (reader != null && tokens != null && tokens.Count != 0)
						{
							int line = 0, col = 0;
							string sourceFilePath = null;

							for (int i = 0; i < tokens.Count && sourceFilePath == null; i++)
							{
								ISymUnmanagedMethod um;
								if (reader.GetMethod(new SymbolToken((int) tokens[i]), out um) < 0)
									continue;

								sourceFilePath = getMethodSourceFile(um, out line, out col);
							}

							if (sourceFilePath != null)
							{
								// Download from the source server if enabled.
								//
								if (!File.Exists(sourceFilePath) && reader is ISymUnmanagedSourceServerModule)
								{
									ISymUnmanagedSourceServerModule ssm = (ISymUnmanagedSourceServerModule) reader;
									long moduleCookie = SrcSrv.Instance.LoadModule(pathToAssemblyFile, ssm);
									if (moduleCookie != 0L)
									{
										string sourceFileUrl = SrcSrv.Instance.GetFileUrl(sourceFilePath, moduleCookie);
										if (!string.IsNullOrEmpty(sourceFileUrl))
										{
											sourceFilePath = SymSrv.DownloadFile(sourceFileUrl, Options.SymbolCacheDir);
										}
									}
								}

								if (File.Exists(sourceFilePath))
								{
									Logger.LogMessage(LoggingLevel.NORMAL, "Open {0}", sourceFilePath);

#if RS40
									// Open the file as read only.
									//
									ITextControl textControl = EditorManager.GetInstance(element.GetManager().Solution)
										.OpenFile(sourceFilePath, true, true);
#else

									// Open the file using ReSharper services.
									//
									ITextControl textControl = EditorManager.GetInstance(element.GetManager().Solution)
										.OpenProjectFile(sourceFilePath, true);

									// Open the file again using EnvDTE services to make it ReadOnly.
									//
									VSShell.Instance.ApplicationObject.ItemOperations.OpenFile(
										sourceFilePath, EnvDTE.Constants.vsViewKindCode).Document.ReadOnly = true;
#endif
									// Jump to somewhere near to our target.
									//
									textControl.CaretModel.MoveTo(new VisualPosition(line - 1, col - 1));

									// Adjust the position, is possible.
									//
									fineTuneElementPosition(element, textControl);
									return;
								}
							}
						}
					}

					// Pdb recovery was failed or is turned off.
					//
					if (Options.UseReflector)
					{
						if (element.Module != null)
							loadModule(element.Module);

						RemoteController.Instance.Select(element.XMLDocId);
						return;
					}
				}

				// An internal declaration or nothing is enabled.
				//
				ActionManager.Instance.ExecuteAction("GotoDeclaration");
			}
			else if (Options.UseReflector)
			{
				// Check for call stack & modules
				//
				DTE dte = VSShell.Instance.ApplicationObject;
				if (dte.ActiveWindow != null)
				{
					string toolWindowKind = dte.ActiveWindow.ObjectKind;

					if (toolWindowKind == ToolWindowGuids80.SolutionExplorer)
					{
						IProjectModelElement project = context.GetData(ProjectModelDataConstants.PROJECT_MODEL_ELEMENT);
						loadModuleByReference(project);
					}
					else if (toolWindowKind == ToolWindowGuids80.ObjectBrowser)
						loadFromObjectBrowserWindow();
					else if (toolWindowKind == ToolWindowGuids80.Modules)
						loadFromModulesWindow(dte);
					else if (toolWindowKind == ToolWindowGuids80.CallStack)
						loadFromStackFrameWindow(dte);
				}
			}
		}

		private static void fineTuneElementPosition(IDeclaredElement element, ITextControl textCtl)
		{
			ISolution solution = element.GetManager().Solution;
			IProjectFile file = DocumentManager.GetInstance(solution).GetProjectFile(textCtl.Document);

			if (file == null)
				return;

			PsiLanguageType languageType = ProjectFileLanguageServiceManager.Instance.GetPsiLanguageType(file);
			LanguageService languageService = LanguageServiceManager.Instance.GetLanguageService(languageType);

			if (languageService == null)
				return;

			int targetOffset = textCtl.CaretModel.Offset;

			ILexer  lexer  = languageService.CreateCachingLexer(textCtl.Document.Buffer);
			IParser parser = languageService.CreateParser(lexer, solution, null);

			ITreeNode tn = parser.ParseFile().ToTreeNode();

			tn = findParsedNode(targetOffset, element, tn);
			if (tn != null)
			{
				JetBrains.Util.TextRange range = (tn is IDeclaration)? ((IDeclaration)tn).GetNameRange(): tn.GetTreeTextRange();
				targetOffset = range.StartOffset;
			}

			textCtl.CaretModel.MoveTo(targetOffset);
		}

		private static ITreeNode findParsedNode(int targetOffset, IDeclaredElement elm, ITreeNode root)
		{
			if (elm == null)
				throw new ArgumentNullException("elm");

			if (elm is IFunction && !((IFunction)elm).IsAbstract)
			{
				return root.FindNextNode(delegate(ITreeNode n)
				{
					if (!n.GetTreeTextRange().Contains(targetOffset))
						return TreeNodeActionType.IGNORE_SUBTREE;

					if (n is ITypeMemberDeclaration && !(n is ITypeDeclaration))
						return TreeNodeActionType.ACCEPT;

					return TreeNodeActionType.CONTINUE;
				});
			}

			ITypeElement typeToFind  = elm is ITypeElement? (ITypeElement)elm : elm.GetContainingType();

			ITypeDeclaration tyDecl = (ITypeDeclaration) root.FindNextNode(delegate(ITreeNode n)
			{
				if (!n.GetTreeTextRange().Contains(targetOffset))
					return TreeNodeActionType.IGNORE_SUBTREE;

				if (n is ITypeDeclaration)
				{
					ITypeDeclaration typeDecl = (ITypeDeclaration)n;

					// TODO: There can be a nested type with same name.
					//
					if (typeDecl.DeclaredName == typeToFind.ShortName)
						return TreeNodeActionType.ACCEPT;
				}

				return TreeNodeActionType.CONTINUE;
			});

			if (tyDecl == null)
				return null;

			// We are looking for a type declaration and got one.
			//
			if (elm.Equals(typeToFind))
				return tyDecl.ToTreeNode();

			// Loop through all type members
			//
			foreach (ITypeMemberDeclaration memDecl in tyDecl.MemberDeclarations)
			{
				// TODO: There is still an issue with absract methods.
				// For fields\events\properties name check is enought.
				//
				if (memDecl.DeclaredName == elm.ShortName)
				{
					return memDecl.ToTreeNode();
				}
			}

			return null;
		}

		private static string getMethodSourceFile(ISymUnmanagedMethod um, out int line, out int col)
		{
			int count;
			um.GetSequencePointCount(out count);
			if (count > 0)
			{
				ISymUnmanagedDocument[] documents = new ISymUnmanagedDocument[count];
				int[] lines             = new int[count];
				int[] columns           = new int[count];
				int[] endLines          = new int[count];
				int[] endColumns        = new int[count];
				int[] offsets           = new int[count];

				um.GetSequencePoints(count, out count, offsets, documents, lines, columns, endLines, endColumns);

				StringBuilder sb = new StringBuilder(260);
				for (int i = 0; i < count; ++i)
				{
					int urlLen;
					documents[i].GetURL(260, out urlLen, sb);
					if (urlLen > 0)
					{
						line = lines[i];
						col  = columns[i];

						// TODO: There are possible be methods located in more then one file.
						// Due to C++ defines, Nemerle macros or something else.
						//
						return sb.ToString();
					}
				}
			}

			line = col = 0;
			return null;
		}

		private static ISymUnmanagedReader getSymbolReader(string assemblyFilePath)
		{
			IVsSmartOpenScope vsScope = (IVsSmartOpenScope)
				VSShell.Instance.GetService(typeof(SVsSmartOpenScope));

			// Visual studio is broken?
			//
			if (vsScope == null)
			{
				Logger.LogMessage(LoggingLevel.NORMAL, "Failed to query VS for IVsSmartOpenScope");
				return null;
			}

			Guid iid = JetBrains.Metadata.Access.Constants.IID_IMetaDataImport;
			ISymUnmanagedBinder2 binder = (ISymUnmanagedBinder2)new JetBrains.Metadata.Access.CorSymBinder_SxSClass();
			object unkMetaDataImport;
			int hr = vsScope.OpenScope(assemblyFilePath, 0, ref iid, out unkMetaDataImport);

			if (hr < 0)
			{
				Logger.LogMessage(LoggingLevel.NORMAL,
					"IVsSmartOpenScope.OpenScope({0}) == {1:X}", assemblyFilePath, hr);
				return null;
			}

			string symbolPath = string.Join("*", new string[]
				{"srv", Options.SymbolCacheDir, Options.SymbolPath});

			ISymUnmanagedReader reader;
			return binder.GetReaderForFile2(unkMetaDataImport,
				assemblyFilePath, symbolPath, 0x0F, out reader) < 0? null: reader;
		}

#if RS40

		private static List<uint> getElementTokens(IDeclaredElement elm)
		{
			if (elm == null)
				throw new ArgumentNullException("elm");

			List<uint>   tokens  = new List<uint>();
			ITypeElement typeElm = elm is ITypeElement ? (ITypeElement)elm : elm.GetContainingType();

			if (elm is IFunction && !((IFunction) elm).IsAbstract && elm is IMetadataTokenOwner)
			{
				tokens.Add(((IMetadataTokenOwner)elm).Token);
			}
			else if (elm is IProperty)
			{
				IProperty prop = (IProperty) elm;
				IMetadataTokenOwner getter = prop.Getter(true) as IMetadataTokenOwner;
				IMetadataTokenOwner setter = prop.Setter(true) as IMetadataTokenOwner;

				if (getter != null)
					tokens.Add(getter.Token);
				if (setter != null)
					tokens.Add(setter.Token);
			}
			else if (elm is IEvent)
			{
				IEvent evt = (IEvent) elm;
				IMetadataTokenOwner adder   = evt.Adder   as IMetadataTokenOwner;
				IMetadataTokenOwner remover = evt.Remover as IMetadataTokenOwner;
				IMetadataTokenOwner raiser  = evt.Raiser  as IMetadataTokenOwner;

				if (adder != null)
					tokens.Add(adder.Token);
				if (remover != null)
					tokens.Add(remover.Token);
				if (raiser != null)
					tokens.Add(raiser.Token);
			}

			if (typeElm != null)
			{
				foreach (IMethod m in typeElm.Methods)
				{
					if (m is IMetadataTokenOwner)
						tokens.Add(((IMetadataTokenOwner) m).Token);
				}
			}

			return tokens;
		}

#else

		private static List<uint> getElementTokens(IDeclaredElement e)
		{
			List<uint>   tokens  = new List<uint>();
			ITypeElement typeElm = e is ITypeElement? (ITypeElement)e: e.GetContainingType();

			using (AssemblyLoader2 loader = new AssemblyLoader2())
			{
				loader.LoadAssembly((IAssembly)e.Module);
				IMetadataAssembly mdAssembly = loader.GetMdAssembly((IAssembly)e.Module);
				if (mdAssembly != null)
				{
					IMetadataTypeInfo typeInfo = mdAssembly.GetTypeInfoFromQualifiedName(typeElm.CLRName, false);

					if (e is IFunction)
					{
						IFunction f = (IFunction)e;

						foreach (IMetadataMethod mm in typeInfo.GetMethods())
						{
							if (mm.Name == e.ShortName && checkSignature(f, mm))
								tokens.Add(mm.Token.Value);
						}
					}
					else if (e is IProperty)
					{
						foreach (IMetadataProperty mp in typeInfo.GetProperties())
						{
							if (mp.Name != e.ShortName)
								continue;

							if (mp.Getter   != null)
								tokens.Add(mp.Getter.Token.Value);
							if (mp.Setter != null)
								tokens.Add(mp.Setter.Token.Value);
						}
					}
					else if (e is IEvent)
					{
						foreach (IMetadataEvent me in typeInfo.GetEvents())
						{
							if (me.Name != e.ShortName)
								continue;

							if (me.Adder   != null)
								tokens.Add(me.Adder.Token.Value);
							if (me.Remover != null)
								tokens.Add(me.Remover.Token.Value);
							if (me.Raiser  != null)
								tokens.Add(me.Raiser.Token.Value);
						}
					}

					foreach (IMetadataMethod method in typeInfo.GetMethods())
						tokens.Add(method.Token.Value);

					return tokens;
				}
			}

			return null;
		}

		private static bool checkSignature(IParametersOwner po, IMetadataMethod mm)
		{
			if (po.Parameters.Count != mm.Parameters.Length)
				return false;

			for (int i = 0; i < mm.Parameters.Length; i++)
			{
				IParameter         pp = po.Parameters[i];
				IMetadataParameter mp = mm.Parameters[i];

				if (!mp.Type.PresentableName.Equals(pp.Type.ToString()))
					return false;
			}

			return true;
		}

#endif

		private static void loadFromStackFrameWindow(_DTE dte)
		{
			StackFrame savedStack = dte.Debugger.CurrentStackFrame;

			if (runCommand(dte, "DebuggerContextMenus.CallStackWindow.SwitchToFrame"))
			{
				RemoteController.Instance.LoadAssembly(dte.Debugger.CurrentStackFrame.Module);
				RemoteController.Instance.Select(stackFrameToXmlDoc(dte.Debugger.CurrentStackFrame));
				dte.Debugger.CurrentStackFrame = savedStack;
			}
		}

		private static void loadFromModulesWindow(_DTE dte)
		{
			if (runCommand(dte, "Edit.Copy") && Clipboard.ContainsText())
			{
				string[] lines = Clipboard.GetText().Split('\n');
				foreach (string line in lines)
				{
					string[] columns = line.Split('\t');
					if (columns.Length > 2)
						RemoteController.Instance.LoadAssembly(columns[2]);
				}
			}
		}

		private static void loadFromObjectBrowserWindow()
		{
			object vsService = VSShell.Instance.GetService(typeof(SVsObjBrowser));
			IVsNavigationTool vsNavigationTool = vsService as IVsNavigationTool;
			if (vsNavigationTool == null)
				return;

			IVsSelectedSymbols ppIVsSelectedSymbols;
			uint pcItems;

			vsNavigationTool.GetSelectedSymbols(out ppIVsSelectedSymbols);
			ppIVsSelectedSymbols.GetCount(out pcItems);

			if (pcItems == 1)
			{
				IVsSelectedSymbol   ppIVsSelectedSymbol;
				IVsNavInfo          ppNavInfo;
				IVsEnumNavInfoNodes ppEnum;
				IVsNavInfoNode[]    nodes = new IVsNavInfoNode[1];
				string              @namespace = string.Empty;
				string              typeName   = string.Empty;
				string              nodeName;
				uint                nodeType;
				uint                pceltFetched;

				ppIVsSelectedSymbols.GetItem(0, out ppIVsSelectedSymbol);
				ppIVsSelectedSymbol.GetNavInfo(out ppNavInfo);
				ppNavInfo.EnumPresentationNodes(0, out ppEnum);

				while (ppEnum.Next(1, nodes, out pceltFetched) == 0)
				{
					nodes[0].get_Type(out nodeType);
					nodes[0].get_Name(out nodeName);

					switch ((_LIB_LISTTYPE)nodeType)
					{
						case _LIB_LISTTYPE.LLT_PHYSICALCONTAINERS:
							RemoteController.Instance.LoadAssembly(nodeName);
							break;

						case _LIB_LISTTYPE.LLT_NAMESPACES:
							@namespace = nodeName;
							break;

						case _LIB_LISTTYPE.LLT_CLASSES:
							typeName = nodeName;
							RemoteController.Instance.Select("T:" + @namespace + "." + nodeName);
							break;

						case _LIB_LISTTYPE.LLT_MEMBERS:
							selectMember(@namespace, typeName, nodeName);
							break;
					}
				}
			}
		}

		private static void selectMember(string @namespace, string typeName, string member)
		{
			bool isMethod  = member.Contains("(");
			bool isIndexer = member.StartsWith("this[");

			if (isMethod || isIndexer)
			{
				// Strip off spaces and closing bracket
				//
				member = member
					.Substring(0, member.Length - 1)
					.Replace(" ", string.Empty);

				if (isIndexer)
					member = "Item(" + member.Substring(5);

				string[] methodAndParams = member.Split(new char[] {'(', ','}, StringSplitOptions.RemoveEmptyEntries);
				StringBuilder builder = new StringBuilder(64);
				builder
					.Append(isIndexer? "P:": "M:")
					.Append(@namespace)
					.Append(".")
					.Append(typeName)
					.Append(".")
					.Append(methodAndParams[0] == typeName? "#ctor": methodAndParams[0]);

				if (methodAndParams.Length > 1)
				{
					builder.Append('(').Append(fixTypeName(methodAndParams[1]));
					for (int i = 0x2; i < methodAndParams.Length; i++)
					{
						builder.Append(',').Append(fixTypeName(methodAndParams[i]));
					}
					builder.Append(')');
				}

				RemoteController.Instance.Select(builder.ToString());
			}
			else
			{
				string fullName = string.Join(".", new string[] {@namespace, typeName, member});

				RemoteController.Instance.Select("E:" + fullName);
				RemoteController.Instance.Select("F:" + fullName);
				RemoteController.Instance.Select("P:" + fullName);
			}
		}

		private static void loadModuleByReference(IProjectModelElement modelElement)
		{
			if (modelElement is IModuleReference)
			{
				IModuleReference reference = (IModuleReference) modelElement;
				IModule module = reference.ResolveReferencedModule();

				// The assembly will be selected in the browser automatically 
				//
				loadModule(module);
			}
		}

		private static void loadModule(IModule module)
		{
			if (module == null) throw new ArgumentNullException("module");

			if (module is IAssembly)
			{
				string asmFilePath = getAssemblyFile((IAssembly)module);
				if (asmFilePath != null)
					RemoteController.Instance.LoadAssembly(asmFilePath);
			}
			else
			{
				// Impossible case, but this also works.
				//
				RemoteController.Instance.LoadAssembly(module.Name);
			}
		}

		private static string getAssemblyFile(IAssembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");

			foreach (IAssemblyFile file in assembly.GetFiles())
			{
				if (!file.IsMissing)
					return file.Location.FullPath;
			}

			return null;
		}

		private static bool runCommand(_DTE dte, string command)
		{
			Command cmd = dte.Commands.Item(command, 0);

			if (cmd != null && cmd.IsAvailable)
			{
				object customIn  = null;
				object customOut = null;
				cmd.Collection.Raise(cmd.Guid, cmd.ID, ref customIn, ref customOut);
				return true;
			}

			return false;
		}

		private static string stackFrameToXmlDoc(StackFrame frame)
		{
			StringBuilder sb = new StringBuilder(64);

			sb.Append("M:").Append(frame.FunctionName);

			if (frame.Arguments.Count != 0)
			{
				sb.Append('(').Append(fixTypeName(frame.Arguments.Item(1).Type));

				for (int i = 2; i <= frame.Arguments.Count; ++i)
					sb.Append(',').Append(fixTypeName(frame.Arguments.Item(i).Type));

				sb.Append(')');
			}

			return sb.ToString();
		}

		private static string fixTypeName(string type)
		{
			// TODO: generics

			int arrIdx = type.IndexOf('[');
			if (arrIdx > 0)
				return fixTypeName(type.Substring(0, arrIdx)) + type.Substring(arrIdx);

			switch (type)
			{
				case "int":     return typeof(int)    .FullName;
				case "byte":    return typeof(byte)   .FullName;
				case "string":  return typeof(string) .FullName;
				case "object":  return typeof(object) .FullName;
				case "bool":    return typeof(bool)   .FullName;
				case "short":   return typeof(short)  .FullName;
				case "long":    return typeof(long)   .FullName;
				case "char":    return typeof(char)   .FullName;
				case "sbyte":   return typeof(sbyte)  .FullName;
				case "uint":    return typeof(uint)   .FullName;
				case "ushort":  return typeof(ushort) .FullName;
				case "ulong":   return typeof(ulong)  .FullName;
				case "float":   return typeof(float)  .FullName;
				case "double":  return typeof(double) .FullName;
				case "decimal": return typeof(decimal).FullName;
				case "void":    return typeof(void)   .FullName; // No chance ;)
				default:        return type;
			}
		}

		#endregion
	}
}