using System;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

using JetBrains.ActionManagement;
using JetBrains.Metadata.Reader.API;
using JetBrains.ProjectModel;
using JetBrains.ReSharper;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Shell.VSIntegration;
using JetBrains.Util;

namespace ReSharper.Scout
{
	using DebugSymbols;
	using Reflector;

	[ActionHandler("Scout.GotoDeclaration", "Scout.OpenWithReflector")]
	internal class GotoDeclarationAction : IActionHandler
	{
		#region IActionHandler Members

		public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
		{
			if (!isAvailable(context))
			{
				return nextUpdate();
			}
			return true;
		}

		public void Execute(IDataContext context, DelegateExecute nextExecute)
		{
			if (!isAvailable(context))
			{
				nextExecute();
			}
			else
			{
				execute(context);
			}
		}

		#endregion

		#region Implementation

		private static bool isAvailable(IDataContext context)
		{
			IDeclaredElement element = context.GetData(DataConstants.DECLARED_ELEMENT);

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
				IProjectModelElement projectModelElement = context.GetData(DataConstants.PROJECT_MODEL_ELEMENT);

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

		private void execute(IDataContext context)
		{
			IDeclaredElement element = context.GetData(DataConstants.DECLARED_ELEMENT);

			if (element != null)
			{
				Logger.LogMessage(LoggingLevel.VERBOSE, "Navigate to '{0}'", element.XMLDocId);

				if (element.Module is IProject)
				{
					// An internal declaration
					//
					Logger.LogMessage(LoggingLevel.VERBOSE, "An Internal Declaration => ExecuteAction(GotoDeclaration)");
					ActionManager.Instance.ExecuteAction("GotoDeclaration");
					return;
				}

				if (Options.UsePdbFiles && element.Module is IAssembly)
				{
					string pathToAssemblyFile  = getAssemblyFile((IAssembly)element.Module);
					ISymUnmanagedReader reader = getSymbolReader(pathToAssemblyFile);
					IMetadataMethod[] tokens   = getElementTokens(element);

					if (reader != null && tokens != null && tokens.Length != 0)
					{
						int line = 0, col = 0;
						string sourceFilePath = null;

						for (int i = 0; i < tokens.Length && sourceFilePath == null; i++)
						{
							IMetadataMethod method = tokens[i];
							ISymUnmanagedMethod um;
							if (reader.GetMethod(new SymbolToken((int) method.Token.Value), out um) < 0)
							{
								// The pdb file has no source information.
								//
								break;
							}
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

								// Use VS heuristic to figure out the encoding.
								//
								Document document = VSShell.Instance.ApplicationObject.ItemOperations.OpenFile(sourceFilePath, EnvDTE.Constants.vsViewKindCode).Document;
								document.ReadOnly = true;

								// Jump to somewhere near to our tarjet.
								//
								TextSelection selection = (TextSelection)document.Selection;
								selection.MoveTo(line, col, false);

								// Adjust the position, is possible.
								//
								fineTuneElementPosition(element, sourceFilePath, selection);
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
				}
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
						IProjectModelElement project = context.GetData(DataConstants.PROJECT_MODEL_ELEMENT);
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

		private static void fineTuneElementPosition(IDeclaredElement element, string sourceFilePath, TextSelection selection)
		{
			ProjectFileType fileType = ProjectFileTypeRegistry.Instance[new FileSystemPath(sourceFilePath).Extension];
			PsiLanguageType languageType = ProjectFileLanguageServiceManager.Instance.GetPsiLanguageType(fileType);
			LanguageService languageService = LanguageServiceManager.Instance.GetLanguageService(languageType);
			if (languageService != null)
			{
				int targetOffset = selection.ActivePoint.AbsoluteCharOffset - 1;

				selection.SelectAll();

				string normalized = selection.Text.Replace("\r\n", "\n").Replace("\n\r", "\n");

				ITreeNode tn = languageService.ParseUsingCapability(normalized, null, element.GetManager().Solution, null);
				tn = findParsedNode(targetOffset, element, tn);
				if (tn != null)
				{
					JetBrains.Util.TextRange range = (tn is IDeclaration)? ((IDeclaration)tn).GetNameRange(): tn.GetTreeTextRange();
					targetOffset = range.StartOffset + 1;
				}

				selection.MoveToAbsoluteOffset(targetOffset, false);
			}
		}

		private static ITreeNode findParsedNode(int targetOffset, IDeclaredElement e, ITreeNode root)
		{
			if (e is IFunction && !((IFunction)e).IsAbstract)
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

			ITypeElement typeToFind  = e is ITypeElement? (ITypeElement)e : e.GetContainingType();

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
			if (typeToFind == e)
				return tyDecl.ToTreeNode();

			// Loop through all type members
			//
			foreach (ITypeMemberDeclaration memDecl in tyDecl.MemberDeclarations)
			{
				// TODO: There is still an issue with absract methods.
				// For fields\events\properties name check is enought.
				//
				if (memDecl.DeclaredName == e.ShortName)
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

		private static IMetadataMethod[] getElementTokens(IDeclaredElement e)
		{
			using (AssemblyLoader2 loader = new AssemblyLoader2())
			{
				loader.LoadAssembly((IAssembly)e.Module);
				IMetadataAssembly mdAssembly = loader.GetMdAssembly((IAssembly)e.Module);
				if (mdAssembly != null)
				{
					ITypeElement typeElm = e is ITypeElement? (ITypeElement)e: e.GetContainingType();
					IMetadataTypeInfo typeInfo = mdAssembly.GetTypeInfoFromQualifiedName(typeElm.CLRName, false);

					if (e is IFunction)
					{
						IFunction f = (IFunction)e;
						IMetadataMethod[] matches = Array.FindAll(typeInfo.GetMethods(),
							delegate(IMetadataMethod m) { return m.Name == f.ShortName && checkSignature(f, m); });

						if (matches.Length != 0)
							return matches;
					}

					// Property, field, abstract method or something else.
					//
					return typeInfo.GetMethods();
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
					if (columns != null && columns.Length > 2)
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
				if (file.IsValid && file.Location != null)
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