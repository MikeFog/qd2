using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть ActionOnMassmedia: диалоги и показ сообщений пользователю.
	// Бизнес-часть тех же операций — в ActionOnMassmedia.cs, она не знает про UI.
	// Эталон разреза, конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class ActionOnMassmedia
	{
		public override bool ShowPassport(IWin32Window owner)
		{
			ActionForm fAction = new ActionForm(this /*, false*/);
			fAction.ShowDialog(owner);
			return true;
		}

		private bool IsSplitOrMergeEnabled(DateTime startDate)
		{
			if (CanSplitOrMerge(startDate, out string messageKey)) return true;

			UserMessage.ShowExclamation(MessageAccessor.GetMessage(messageKey));
			return false;
		}

		private bool CheckCampaignsSelectionResultForActionSplit(SelectionForm selectionForm)
		{
			if (IsSplitSelectionValid(selectionForm.AddedItems.Count, out string messageKey)) return true;

			UserMessage.ShowExclamation(MessageAccessor.GetMessage(messageKey));
			return false;
		}

		private void SplitAction()
		{
			try
			{
				if (!IsSplitOrMergeEnabled(StartDate.Date)) return;

				DataTable dt = GetCampaignsForSplit(out string messageKey);
				if (dt == null)
				{
					UserMessage.ShowInformation(MessageAccessor.GetMessage(messageKey));
					return;
				}

				SelectionForm fSelector = new SelectionForm(EntityManager.GetEntity((int)Entities.CampaignOnMassmedia),
						dt.DefaultView, "Выберите рекламные компании которые хотите перенести в новую акцию", true,
						CheckCampaignsSelectionResultForActionSplit);

				if (fSelector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
				{
					Cursor.Current = Cursors.WaitCursor;
					ApplySplitAction(fSelector.AddedItems);
					FireContainerRefreshed();
				}
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}
	}
}
