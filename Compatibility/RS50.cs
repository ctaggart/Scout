using System;
using System.Collections.Generic;
using EnvDTE;
using JetBrains.ActionManagement;
using JetBrains.Annotations;
using JetBrains.Application.Progress;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Application.Progress;
using JetBrains.UI.PopupWindowManager;
using JetBrains.Util;
using JetBrains.VsIntegration.Application;
using Microsoft.VisualStudio.Shell.Interop;

namespace ReSharper.Scout
{
	internal class ReSharper
	{
		public static bool ExecuteTask([NotNull] string name, bool cancelable, [NotNull] Action<IProgressIndicator> task)
		{
			return UITaskExecutor.FreeThreaded.ExecuteTask(
				name, cancelable? TaskCancelable.Yes: TaskCancelable.No, task);
		}

        [NotNull]
        public static DTE Dte
        {
            get { return GetVsService<SDTE, DTE>(); }
        }

        [NotNull]
		public static TInterface GetVsService<TService, TInterface>() where TInterface: class
		{
			return VsShell.ServiceProvider.GetService<TService, TInterface>();
		}

		[CanBeNull]
		public static ITextControl OpenSourceFile([NotNull] string sourceFilePath, [NotNull] ISolution solution)
		{
			if (solution == null)
				throw new ArgumentNullException("solution");

			return EditorManager.GetInstance(solution).OpenFile(sourceFilePath, true);
		}

		public static string GetUserSettingsFolder([NotNull] string relativePath)
		{
			return VsShell.UserSettingsLocalDir.Combine(relativePath).FullPath;
		}

		public static uint GetToken(uint token)
		{
			return token;
		}

		[CanBeNull]
		public static IAssembly GetAssembly(IDeclaredElement element)
		{
			return element.Module == null? null:
				((IAssemblyPsiModule)element.Module).Assembly;
		}

		[NotNull]
		public static INavigationPoint CreateSourceNavigationPoint([CanBeNull] ISolution solution, [NotNull] IProjectFile projectFile, [NotNull] ITreeNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");

			TreeTextRange range = (node is IDeclaration)? ((IDeclaration)node).GetNameRange(): node.GetTreeTextRange();
			return new ProjectFileNavigationPoint(new ProjectFileTextRange(projectFile, range.StartOffset.Offset));
		}

		[NotNull]
		public static IDisposable CreateLockCookie([NotNull] ISolution solution)
		{
			return CommitCookie.Commit(solution);
		}

		public static void Navigate(ISolution solution, List<INavigationPoint> results, string target)
		{
			NavigationOptions options = NavigationOptions.FromWindowContext(PopupWindowContext.Empty, target, true);
			NavigationManager.GetInstance(solution).Navigate(results, options);
		}

		public static void Navigate(IDeclaredElement element)
		{
			NavigationManager.Navigate(element, true);
		}

		public static DataConstant<IDeclaredElement> DECLARED_ELEMENT
		{
			get { return JetBrains.ReSharper.Psi.Services.DataConstants.DECLARED_ELEMENT; }
		}

		public static VSShell VsShell
		{
			[NotNull]
			get { return VSShell.Instance; }
		}

		public static Version VsVersion
		{
			[NotNull]
			get { return VsShell.VsVersion4; }
		}
	}
}