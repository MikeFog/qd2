using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using static FogSoft.WinForm.Classes.Entity;

namespace Merlin.Reports
{
	internal class BillReport : GenericReport
	{
        private readonly DateTime? _month;

        public BillReport(Classes.Action action, Agency agency, PresentationObject bill)
            : this(action, agency, bill, new GenericBill())
        {
        }

        public BillReport(Classes.Action action, Agency agency, PresentationObject bill, ReportClass report)
            : base(report, agency, action, bill)
		{
        }

        protected BillReport(Firm firm, Agency agency, PresentationObject bill, ReportClass report)
			: base(report, agency, null, bill)
        {
            _firm = firm;
        }

        public BillReport(Classes.Action action, Agency agency, PresentationObject bill, DateTime? month)
			: base(new GenericBill(), agency, action, bill)
		{
            _month = month;
            _firm = action.Firm;
		}

        protected override void ProcessReport()
        {
			DataTable dtData = LoadBillData(_agency, _action, _month);
            GenericReport.LoadReportPartTexts();
            SetPaintings(_agency, dtData);

			_report.SetDataSource(dtData);			
			SetTextObjectText("txtBillNo", _month.HasValue ? string.Format("Счёт № {0} от {1} за месяц {2} {3} года к {5} № {4}", BillNo, BillDate.ToShortDateString()
				, DateTimeFormatInfo.CurrentInfo.MonthNames[_month.Value.Month - 1], _month.Value.Year, _action.ActionId, "акции") :
							  string.Format("Счёт № {0} от {1} к {3} № {2}", BillNo, BillDate.ToShortDateString(), _action.ActionId, "акции"));
			SetTextObjectText("txtAgency", _agency.PrefixWithName);
			SetTextObjectText("txtAgencyINN", _agency.INN);
            SetTextObjectText("txtAgencyKPP", _agency.KPP);
            SetTextObjectText("txtAgencyAddress", _agency.Address);
			SetTextObjectText("txtAgencyPhoneAndFax", string.Format("{0}, {1}", _agency.Phone, _agency.Email));
			SetTextObjectText("txtAgencyAccount", _agency.Account);

			PresentationObject bank = _agency.Bank;
			if (bank != null)
			{
				SetTextObjectText("txtBank", bank.Name);
				SetTextObjectText("txtCorAccount", bank[TableColumns.Bank.CorAccount].ToString());
				SetTextObjectText("txtBIK", bank[TableColumns.Bank.Bik].ToString());
			}
			SetTextObjectText("txtFirm", _firm.PrefixWithName);
			SetTextObjectText("txtFirmINN", _firm.INN);
			SetTextObjectText("txtFirmAddress", _firm.Address);

			SetTextObjectText("txtSummaInWords", Money.MoneyToString(CalculateBillTotal(dtData), false));
			SetTextObjectText("txtDirector", _agency.Director);
			SetTextObjectText("txtBookKeeper", _agency.BookKeeper);
            SetTextObjectText("txtTaxString", GenericReport.NoNDSText);
            SetTextObjectText("txtContactPerson", _action.Creator.ContactInfo);
            SetTextObjectText("txtNDSColumn", CalcTotalTax(dtData) == 0 ? "НДС" : "НДС (5%)");
        }

        private decimal CalcTotalTax(DataTable dtData)
        {
            object sumObject = dtData.Compute("SUM(tax)", string.Empty);
            return sumObject == DBNull.Value ? 0 : Convert.ToDecimal(sumObject);
        }

		protected DateTime BillDate
		{
			get { return _month.HasValue ? DateTime.Now : _contractDate; }
		}

        protected virtual DataTable LoadBillData(Agency agency, Classes.Action action, DateTime? month)
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
            procParameters[Classes.Action.ParamNames.ActionId] = action.ActionId;
            procParameters[Agency.ParamNames.AgencyId] = agency.AgencyId;

            if (month.HasValue)
            {
                procParameters["beginDate"] = new DateTime(month.Value.Year, month.Value.Month, 1);
                procParameters["endDate"] = new DateTime(month.Value.Year, month.Value.Month, DateTime.DaysInMonth(month.Value.Year, month.Value.Month));
            }

            return DataAccessor.LoadDataSet("rpt_GenericBill", procParameters).Tables[0];
        }

        protected decimal CalculateBillTotal(DataTable dtData)
        {
            decimal summa = 0;
            foreach (DataRow row in dtData.Rows)
            {
                if (row["price"] != DBNull.Value)
                {
                    decimal price = decimal.Parse(row["price"].ToString());
                    summa += price;
                }
            }
            return Math.Round(summa, 2, MidpointRounding.ToEven);
        }

        protected override CrystalDecisions.CrystalReports.Engine.BlobFieldObject DirPainting
        {
            get
            {
                return GetBlobObject("dirPainting1");
            }
        }

        protected override string PaintingsSectionName
        {
            get { return "Section4"; }
        }

        protected void ProcessDataAndPlaceFields()
        {
            SetTextObjectText("txtContractDate", BillDate.ToShortDateString());
            SetTextObjectText("txtAgencyContractPlace", _agency.ReportPlace);
        }
    }
}