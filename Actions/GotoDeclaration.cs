using System.Collections.Generic;
using JetBrains.ActionManagement;
using JetBrains.ProjectModel;
#if RS45 || RS50
using JetBrains.ReSharper.Feature.Services.Navigation;
#else
using JetBrains.ReSharper.Navigation;
using INavigationPoint = JetBrains.ReSharper.Navigation.INavigationResult;
#endif
using JetBrains.ReSharper.Psi;
using JetBrains.Util;

namespace ReSharper.Scout.Actions
{
	using DebugSymbols;

	[ActionHandler(ActionId, ActionId + "InContextMenu")]
	internal class GotoDeclaration : IActionHandler
	{
		public const string OverridenActionId = "GotoDeclaration";
		public const string ActionId = "Scout." + OverridenActionId;

		#region IActionHandler Members

		public bool Update(IDataContext context, ActionPresentation presentation, DelegateUpdate nextUpdate)
		{
			return IsAvailable(context) || nextUpdate();
		}

		public void Execute(IDataContext context, DelegateExecute nextExecute)
		{
			if (IsAvailable(context))
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

		private static bool IsAvailable(IDataContext context)
		{
			IDeclaredElement element = context.GetData(ReSharper.DECLARED_ELEMENT);

			if (element != null && element.Module != null &&
				element.Module.Name != null && !string.IsNullOrEmpty(ReSharper.GetDocId(element)))
				return true;

			IUpdatableAction action = ActionManager.Instance.GetAction(OverridenActionId);
			return action.Update(context);
		}

		private void execute(IDataContext context)
		{
			bool succeeded = false;
			IDeclaredElement targetElement = context.GetData(ReSharper.DECLARED_ELEMENT);

			if (targetElement == null)
				return;

			ISolution solution = targetElement.GetManager().Solution;

#if RS45 || RS50
			if (targetElement.Module is IAssemblyPsiModule)
#else
			if (targetElement.Module is IAssembly)
#endif
				{
					List<INavigationPoint> results;
					using (ReSharper.CreateLockCookie(solution))
					{
						Logger.LogMessage(LoggingLevel.VERBOSE, "Navigate to '{0}'", ReSharper.GetDocId(targetElement));

						results = new ReferenceSource(targetElement).GetNavigationPoints();
					}

					if (results.Count != 0)
					{
#if RS40
					results = results.FindAll(delegate(INavigationPoint result) { return result != null; });
#endif
						if (results.Count != 0)
						{
							string target = DeclaredElementPresenter.Format(targetElement.Language,
								DeclaredElementPresenter.KIND_NAME_PRESENTER, targetElement);
							ReSharper.Navigate(solution, results, target);
						}

						succeeded = true;
					}
					else
						succeeded = ExecuteAction(OpenWithReflector.ActionId, context);
				}

			if (!succeeded)
				ReSharper.Navigate(targetElement);
		}

		private static bool ExecuteAction(string actionId, IDataContext context)
		{
			IExecutableAction action = (IExecutableAction)ActionManager.Instance.GetAction(actionId);
			bool available = action != null && action.Update(context);

			if (available)
				action.Execute(context);

			return available;
		}

		#endregion
	}
}