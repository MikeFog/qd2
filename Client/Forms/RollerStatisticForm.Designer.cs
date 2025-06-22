namespace Merlin.Forms {
  partial class RollerStatisticForm {
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
            this.tlbRollerStatistic = new System.Windows.Forms.ToolStrip();
            this.tsbRefresh = new System.Windows.Forms.ToolStripButton();
            this.tsbExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tbbPlay = new System.Windows.Forms.ToolStripButton();
            this.tsbStop = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbSplitByManager = new System.Windows.Forms.ToolStripButton();
            this.tsbSplitByDays = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.lbl = new System.Windows.Forms.Label();
            this.grdMassmedia = new FogSoft.WinForm.Controls.SmartGrid();
            this.cmbRadioStationGroup = new FogSoft.WinForm.LookUp();
            this.grdActions = new FogSoft.WinForm.Controls.SmartGrid();
            this.grid = new FogSoft.WinForm.Controls.SmartGrid();
            this.opAdvertType = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.checkBoxShowWhite = new System.Windows.Forms.CheckBox();
            this.checkBoxShowBlack = new System.Windows.Forms.CheckBox();
            this.objectPickerFirm = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.label2 = new System.Windows.Forms.Label();
            this.objectPickerUser = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.label1 = new System.Windows.Forms.Label();
            this.dtFinish = new System.Windows.Forms.DateTimePicker();
            this.dtStart = new System.Windows.Forms.DateTimePicker();
            this.label3 = new System.Windows.Forms.Label();
            this.tlbRollerStatistic.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tlbRollerStatistic
            // 
            this.tlbRollerStatistic.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.tlbRollerStatistic.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsbRefresh,
            this.tsbExcel,
            this.toolStripSeparator3,
            this.tbbPlay,
            this.tsbStop,
            this.toolStripSeparator1,
            this.tsbSplitByManager,
            this.tsbSplitByDays});
            this.tlbRollerStatistic.Location = new System.Drawing.Point(0, 0);
            this.tlbRollerStatistic.Name = "tlbRollerStatistic";
            this.tlbRollerStatistic.Size = new System.Drawing.Size(1419, 25);
            this.tlbRollerStatistic.TabIndex = 0;
            // 
            // tsbRefresh
            // 
            this.tsbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbRefresh.Name = "tsbRefresh";
            this.tsbRefresh.Size = new System.Drawing.Size(23, 22);
            this.tsbRefresh.Text = "toolStripButton1";
            this.tsbRefresh.ToolTipText = "Обновить информацию";
            this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
            // 
            // tsbExcel
            // 
            this.tsbExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbExcel.Name = "tsbExcel";
            this.tsbExcel.Size = new System.Drawing.Size(23, 22);
            this.tsbExcel.Text = "Экспорт таблицы";
            this.tsbExcel.Click += new System.EventHandler(this.tsbExcel_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 25);
            // 
            // tbbPlay
            // 
            this.tbbPlay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbbPlay.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbPlay.Name = "tbbPlay";
            this.tbbPlay.Size = new System.Drawing.Size(23, 22);
            this.tbbPlay.Text = "Прослушать ролик";
            this.tbbPlay.Click += new System.EventHandler(this.tbbPlay_Click);
            // 
            // tsbStop
            // 
            this.tsbStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbStop.Enabled = false;
            this.tsbStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbStop.Name = "tsbStop";
            this.tsbStop.Size = new System.Drawing.Size(23, 22);
            this.tsbStop.Text = "Остановить прослушивание";
            this.tsbStop.ToolTipText = "Остановить прослушивание";
            this.tsbStop.Click += new System.EventHandler(this.tsbStop_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 25);
            // 
            // tsbSplitByManager
            // 
            this.tsbSplitByManager.CheckOnClick = true;
            this.tsbSplitByManager.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSplitByManager.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSplitByManager.Name = "tsbSplitByManager";
            this.tsbSplitByManager.Size = new System.Drawing.Size(23, 22);
            this.tsbSplitByManager.Text = "С разбивкой по менеджерам";
            this.tsbSplitByManager.Click += new System.EventHandler(this.tsbSetting_Click);
            // 
            // tsbSplitByDays
            // 
            this.tsbSplitByDays.CheckOnClick = true;
            this.tsbSplitByDays.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSplitByDays.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSplitByDays.Name = "tsbSplitByDays";
            this.tsbSplitByDays.Size = new System.Drawing.Size(23, 22);
            this.tsbSplitByDays.Text = "С разбивкой по дням";
            this.tsbSplitByDays.Click += new System.EventHandler(this.tsbSetting_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 25);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.grid);
            this.splitContainer1.Size = new System.Drawing.Size(1419, 1023);
            this.splitContainer1.SplitterDistance = 495;
            this.splitContainer1.SplitterWidth = 5;
            this.splitContainer1.TabIndex = 1;
            this.splitContainer1.Text = "splitContainer1";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.grdMassmedia);
            this.splitContainer2.Panel1.Controls.Add(this.cmbRadioStationGroup);
            this.splitContainer2.Panel1.Controls.Add(this.lbl);
            this.splitContainer2.Panel1.Controls.Add(this.checkBoxShowWhite);
            this.splitContainer2.Panel1.Controls.Add(this.checkBoxShowBlack);
            this.splitContainer2.Panel1.Controls.Add(this.opAdvertType);
            this.splitContainer2.Panel1.Controls.Add(this.label3);
            this.splitContainer2.Panel1.Controls.Add(this.objectPickerFirm);
            this.splitContainer2.Panel1.Controls.Add(this.label2);
            this.splitContainer2.Panel1.Controls.Add(this.objectPickerUser);
            this.splitContainer2.Panel1.Controls.Add(this.label1);
            this.splitContainer2.Panel1.Controls.Add(this.dtFinish);
            this.splitContainer2.Panel1.Controls.Add(this.dtStart);
            this.splitContainer2.Panel1.RightToLeft = System.Windows.Forms.RightToLeft.No;
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.grdActions);
            this.splitContainer2.Panel2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.splitContainer2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.splitContainer2.Size = new System.Drawing.Size(495, 1023);
            this.splitContainer2.SplitterDistance = 630;
            this.splitContainer2.SplitterWidth = 5;
            this.splitContainer2.TabIndex = 50;
            this.splitContainer2.Text = "splitContainer2";
            // 
            // lbl
            // 
            this.lbl.AutoSize = true;
            this.lbl.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl.Location = new System.Drawing.Point(0, 197);
            this.lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl.Name = "lbl";
            this.lbl.Size = new System.Drawing.Size(49, 15);
            this.lbl.TabIndex = 56;
            this.lbl.Text = "Группа:";
            // 
            // grdMassmedia
            // 
            this.grdMassmedia.Caption = "Радиостанции";
            this.grdMassmedia.CaptionVisible = true;
            this.grdMassmedia.CheckBoxes = true;
            this.grdMassmedia.ColumnNameHighlight = null;
            this.grdMassmedia.DataSource = null;
            this.grdMassmedia.DependantGrid = null;
            this.grdMassmedia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdMassmedia.Entity = null;
            this.grdMassmedia.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdMassmedia.InterfaceObject = FogSoft.WinForm.InterfaceObjects.FakeModule;
            this.grdMassmedia.IsHighlightInvertColor = false;
            this.grdMassmedia.IsNeedHighlight = false;
            this.grdMassmedia.Location = new System.Drawing.Point(0, 235);
            this.grdMassmedia.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.grdMassmedia.MenuEnabled = false;
            this.grdMassmedia.Name = "grdMassmedia";
            this.grdMassmedia.QuickSearchVisible = false;
            this.grdMassmedia.SelectedObject = null;
            this.grdMassmedia.ShowMultiselectColumn = true;
            this.grdMassmedia.Size = new System.Drawing.Size(495, 395);
            this.grdMassmedia.TabIndex = 0;
            // 
            // cmbRadioStationGroup
            // 
            this.cmbRadioStationGroup.AccessibleRole = System.Windows.Forms.AccessibleRole.MenuBar;
            this.cmbRadioStationGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbRadioStationGroup.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbRadioStationGroup.IsNullable = false;
            this.cmbRadioStationGroup.Location = new System.Drawing.Point(0, 212);
            this.cmbRadioStationGroup.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbRadioStationGroup.Name = "cmbRadioStationGroup";
            this.cmbRadioStationGroup.SelectedIndex = -1;
            this.cmbRadioStationGroup.SelectedValue = null;
            this.cmbRadioStationGroup.Size = new System.Drawing.Size(495, 23);
            this.cmbRadioStationGroup.TabIndex = 54;
            this.cmbRadioStationGroup.SelectedItemChanged += new System.EventHandler(this.lookUpGroups_SelectedItemChanged);
            // 
            // grdActions
            // 
            this.grdActions.Caption = "Рекламные акции";
            this.grdActions.CaptionVisible = true;
            this.grdActions.CheckBoxes = false;
            this.grdActions.ColumnNameHighlight = null;
            this.grdActions.DataSource = null;
            this.grdActions.DependantGrid = null;
            this.grdActions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdActions.Entity = null;
            this.grdActions.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdActions.IsHighlightInvertColor = false;
            this.grdActions.IsNeedHighlight = false;
            this.grdActions.Location = new System.Drawing.Point(0, 0);
            this.grdActions.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.grdActions.MenuEnabled = false;
            this.grdActions.Name = "grdActions";
            this.grdActions.QuickSearchVisible = false;
            this.grdActions.SelectedObject = null;
            this.grdActions.ShowMultiselectColumn = true;
            this.grdActions.Size = new System.Drawing.Size(495, 388);
            this.grdActions.TabIndex = 0;
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
            this.grid.Location = new System.Drawing.Point(0, 0);
            this.grid.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.grid.MenuEnabled = true;
            this.grid.Name = "grid";
            this.grid.QuickSearchVisible = false;
            this.grid.SelectedObject = null;
            this.grid.ShowMultiselectColumn = true;
            this.grid.Size = new System.Drawing.Size(919, 1023);
            this.grid.TabIndex = 0;
            this.grid.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.grid_ObjectSelected);
            // 
            // opAdvertType
            // 
            this.opAdvertType.Dock = System.Windows.Forms.DockStyle.Top;
            this.opAdvertType.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.opAdvertType.Location = new System.Drawing.Point(0, 136);
            this.opAdvertType.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.opAdvertType.Name = "opAdvertType";
            this.opAdvertType.Size = new System.Drawing.Size(495, 23);
            this.opAdvertType.TabIndex = 68;
            // 
            // checkBoxShowWhite
            // 
            this.checkBoxShowWhite.AutoSize = true;
            this.checkBoxShowWhite.Checked = true;
            this.checkBoxShowWhite.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxShowWhite.Dock = System.Windows.Forms.DockStyle.Top;
            this.checkBoxShowWhite.Location = new System.Drawing.Point(0, 178);
            this.checkBoxShowWhite.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxShowWhite.Name = "checkBoxShowWhite";
            this.checkBoxShowWhite.Size = new System.Drawing.Size(495, 19);
            this.checkBoxShowWhite.TabIndex = 63;
            this.checkBoxShowWhite.Text = "Показывать с оплатой";
            this.checkBoxShowWhite.UseVisualStyleBackColor = true;
            // 
            // checkBoxShowBlack
            // 
            this.checkBoxShowBlack.AutoSize = true;
            this.checkBoxShowBlack.Checked = true;
            this.checkBoxShowBlack.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxShowBlack.Dock = System.Windows.Forms.DockStyle.Top;
            this.checkBoxShowBlack.Location = new System.Drawing.Point(0, 159);
            this.checkBoxShowBlack.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxShowBlack.Name = "checkBoxShowBlack";
            this.checkBoxShowBlack.Size = new System.Drawing.Size(495, 19);
            this.checkBoxShowBlack.TabIndex = 64;
            this.checkBoxShowBlack.Text = "Показывать без оплаты";
            this.checkBoxShowBlack.UseVisualStyleBackColor = true;
            // 
            // objectPickerFirm
            // 
            this.objectPickerFirm.Dock = System.Windows.Forms.DockStyle.Top;
            this.objectPickerFirm.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.objectPickerFirm.Location = new System.Drawing.Point(0, 99);
            this.objectPickerFirm.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.objectPickerFirm.Name = "objectPickerFirm";
            this.objectPickerFirm.Size = new System.Drawing.Size(495, 22);
            this.objectPickerFirm.TabIndex = 65;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Location = new System.Drawing.Point(0, 84);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(48, 15);
            this.label2.TabIndex = 67;
            this.label2.Text = "Фирма:";
            // 
            // objectPickerUser
            // 
            this.objectPickerUser.Dock = System.Windows.Forms.DockStyle.Top;
            this.objectPickerUser.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.objectPickerUser.Location = new System.Drawing.Point(0, 61);
            this.objectPickerUser.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.objectPickerUser.Name = "objectPickerUser";
            this.objectPickerUser.Size = new System.Drawing.Size(495, 23);
            this.objectPickerUser.TabIndex = 62;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 46);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(68, 15);
            this.label1.TabIndex = 66;
            this.label1.Text = "Менеджер:";
            // 
            // dtFinish
            // 
            this.dtFinish.Dock = System.Windows.Forms.DockStyle.Top;
            this.dtFinish.Location = new System.Drawing.Point(0, 23);
            this.dtFinish.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dtFinish.Name = "dtFinish";
            this.dtFinish.Size = new System.Drawing.Size(495, 23);
            this.dtFinish.TabIndex = 61;
            // 
            // dtStart
            // 
            this.dtStart.Dock = System.Windows.Forms.DockStyle.Top;
            this.dtStart.Location = new System.Drawing.Point(0, 0);
            this.dtStart.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dtStart.Name = "dtStart";
            this.dtStart.Size = new System.Drawing.Size(495, 23);
            this.dtStart.TabIndex = 60;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Top;
            this.label3.Location = new System.Drawing.Point(0, 121);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(111, 15);
            this.label3.TabIndex = 69;
            this.label3.Text = "Предмет рекламы:";
            // 
            // RollerStatisticForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1419, 1048);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.tlbRollerStatistic);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "RollerStatisticForm";
            this.Text = "Рекламные ролики на радиостанции";
            this.Load += new System.EventHandler(this.RollerStatisticForm_Load);
            this.tlbRollerStatistic.ResumeLayout(false);
            this.tlbRollerStatistic.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel1.PerformLayout();
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ToolStrip tlbRollerStatistic;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.SplitContainer splitContainer2;
    private System.Windows.Forms.ToolStripButton tsbRefresh;
    private System.Windows.Forms.ToolStripButton tsbSplitByManager;
	private System.Windows.Forms.ToolStripButton tsbExcel;
	private System.Windows.Forms.ToolStripButton tsbSplitByDays;
	private FogSoft.WinForm.Controls.SmartGrid grdMassmedia;
	private FogSoft.WinForm.Controls.SmartGrid grdActions;
	private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
	private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
	private System.Windows.Forms.ToolStripButton tbbPlay;
	private System.Windows.Forms.ToolStripButton tsbStop;
	private System.Windows.Forms.Label lbl;
	private FogSoft.WinForm.LookUp cmbRadioStationGroup;
    private FogSoft.WinForm.Controls.SmartGrid grid;
        private System.Windows.Forms.Label label3;
        private FogSoft.WinForm.Controls.ObjectPicker2 opAdvertType;
        private System.Windows.Forms.CheckBox checkBoxShowWhite;
        private System.Windows.Forms.CheckBox checkBoxShowBlack;
        private FogSoft.WinForm.Controls.ObjectPicker2 objectPickerFirm;
        private System.Windows.Forms.Label label2;
        private FogSoft.WinForm.Controls.ObjectPicker2 objectPickerUser;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.DateTimePicker dtFinish;
        private System.Windows.Forms.DateTimePicker dtStart;
    }
}