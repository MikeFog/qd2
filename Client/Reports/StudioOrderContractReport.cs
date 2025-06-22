using System;
using System.Collections.Generic;
using System.Data;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Classes.Domain.StudioOrder;

namespace Merlin.Reports
{
	internal class StudioOrderContractReport : GenericReport
	{
		public StudioOrderContractReport(StudioOrderAction action, Agency agency, Firm firm, DateTime billDate)
			: base(new StudioOrderContract(), agency)
		{
			// Get summa for selected agency
			decimal summa = GetAgencySumma(action, agency);

			TextObject textObj = GetTextObject("txtHeader");
			textObj.Text = textObj.Text.Replace("@NUMBER", action.ActionId.ToString());
			SetTextObjectText("txtDate", billDate.ToShortDateString());

			textObj = GetTextObject("txtCaption");
			string text = textObj.Text.Replace("@AGENCY_DESCRIPTION", agency.PrefixWithName);
			text = text.Replace("@AGENCY_REPORT_STRING", agency.ReportString);
			text = text.Replace("@FIRM_DESCRIPTION", firm.PrefixWithName);
			textObj.Text = text;

			ReplaceTextObjectText("txtPrice", "@PRICE", string.Format("{0} ({1})", summa.ToString("c"), Money.MoneyToString(summa, true)));

			DataTable dt = action.LoadBillData(agency, action.FinishDate != DateTime.MinValue ? action.FinishDate : DateTime.Now);

			decimal sumTax = 0;

			foreach (DataRow row in dt.Rows)
				sumTax +=ParseHelper.GetDecimalFromObject(row["tax"], 0);

			ReplaceTextObjectText("txtPrice", "@TAX_PRICE", sumTax.ToString("c"));
			PrintFooter(agency, firm);
		}

		private static decimal GetAgencySumma(Classes.Action action, Agency agency)
		{
			Dictionary<string, object> procParameters =
				new Dictionary<string, object>(2, StringComparer.CurrentCultureIgnoreCase);
			procParameters.Add(Classes.Action.ParamNames.ActionId, action.ActionId);
			procParameters.Add(Agency.ParamNames.AgencyId, agency.AgencyId);
			DataAccessor.ExecuteNonQuery("StudioOrderActionPriceForAgency", procParameters);
			decimal summa = decimal.Parse(procParameters["summa"].ToString());
			return summa;
		}
	}
}