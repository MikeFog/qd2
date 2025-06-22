using System;
using System.Data;
using FogSoft.WinForm.Classes;

namespace Merlin.Classes
{
	public class StudioPricelist : Pricelist
	{
		public StudioPricelist() :
			base(EntityManager.GetEntity((int) Entities.StudioPricelist))
		{
		}

		public override DataTable GetTariffList()
		{
			throw new NotImplementedException();
		}
	}
}