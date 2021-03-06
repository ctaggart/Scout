using System;
using System.Text;
using System.Windows.Forms;

using EnvDTE;
using Microsoft.VisualStudio.Shell.Interop;

using JetBrains.ActionManagement;
using JetBrains.ProjectModel;
#if !RS50
using JetBrains.ProjectModel.Build;
#endif
#if RS30
using ProjectModelDataConstants=JetBrains.ReSharper.DataConstants;
#else
using ProjectModelDataConstants=JetBrains.IDE.DataConstants;
#endif

namespace ReSharper.Scout.Actions
{
	using Reflector;

	[ActionHandler(ActionId)]
	internal class OpenWithReflectorVsWindows : IActionHandler
	{
		public const string ActionId = "OpenWithReflectorVsWindows";

		public const string SwitchToThisFrameActionId = "DebuggerContextMenus.CallStackWindow.SwitchToFrame";
		public const string CopyToClipboardActionId   = "Edit.Copy";

		#region IActionHandler Members

		public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
		{
			return IsAvailable(context) || nextUpdate();
		}

		public void Execute(IDataContext context, DelegateExecute nextExecute)
		{
			if (IsAvailable(context))
			{
				Execute(context);
			}
			else
			{
				nextExecute();
			}
		}

		#endregion

		#region Implementation

		private static bool IsAvailable(IDataContext context)
		{
			if (!Options.UseReflector)
				return false;

			//
			// DTE only related code.
			//

			_DTE dte = ReSharper.Dte;
			if (dte.ActiveWindow == null)
				return false;

			string  toolWindowKind;

			try
			{
				toolWindowKind = dte.ActiveWindow.ObjectKind;
			}
			catch (NotImplementedException)
			{
				// Unbelievable, but possible. See http://www.jetbrains.net/jira/browse/RSPL-313
				//
				return false;
			}

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
			{
				requiredCommand = dte.Commands.Item(CopyToClipboardActionId, 0);
			}
			else if (toolWindowKind == ToolWindowGuids80.CallStack)
			{
				requiredCommand = dte.Commands.Item(SwitchToThisFrameActionId, 0);
			}

			return requiredCommand != null && requiredCommand.IsAvailable;
		}

		private static void Execute(IDataContext context)
		{
			// Check for call stack & modules
			//
			DTE dte = ReSharper.Dte;
			if (dte.ActiveWindow != null)
			{
				string toolWindowKind = dte.ActiveWindow.ObjectKind;

				if (toolWindowKind == ToolWindowGuids80.SolutionExplorer)
				{
					IProjectModelElement project = context.GetData(ProjectModelDataConstants.PROJECT_MODEL_ELEMENT);
					LoadModuleByReference(project);
				}
				else if (toolWindowKind == ToolWindowGuids80.ObjectBrowser)
					LoadFromObjectBrowserWindow();
				else if (toolWindowKind == ToolWindowGuids80.Modules)
					LoadFromModulesWindow(dte);
				else if (toolWindowKind == ToolWindowGuids80.CallStack)
					LoadFromStackFrameWindow(dte);
			}
		}

		private static void LoadFromStackFrameWindow(_DTE dte)
		{
			StackFrame savedStack = dte.Debugger.CurrentStackFrame;

			if (RunCommand(dte, SwitchToThisFrameActionId))
			{
				RemoteController.Instance.LoadAssembly(dte.Debugger.CurrentStackFrame.Module);
				RemoteController.Instance.Select(StackFrameToXmlDoc(dte.Debugger.CurrentStackFrame));
				dte.Debugger.CurrentStackFrame = savedStack;
			}
		}

		private static void LoadFromModulesWindow(_DTE dte)
		{
			if (RunCommand(dte, CopyToClipboardActionId) && Clipboard.ContainsText())
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

		private static void LoadFromObjectBrowserWindow()
		{
			IVsNavigationTool vsNavigationTool = ReSharper.GetVsService<SVsObjBrowser, IVsNavigationTool>();
			IVsSelectedSymbols ppIVsSelectedSymbols;
			uint pcItems;

			vsNavigationTool.GetSelectedSymbols(out ppIVsSelectedSymbols);
			ppIVsSelectedSymbols.GetCount(out pcItems);

			if (pcItems != 1)
				return;

			IVsSelectedSymbol   ppIVsSelectedSymbol;
			IVsNavInfo          ppNavInfo;
			IVsEnumNavInfoNodes ppEnum;
			IVsNavInfoNode[]    nodes = new IVsNavInfoNode[1];
			string              @namespace = string.Empty;
			string              typeName   = string.Empty;
			uint                pceltFetched;

			ppIVsSelectedSymbols.GetItem(0, out ppIVsSelectedSymbol);
			ppIVsSelectedSymbol.GetNavInfo(out ppNavInfo);
			ppNavInfo.EnumPresentationNodes(0, out ppEnum);

			while (ppEnum.Next(1, nodes, out pceltFetched) == 0)
			{
				uint   nodeType; nodes[0].get_Type(out nodeType);
				string nodeName; nodes[0].get_Name(out nodeName);

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
						SelectMember(@namespace, typeName, nodeName);
						break;
				}
			}
		}

		private static void SelectMember(string @namespace, string typeName, string member)
		{
			// TODO: VB

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
					builder.Append('(').Append(FixTypeName(methodAndParams[1]));
					for (int i = 2; i < methodAndParams.Length; i++)
					{
						builder.Append(',').Append(FixTypeName(methodAndParams[i]));
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

		private static void LoadModuleByReference(IProjectModelElement modelElement)
		{
			if (modelElement is IModuleReference)
			{
				IModuleReference reference = (IModuleReference) modelElement;
				IModule module = reference.ResolveReferencedModule();

				// The assembly will be selected in the browser automatically 
				//
				LoadModule(module);
			}
		}

		private static void LoadModule(IModule module)
		{
			if (module == null) throw new ArgumentNullException("module");

			if (module is IAssembly)
			{
				string asmFilePath = GetAssemblyFile((IAssembly)module);
				if (asmFilePath != null)
					RemoteController.Instance.LoadAssembly(asmFilePath);
			}
			else if (module is IProject)
			{
				IProject prj = (IProject)module;
				BuildSettingsManager buildMgr = prj.GetComponent<BuildSettingsManager>();
				if (buildMgr.ActiveConfiguration is IManagedProjectConfiguration)
				{
					IManagedProjectConfiguration cfg = (IManagedProjectConfiguration)buildMgr.ActiveConfiguration;
					RemoteController.Instance.LoadAssembly(cfg.OutputAssemblyFilePath.FullPath);
				}
			}
			else
			{
				// Impossible case, but this also works.
				//
				RemoteController.Instance.LoadAssembly(module.Name);
			}
		}

		private static string GetAssemblyFile(IAssembly assembly)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");

			foreach (IAssemblyFile file in assembly.GetFiles())
			{
				if (!file.IsMissing)
					return file.Location.FullPath;
			}

			return null;
		}

		private static bool RunCommand(_DTE dte, string command)
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

		private static string StackFrameToXmlDoc(StackFrame frame)
		{
			StringBuilder sb = new StringBuilder(64);

			sb.Append("M:").Append(frame.FunctionName);

			if (frame.Arguments.Count != 0)
			{
				sb.Append('(').Append(FixTypeName(frame.Arguments.Item(1).Type));

				for (int i = 2; i <= frame.Arguments.Count; ++i)
					sb.Append(',').Append(FixTypeName(frame.Arguments.Item(i).Type));

				sb.Append(')');
			}

			return sb.ToString();
		}

		private static string FixTypeName(string type)
		{
			// TODO: generics && VB names

			int arrIdx = type.IndexOf('[');
			if (arrIdx > 0)
				return FixTypeName(type.Substring(0, arrIdx)) + type.Substring(arrIdx);

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