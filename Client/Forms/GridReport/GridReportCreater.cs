using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using CrystalDecisions.CrystalReports.Engine;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using log4net;
using Merlin.Classes;
using Merlin.Reports;

namespace Merlin.Forms.GridReport
{
	internal class GridReportCreater
	{
		private Massmedia Massmedia { get; set;}
		private PresentationObject User { get; set;}
		private DateTime DateTime { get; set; }

		public GridReportCreater(Massmedia massmedia, DateTime dateTime, PresentationObject user)
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
				Classes.GridExport.ExportDocument document = Classes.GridExport.ExportDocument.GetDocument((RolType)Massmedia.RolTypeId);
				if (document != null)
					document.Export(data.Tables[0], Massmedia, DateTime);
			}
		}

		public void ExportDocument(string fileName)
		{
			if (Massmedia != null)
			{
				DataSet data = LoadData(true);
				Classes.GridExport.ExportDocument document = Classes.GridExport.ExportDocument.GetDocument((RolType)Massmedia.RolTypeId);
				if (document != null)
					document.Export(data.Tables[0], Massmedia, DateTime, fileName);
			}
		}

		private DataSet LoadData(bool isExport)
		{
			if (Massmedia == null)
				return null;

			Dictionary<string, object> procParameters =
				new Dictionary<string, object>(StringComparer.CurrentCultureIgnoreCase);
			procParameters[Massmedia.ParamNames.MassmediaId] = Massmedia.MassmediaId;
			procParameters["theDate"] = DateTime;
			if (User != null)
				procParameters["userID"] = User.IDs[0];
			procParameters["isExport"] = isExport;
			procParameters["loggedUserID"] = SecurityManager.LoggedUser.Id;

			try
			{
				Log.InfoFormat("Start rpt_grid {0}", DateTime.Now);
				DataSet ds = DataAccessor.LoadDataSet("rpt_Grid", procParameters, 120);
				Log.InfoFormat("End rpt_grid {0}", DateTime.Now);
				return ds;
			}
			catch (Exception exp)
			{
				Log.InfoFormat("Error rpt_grid {0} : {1}", DateTime.Now, exp);
				throw;
			}
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
			procParameters["loggedUserID"] = SecurityManager.LoggedUser.Id;
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
