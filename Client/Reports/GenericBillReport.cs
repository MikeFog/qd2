using System;
using System.Collections.Generic;
using System.Data;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Reports
{
	internal class GenericBillReport : BillReport
	{
		private const string SignName = "акции";

		public GenericBillReport(Classes.Action action, Agency agency, PresentationObject bill)
			: base(action, agency, bill, new GenericBill(), SignName)
		{
		}

		public GenericBillReport(Classes.Action action, Agency agency, PresentationObject bill, DateTime month)
			: base(action, agency, bill, month, new GenericBill(), SignName)
		{
		}

		public GenericBillReport(Classes.Action action, Agency agency, PresentationObject bill, ReportClass report) 
			: base(action, agency, bill, report, SignName)
		{
		}
		public GenericBillReport(Firm firm, Agency agency, PresentationObject bill, ReportClass report)
			: base(firm, agency, bill, report, SignName)
		{
		}






	}
}