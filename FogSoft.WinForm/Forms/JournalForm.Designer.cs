namespace FogSoft.WinForm.Forms {
  partial class JournalForm {
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
            this.tsbHighlight = new System.Windows.Forms.ToolStripButton();
            this.sbJournal = new System.Windows.Forms.StatusStrip();
            this.slTotal = new System.Windows.Forms.ToolStripStatusLabel();
            this.grid = new FogSoft.WinForm.Controls.SmartGrid();
            this.tsJournal.SuspendLayout();
            this.sbJournal.SuspendLayout();
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
            this.tsbSumma,
            this.tsbHighlight});
            this.tsJournal.Location = new System.Drawing.Point(0, 0);
            this.tsJournal.Name = "tsJournal";
            this.tsJournal.Size = new System.Drawing.Size(783, 38);
            this.tsJournal.TabIndex = 0;
            this.tsJournal.Text = "toolStrip1";
            // 
            // tsbNew
            // 
            this.tsbNew.Image = global::FogSoft.WinForm.Properties.Resources.NewItem;
            this.tsbNew.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbNew.Name = "tsbNew";
            this.tsbNew.Size = new System.Drawing.Size(97, 33);
            this.tsbNew.Text = "Новый";
            this.tsbNew.Click += new System.EventHandler(this.tsbNew_Click);
            // 
            // tsbEdit
            // 
            this.tsbEdit.Image = global::FogSoft.WinForm.Properties.Resources.EditItem;
            this.tsbEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbEdit.Name = "tsbEdit";
            this.tsbEdit.Size = new System.Drawing.Size(161, 33);
            this.tsbEdit.Text = "Редактировать";
            this.tsbEdit.Click += new System.EventHandler(this.tsbEdit_Click);
            // 
            // tsbDelete
            // 
            this.tsbDelete.Image = global::FogSoft.WinForm.Properties.Resources.DeleteItem;
            this.tsbDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbDelete.Name = "tsbDelete";
            this.tsbDelete.Size = new System.Drawing.Size(104, 33);
            this.tsbDelete.Text = "Удалить";
            this.tsbDelete.Click += new System.EventHandler(this.tsbDelete_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 38);
            // 
            // tsbRefresh
            // 
            this.tsbRefresh.Image = global::FogSoft.WinForm.Properties.Resources.Refresh;
            this.tsbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRefresh.Name = "tsbRefresh";
            this.tsbRefresh.Size = new System.Drawing.Size(121, 33);
            this.tsbRefresh.Text = "Обновить";
            this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
            // 
            // tsbFilter
            // 
            this.tsbFilter.Image = global::FogSoft.WinForm.Properties.Resources.Filter;
            this.tsbFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbFilter.Name = "tsbFilter";
            this.tsbFilter.Size = new System.Drawing.Size(99, 33);
            this.tsbFilter.Text = "Фильтр";
            this.tsbFilter.Click += new System.EventHandler(this.tsbFilter_Click);
            // 
            // tsbExcel
            // 
            this.tsbExcel.Image = global::FogSoft.WinForm.Properties.Resources.ExportExcel;
            this.tsbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbExcel.Name = "tsbExcel";
            this.tsbExcel.Size = new System.Drawing.Size(107, 33);
            this.tsbExcel.Text = "Экспорт";
            this.tsbExcel.Click += new System.EventHandler(this.tsbExcel_Click);
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
            // tsbHighlight
            // 
            this.tsbHighlight.Image = global::FogSoft.WinForm.Properties.Resources.color;
            this.tsbHighlight.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbHighlight.Name = "tsbHighlight";
            this.tsbHighlight.Size = new System.Drawing.Size(126, 29);
            this.tsbHighlight.Text = "Подсветка";
            this.tsbHighlight.Click += new System.EventHandler(this.tsbHighlight_Click);
            // 
            // sbJournal
            // 
            this.sbJournal.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.sbJournal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.slTotal});
            this.sbJournal.Location = new System.Drawing.Point(0, 413);
            this.sbJournal.Name = "sbJournal";
            this.sbJournal.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            this.sbJournal.Size = new System.Drawing.Size(783, 22);
            this.sbJournal.TabIndex = 1;
            this.sbJournal.Text = "statusStrip1";
            // 
            // slTotal
            // 
            this.slTotal.Name = "slTotal";
            this.slTotal.Size = new System.Drawing.Size(0, 15);
            // 
            // grid
            // 
            this.grid.Caption = "";
            this.grid.CaptionVisible = false;
            this.grid.CheckBoxes = false;
            this.grid.ColumnNameHighlight = null;
            this.grid.DataSource = null;
            this.grid.DependantGrid = null;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Entity = null;
            this.grid.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grid.IsHighlightInvertColor = false;
            this.grid.IsNeedHighlight = false;
            this.grid.Location = new System.Drawing.Point(0, 38);
            this.grid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grid.MenuEnabled = true;
            this.grid.Name = "grid";
            this.grid.QuickSearchVisible = true;
            this.grid.SelectedObject = null;
            this.grid.ShowMultiselectColumn = true;
            this.grid.Size = new System.Drawing.Size(783, 375);
            this.grid.TabIndex = 2;
            this.grid.ObjectDeleted += new FogSoft.WinForm.ObjectDelegate(this.grid_ObjectDeleted);
            this.grid.ColumnSelected += new FogSoft.WinForm.Controls.ColumnSelectedDelegate(this.grid_ColumnSelected);
            // 
            // JournalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(783, 435);
            this.Controls.Add(this.grid);
            this.Controls.Add(this.tsJournal);
            this.Controls.Add(this.sbJournal);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MinimumSize = new System.Drawing.Size(347, 225);
            this.Name = "JournalForm";
            this.Text = "JournalTemplateForm";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.JournalForm_KeyDown);
            this.tsJournal.ResumeLayout(false);
            this.tsJournal.PerformLayout();
            this.sbJournal.ResumeLayout(false);
            this.sbJournal.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ToolStrip tsJournal;
    private System.Windows.Forms.StatusStrip sbJournal;
    private System.Windows.Forms.ToolStripButton tsbNew;
    private System.Windows.Forms.ToolStripButton tsbEdit;
    private System.Windows.Forms.ToolStripButton tsbDelete;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripButton tsbRefresh;
    private System.Windows.Forms.ToolStripButton tsbFilter;
    private System.Windows.Forms.ToolStripButton tsbExcel;
	private System.Windows.Forms.ToolStripButton tsbSumma;
    private System.Windows.Forms.ToolStripStatusLabel slTotal;
    private FogSoft.WinForm.Controls.SmartGrid grid;
	private System.Windows.Forms.ToolStripButton tsbHighlight;
  }
}