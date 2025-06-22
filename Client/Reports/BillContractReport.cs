using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Merlin.Reports
{
    internal class BillContractReport : BillReport
    {
        public BillContractReport(Classes.Action action, Agency agency, PresentationObject bill) 
            : base(action, agency, bill, new BillContract())
        {
        }

        protected override void ProcessReport()
        {
            base.ProcessReport();
            ProcessDataAndPlaceFields();
            _report.DataDefinition.FormulaFields["txtTitle"].Text = CreateFormulaText(ReportParts.BillContractTitle);
            _report.DataDefinition.FormulaFields["txtHeader"].Text = CreateFormulaText(ReportParts.BillContractHeader);
            _report.DataDefinition.FormulaFields["txtPart1"].Text = CreateFormulaText(ReportParts.BillContract1); 
            _report.DataDefinition.FormulaFields["txtPart2"].Text = CreateFormulaText(ReportParts.BillContract2);
            _report.DataDefinition.FormulaFields["txtLegalInfoTitle"].Text = CreateFormulaText(ReportParts.BillContractLegalInfoTitle);
            _report.DataDefinition.FormulaFields["txtFooterLeftPart"].Text = CreateFormulaText(ReportParts.BillContractFooterLeftPart);
            _report.DataDefinition.FormulaFields["txtFooterRightPart"].Text = CreateFormulaText(ReportParts.BillContractFooterRightPart);
            if(!_isSealPrinted)
                _report.DataDefinition.FormulaFields["txtSealPlaceLeft"].Text = CreateFormulaText(ReportParts.BillContractSealPlaceLeft);
            _report.DataDefinition.FormulaFields["txtSealPlaceRight"].Text = CreateFormulaText(ReportParts.BillContractSealPlaceRight);
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
            get {  return "ReportFooterSection6";  }
        }
    }
}
