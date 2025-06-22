using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
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
		private class Window
		{
			public DataRow FirstRow;
            public DataRow SecondRow;
            public DataRow LastRow;

            public class IssuesWithRoltype
			{
				public IList<DataRow> Issues = new List<DataRow>();
			}

			public IDictionary<string, IssuesWithRoltype> IssuesByRoltype = new Dictionary<string, IssuesWithRoltype>();

            public int IssuesUnprocessed
			{
				get
				{
					int res = 0;
					foreach (IssuesWithRoltype item in IssuesByRoltype.Values)
						res += item.Issues.Count;

                    return res;
				}
			}
		}

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
                DataTable dt = AdjustIssuePositions(data.Tables[0]);
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
				DataTable dt = AdjustIssuePositions(data.Tables[0]);
				ExportDocument document = Classes.GridExport.ExportDocument.GetDocument();
				if (document != null)
					document.Export(dt, Massmedia, DateTime, fileName);
			}
		}

        private DataTable AdjustIssuePositions(DataTable dataTable)
        {
			DataTable dt = dataTable.Clone();
			string time = string.Empty;
            Window window = null;

			foreach(DataRow row in dataTable.Rows) 
			{
				if(time != row[ExportParams.tariffTime].ToString())
				{
					// началось новое рекламное окно
					ProcessTariffWindow(dt, window);
                    window = new Window();
					time = row[ExportParams.tariffTime].ToString();
                }

				string advertType = row[ExportParams.advertTypeId].ToString();
				int position = 0;

				if (row[ExportParams.positionId] != DBNull.Value)
					position = int.Parse(row[ExportParams.positionId].ToString());
				if (position == (int)RollerPositions.First)
					window.FirstRow = row;
				else if (position == (int)RollerPositions.Second)
					window.SecondRow = row;
				else if (position == (int)RollerPositions.Last)
					window.LastRow = row;
				else
				{
					if (!window.IssuesByRoltype.ContainsKey(advertType))
						window.IssuesByRoltype.Add(advertType, new Window.IssuesWithRoltype());

					Window.IssuesWithRoltype list = window.IssuesByRoltype[advertType];
					list.Issues.Add(row);
				}
            }
            ProcessTariffWindow(dt, window);
            return dt;
        }

        private void ProcessTariffWindow(DataTable dt, Window window)
        {
			if (window == null) return;
			if(window.FirstRow != null)
                dt.Rows.Add(window.FirstRow.ItemArray);
            if (window.SecondRow != null)
                dt.Rows.Add(window.SecondRow.ItemArray);

            while (window.IssuesUnprocessed > 0)
			{
				int count = 0;
                foreach (Window.IssuesWithRoltype item in window.IssuesByRoltype.Values.OrderByDescending(val => val.Issues.Count))
                {
					if (item.Issues.Count > 0)
					{
						dt.Rows.Add(item.Issues[0].ItemArray);
						item.Issues.RemoveAt(0);
					}
					if (++count == 2) break;
                }
            }
            if (window.LastRow != null)
                dt.Rows.Add(window.LastRow.ItemArray);
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
				procParameters[SecurityManager.ParamNames.UserId] = User.IDs[0];
			procParameters["isExport"] = isExport;

			try
			{
				DataSet ds = DataAccessor.LoadDataSet("rpt_Grid", procParameters, 120);
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
