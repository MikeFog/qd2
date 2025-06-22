using System;
using System.Data;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System.Collections.Generic;
using FogSoft.WinForm.DataAccess;
using System.Text;
using CrystalDecisions.Web;
using static FogSoft.WinForm.Classes.Entity;
using System.Runtime.Remoting.Lifetime;

namespace Merlin.Reports
{
	internal class ActionAgreementAudioReport : ActionAgreementReport
	{
		public ActionAgreementAudioReport(Classes.Action action, Agency agency, DateTime date)
			: base(new ActionAgreementAudio(), action, agency, date, false)
		{
		}
	}

    internal class ActionSponsorAgreementAudioReport : ActionAgreementReport
    {
        public ActionSponsorAgreementAudioReport(Classes.Action action, Agency agency, DateTime date)
            : base(new ActionSponsorAgreementAudio(), action, agency, date, true)
        {
        }
    }

	internal class ActionAgreementReport : GenericReport
	{
        private bool _isSponsor = false;
		public ActionAgreementReport(ReportClass report, Classes.Action action, Agency agency, DateTime date, bool sponsor)
			: base(report, agency, action, null)
		{
            _isSponsor = sponsor;
		}

		protected override BlobFieldObject DirPainting
		{
			get
			{
				return GetBlobObject("dirPainting");
			}
		}

        private String GetActionPrograms(Classes.Action action)
        {
            Dictionary<string, object> procParameters =
                new Dictionary<string, object>(2, StringComparer.InvariantCultureIgnoreCase);

            procParameters[Merlin.Classes.Action.ParamNames.ActionId] = action.ActionId;
            StringBuilder sb = new StringBuilder();

            DataSet ds = DataAccessor.LoadDataSet("rpt_ActionPrograms", procParameters);
            DataTable dt = ds.Tables[0];
            foreach (DataRow dr in dt.Rows)
            {
                if (sb.Length > 0)
                {
                    sb.Append(" ");
                }

                sb.Append(dr["name"].ToString());
            }

            return sb.ToString();
        }

        protected override void ProcessReport()
        {
            DataTable dataSource = new DataTable();
            dataSource.Columns.Add("rowNum", typeof(int));
            dataSource.Rows.Add(1);
            SetPaintings(_agency, dataSource);
            _report.SetDataSource(dataSource);
            /*
            action = ActionOnMassmedia.GetActionById(_action.ActionId);
            Firm firm = action.Firm;
            */
            PrintFooter(_agency, _action.Firm);
            SetTextObjectText("txtNumber", (_isSponsor ? "Спонсорский договор к рекламной акции № " : "Договор для рекламной акции № ") + _action.ActionId);

            SetTextObjectText("txtBillDate", date.ToShortDateString());

            TextObject textObject = GetTextObject("txtCaption");
            if (textObject != null)
            {
                string text = textObject.Text;
                text = text.Replace("@AGENCY_DIRECTOR", agency.ReportString);
                text = text.Replace("@AGENCY_REGISTRATION", agency.Registration);
                text = text.Replace("@AGENCY", agency.PrefixWithName);
                text = text.Replace("@FIRM", firm.PrefixWithName);
                textObject.Text = text;
            }

            StringBuilder sMassMedia = new StringBuilder();
            DataSet ds = action.GetMassmedias(agency);
            if (ds.Tables.Count > 0)
            {
                DataTable dt = ds.Tables[0];
                foreach (DataRow dr in dt.Rows)
                {
                    if (sMassMedia.Length > 0)
                    {
                        sMassMedia.Append(", ");
                    }

                    string massmediaPrefix = ParseHelper.GetStringFromObject(dr["massmediaPrefix"], null);
                    if (!string.IsNullOrEmpty(massmediaPrefix))
                    {
                        sMassMedia.AppendFormat("{0} ", massmediaPrefix);
                    }

                    sMassMedia.Append(dr["massmediaName"]);
                }
            }

            ReplaceTextObjectText("txtMassMedia", "@MASSMEDIA", sMassMedia.ToString());

            if (sponsor)
            {
                ReplaceTextObjectText("txtMassMedia", "@PROGRAM", GetActionPrograms(action));
            }

            decimal price = 0;
            decimal taxPrice = 0;
            foreach (DataRow row in action.Campaigns.Rows)
            {
                Campaign c = Campaign.GetCampaignById(int.Parse(row["campaignID"].ToString()));
                if (c.Agency.AgencyId == agency.AgencyId)
                {
                    decimal cPrice;
                    decimal cTax;
                    c.GetPriceByPeriodWithTax(c.StartDate, c.FinishDate, out cPrice, out cTax);
                    price += cPrice;
                    taxPrice += cTax;
                }
            }

            ReplaceTextObjectText("txtPrice", "@PRICE",
                                  string.Format("{0} ({1})", price.ToString("c"),
                                                Money.MoneyToString(price, true)));
            ReplaceTextObjectText("txtPrice", "@TAX_PRICE", string.Format(" {0}", taxPrice.ToString("c")));
        }
    }
}