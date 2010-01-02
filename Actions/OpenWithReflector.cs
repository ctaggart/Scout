using System;

using JetBrains.ActionManagement;
using JetBrains.ProjectModel;
#if !RS50
using JetBrains.ProjectModel.Build;
#endif
using JetBrains.ReSharper.Psi;
using JetBrains.Util;
#if !RS45 && !RS50
using IPsiModule = JetBrains.ProjectModel.IModule;
#endif

namespace ReSharper.Scout.Actions
{
	using Reflector;

	[ActionHandler(ActionId)]
	internal class OpenWithReflector : IActionHandler
	{
		public const string ActionId = "OpenWithReflector";

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

			IDeclaredElement element = context.GetData(ReSharper.DECLARED_ELEMENT);

			return element != null && element.Module is IAssembly &&
				element.Module.Name != null && !string.IsNullOrEmpty(ReSharper.GetDocId(element));
		}

		private static void Execute(IDataContext context)
		{
			IDeclaredElement element = context.GetData(ReSharper.DECLARED_ELEMENT);
			if (element == null)
				return;

			Logger.LogMessage(LoggingLevel.VERBOSE, "Navigate to '{0}'", ReSharper.GetDocId(element));

			LoadModule(element.Module);
			RemoteController.Instance.Select(ReSharper.GetDocId(element));
		}

		private static void LoadModule(IPsiModule module)
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

		#endregion
	}
}