using System;
using System.Collections.Generic;
using System.Drawing;
using JetBrains.Application;
using JetBrains.ComponentModel;
using JetBrains.ProjectModel;
#if !RS50
using JetBrains.ProjectModel.Build;
#endif
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Feature.Services.Util;
using JetBrains.ReSharper.Psi;
using JetBrains.UI.RichText;
using JetBrains.Util;
using ReSharper.Scout.Properties;

namespace ReSharper.Scout.Providers
{
	using Reflector;

	[NavigationProvider(ProgramConfigurations.VS_ADDIN)]
	internal class ReflectorNavigationProvider : INavigationProvider
	{
		#region INavigationProvider members

		public IEnumerable<INavigationPoint> CreateNavigationPoints(object target, IEnumerable<INavigationPoint> basePoints)
		{
			ICompiledElement element = (ICompiledElement) target;
			Shell.Instance.Locks.AssertReadAccessAllowed();
			Logger.Assert(element.IsValid(), "Target should be valid");
			element.GetManager().AssertAllDocumentAreCommited();

			if (Options.UseReflector && !string.IsNullOrEmpty(ReSharper.GetDocId(element)) &&
				element.Module != null && element.Module.Name != null)
			{
				List<INavigationPoint> points = new List<INavigationPoint>();
				points.AddRange(basePoints);
				points.Add(new ReflectorNavigationPoint(element));
				return points;
			}

			return basePoints;
		}

		public IEnumerable<Type> GetSupportedTargetTypes()
		{
			return new Type[] { typeof(ICompiledElement) };
		}

		public double Priority
		{
			get { return 1000.0; }
		}

		#endregion

		private class ReflectorNavigationPoint : INavigationPoint
		{
			private readonly ICompiledElement _compiledElement;

			public ReflectorNavigationPoint(ICompiledElement compiledElement)
			{
				_compiledElement = compiledElement;
			}

			public Image GetPresentationImage()
			{
				// TODO: real image
				//
				return Resources.OptionsPageImage;
			}

			public RichText GetPresentationText()
			{
				// TODO: "open 'foobar' with Reflector
				//
				return DeclaredElementPresenter.Format(
					PresentationUtil.GetPresentationLanguage(_compiledElement),
					DeclaredElementPresenter.QUALIFIED_NAME_PRESENTER, _compiledElement);
			}

			public RichText GetSecondaryPresentationText()
			{
				return RichText.Empty;
			}

			public bool Navigate(NavigationOptions options)
			{
				LoadModule(_compiledElement.Module);
				return RemoteController.Instance.Select(ReSharper.GetDocId(_compiledElement));
			}

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
}