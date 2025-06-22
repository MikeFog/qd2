namespace FogSoft.WinForm.Forms {
  partial class MasterDetailForm {
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
            this.tsbNew = new System.Windows.Forms.ToolStripButton();
            this.tsbEdit = new System.Windows.Forms.ToolStripButton();
            this.tsbDelete = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsbFilter = new System.Windows.Forms.ToolStripButton();
            this.tsbExcel = new System.Windows.Forms.ToolStripButton();
            this.tsbSumma = new System.Windows.Forms.ToolStripButton();
            this.sbJournal = new System.Windows.Forms.StatusStrip();
            this.slTotal = new System.Windows.Forms.ToolStripStatusLabel();
            this.slMasterTotal = new System.Windows.Forms.ToolStripStatusLabel();
            this.slDetailTotal = new System.Windows.Forms.ToolStripStatusLabel();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.grdMaster = new FogSoft.WinForm.Controls.SmartGrid();
            this.grdDetails = new FogSoft.WinForm.Controls.SmartGrid();
            this.tsJournal.SuspendLayout();
            this.sbJournal.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tsJournal
            // 
            this.tsJournal.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.tsJournal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbNew,
            this.tsbEdit,
            this.tsbDelete,
            this.toolStripSeparator1,
            this.tsbRefresh,
            this.tsbFilter,
            this.tsbExcel,
            this.tsbSumma});
            this.tsJournal.Location = new System.Drawing.Point(0, 0);
            this.tsJournal.Name = "tsJournal";
            this.tsJournal.Padding = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.tsJournal.Size = new System.Drawing.Size(1220, 34);
            this.tsJournal.TabIndex = 2;
            this.tsJournal.Text = "toolStrip1";
            // 
            // tsbNew
            // 
            this.tsbNew.Image = global::FogSoft.WinForm.Properties.Resources.NewItem;
            this.tsbNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbNew.Name = "tsbNew";
            this.tsbNew.Size = new System.Drawing.Size(97, 29);
            this.tsbNew.Text = "Новый";
            this.tsbNew.Click += new System.EventHandler(this.tsbNew_Click);
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
            // tsbRefresh
            // 
            this.tsbRefresh.Image = global::FogSoft.WinForm.Properties.Resources.Refresh;
            this.tsbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRefresh.Name = "tsbRefresh";
            this.tsbRefresh.Size = new System.Drawing.Size(121, 29);
            this.tsbRefresh.Text = "Обновить";
            this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
            // 
            // tsbFilter
            // 
            this.tsbFilter.Image = global::FogSoft.WinForm.Properties.Resources.Filter;
            this.tsbFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFilter.Name = "tsbFilter";
            this.tsbFilter.Size = new System.Drawing.Size(99, 29);
            this.tsbFilter.Text = "Фильтр";
            this.tsbFilter.Click += new System.EventHandler(this.tsbFilter_Click);
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
            // tsbSumma
            // 
            this.tsbSumma.Enabled = false;
            this.tsbSumma.Image = global::FogSoft.WinForm.Properties.Resources.ExportExcel;
            this.tsbSumma.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSumma.Name = "tsbSumma";
            this.tsbSumma.Size = new System.Drawing.Size(194, 29);
            this.tsbSumma.Text = "Сумма по колонке";
            this.tsbSumma.Click += new System.EventHandler(this.tsbSumma_Click);
            // 
            // sbJournal
            // 
            this.sbJournal.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.sbJournal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.slTotal,
            this.slMasterTotal,
            this.slDetailTotal});
            this.sbJournal.Location = new System.Drawing.Point(0, 897);
            this.sbJournal.Name = "sbJournal";
            this.sbJournal.Padding = new System.Windows.Forms.Padding(2, 0, 23, 0);
            this.sbJournal.Size = new System.Drawing.Size(1220, 22);
            this.sbJournal.TabIndex = 3;
            this.sbJournal.Text = "statusStrip1";
            // 
            // slTotal
            // 
            this.slTotal.Name = "slTotal";
            this.slTotal.Size = new System.Drawing.Size(0, 15);
            // 
            // slMasterTotal
            // 
            this.slMasterTotal.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
            this.slMasterTotal.Name = "slMasterTotal";
            this.slMasterTotal.Size = new System.Drawing.Size(4, 15);
            // 
            // slDetailTotal
            // 
            this.slDetailTotal.Name = "slDetailTotal";
            this.slDetailTotal.Size = new System.Drawing.Size(0, 15);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 34);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.grdMaster);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.grdDetails);
            this.splitContainer1.Size = new System.Drawing.Size(1220, 863);
            this.splitContainer1.SplitterDistance = 486;
            this.splitContainer1.SplitterWidth = 8;
            this.splitContainer1.TabIndex = 4;
            // 
            // grdMaster
            // 
            this.grdMaster.Caption = "";
            this.grdMaster.CaptionVisible = false;
            this.grdMaster.CheckBoxes = false;
            this.grdMaster.ColumnNameHighlight = null;
            this.grdMaster.DataSource = null;
            this.grdMaster.DependantGrid = null;
            this.grdMaster.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdMaster.Entity = null;
            this.grdMaster.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdMaster.IsHighlightInvertColor = false;
            this.grdMaster.IsNeedHighlight = false;
            this.grdMaster.Location = new System.Drawing.Point(0, 0);
            this.grdMaster.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.grdMaster.MenuEnabled = true;
            this.grdMaster.Name = "grdMaster";
            this.grdMaster.QuickSearchVisible = true;
            this.grdMaster.SelectedObject = null;
            this.grdMaster.ShowMultiselectColumn = true;
            this.grdMaster.Size = new System.Drawing.Size(1220, 486);
            this.grdMaster.TabIndex = 0;
            this.grdMaster.ObjectDeleted += new FogSoft.WinForm.ObjectDelegate(this.GridsItemCountChanged);
            this.grdMaster.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.OnMasterObjectSelected);
            this.grdMaster.ObjectCreated += new FogSoft.WinForm.ObjectDelegate(this.GridsItemCountChanged);
            this.grdMaster.ColumnSelected += new FogSoft.WinForm.Controls.ColumnSelectedDelegate(this.grdMaster_ColumnSelected);
            this.grdMaster.Enter += new System.EventHandler(this.grdMasterOrDetails_Enter);
            // 
            // grdDetails
            // 
            this.grdDetails.Caption = "";
            this.grdDetails.CaptionVisible = true;
            this.grdDetails.CheckBoxes = false;
            this.grdDetails.ColumnNameHighlight = null;
            this.grdDetails.DataSource = null;
            this.grdDetails.DependantGrid = null;
            this.grdDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdDetails.Entity = null;
            this.grdDetails.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdDetails.IsHighlightInvertColor = false;
            this.grdDetails.IsNeedHighlight = false;
            this.grdDetails.Location = new System.Drawing.Point(0, 0);
            this.grdDetails.Margin = new System.Windows.Forms.Padding(7, 6, 7, 6);
            this.grdDetails.MenuEnabled = true;
            this.grdDetails.Name = "grdDetails";
            this.grdDetails.QuickSearchVisible = false;
            this.grdDetails.SelectedObject = null;
            this.grdDetails.ShowMultiselectColumn = true;
            this.grdDetails.Size = new System.Drawing.Size(1220, 369);
            this.grdDetails.TabIndex = 0;
            this.grdDetails.ObjectDeleted += new FogSoft.WinForm.ObjectDelegate(this.GridsItemCountChanged);
            this.grdDetails.ObjectCreated += new FogSoft.WinForm.ObjectDelegate(this.GridsItemCountChanged);
            this.grdDetails.Enter += new System.EventHandler(this.grdMasterOrDetails_Enter);
            // 
            // MasterDetailForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1220, 919);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.tsJournal);
            this.Controls.Add(this.sbJournal);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.Name = "MasterDetailForm";
            this.Text = "MasterDetailForm";
            this.tsJournal.ResumeLayout(false);
            this.tsJournal.PerformLayout();
            this.sbJournal.ResumeLayout(false);
            this.sbJournal.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

	private System.Windows.Forms.ToolStrip tsJournal;
    private System.Windows.Forms.ToolStripButton tsbNew;
    private System.Windows.Forms.ToolStripButton tsbEdit;
    private System.Windows.Forms.ToolStripButton tsbDelete;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripButton tsbRefresh;
    private System.Windows.Forms.ToolStripButton tsbFilter;
    private System.Windows.Forms.ToolStripButton tsbExcel;
    private System.Windows.Forms.ToolStripButton tsbSumma;
    private System.Windows.Forms.StatusStrip sbJournal;
    private System.Windows.Forms.ToolStripStatusLabel slTotal;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private FogSoft.WinForm.Controls.SmartGrid grdMaster;
    private FogSoft.WinForm.Controls.SmartGrid grdDetails;
    private System.Windows.Forms.ToolStripStatusLabel slMasterTotal;
    private System.Windows.Forms.ToolStripStatusLabel slDetailTotal;
  }
}