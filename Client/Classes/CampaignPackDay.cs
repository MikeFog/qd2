using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

namespace Merlin.Classes
{
	internal class CampaignPackDay : CampaignDay
	{
		public CampaignPackDay()
			: base(EntityManager.GetEntity((int) Entities.PackCampaignDay))
		{
		}

		protected override Pricelist GetPriceList(DateTime date)
		{
			Dictionary<string, object> parametrsPL = new Dictionary<string, object>();
			parametrsPL["theDate"] = date;
			parametrsPL["campaignID"] = Parameters["campaignID"];
			DataSet ds = DataAccessor.LoadDataSet("PricelistByDate", parametrsPL);
			return new PackModulePricelist(EntityManager.GetEntity((int)Entities.PackModulePricelist), ds.Tables[0].Rows[0]);
		}
	}
}