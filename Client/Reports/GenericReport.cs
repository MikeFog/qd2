using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Reports
{
    internal abstract class GenericReport
	{
		protected struct ReportParts
		{
            public const string SponsorContractTitle = "SponsorContractTitle";
            public const string SponsorContractTitle2 = "SponsorContractTitle2";
            public const string SponsorContractHeader = "SponsorContractHeader";
            public const string SponsorContractSubject = "SponsorContract.ContractSubject";
            public const string SponsorContractLegalInfoTitle = "SponsorContractLegalInfoTitle";
            public const string SponsorContractFooterLeftPart = "SponsorContractFooterLeftPart";
            public const string SponsorContractFooterRightPart = "SponsorContractFooterRightPart";
            public const string SponsorContractSealPlaceLeft = "SponsorContractSealPlaceLeft";
            public const string SponsorContractSealPlaceRight = "SponsorContractSealPlaceRight";

            public const string ContractTitle = "ContractTitle";
            public const string ContractTitle2 = "ContractTitle2";
            public const string ContractHeader = "ContractHeader";
            public const string ContractSubject = "Contract.ContractSubject";
            public const string ContractLegalInfoTitle = "ContractLegalInfoTitle";
            public const string ContractFooterLeftPart = "ContractFooterLeftPart";
            public const string ContractFooterRightPart = "ContractFooterRightPart";
            public const string ContractSealPlaceLeft = "ContractSealPlaceLeft";
            public const string ContractSealPlaceRight = "ContractSealPlaceRight";


            public const string BillContractTitle = "BillContractTitle";
            public const string BillContractHeader = "BillContractHeader";
			public const string BillContract1 = "billContract1";
            public const string BillContract2 = "billContract2";
            public const string BillContractLegalInfoTitle = "BillContractLegalInfoTitle";
            public const string BillContractFooterLeftPart = "BillContractFooterLeftPart";
            public const string BillContractFooterRightPart = "BillContractFooterRightPart";
            public const string BillContractSealPlaceLeft = "BillContractSealPlaceLeft";
            public const string BillContractSealPlaceRight = "BillContractSealPlaceRight";
        }

        protected readonly PresentationObject _bill;
		protected DateTime _contractDate;
        protected readonly Classes.Action _action;
        protected Agency _agency;
		protected IOrganization _firm;
		protected ReportClass _report;
        protected bool _isSealPrinted = false;
        private static Dictionary<string, string> _reportTextParts;

        protected GenericReport(ReportClass report, Agency agency)
        {
            _report = report;
            _agency = agency;
            _report.InitReport += InitReport;
        }

        protected GenericReport(ReportClass report, Agency agency, Classes.Action action, PresentationObject bill)
        {
			_report = report;
			_agency = agency;
            _bill = bill;
            _action = action;
			if (action != null)
				_firm = action.Firm;
			_contractDate = DateTime.Parse(_bill[TableColumns.Bill.BillDate].ToString());
            _report.InitReport += InitReport;
        }

        private void InitReport(object sender, EventArgs e)
        {
            ProcessReport();
        }

        internal void Show(string caption)
		{
			FrmReport fReport = new FrmReport(_report) {Text = caption, MdiParent = Globals.MdiParent};
			fReport.Show();
		}

        protected FieldObject GetFieldObject(string objectName)
        {
            return GetObject<FieldObject>(objectName);
        }

        protected TextObject GetTextObject(string objectName)
		{
			return GetObject<TextObject>(objectName);
		}

		protected BlobFieldObject GetBlobObject(string objectName)
		{
			return GetObject<BlobFieldObject>(objectName);
		}

        [DebuggerNonUserCode] // Note: to undebbug try-catch
		private T GetObject<T>(string objectName)
			where T : ReportObject 
		{
			try
			{
				return _report.ReportDefinition.ReportObjects[objectName] as T;
			}
			catch
			{
				return null;
			}
		}

		protected void SetTextObjectText(string objectName, string text)
		{
			//if (StringUtil.IsDBNullOrEmpty(text)) return;

			TextObject to = GetTextObject(objectName);
			if (to != null) to.Text = text;
		}

		protected void ReplaceTextObjectText(string objectName, string oldString, string newString)
		{
			TextObject to = GetTextObject(objectName);
			if (to != null)
			{
				to.Text = to.Text.Replace(oldString, newString);
			}
		}

		protected void PrintFooter(Agency agency, Firm firm)
		{
			// Agency related data
			SetTextObjectText("txtAgencyName", agency.PrefixWithName);
			SetTextObjectText("txtAgencyAddress", agency.Address);

			ReplaceTextObjectText("txtAgencyINN", "@AgencyINN", agency.INN);
            ReplaceTextObjectText("txtAgencyRAccount", "@AgencyRAccount", agency.Account);
			ReplaceTextObjectText("txtAgencyEGRN", "@AgencyEGRN", agency.EGRN);

			PresentationObject bank = agency.Bank;
			ReplaceTextObjectText("txtAgencyBank", "@AgencyBank", bank != null ? bank.Name : string.Empty);
			ReplaceTextObjectText("txtAgencyCorAccount", "@AgencyCorAccount",
			                      bank != null ? bank["corAccount"].ToString() : string.Empty);
			ReplaceTextObjectText("txtAgencyBIK", "@AgencyBIK", bank != null ? bank["bik"].ToString() : string.Empty);

			// Firm related data
			SetTextObjectText("txtFirmName", firm.PrefixWithName);
			SetTextObjectText("txtFirmAddress", firm.Address);

			ReplaceTextObjectText("txtFirmINN", "@FirmINN", firm.INN);
			ReplaceTextObjectText("txtFirmRAccount", "@FirmRAccount", firm.Account);
			ReplaceTextObjectText("txtFirmEGRN", "@FirmEGRN", firm.EGRN);

			bank = firm.Bank;

			ReplaceTextObjectText("txtFirmBank", "@FirmBank", bank != null ? bank.Name : string.Empty);
			ReplaceTextObjectText("txtFirmCorAccount", "@FirmCorAccount",
			                      bank != null ? bank["corAccount"].ToString() : string.Empty);
			ReplaceTextObjectText("txtFirmBIK", "@FirmBIK", bank != null ? bank["bik"].ToString() : string.Empty);
		}

		protected void SetPaintings(Organization organization, DataTable dtData)
		{
			if(organization.Signature != null && MessageBox.ShowQuestion("Распечатать документ с подготовленными подписями?") == DialogResult.Yes)
			{
                UpdateTopsOnChange(SetResolution(organization.Signature, DirPainting), DirPainting);
                _report.Refresh();
				_isSealPrinted = true;
            }
			else
			{
				dtData.Columns.Remove("dirPainting");
                _isSealPrinted = false;
            }
        }

		private void UpdateTopsOnChange(int heightChange, ReportObject changedObject)
		{
			if (heightChange == 0 || string.IsNullOrEmpty(PaintingsSectionName))
				return;

			foreach (ReportObject reportObject in _report.ReportDefinition.Sections["Section4"].ReportObjects)
			{
				if (reportObject.Top > changedObject.Top)
					reportObject.Top = Math.Max(reportObject.Top + heightChange, changedObject.Top);
			}
		}
        
		/// <summary>
		/// 
		/// </summary>
		/// <param name="row"></param>
		/// <param name="painting"></param>
		/// <returns>Возвращает измененную высоту в twips</returns>
		private int SetResolution(Image img, BlobFieldObject painting)
		{
			if (painting == null)
				return 0;
			
			int oldHeight = painting.Height;
			painting.Width = GetInTwips(img.Width, img.HorizontalResolution);
			painting.Height = GetInTwips(img.Height, img.VerticalResolution);
			return painting.Height - oldHeight;
		}

		protected virtual BlobFieldObject DirPainting
		{
			get { return null; }
		}

		protected virtual string PaintingsSectionName
		{
			get { return null; }
		}

		/// <summary>
		/// Convert cm in twips (1/1440 inch)
		/// </summary>
		/// <param name="inch"></param>
		/// <returns></returns>
		public int GetInTwips(float inch)
		{
			return (int)(inch * (float)1440);
		}

		public int GetInTwips(int pixels, float resolution)
		{
			return GetInTwips((float)pixels / resolution);
		}

		internal void Export(ReportExportFormat exportFormatType)
		{
			Globals.ReportExporter.Export(_report, exportFormatType, true);
		}

		protected static void LoadReportPartTexts()
        {
			DataTable dt = EntityManager.GetEntity((int)Entities.ReportPartText).GetContent();
			_reportTextParts = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);
			foreach (DataRow row in dt.Rows)
				_reportTextParts.Add(row["codeName"].ToString(), row["reportText"].ToString());
        }

		protected static string OnAirInquireTextPart1
        {
            get
            {
                _reportTextParts.TryGetValue("efir1", out string res);
                return res;
			}
        }

        protected static string NoNDSText
        {
            get
            {
                _reportTextParts.TryGetValue("NoNDSText", out string res);
                return res;
            }
        }

        protected string BillNo
        {
            get { return _bill[TableColumns.Bill.BillNo].ToString(); }
        }

		protected string CreateFormulaText(string textPartName)
		{
            return string.Format("\"{0}\"", GetTextPart(textPartName));
        }

        protected string GetTextPart(string name)
        {
			_reportTextParts.TryGetValue(name, out string res);
			res = res?.Replace("@agencyName", _agency.Name).Replace("@agencyFullPrefix", _agency.FullPrefix).Replace("@agencyRegistration", _agency.Registration)
				.Replace("@agencyBossText", _agency.ReportString).Replace("@agencyAddress", _agency.Address).Replace("@agencyINN", _agency.INN)
				.Replace("@agencyKPP", _agency.KPP).Replace("@agencyAccount", _agency.Account).Replace("@agencyPrefixWithName", _agency.PrefixWithName)
				.Replace("@agencyOGRN", _agency.EGRN).Replace("@agencyDirector", _agency.Director)
				.Replace("@firmPrefixWithName", _firm.PrefixWithName).Replace("@firmOGRN", _firm.EGRN)
				.Replace("@firmRegistration", _firm.Registration == string.Empty ? "_________________________" : _firm.Registration)
				.Replace("@firmBossText", _firm.ReportString == string.Empty ? "_________________________" : _firm.ReportString)
				.Replace("@firmDirector", _firm.Director)
				.Replace("@firmAddress", _firm.Address).Replace("@firmINN", _firm.INN)
				.Replace("@firmKPP", _firm.KPP).Replace("@firmAccount", _firm.Account)
				;
			if (_agency.Bank != null)
				res = res?.Replace("@agencyBankName", _agency.Bank.Name).Replace("@agencyBankAccount", _agency.Bank[Organization.ParamNames.BankAccount].ToString())
				.Replace("@agencyBankBIK", _agency.Bank[Organization.ParamNames.BankBIK].ToString());
			else
                res = res?.Replace("@agencyBankName", string.Empty).Replace("@agencyBankAccount", string.Empty).Replace("@agencyBankBIK", string.Empty);
            if (_firm.Bank != null)
				res = res?.Replace("@firmBankName", _firm.Bank.Name).Replace("@firmBankAccount", _firm.Bank[Organization.ParamNames.BankAccount].ToString())
				.Replace("@firmBankBIK", _firm.Bank[Organization.ParamNames.BankBIK].ToString());
			else
                res = res?.Replace("@firmBankName", string.Empty).Replace("@firmBankAccount", string.Empty).Replace("@firmBankBIK", string.Empty);
			if (_action != null)
				res = res?.Replace("@actionID", _action.ActionId.ToString()).Replace("@billNo", BillNo);

            res = res?.Replace("\"", "\"\"")
				.Replace("\r\n", "<br>")
				.Replace("\r", "<br>")
				.Replace("\n", "<br>");
            return res;
        }

		protected abstract void ProcessReport();
    }
}