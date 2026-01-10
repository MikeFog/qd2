namespace FogSoft.WinForm.Forms {
  partial class ExplorerForm {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if(disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
            this.tsJournal = new System.Windows.Forms.ToolStrip();
            this.tsbEdit = new System.Windows.Forms.ToolStripButton();
            this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsbDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbExcel = new System.Windows.Forms.ToolStripButton();
            this.tsbFilter = new System.Windows.Forms.ToolStripButton();
            this.tsbSumma = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tvwStructure = new FogSoft.WinForm.Controls.TreeView2();
            this.grid = new FogSoft.WinForm.Controls.SmartGrid();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.slTotal = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsJournal.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tsJournal
            // 
            this.tsJournal.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.tsJournal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbEdit,
            this.tsbRefresh,
            this.tsbDelete,
            this.toolStripSeparator1,
            this.tsbExcel,
            this.tsbFilter,
            this.tsbSumma});
            this.tsJournal.Location = new System.Drawing.Point(0, 0);
            this.tsJournal.Name = "tsJournal";
            this.tsJournal.Size = new System.Drawing.Size(742, 34);
            this.tsJournal.TabIndex = 1;
            this.tsJournal.Text = "toolStrip1";
            // 
            // tsbEdit
            // 
            this.tsbEdit.Image = global::FogSoft.WinForm.Properties.Resources.EditItem;
            this.tsbEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbEdit.Name = "tsbEdit";
            this.tsbEdit.Size = new System.Drawing.Size(161, 29);
            this.tsbEdit.Text = "Редактировать";
            this.tsbEdit.Click += new System.EventHandler(this.tsbEdit_Click);
            // 
            // tsbRefresh
            // 
            this.tsbRefresh.Image = global::FogSoft.WinForm.Properties.Resources.Refresh;
            this.tsbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRefresh.Name = "tsbRefresh";
            this.tsbRefresh.Size = new System.Drawing.Size(121, 29);
            this.tsbRefresh.Text = "Обновить";
            this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
            // 
            // tsbDelete
            // 
            this.tsbDelete.Image = global::FogSoft.WinForm.Properties.Resources.DeleteItem;
            this.tsbDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbDelete.Name = "tsbDelete";
            this.tsbDelete.Size = new System.Drawing.Size(104, 29);
            this.tsbDelete.Text = "Удалить";
            this.tsbDelete.Click += new System.EventHandler(this.tsbDelete_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 34);
            // 
            // tsbExcel
            // 
            this.tsbExcel.Image = global::FogSoft.WinForm.Properties.Resources.ExportExcel;
            this.tsbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbExcel.Name = "tsbExcel";
            this.tsbExcel.Size = new System.Drawing.Size(107, 29);
            this.tsbExcel.Text = "Экспорт";
            this.tsbExcel.Click += new System.EventHandler(this.tsbExcel_Click);
            // 
            // tsbFilter
            // 
            this.tsbFilter.Image = global::FogSoft.WinForm.Properties.Resources.Filter;
            this.tsbFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFilter.Name = "tsbFilter";
            this.tsbFilter.Size = new System.Drawing.Size(99, 29);
            this.tsbFilter.Text = "Фильтр";
            this.tsbFilter.ToolTipText = "Установить Фильтр";
            this.tsbFilter.Click += new System.EventHandler(this.tsbFilter_Click);
            // 
            // tsbSumma
            // 
            this.tsbSumma.Enabled = false;
            this.tsbSumma.Image = global::FogSoft.WinForm.Properties.Resources.SumColumn;
            this.tsbSumma.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSumma.Name = "tsbSumma";
            this.tsbSumma.Size = new System.Drawing.Size(194, 29);
            this.tsbSumma.Text = "Сумма по колонке";
            this.tsbSumma.Click += new System.EventHandler(this.tsbSumma_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 34);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tvwStructure);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.grid);
            this.splitContainer1.Size = new System.Drawing.Size(742, 463);
            this.splitContainer1.SplitterDistance = 262;
            this.splitContainer1.TabIndex = 2;
            this.splitContainer1.TabStop = false;
            // 
            // tvwStructure
            // 
            this.tvwStructure.CheckBoxes = false;
            this.tvwStructure.DependantGrid = null;
            this.tvwStructure.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvwStructure.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tvwStructure.Location = new System.Drawing.Point(0, 0);
            this.tvwStructure.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tvwStructure.Name = "tvwStructure";
            this.tvwStructure.SelectedItemsBitColumn = null;
            this.tvwStructure.SelectedItemsImageColumn = null;
            this.tvwStructure.ShowExpandButton = false;
            this.tvwStructure.Size = new System.Drawing.Size(262, 463);
            this.tvwStructure.TabIndex = 0;
            this.tvwStructure.ContainerSelected += new FogSoft.WinForm.ContainerDelegate(this.tvwStructure_ContainerSelected);
            // 
            // grid
            // 
            this.grid.Caption = "";
            this.grid.CaptionVisible = true;
            this.grid.CheckBoxes = false;
            this.grid.ColumnNameHighlight = null;
            this.grid.DataSource = null;
            this.grid.DependantGrid = null;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Entity = null;
            this.grid.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grid.IsHighlightInvertColor = false;
            this.grid.IsNeedHighlight = false;
            this.grid.Location = new System.Drawing.Point(0, 0);
            this.grid.Margin = new System.Windows.Forms.Padding(4);
            this.grid.MenuEnabled = true;
            this.grid.Name = "grid";
            this.grid.QuickSearchVisible = true;
            this.grid.SelectedObject = null;
            this.grid.ShowMultiselectColumn = true;
            this.grid.Size = new System.Drawing.Size(476, 463);
            this.grid.TabIndex = 0;
            this.grid.ObjectDeleted += new FogSoft.WinForm.ObjectDelegate(this.grid_ObjectCreatedOrDeleted);
            this.grid.ObjectCreated += new FogSoft.WinForm.ObjectDelegate(this.grid_ObjectCreatedOrDeleted);
            this.grid.ColumnSelected += new FogSoft.WinForm.Controls.ColumnSelectedDelegate(this.grid_ColumnSelected);
            this.grid.Enter += new System.EventHandler(this.grid_Enter);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.slTotal});
            this.statusStrip1.Location = new System.Drawing.Point(0, 497);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            this.statusStrip1.Size = new System.Drawing.Size(742, 22);
            this.statusStrip1.TabIndex = 3;
            this.statusStrip1.Text = "statusBar";
            // 
            // slTotal
            // 
            this.slTotal.Name = "slTotal";
            this.slTotal.Size = new System.Drawing.Size(0, 15);
            // 
            // ExplorerForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(742, 519);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.tsJournal);
            this.Controls.Add(this.statusStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MinimumSize = new System.Drawing.Size(331, 227);
            this.Name = "ExplorerForm";
            this.ShowInTaskbar = false;
            this.Text = "ExplorerForm";
            this.tsJournal.ResumeLayout(false);
            this.tsJournal.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ToolStrip tsJournal;
    private System.Windows.Forms.ToolStripButton tsbEdit;
    private System.Windows.Forms.ToolStripButton tsbDelete;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripButton tsbRefresh;
    private System.Windows.Forms.ToolStripButton tsbExcel;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private FogSoft.WinForm.Controls.TreeView2 tvwStructure;
	private FogSoft.WinForm.Controls.SmartGrid grid;
    private System.Windows.Forms.StatusStrip statusStrip1;
    private System.Windows.Forms.ToolStripStatusLabel slTotal;
	  private System.Windows.Forms.ToolStripButton tsbSumma;
		private System.Windows.Forms.ToolStripButton tsbFilter;
  }
}