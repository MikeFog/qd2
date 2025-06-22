using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using Merlin.Forms;

namespace Merlin.Classes
{
	public class FirmBalanceStudioOrder : FirmBalance
	{
		public FirmBalanceStudioOrder() : base(EntityManager.GetEntity((int) Entities.BalanceStudioOrder))
		{
		}

		public FirmBalanceStudioOrder(Entity entity) : base(entity)
		{
		}

		public FirmBalanceStudioOrder(Entity entity, DataRow row)
			: base(entity, row)
		{
		}

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
