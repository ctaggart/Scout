using System;
using JetBrains.Annotations;
using JetBrains.IDE;
using JetBrains.ProjectModel;
using JetBrains.ReSharper.Feature.Services.Navigation;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.TextControl;
using JetBrains.UI.Shell.Progress;
using JetBrains.Application.Progress;
using JetBrains.Util;
using JetBrains.VSIntegration.Shell;

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
		public static I GetVsService<S, I>() where I: class
		{
			return VsShell.GetVsService<S, I>();
		}

		[CanBeNull]
		public static ITextControl OpenSourceFile([NotNull] string sourceFilePath, [NotNull] ISolution solution)
		{
			if (solution == null)
				throw new ArgumentNullException("solution");

			return EditorManager.GetInstance(solution).OpenFile(sourceFilePath, true, true);
		}

		public static string GetUserSettingsFolder([NotNull] string relativePath)
		{
			return VsShell.UserSettingsLocalDir.Combine(relativePath).FullPath;
		}

		public static uint GetToken(Pair<uint, uint> token)
		{
			return token.Second;
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

			TextRange range = (node is IDeclaration)? ((IDeclaration)node).GetNameRange(): node.GetTreeTextRange();
			return new ProjectFileNavigationPoint(new ProjectFileTextRange(projectFile, range));
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