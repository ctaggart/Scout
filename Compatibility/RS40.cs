using System;
using System.Collections.Generic;
using EnvDTE;
using JetBrains.ActionManagement;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.PopupWindowManager;
using JetBrains.UI.Shell.Progress;
using JetBrains.Application.Progress;
using JetBrains.VSIntegration.Shell;
using TextRange=JetBrains.Util.TextRange;

namespace ReSharper.Scout
{
	internal class ReSharper
	{
		public static bool ExecuteTask(string name, bool cancelable, Action<IProgressIndicator> task)
		{
			using (SyncProgressWindow progressWindow = new SyncProgressWindow())
			{
				if (cancelable)
				{
				bool cancelled;
				progressWindow.ExecuteTask(delegate(IProgressIndicator indicator)
				{
					task(indicator);
					return null;
				}, name, out cancelled);

				return !cancelled;
			}

				progressWindow.ExecuteTaskNonCancelable(delegate(IProgressIndicator indicator)
				{
					task(indicator);
					return null;
				}, name);
				return true;
			}
		}

        public static DTE Dte
        {
            get { return VsShell.ApplicationObject; }
        }

        public static I GetVsService<S, I>() where I : class
		{
			return VsShell.GetVsService<S, I>();
		}

		public static ITextControl OpenSourceFile(string sourceFilePath, ISolution solution)
		{
			return EditorManager.GetInstance(solution).OpenFile(sourceFilePath, true, true);
		}

		public static string GetUserSettingsFolder(string relativePath)
		{
			return VsShell.UserSettingsLocalDir.Combine(relativePath).FullPath;
		}

		public static uint GetToken(uint token)
		{
			return token;
		}

		public static IAssembly GetAssembly(IDeclaredElement element)
		{
			return (IAssembly)element.Module;
		}

		public static INavigationResult CreateSourceNavigationPoint(ISolution solution, IProjectFile projectFile, ITreeNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			
			TextRange range = (node is IDeclaration)? ((IDeclaration)node).GetNameRange(): node.GetTreeTextRange();
			return new TextControlNavigationResult(solution, projectFile, range, range.StartOffset);
		}

		public static IDisposable CreateLockCookie(ISolution solution)
		{
			return CommitCookie.Commit(solution);
		}

		public static void Navigate(ISolution solution, List<INavigationResult> results, string target)
		{
			Navigator.Navigate(true, solution, PopupWindowContext.Empty.CreateLayouter(),
				PopupWindowContext.Empty, results, target);
		}

		public static void Navigate(IDeclaredElement element)
		{
			Navigator.Navigate(element, false, true);
		}

		public static DataConstant<IDeclaredElement> DECLARED_ELEMENT
		{
			get { return JetBrains.ReSharper.DataConstants.DECLARED_ELEMENT; }
		}

		public static VSShell VsShell
		{
			get { return VSShell.Instance; }
		}

		public static Version VsVersion
		{
			get { return VsShell.VsVersion; }
		}
	}
}