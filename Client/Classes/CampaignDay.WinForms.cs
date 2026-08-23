using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть CampaignDay: перенос выпуска на другую дату.
	// Бизнес-часть — в CampaignDay.cs (ApplyDayTransfer).
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	internal partial class CampaignDay
	{
		public override void DoAction(string actionName, IWin32Window owner, InterfaceObjects interfaceObject)
		{
			if (actionName == Constants.EntityActions.Transfer)
				TransferDay((Form)owner);
			base.DoAction(actionName, owner, interfaceObject);
		}

		private void TransferDay(Form form)
		{
			decimal price = decimal.Zero;

			if (Campaign != null && Campaign.Action != null)
			{
				Campaign.Action.Refresh();
				price = Campaign.Action.TotalPrice;
			}

			DateTime date = DateTime.Parse(this[RollerIssue.ParamNames.IssueDate].ToString());
			TransferDayForm fTransfer = new TransferDayForm(date, GetPriceList(date));
			if (fTransfer.ShowDialog(form) == DialogResult.OK)
			{
				try
				{
					Cursor.Current = Cursors.WaitCursor;
					Application.DoEvents();
					ApplyDayTransfer(fTransfer.TargetDate, price);
				}
				finally { Cursor.Current = Cursors.Default; }
			}
		}
	}
}
