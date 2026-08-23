using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть FirmBalanceStudioOrder: Jump2FirmBalanceJournal. Дословный перенос.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class FirmBalanceStudioOrder
	{
		protected override void Jump2FirmBalanceJournal(IWin32Window owner)
		{
			IJournal journal = owner as IJournal;
			DateTime startDate = DateTime.Parse(journal.Filters["theDate"].ToString());

			FrmFirmStudioOrderBalance fFirmBalance = new FrmFirmStudioOrderBalance(this, startDate);
			fFirmBalance.MdiParent = ((Form) owner).MdiParent;
			fFirmBalance.Show();
		}
	}
}
