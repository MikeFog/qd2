using System;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	// UI-часть FirmBalanceIssues: Jump2FirmBalanceJournal. Дословный перенос.
	// Конвенция — docs/tasks/web-migration-dialogs.md.
	public partial class FirmBalanceIssues
	{
		protected override void Jump2FirmBalanceJournal(IWin32Window owner)
		{
			IJournal journal = owner as IJournal;

			DateTime startDate = DateTime.Today.AddDays(-7);

			if (journal != null && journal.Filters.ContainsKey("theDate"))
				startDate = DateTime.Parse(journal.Filters["theDate"].ToString());

			FrmFirmIssuesBalance fFirmBalance = new FrmFirmIssuesBalance(this, startDate);
			fFirmBalance.MdiParent = ((Form) owner).MdiParent;
			fFirmBalance.Show();
		}
	}
}
