using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;
using Merlin.Classes;
using Merlin.Classes.GridExport;
using Merlin.Reports;

namespace Merlin.Forms.GridReport
{
	internal class GridReportCreator
	{
		private Massmedia Massmedia { get; set;}
		private PresentationObject User { get; set;}
		private DateTime DateTime { get; set; }

		public GridReportCreator(Massmedia massmedia, DateTime dateTime, PresentationObject user)
		{
			Massmedia = massmedia;
			User = user;
			DateTime = dateTime.Date;
		}

		private static readonly ILog Log =
			LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public Grid GetReport()
		{
			return InitializeReport(LoadData(false));
		}

		public void ExportDocument()
		{
			if (Massmedia != null)
			{
				DataSet data = LoadData(true);
                DataTable dt = BroadcastGridExport.AdjustIssuePositions(data.Tables[0]);
                ExportDocument document = Classes.GridExport.ExportDocument.GetDocument();
				if (document != null)
					document.Export(dt, Massmedia, DateTime);
			}
		}

		public void ExportDocument(string fileName)
		{
			if (Massmedia != null)
			{
				DataSet data = LoadData(true);
				DataTable dt = BroadcastGridExport.AdjustIssuePositions(data.Tables[0]);
				ExportDocument document = Classes.GridExport.ExportDocument.GetDocument();
				if (document != null)
					document.Export(dt, Massmedia, DateTime, fileName);
			}
		}

        private DataSet LoadData(bool isExport)
		{
			if (Massmedia == null)
				return null;

            Dictionary<string, object> procParameters = new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase)
            {
                [Massmedia.ParamNames.MassmediaId] = Massmedia.MassmediaId,
                ["theDate"] = DateTime,
                ["isExport"] = isExport
            };
			// Менеджер — только для просмотра. Выгрузка для эфира (DJin) — вся сетка станции,
			// иначе в эфир ушли бы выпуски одного менеджера.
			if (User != null && !isExport)
				procParameters[SecurityManager.ParamNames.UserId] = User.IDs[0];

			DataSet ds = DataAccessor.LoadDataSet("rpt_Grid_v3", procParameters, 120);
			return ds;
		}
        
		private Grid InitializeReport(DataSet ds)
		{
			// Set data
			Grid report = new Grid();
			report.SetDataSource(ds.Tables[0]);

			// Init text fields
			TextObject textObj = report.ReportDefinition.ReportObjects["txtMassmedia"] as TextObject;
			textObj.Text = Massmedia.Name;

			textObj = report.ReportDefinition.ReportObjects["txtDate"] as TextObject;
			textObj.Text =
				string.Format("{0} ({1})", DateTime.ToShortDateString(),
							  DateTimeUtils.ResolveWeekDayName(DateTime.DayOfWeek));

			if (User == null)
			{
				textObj = report.ReportDefinition.ReportObjects["lblManager"] as TextObject;
				textObj.Text = string.Empty;
			}
			else
			{
				textObj = report.ReportDefinition.ReportObjects["txtManager"] as TextObject;
				textObj.Text = User.Name;
			}

			// Statistics
			Dictionary<string, object> procParameters =
				new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);

			procParameters["StartDay"] = procParameters["FinishDay"] = DateTime;
			procParameters[Massmedia.ParamNames.MassmediaId] = Massmedia.MassmediaId;
			try
			{
				DataSet dsStats = DataAccessor.LoadDataSet("stat_FillPercentage", procParameters, 60);
				if (dsStats != null && dsStats.Tables.Count > 0 && dsStats.Tables[0].Rows.Count > 0)
				{
					DataRow row = dsStats.Tables[0].Rows[0];
					textObj = report.ReportDefinition.ReportObjects["txtLoading"] as TextObject;
					if (textObj != null)
						textObj.Text = row["fill"].ToString();

					textObj = report.ReportDefinition.ReportObjects["txtDayLoading"] as TextObject;
					if (textObj != null)
						textObj.Text = row["realTime"].ToString();
				}
			}
			catch (Exception e)
			{
				ErrorManager.LogError("Cannot stat_FillPercentage", e);
			}

			return report;
		}
	}
}
