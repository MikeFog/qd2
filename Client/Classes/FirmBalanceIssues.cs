using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	public class FirmBalanceIssues : FirmBalance
	{
		public FirmBalanceIssues() : base(EntityManager.GetEntity((int) Entities.BalanceIssues))
		{
		}

		public FirmBalanceIssues(Entity entity) : base(entity)
		{
		}

		public FirmBalanceIssues(Entity entity, DataRow row)
			: base(entity, row)
		{
		}

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