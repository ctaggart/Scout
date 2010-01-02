using System;
using System.Collections.Generic;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.ExternalSources.Core;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.Options;
using JetBrains.Util;
using ReSharper.Scout.Reflector;

namespace ReSharper.Scout.Providers
{
	[ExternalSourcesProvider]
	internal class ReflectorExternalSourcesProvider : IExternalSourcesProvider
	{
		#region IExternalSourcesProvider Members

		public IOptionsPage CreateOptionsPage(IOptionsDialog optionsDialog)
		{
			return null;
		}

		public IAssembly MapProjectFileToAssembly(IProjectFile projectFile)
		{
			return null;
		}

		public IEnumerable<FileSystemPath> NavigateToSources(ICompiledElement compiledElement)
		{
			LoadModule(compiledElement.Module);
			RemoteController.Instance.Select(ReSharper.GetDocId(compiledElement));
			return null;
		}

		// Properties
		public int DefaultPriority
		{
			get { return 3000; }
		}

		public string Id
		{
			get { return "openWithReflector"; }
		}

		public string PresentableShortName
		{
			get { return "Open with Reflector"; }
		}

		#endregion

		private static void LoadModule(IPsiModule module)
		{
			if (module == null) throw new ArgumentNullException("module");

			if (module is IAssemblyPsiModule)
			{
				string asmFilePath = GetAssemblyFile((IAssemblyPsiModule)module);
				if (asmFilePath != null)
					RemoteController.Instance.LoadAssembly(asmFilePath);
			}
			else if (module is IProjectPsiModule)
			{
				IProjectPsiModule prj = (IProjectPsiModule)module;
				BuildSettingsManager buildMgr = prj.Project.GetComponent<BuildSettingsManager>();
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

		private static string GetAssemblyFile(IAssemblyPsiModule assembly)
		{
			if (assembly == null) throw new ArgumentNullException("assembly");

			foreach (IAssemblyFile file in assembly.Assembly.GetFiles())
			{
				if (!file.IsMissing)
					return file.Location.FullPath;
			}

			return null;
		}
	}
}
