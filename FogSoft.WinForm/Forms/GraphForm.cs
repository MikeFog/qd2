using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Properties;

namespace FogSoft.WinForm.Forms
{
	public enum PieType
	{
		Pie,
		Doughnut
	}

	public enum LabelType
	{
		Inside,
		Outside,
		Disabled
	}

	public enum DrawningStyle
	{
		Default,
		SoftEdge,
		Concave
	}

	/// <summary>
	/// For graph
	/// </summary>
	public partial class GraphForm : Form
	{
		private const string propertyCollectedSliceExploded = "CollectedSliceExploded";

		private readonly ChartSettingsForm settingsFrm = new ChartSettingsForm
		                                                 	{
		                                                 		UseLegend = true,
		                                                 		CollectedThreshold = true,
		                                                 		CollectedLabel = "Остальное",
		                                                 		CollectedThresholdValue = 5,
		                                                 		Is3D = true,
		                                                 		PieType = PieType.Pie,
		                                                 		LabelType = LabelType.Outside,
		                                                 		DrawningStyle = DrawningStyle.Default
		                                                 	};

		public GraphForm(ReportGenerator generator)
			: this (generator, true)
		{
		}

		public GraphForm(ReportGenerator generator, bool isAutoLoaded)
		{
			InitializeComponent();
			Generator = generator;
			if (Globals.MdiParent != null)
				Icon = Globals.MdiParent.Icon;
			tsbFilter.Enabled = generator.Entity.IsFilterable;
			grid.Entity = generator.Entity;
			if (isAutoLoaded)
				RefreshJournal();
		}

		private void SetHighlightEnabled()
		{
			tsbHighlight.Visible = grid.Entity != null && grid.DataSource != null && grid.Entity.GetColumnsForHighlight().Count > 0;
		}

		protected void InitReportTypes()
		{
			string selText = tscbType.SelectedItem != null ? tscbType.SelectedItem.ToString() : string.Empty;
			tscbType.Items.Clear();
			Dictionary<ReportType, string> types = Generator.ReportTypes;
			foreach (KeyValuePair<ReportType, string> type in types)
			{
				tscbType.Items.Add(type.Value);
			}
			if (types.Values.Contains(selText))
				tscbType.SelectedItem = selText;
			else 
				tscbType.SelectedIndex = 0;
		}

		public ReportType SelectedType
		{
			get { return Generator.ReportTypes.ToArray()[tscbType.SelectedIndex].Key; }
		}

		#region Members ---------------------------------------

		private ReportGenerator Generator { get; set;}

		#endregion

		public void RefreshJournal()
		{
			WaitCallback async = new WaitCallback(LoadData);

			if (Generator.FilterValues.Count > 0)
			{
				tsbFilter.Image = Resources.FilterSet;
				ThreadPool.QueueUserWorkItem(async, Generator.FilterValues);
			}
			else
			{
				tsbFilter.Image = Resources.Filter;
				ThreadPool.QueueUserWorkItem(async);
			}
		}

		protected virtual void LoadData(object stateInfo)
		{
			try
			{
				Application.DoEvents();

				SetButtonsEnabled(false);

				if (!IsDisposed && IsHandleCreated)
				{
					Globals.SetWaitCursor(this);
				}

				Generator.ReloadData();

				if (!IsHandleCreated)
					Thread.Sleep(500);

				if (!IsDisposed && IsHandleCreated)
					Invoke(new Globals.VoidCallback(InitReportTypes));
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				if (!IsDisposed && IsHandleCreated)
				{
					Globals.SetDefaultCursor(this);
					SetButtonsEnabled(true);
				}
			}
		}

		#region Set Enabled buttons (thread safe)

		delegate void SetButtonsEnabledCallback(bool enabled);
		public void SetButtonsEnabled(bool enabled)
		{
			if (InvokeRequired)
			{
				SetButtonsEnabledCallback d = new SetButtonsEnabledCallback(SetButtonsEnabled);
				MdiParent.Invoke(d, new object[] { enabled });
			}
			else
			{
				if (!enabled)
				{
					chartView.Visible = false;
					grid.Visible = false;
					tsbSum.Visible = false;
					tsbExcel.Visible = false;
					tsbSave.Visible = false;
					tsbHighlight.Visible = false;
					tsbChartSettings.Visible = false;
				}
				tscbType.Enabled = enabled;
				tsbFilter.Enabled = enabled;
				tsbExcel.Enabled = enabled;
				tsbRefresh.Enabled = enabled;
			}
		}

		#endregion

		protected virtual void PopulateData()
		{
			try
			{
				if (Generator.Data != null)
				{
					InitGraphData();
					InitGridData();
				}
				RefreshStatusInfo();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void InitGraphData()
		{
			chartView.Visible = chartView.Visible && Generator.Data != null;
			if (Generator.Data != null)
			{
				chartView.Series.Clear();
				Series series = chartView.Series.Add("Default");
				series.Font = chartView.Legends["Default"].Font;
				chartView.Legends["Default"].Enabled = false;
				chartView.MouseDown -= chartView_MouseDown;
				chartView.MouseMove -= chartView_MouseMove;
				series.Points.DataBindXY(Generator.Data.DefaultView, Generator.TitleColumn, Generator.Data.DefaultView, Generator.GetValueColumn(SelectedType));
				if (SelectedType == ReportType.Pie)
				{
					CreatePie(series);
					chartView.MouseDown += chartView_MouseDown;
					chartView.MouseMove += chartView_MouseMove;
				}
				else if (SelectedType == ReportType.Bar)
				{
					CreateBar(series);
				}
			}
			chartView.Refresh();
		}

		private static void CreateBar(Series series)
		{
			series.ChartType = SeriesChartType.Bar;
		}

		private void CreatePie(Series series)
		{
			series.Label = "#AXISLABEL (#PERCENT{P2})";
			series.LegendText = "#AXISLABEL";
			series.ChartType = settingsFrm.PieType == PieType.Pie ? SeriesChartType.Pie : SeriesChartType.Doughnut;
			series["PieLabelStyle"] = settingsFrm.LabelType.ToString();
			series["PieDrawingStyle"] = settingsFrm.DrawningStyle.ToString();
			chartView.Legends["Default"].Enabled = settingsFrm.UseLegend;
			chartView.ChartAreas["Default"].Area3DStyle.Enable3D = settingsFrm.Is3D;
			if (settingsFrm.CollectedThreshold)
			{
				series["CollectedThreshold"] = settingsFrm.CollectedThresholdValue.ToString();
				series["CollectedThresholdUsePercent"] = "true";
				series["CollectedLabel"] = settingsFrm.CollectedLabel;
				series["CollectedLegendText"] = settingsFrm.CollectedLabel;
				series["CollectedToolTip"] = settingsFrm.CollectedLabel;
			}
			series.SmartLabelStyle.Enabled = true;
		}
        
		private void InitGridData()
		{
			grid.DataSource = SelectedType == ReportType.Grid ? Generator.Data.DefaultView : null;
			SetHighlightEnabled();
		}

		private void RefreshStatusInfo()
		{
			tsLabel.Text = (Generator.Data != null)
							? string.Format(Constants.ItemsCountTemplates.Default, Generator.Data.Rows.Count)
			               	: string.Empty;
		}

		private void tsbRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				Cursor = Cursors.WaitCursor;
				RefreshJournal();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		public event JournalForm.FilterClick OnFilterClick;

		private void tsbFilter_Click(object sender, EventArgs e)
		{
			try
			{
				bool filterReturn = OnFilterClick != null ? (OnFilterClick(this, Generator.Entity, null, Generator.FilterValues)) 
					: Globals.ShowFilter(this, Generator.Entity, Generator.FilterValues);

				if (filterReturn)
				{
					Cursor = Cursors.WaitCursor;
					RefreshJournal();
				}
			}
			catch (Exception ex)
			{
				Cursor = Cursors.Default;
				ErrorManager.PublishError(ex);
			}
		}

		private void tsbExcel_Click(object sender, EventArgs e)
		{
			if (SelectedType == ReportType.Grid)
			{
				try
				{
					Application.DoEvents();
					Cursor = Cursors.WaitCursor;

					ExportManager.ExportExcel(grid.InternalGrid, grid.Entity, grid.IsNeedHighlight);
				}
				catch (Exception ex)
				{
					ErrorManager.PublishError(ex);
				}
				finally
				{
					Cursor = Cursors.Default;
				}
			}
		}

		private void tscbType_SelectedIndexChanged(object sender, EventArgs e)
		{
			grid.DataSource = null;
			ReportType type = SelectedType;
            grid.Visible = (type == ReportType.Grid);
			chartView.Visible = (type != ReportType.Grid);
			tsbSum.Visible = (type == ReportType.Grid);
			tsbExcel.Visible = (type == ReportType.Grid);
			tsbSave.Visible = (type != ReportType.Grid);
			tsbChartSettings.Visible = (type != ReportType.Grid);
			PopulateData();
		}

		private void tsbSum_Click(object sender, EventArgs e)
		{
			if (SelectedType == ReportType.Grid)
			{
				try
				{
					Application.DoEvents();
					Cursor = Cursors.WaitCursor;

					grid.CalculateColumnSummary();
				}
				catch (Exception ex)
				{
					ErrorManager.PublishError(ex);
				}
				finally
				{
					Cursor = Cursors.Default;
				}
			}
		}

		private void GraphForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F5)
				RefreshJournal();
		}

		private void grid_ColumnSelected(Type columnType)
		{
			if (SelectedType == ReportType.Grid)
			{
				Globals.EnableSummaryButton(tsbSum, columnType);
			}
		}

		private void tsbSave_Click(object sender, EventArgs e)
		{
			if (SelectedType != ReportType.Grid)
			{
				SaveFileDialog dlg = new SaveFileDialog
				                     	{
				                     		Title = "Выберите файл для сохранения",
				                     		Filter = "Рисунок (*.png)|*.png",
				                     		FilterIndex = 1
				                     	};
				if (dlg.ShowDialog(this) == DialogResult.OK)
				{
					chartView.SaveImage(dlg.FileName, ImageFormat.Png);
				}
			}
		}

		private void tsbHighlight_Click(object sender, EventArgs e)
		{
			if (SelectedType == ReportType.Grid)
			{
				FormGridHighlight frm = new FormGridHighlight(grid);
				frm.ShowDialog(this);
			}
		}

		private void tsbChartSettings_Click(object sender, EventArgs e)
		{
			if (settingsFrm.ShowDialog(this) == DialogResult.OK)
				PopulateData();
		}

		#region Mouse Events

		private bool locked = false;

		/// <summary>
		/// Mouse Down Event
		/// </summary>
		private void chartView_MouseDown(object sender, MouseEventArgs e)
		{
			if (locked)
				return;

			locked = true;
				
			const string sExploded = "Exploded=true";
			HitTestResult result = chartView.HitTest(e.X, e.Y);

			if (result.ChartElementType == ChartElementType.DataPoint)
			{
				bool exploded = result.PointIndex >= 0
				                	? ((chartView.Series[0].Points[result.PointIndex].CustomProperties.Contains(sExploded))
				                	   	? true : false)
				                	: ((chartView.Series[0][propertyCollectedSliceExploded] == "true") ? true : false);

				if (exploded)
				{
					if (result.PointIndex >= 0)
					{
						DataPoint point = chartView.Series[0].Points[result.PointIndex];
						point.CustomProperties = point.CustomProperties.Replace(sExploded, string.Empty);
					}
					else
					{
						chartView.Series[0][propertyCollectedSliceExploded] = "false";
					}
				}
				else
				{
					if (result.PointIndex >= 0)
					{
						DataPoint point = chartView.Series[0].Points[result.PointIndex];
						point.CustomProperties = point.CustomProperties.Replace(sExploded, string.Empty) +
						                         (!string.IsNullOrEmpty(point.CustomProperties) ? ", " : string.Empty) + sExploded;
					}
					else
					{
						chartView.Series[0][propertyCollectedSliceExploded] = "true";
					}
				}
			}

			locked = false;
		}

		/// <summary>
		/// Mouse Move Event
		/// </summary>
		private void chartView_MouseMove(object sender, MouseEventArgs e)
		{
			HitTestResult result = chartView.HitTest(e.X, e.Y);

			foreach (DataPoint point in chartView.Series[0].Points)
			{
				point.BackSecondaryColor = Color.Black;
				point.BackHatchStyle = ChartHatchStyle.None;
				point.BorderWidth = 1;
			}

			if (result.ChartElementType == ChartElementType.DataPoint)
			{
				Cursor = Cursors.Hand;

				if (result.PointIndex != -1)
				{
					DataPoint point = chartView.Series[0].Points[result.PointIndex];
					point.BackSecondaryColor = Color.White;
					point.BackHatchStyle = ChartHatchStyle.Percent25;
					point.BorderWidth = 2;
				}
			}
			else
			{
				Cursor = Cursors.Default;
			}

		}

		#endregion
	}
}
