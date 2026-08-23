using System;
using System.Data;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public partial class FirmBalanceStudioOrder : FirmBalance
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

		// Jump2FirmBalanceJournal переехал в FirmBalanceStudioOrder.WinForms.cs.
	}
}
