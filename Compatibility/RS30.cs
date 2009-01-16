using System;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.EditorManager;
using JetBrains.ReSharper.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.ReSharper.TextControl;
using JetBrains.Shell.Progress;
using JetBrains.Shell.VSIntegration;
using JetBrains.UI.Shell.Progress;
using JetBrains.Util;

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

		public static I GetVsService<S, I>() where I: class
		{
			I svc = (I)VsShell.GetService(typeof(S));

			if (svc == null)
				throw new InvalidOperationException(string.Format(
					"Could not query the service provider for the service ID {0}.", typeof(S).FullName));

			return svc;
		}

		public static ITextControl OpenSourceFile(string sourceFilePath, ISolution solution)
		{
			// Open the file using ReSharper services.
			//
			ITextControl textControl = EditorManager.GetInstance(solution)
				.OpenProjectFile(sourceFilePath, true);

			// Open the file again using EnvDTE services to make it ReadOnly.
			//
			VsShell.ApplicationObject.ItemOperations.OpenFile(
				sourceFilePath, EnvDTE.Constants.vsViewKindCode).Document.ReadOnly = true;

			return textControl;
		}

		public static string GetUserSettingsFolder(string relativePath)
		{
			return System.IO.Path.Combine(VsShell.UserSettingsLocalDir, relativePath);
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