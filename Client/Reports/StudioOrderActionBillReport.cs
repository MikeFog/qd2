using System;
using System.Collections.Generic;
using System.Data;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Classes.Domain.StudioOrder;

namespace Merlin.Reports
{
	internal class StudioOrderActionBillReport : BillReport
	{
		public StudioOrderActionBillReport(StudioOrderAction action, Agency agency, IOrganization firm, PresentationObject bill)
			: base(action, agency, firm, bill, new StudioOrderActionBill(), "договору")
		{
		}

		protected override DataTable LoadBillData(Agency agency, Classes.Action action, DateTime? mounth)
		{
			return ((StudioOrderAction) action).LoadBillData(agency, mounth);
		}

		protected override decimal CalculateBillTotal(DataTable dtData)
		{
			decimal summa = 0;
			foreach (DataRow row in dtData.Rows)
			{
				if (!StringUtil.IsDBNullOrEmpty(row["price"]))
				{
					decimal price = decimal.Parse(row["price"].ToString());
					summa += price;
				}
			}
			return summa;
		}

		protected override CrystalDecisions.CrystalReports.Engine.BlobFieldObject DirPainting
		{
			get
			{
				return GetBlobObject("dirPainting2");
			}
		}

		protected override string PaintingsSectionName { get { return "Section4"; } }
	}
}