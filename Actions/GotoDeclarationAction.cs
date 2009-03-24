using System.Collections.Generic;
using JetBrains.ActionManagement;
using JetBrains.ProjectModel;
#if RS45
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
	internal class GotoDeclarationAction : IActionHandler
	{
		public const string OverridenActionId = "GotoDeclaration";
		public const string ActionId = "Scout." + OverridenActionId;

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
			IDeclaredElement element = context.GetData(ReSharper.DECLARED_ELEMENT);

			if (element != null && element.Module != null &&
				element.Module.Name != null && !string.IsNullOrEmpty(element.XMLDocId))
				return true;

			IUpdatableAction action = ActionManager.Instance.GetAction(OverridenActionId);
			return action != null && action.Update(context);
		}

		private void execute(IDataContext context)
		{
			bool succeeded = false;
			IDeclaredElement targetElement = context.GetData(ReSharper.DECLARED_ELEMENT);

			if (targetElement == null)
				return;

			ISolution solution = targetElement.GetManager().Solution;

			if (targetElement.Module is IAssembly)
			{
				List<INavigationPoint> results;
				using (ReSharper.CreateLockCookie(solution))
				{
					Logger.LogMessage(LoggingLevel.VERBOSE, "Navigate to '{0}'", targetElement.XMLDocId);

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
					succeeded = executeAction(OpenWithReflectorAction.ActionId, context);
			}

			if (!succeeded)
				ReSharper.Navigate(targetElement);
		}

		private static bool executeAction(string actionId, IDataContext context)
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