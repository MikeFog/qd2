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
	internal class StudioOrderActReport : GenericReport
	{
		internal StudioOrderActReport(Classes.Action action, Agency agency, StudioOrder order)
			: base(new StudioOrderAct(), agency)
		{
			// Load data

			DataTable dtData = LoadData(action, agency);
			decimal totalPrice = ProcessTotalPrice(dtData);
			_report.SetDataSource(dtData);

			// Set report parameters
			if (order.FinishDate != null)
				SetTextObjectText("txtDate", ((DateTime)order.FinishDate).ToShortDateString());

			TextObject textTitleObj = GetTextObject("txtTitle");
			textTitleObj.Text = textTitleObj.Text.Replace("@ACTIONID", action.ActionId.ToString());

			TextObject textObj = GetTextObject("txtHeader");
			string text = textObj.Text.Replace("@AGENCY", agency.PrefixWithName);
			text = text.Replace("@AG_REPORT_STRING", agency.ReportString);
			text = text.Replace("@REGISTRATION", agency.Registration);
			text = text.Replace("@FIRM", action.FirmName);
			textObj.Text = text;

			textObj = GetTextObject("txtTotal");
			textObj.Text = textObj.Text.Replace("@TOTALSUM",
			                                    string.Format("{0} ({1})", totalPrice.ToString("c"),
			                                                  Money.MoneyToString(totalPrice, true)));

			PrintFooter(agency, action.Firm);
		}

		private static decimal ProcessTotalPrice(DataTable dtData)
		{
			decimal totalPrice = 0;
			foreach (DataRow row in dtData.Rows)
			{
				decimal price = decimal.Parse(row["price"].ToString());
				string priceString = Money.MoneyToString(price, true);
				row["priceString"] = string.Format("На сумму: {0} ({1})", price.ToString("c"), priceString);
				totalPrice += price;
			}
			return totalPrice;
		}

		private static DataTable LoadData(Classes.Action action, Agency agency)
		{
			Dictionary<string, object> procParameters =
				new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
			procParameters[Classes.Action.ParamNames.ActionId] = action.ActionId;
			procParameters[Agency.ParamNames.AgencyId] = agency.AgencyId;

			return DataAccessor.LoadDataSet("rpt_StudioOrderAct", procParameters).Tables[0];
		}
	}
}