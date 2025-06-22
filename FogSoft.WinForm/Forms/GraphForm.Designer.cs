namespace FogSoft.WinForm.Forms
{
	partial class GraphForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataVisualization.Charting.ChartArea chartArea1 = new System.Windows.Forms.DataVisualization.Charting.ChartArea();
			System.Windows.Forms.DataVisualization.Charting.Legend legend1 = new System.Windows.Forms.DataVisualization.Charting.Legend();
			this.toolStrip = new System.Windows.Forms.ToolStrip();
			this.tslType = new System.Windows.Forms.ToolStripLabel();
			this.tscbType = new System.Windows.Forms.ToolStripComboBox();
			this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
			this.tsbFilter = new System.Windows.Forms.ToolStripButton();
			this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbExcel = new System.Windows.Forms.ToolStripButton();
			this.tsbSum = new System.Windows.Forms.ToolStripButton();
			this.tsbSave = new System.Windows.Forms.ToolStripButton();
			this.tsbHighlight = new System.Windows.Forms.ToolStripButton();
			this.tsbChartSettings = new System.Windows.Forms.ToolStripButton();
			this.statusStrip = new System.Windows.Forms.StatusStrip();
			this.tsLabel = new System.Windows.Forms.ToolStripStatusLabel();
			this.chartView = new System.Windows.Forms.DataVisualization.Charting.Chart();
			this.grid = new FogSoft.WinForm.Controls.SmartGrid();
			this.toolStrip.SuspendLayout();
			this.statusStrip.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.chartView)).BeginInit();
			this.SuspendLayout();
			// 
			// toolStrip
			// 
			this.toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tslType,
            this.tscbType,
            this.toolStripSeparator2,
            this.tsbRefresh,
            this.tsbFilter,
            this.toolStripSeparator1,
            this.tsbExcel,
            this.tsbSum,
            this.tsbSave,
            this.tsbHighlight,
            this.tsbChartSettings});
			this.toolStrip.Location = new System.Drawing.Point(0, 0);
			this.toolStrip.Name = "toolStrip";
			this.toolStrip.Size = new System.Drawing.Size(732, 25);
			this.toolStrip.TabIndex = 0;
			this.toolStrip.Text = "toolStrip1";
			// 
			// tslType
			// 
			this.tslType.Name = "tslType";
			this.tslType.Size = new System.Drawing.Size(69, 22);
			this.tslType.Text = "Вид отчета:";
			// 
			// tscbType
			// 
			this.tscbType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.tscbType.Name = "tscbType";
			this.tscbType.Size = new System.Drawing.Size(121, 25);
			this.tscbType.SelectedIndexChanged += new System.EventHandler(this.tscbType_SelectedIndexChanged);
			// 
			// toolStripSeparator2
			// 
			this.toolStripSeparator2.Name = "toolStripSeparator2";
			this.toolStripSeparator2.Size = new System.Drawing.Size(6, 25);
			// 
			// tsbRefresh
			// 
			this.tsbRefresh.Image = global::FogSoft.WinForm.Properties.Resources.Refresh;
			this.tsbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbRefresh.Name = "tsbRefresh";
			this.tsbRefresh.Size = new System.Drawing.Size(77, 22);
			this.tsbRefresh.Text = "Обновить";
			this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
			// 
			// tsbFilter
			// 
			this.tsbFilter.Image = global::FogSoft.WinForm.Properties.Resources.Filter;
			this.tsbFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbFilter.Name = "tsbFilter";
			this.tsbFilter.Size = new System.Drawing.Size(65, 22);
			this.tsbFilter.Text = "Фильтр";
			this.tsbFilter.Click += new System.EventHandler(this.tsbFilter_Click);
			// 
			// toolStripSeparator1
			// 
			this.toolStripSeparator1.Name = "toolStripSeparator1";
			this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
			// 
			// tsbExcel
			// 
			this.tsbExcel.Image = global::FogSoft.WinForm.Properties.Resources.ExportExcel;
			this.tsbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbExcel.Name = "tsbExcel";
			this.tsbExcel.Size = new System.Drawing.Size(69, 22);
			this.tsbExcel.Text = "Экспорт";
			this.tsbExcel.Click += new System.EventHandler(this.tsbExcel_Click);
			// 
			// tsbSum
			// 
			this.tsbSum.Enabled = false;
			this.tsbSum.Image = global::FogSoft.WinForm.Properties.Resources.SumColumn;
			this.tsbSum.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbSum.Name = "tsbSum";
			this.tsbSum.Size = new System.Drawing.Size(118, 22);
			this.tsbSum.Text = "Сумма по колонке";
			this.tsbSum.Click += new System.EventHandler(this.tsbSum_Click);
			// 
			// tsbSave
			// 
			this.tsbSave.Image = global::FogSoft.WinForm.Properties.Resources.floppydisk;
			this.tsbSave.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbSave.Name = "tsbSave";
			this.tsbSave.Size = new System.Drawing.Size(82, 22);
			this.tsbSave.Text = "Сохранить";
			this.tsbSave.Click += new System.EventHandler(this.tsbSave_Click);
			// 
			// tsbHighlight
			// 
			this.tsbHighlight.Image = global::FogSoft.WinForm.Properties.Resources.color;
			this.tsbHighlight.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbHighlight.Name = "tsbHighlight";
			this.tsbHighlight.Size = new System.Drawing.Size(82, 22);
			this.tsbHighlight.Text = "Подсветка";
			this.tsbHighlight.Click += new System.EventHandler(this.tsbHighlight_Click);
			// 
			// tsbChartSettings
			// 
			this.tsbChartSettings.Image = global::FogSoft.WinForm.Properties.Resources.attributes;
			this.tsbChartSettings.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbChartSettings.Name = "tsbChartSettings";
			this.tsbChartSettings.Size = new System.Drawing.Size(81, 20);
			this.tsbChartSettings.Text = "Настройки";
			this.tsbChartSettings.Click += new System.EventHandler(this.tsbChartSettings_Click);
			// 
			// statusStrip
			// 
			this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsLabel});
			this.statusStrip.Location = new System.Drawing.Point(0, 477);
			this.statusStrip.Name = "statusStrip";
			this.statusStrip.Size = new System.Drawing.Size(732, 22);
			this.statusStrip.TabIndex = 1;
			this.statusStrip.Text = "statusStrip1";
			// 
			// tsLabel
			// 
			this.tsLabel.Name = "tsLabel";
			this.tsLabel.Size = new System.Drawing.Size(0, 17);
			// 
			// chartView
			// 
			chartArea1.Name = "Default";
			this.chartView.ChartAreas.Add(chartArea1);
			this.chartView.Dock = System.Windows.Forms.DockStyle.Fill;
			legend1.AutoFitMinFontSize = 6;
			legend1.Font = new System.Drawing.Font("Tahoma", 8F);
			legend1.IsTextAutoFit = false;
			legend1.Name = "Default";
			this.chartView.Legends.Add(legend1);
			this.chartView.Location = new System.Drawing.Point(0, 25);
			this.chartView.Name = "chartView";
			this.chartView.Size = new System.Drawing.Size(732, 452);
			this.chartView.TabIndex = 4;
			this.chartView.Text = "chart1";
			// 
			// grid
			// 
			this.grid.Caption = "";
			this.grid.CaptionVisible = false;
			this.grid.CheckBoxes = false;
			this.grid.ColorHighlight = FogSoft.WinForm.Controls.ColorHighlight.Red;
			this.grid.ColumnNameHighlight = null;
			this.grid.DataSource = null;
			this.grid.DependantGrid = null;
			this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grid.Entity = null;
			this.grid.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grid.InterfaceObject = FogSoft.WinForm.InterfaceObjects.SimpleJournal;
			this.grid.IsHighlightInvertColor = false;
			this.grid.IsNeedHighlight = false;
			this.grid.Location = new System.Drawing.Point(0, 25);
			this.grid.MenuEnabled = true;
			this.grid.Name = "grid";
			this.grid.QuickSearchVisible = true;
			this.grid.SelectedObject = null;
			this.grid.Size = new System.Drawing.Size(732, 452);
			this.grid.TabIndex = 3;
			this.grid.Visible = false;
			this.grid.ColumnSelected += new FogSoft.WinForm.Controls.ColumnSelectedDelegate(this.grid_ColumnSelected);
			// 
			// GraphForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(732, 499);
			this.Controls.Add(this.chartView);
			this.Controls.Add(this.grid);
			this.Controls.Add(this.statusStrip);
			this.Controls.Add(this.toolStrip);
			this.KeyPreview = true;
			this.MinimumSize = new System.Drawing.Size(660, 515);
			this.Name = "GraphForm";
			this.Text = "График";
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.GraphForm_KeyDown);
			this.toolStrip.ResumeLayout(false);
			this.toolStrip.PerformLayout();
			this.statusStrip.ResumeLayout(false);
			this.statusStrip.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.chartView)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip;
		private System.Windows.Forms.StatusStrip statusStrip;
		private System.Windows.Forms.ToolStripButton tsbRefresh;
		private System.Windows.Forms.ToolStripButton tsbFilter;
		private System.Windows.Forms.ToolStripButton tsbExcel;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
		private System.Windows.Forms.ToolStripComboBox tscbType;
		private System.Windows.Forms.ToolStripLabel tslType;
		private FogSoft.WinForm.Controls.SmartGrid grid;
		private System.Windows.Forms.ToolStripStatusLabel tsLabel;
		private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
		private System.Windows.Forms.ToolStripButton tsbSum;
		private System.Windows.Forms.ToolStripButton tsbSave;
		private System.Windows.Forms.ToolStripButton tsbHighlight;
		private System.Windows.Forms.ToolStripButton tsbChartSettings;
		private System.Windows.Forms.DataVisualization.Charting.Chart chartView;
	}
}