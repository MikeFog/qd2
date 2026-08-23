using System;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public partial class FirmBalanceIssues : FirmBalance
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

		// Jump2FirmBalanceJournal переехал в FirmBalanceIssues.WinForms.cs.
	}
}