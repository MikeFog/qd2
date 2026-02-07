using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System;
using System.Data;

namespace Merlin.Reports
{
    internal class ContractReport : BillReport
    {
        private readonly bool _isSponsor;

        public ContractReport(Classes.Action action, Agency agency, PresentationObject bill, bool isSponsor = false) : base(action, agency, bill, new Contract())
        {
            _isSponsor = isSponsor;
        }

        public ContractReport(Firm firm, Agency agency, PresentationObject bill, bool isSponsor = false) : base(firm, agency, bill, new Contract())
        {
            _isSponsor = isSponsor;
        }

        protected override void ProcessReport()
        {
            ProcessDataAndPlaceFields();
            DataTable dt = _agency.LoadPainting();
            SetPaintings(_agency, dt);
            _report.SetDataSource(dt);

            LoadReportPartTexts();
            if (_action != null)
                _report.DataDefinition.FormulaFields["txtTitle"].Text = CreateFormulaText(_isSponsor ? ReportParts.SponsorContractTitle : ReportParts.ContractTitle);
            else
                _report.DataDefinition.FormulaFields["txtTitle"].Text = CreateFormulaText(_isSponsor ? ReportParts.SponsorContractTitle2: ReportParts.ContractTitle2);
            _report.DataDefinition.FormulaFields["txtHeader"].Text = CreateFormulaText(_isSponsor ? ReportParts.SponsorContractHeader : ReportParts.ContractHeader);

            var taxValue = _agency.GetTaxValue(_contractDate);
            if (taxValue > 0)
                _report.DataDefinition.FormulaFields["txtContractSubject"].Text =
                    CreateFormulaText(_isSponsor ? ReportParts.SponsorContractSubject : ReportParts.ContractSubject).Replace("{tax}", taxValue.ToString());
            else
                _report.DataDefinition.FormulaFields["txtContractSubject"].Text = 
                    CreateFormulaText(_isSponsor ? ReportParts.SponsorContractSubjectNoNDS : ReportParts.ContractSubjectNoNDS);

            _report.DataDefinition.FormulaFields["txtLegalInfoTitle"].Text = 
                CreateFormulaText(_isSponsor ? ReportParts.SponsorContractLegalInfoTitle : ReportParts.ContractLegalInfoTitle);
            _report.DataDefinition.FormulaFields["txtFooterLeftPart"].Text = 
                CreateFormulaText(_isSponsor ? ReportParts.SponsorContractFooterLeftPart : ReportParts.ContractFooterLeftPart);
            _report.DataDefinition.FormulaFields["txtFooterRightPart"].Text = 
                CreateFormulaText(_isSponsor ? ReportParts.SponsorContractFooterRightPart : ReportParts.ContractFooterRightPart);

            if (!_isSealPrinted)
                _report.DataDefinition.FormulaFields["txtSealPlaceLeft"].Text = 
                    CreateFormulaText(_isSponsor ? ReportParts.SponsorContractSealPlaceLeft : ReportParts.ContractSealPlaceLeft);
            _report.DataDefinition.FormulaFields["txtSealPlaceRight"].Text = 
                CreateFormulaText(_isSponsor ? ReportParts.SponsorContractSealPlaceRight : ReportParts.ContractSealPlaceRight);
        }

        protected override DataTable LoadBillData(Agency agency, Classes.Action action, DateTime? month)
        {
            return null;
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
            get { return "ReportFooterSection2"; }
        }
    }
}