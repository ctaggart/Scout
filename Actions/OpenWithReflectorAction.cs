using System;

using JetBrains.ActionManagement;
using JetBrains.ProjectModel;
using JetBrains.ProjectModel.Build;
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
#if !RS45
using IPsiModule = JetBrains.ProjectModel.IModule;
#endif

namespace ReSharper.Scout.Actions
{
	using Reflector;

	[ActionHandler(ActionId)]
	internal class OpenWithReflectorAction : IActionHandler
	{
		public const string ActionId = "Scout.OpenWithReflector";

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
			if (!Options.UseReflector)
				return false;

			IDeclaredElement element = context.GetData(ReSharper.DECLARED_ELEMENT);

			return element != null && element.Module is IAssembly &&
				element.Module.Name != null && !string.IsNullOrEmpty(element.XMLDocId);
		}

		private static void execute(IDataContext context)
		{
			IDeclaredElement element = context.GetData(ReSharper.DECLARED_ELEMENT);

			Logger.LogMessage(LoggingLevel.VERBOSE, "Navigate to '{0}'", element.XMLDocId);

			loadModule(element.Module);
			RemoteController.Instance.Select(element.XMLDocId);
		}

		private static void loadModule(IPsiModule module)
		{
			if (module == null) throw new ArgumentNullException("module");

			if (module is IAssembly)
			{
				string asmFilePath = getAssemblyFile((IAssembly)module);
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

		#endregion
	}
}