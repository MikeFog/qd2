namespace Merlin.Forms
{
  partial class TariffWindowGenerationForm
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
      if(disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
		this.tsJournal = new System.Windows.Forms.ToolStrip();
		this.tsbEdit = new System.Windows.Forms.ToolStripButton();
		this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
		this.tsbDelete = new System.Windows.Forms.ToolStripButton();
		this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
		this.tsbExcel = new System.Windows.Forms.ToolStripButton();
		this.tsJump2Date = new System.Windows.Forms.ToolStripButton();
		this.splitContainer1 = new System.Windows.Forms.SplitContainer();
		this.tvwStructure = new FogSoft.WinForm.Controls.TreeView2();
		this.tsbFilter = new System.Windows.Forms.ToolStripButton();
		this.windowGrid = new Merlin.Controls.TariffWindowGrid();
		this.tsJournal.SuspendLayout();
		this.splitContainer1.Panel1.SuspendLayout();
		this.splitContainer1.Panel2.SuspendLayout();
		this.splitContainer1.SuspendLayout();
		this.SuspendLayout();
		// 
		// tsJournal
		// 
		this.tsJournal.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbEdit,
            this.tsbRefresh,
            this.tsbDelete,
            this.toolStripSeparator1,
            this.tsbExcel,
            this.tsbFilter,
            this.tsJump2Date});
		this.tsJournal.Location = new System.Drawing.Point(0, 0);
		this.tsJournal.Name = "tsJournal";
		this.tsJournal.Size = new System.Drawing.Size(716, 25);
		this.tsJournal.TabIndex = 2;
		this.tsJournal.Text = "toolStrip1";
		// 
		// tsbEdit
		// 
		this.tsbEdit.ImageTransparentColor = System.Drawing.Color.Magenta;
		this.tsbEdit.Name = "tsbEdit";
		this.tsbEdit.Size = new System.Drawing.Size(91, 22);
		this.tsbEdit.Text = "Редактировать";
		this.tsbEdit.Click += new System.EventHandler(this.tsbEdit_Click);
		// 
		// tsbRefresh
		// 
		this.tsbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
		this.tsbRefresh.Name = "tsbRefresh";
		this.tsbRefresh.Size = new System.Drawing.Size(65, 22);
		this.tsbRefresh.Text = "Обновить";
		this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
		// 
		// tsbDelete
		// 
		this.tsbDelete.ImageTransparentColor = System.Drawing.Color.Magenta;
		this.tsbDelete.Name = "tsbDelete";
		this.tsbDelete.Size = new System.Drawing.Size(55, 22);
		this.tsbDelete.Text = "Удалить";
		this.tsbDelete.Click += new System.EventHandler(this.tsbDelete_Click);
		// 
		// toolStripSeparator1
		// 
		this.toolStripSeparator1.Name = "toolStripSeparator1";
		this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
		// 
		// tsbExcel
		// 
		this.tsbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
		this.tsbExcel.Name = "tsbExcel";
		this.tsbExcel.Size = new System.Drawing.Size(56, 22);
		this.tsbExcel.Text = "Экспорт";
		this.tsbExcel.Click += new System.EventHandler(this.tsbExcel_Click);
		// 
		// tsJump2Date
		// 
		this.tsJump2Date.ImageTransparentColor = System.Drawing.Color.Magenta;
		this.tsJump2Date.Name = "tsJump2Date";
		this.tsJump2Date.Size = new System.Drawing.Size(92, 22);
		this.tsJump2Date.Text = "Переход к дате";
		this.tsJump2Date.ToolTipText = "Переход к выбранной дате";
		this.tsJump2Date.Click += new System.EventHandler(this.tsJump2Date_Click);
		// 
		// splitContainer1
		// 
		this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
		this.splitContainer1.Location = new System.Drawing.Point(0, 25);
		this.splitContainer1.Name = "splitContainer1";
		// 
		// splitContainer1.Panel1
		// 
		this.splitContainer1.Panel1.Controls.Add(this.tvwStructure);
		// 
		// splitContainer1.Panel2
		// 
		this.splitContainer1.Panel2.Controls.Add(this.windowGrid);
		this.splitContainer1.Size = new System.Drawing.Size(716, 496);
		this.splitContainer1.SplitterDistance = 252;
		this.splitContainer1.TabIndex = 3;
		this.splitContainer1.TabStop = false;
		// 
		// tvwStructure
		// 
		this.tvwStructure.CheckBoxes = false;
		this.tvwStructure.DependantGrid = null;
		this.tvwStructure.Dock = System.Windows.Forms.DockStyle.Fill;
		this.tvwStructure.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
		this.tvwStructure.Location = new System.Drawing.Point(0, 0);
		this.tvwStructure.Name = "tvwStructure";
		this.tvwStructure.SelectedItemsBitColumn = null;
		this.tvwStructure.SelectedItemsImageColumn = null;
		this.tvwStructure.Size = new System.Drawing.Size(252, 496);
		this.tvwStructure.TabIndex = 0;
		this.tvwStructure.ContainerSelected += new FogSoft.WinForm.ContainerDelegate(this.tvwStructure_ContainerSelected);
		// 
		// tsbFilter
		// 
		this.tsbFilter.ImageTransparentColor = System.Drawing.Color.Magenta;
		this.tsbFilter.Name = "tsbFilter";
		this.tsbFilter.Size = new System.Drawing.Size(52, 22);
		this.tsbFilter.Text = "Фильтр";
		this.tsbFilter.ToolTipText = "Установить Фильтр";
		this.tsbFilter.Click += new System.EventHandler(this.tsbFilter_Click);
		// 
		// windowGrid
		// 
		this.windowGrid.Campaign = null;
		this.windowGrid.CurrentDate = new System.DateTime(2006, 8, 29, 0, 0, 0, 0);
		this.windowGrid.Cursor = System.Windows.Forms.Cursors.Default;
		this.windowGrid.Dock = System.Windows.Forms.DockStyle.Fill;
		this.windowGrid.EditMode = Merlin.Controls.EditMode.View;
		this.windowGrid.ExcludeSpecialTariffs = false;
		this.windowGrid.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
		this.windowGrid.Location = new System.Drawing.Point(0, 0);
		this.windowGrid.Massmedia = null;
		this.windowGrid.Module = null;
		this.windowGrid.Name = "windowGrid";
		this.windowGrid.Pricelist = null;
		this.windowGrid.RollerPosition = Merlin.RollerPositions.Undefined;
		this.windowGrid.ShowUnconfirmed = true;
		this.windowGrid.Size = new System.Drawing.Size(460, 496);
		this.windowGrid.TabIndex = 0;
		// 
		// TariffWindowGenerationForm
		// 
		this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.ClientSize = new System.Drawing.Size(716, 521);
		this.Controls.Add(this.splitContainer1);
		this.Controls.Add(this.tsJournal);
		this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
		this.Name = "TariffWindowGenerationForm";
		this.ShowInTaskbar = false;
		this.Text = "Генерация рекламных окон";
		this.tsJournal.ResumeLayout(false);
		this.tsJournal.PerformLayout();
		this.splitContainer1.Panel1.ResumeLayout(false);
		this.splitContainer1.Panel2.ResumeLayout(false);
		this.splitContainer1.ResumeLayout(false);
		this.ResumeLayout(false);
		this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ToolStrip tsJournal;
    private System.Windows.Forms.ToolStripButton tsbEdit;
    private System.Windows.Forms.ToolStripButton tsbRefresh;
    private System.Windows.Forms.ToolStripButton tsbDelete;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripButton tsbExcel;
		private FogSoft.WinForm.Controls.TreeView2 tvwStructure;
    private System.Windows.Forms.SplitContainer splitContainer1;
	private Controls.TariffWindowGrid windowGrid;
	private System.Windows.Forms.ToolStripButton tsJump2Date;
	private System.Windows.Forms.ToolStripButton tsbFilter;
  }
}