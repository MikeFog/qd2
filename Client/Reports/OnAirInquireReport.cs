using System;
using System.Collections.Generic;
using System.Data;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Reports
{
	internal class OnAirInquireReport : GenericReport
	{
        private readonly Massmedia _radiostation;
        private readonly DataSet _ds;
        private readonly Campaign _campaign;
        private readonly bool _isShowPrice;
        private readonly DateTime _month;

        public OnAirInquireReport(Campaign campaign, Agency agency, DataSet ds, bool showPrice, Massmedia mm, DateTime month)
			: base(new OnAirInquire(), agency)
		{
            _radiostation = mm;
            _ds = ds;   
            _campaign = campaign;
            _isShowPrice = showPrice;   
            _month = month;
        }

        protected override CrystalDecisions.CrystalReports.Engine.BlobFieldObject DirPainting
		{
			get
			{
				return GetBlobObject("dirPainting2");
			}
		}

		protected override string PaintingsSectionName { get { return "Section4"; } }

        protected override void ProcessReport()
        {
            SetPaintings(_radiostation, _ds.Tables[0]);
            _report.SetDataSource(_ds.Tables[0]);
            _report.Subreports[0].SetDataSource(_ds.Tables[1]);

            if (_ds.Tables[1].Rows.Count == 0)
                ((TextObject)(_report.Subreports[0].ReportDefinition.ReportObjects["txtTitle"])).Text = string.Empty;

            Firm firm = _campaign.Action.Firm;
            PrintFooter(_agency, firm);

            SetTextObjectText("txtDirector", _radiostation.Director);
            SetTextObjectText("txtMassMedia", _radiostation.Prefix);
            SetTextObjectText("txtMassMedia2", _radiostation.Prefix);

            SetTextObjectText("txtFounder", _radiostation.Founder);
            SetTextObjectText("txtGroupName", _radiostation.GroupName);
            SetTextObjectText("txtRadioStationName", _radiostation.NameWithoutGroup);
            SetTextObjectText("txtCertificateIssued", _radiostation.CertificateIssued);

            GenericReport.LoadReportPartTexts();
            SetTextObjectText("txtReportPartText1", GenericReport.OnAirInquireTextPart1);

            if (_isShowPrice)
            {
                _campaign.GetPriceByPeriodWithTax(new DateTime(_month.Year, _month.Month, 1),
                    new DateTime(_month.Year, _month.Month, DateTime.DaysInMonth(_month.Year, _month.Month), 23, 59, 59),
                    _radiostation.MassmediaId, false, null, out decimal price, out _, out decimal taxPrice);
                SetTextObjectText("txtPrice", $"Сумма: {price:c} ({Money.MoneyToString(price, false)})");
                if (taxPrice > 0)
                    SetTextObjectText("txtTaxString", $"В том числе  НДС  (5%) - {taxPrice:c}");
            }

            SetTextObjectText("txtCaption", string.Format("Эфирная справка для акции №{0}", _campaign.Action.ActionId));
        }
    }
}