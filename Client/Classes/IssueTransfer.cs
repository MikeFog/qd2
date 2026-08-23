using System;
using System.Collections.Generic;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal class SimpleTransfer
	{
		protected Massmedia massmedia;

		public SimpleTransfer(Massmedia massmedia)
		{
			this.massmedia = massmedia;
		}

		internal Roller Transfer(RollerIssue issue, ITariffWindow destinationWindow, bool needAsk)
		{
			if ((!needAsk || AskUserConfirmation(issue, destinationWindow)) &&
				IsTransferAllowed(destinationWindow, issue.Roller, issue))
			{
				issue.Transfer(destinationWindow, massmedia);
				return issue.Roller;
			}
			return null;
		}

		protected static bool AskUserConfirmation(RollerIssue issue, ITariffWindow destinationWindow)
		{
			Dictionary<string, object> msgParameters =
				new Dictionary<string, object>(3, StringComparer.InvariantCultureIgnoreCase)
					{
						{"rollerName", issue.Name},
						{"sourceDate", DateTimeUtils.ToDateTimeString(issue.IssueDate)},
						{"destinationDate", DateTimeUtils.ToDateTimeString(destinationWindow.WindowDate)}
					};
			MessageAccessor.Parameters = msgParameters;
			return UserInteraction.Confirm(MessageAccessor.GetMessage("IssueTransferConfirmation"));
		}

		protected static bool IsTransferAllowed(ITariffWindow destinationWindow, Roller roller, RollerIssue issue)
		{
			Dictionary<string, object> msgParams =
				new Dictionary<string, object>(StringComparer.InvariantCultureIgnoreCase);
			if (((TariffWindowWithRollerIssues) destinationWindow).IsRollerExist(roller.RollerId))
			{
				msgParams["rollerName"] = roller.Name;
				MessageAccessor.Parameters = msgParams;
				return UserInteraction.Confirm(MessageAccessor.GetMessage("TransferRollerExists"));
			}
			else
			{
				int firmId = ParseHelper.GetInt32FromObject(issue["firmID"], 0);
				if (firmId > 0 && ((TariffWindowWithRollerIssues)destinationWindow).IsRollerOfTheFirmExist(firmId))
				{
					msgParams.Clear();
					msgParams["firmName"] = Firm.GetFirmById(firmId).Name;
					MessageAccessor.Parameters = msgParams;
					return UserInteraction.Confirm(MessageAccessor.GetMessage("TransferFirmExists"));
				}
			}
			return true;
		}
	}
}