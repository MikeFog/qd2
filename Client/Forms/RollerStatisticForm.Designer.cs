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
            this.grdMassmedia = new FogSoft.WinForm.Controls.SmartGrid();
            this.opHeadCompany = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.label4 = new System.Windows.Forms.Label();
            this.cmbRadioStationGroup = new FogSoft.WinForm.LookUp();
            this.lbl = new System.Windows.Forms.Label();
            this.checkBoxShowWhite = new System.Windows.Forms.CheckBox();
            this.checkBoxShowBlack = new System.Windows.Forms.CheckBox();
            this.opAdvertType = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.label3 = new System.Windows.Forms.Label();
            this.objectPickerFirm = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.label2 = new System.Windows.Forms.Label();
            this.objectPickerUser = new FogSoft.WinForm.Controls.ObjectPicker2();
            this.label1 = new System.Windows.Forms.Label();
            this.dtFinish = new System.Windows.Forms.DateTimePicker();
            this.dtStart = new System.Windows.Forms.DateTimePicker();
            this.grdActions = new FogSoft.WinForm.Controls.SmartGrid();
            this.grid = new FogSoft.WinForm.Controls.SmartGrid();
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
            this.tsbRefresh.Size = new System.Drawing.Size(34, 20);
            this.tsbRefresh.Text = "toolStripButton1";
            this.tsbRefresh.ToolTipText = "Обновить информацию";
            this.tsbRefresh.Click += new System.EventHandler(this.tsbRefresh_Click);
            // 
            // tsbExcel
            // 
            this.tsbExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbExcel.Name = "tsbExcel";
            this.tsbExcel.Size = new System.Drawing.Size(34, 20);
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
            this.tbbPlay.Size = new System.Drawing.Size(34, 20);
            this.tbbPlay.Text = "Прослушать ролик";
            this.tbbPlay.Click += new System.EventHandler(this.tbbPlay_Click);
            // 
            // tsbStop
            // 
            this.tsbStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbStop.Enabled = false;
            this.tsbStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbStop.Name = "tsbStop";
            this.tsbStop.Size = new System.Drawing.Size(34, 20);
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
            this.tsbSplitByManager.Size = new System.Drawing.Size(34, 20);
            this.tsbSplitByManager.Text = "С разбивкой по менеджерам";
            this.tsbSplitByManager.Click += new System.EventHandler(this.tsbSetting_Click);
            // 
            // tsbSplitByDays
            // 
            this.tsbSplitByDays.CheckOnClick = true;
            this.tsbSplitByDays.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbSplitByDays.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbSplitByDays.Name = "tsbSplitByDays";
            this.tsbSplitByDays.Size = new System.Drawing.Size(34, 20);
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
            this.splitContainer2.Panel1.Controls.Add(this.opHeadCompany);
            this.splitContainer2.Panel1.Controls.Add(this.label4);
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
            this.splitContainer2.SplitterDistance = 657;
            this.splitContainer2.SplitterWidth = 5;
            this.splitContainer2.TabIndex = 50;
            this.splitContainer2.Text = "splitContainer2";
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
            this.grdMassmedia.Location = new System.Drawing.Point(0, 398);
            this.grdMassmedia.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.grdMassmedia.MenuEnabled = false;
            this.grdMassmedia.Name = "grdMassmedia";
            this.grdMassmedia.QuickSearchVisible = false;
            this.grdMassmedia.SelectedObject = null;
            this.grdMassmedia.ShowMultiselectColumn = true;
            this.grdMassmedia.Size = new System.Drawing.Size(495, 259);
            this.grdMassmedia.TabIndex = 72;
            // 
            // opHeadCompany
            // 
            this.opHeadCompany.Dock = System.Windows.Forms.DockStyle.Top;
            this.opHeadCompany.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.opHeadCompany.Location = new System.Drawing.Point(0, 369);
            this.opHeadCompany.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.opHeadCompany.Name = "opHeadCompany";
            this.opHeadCompany.Size = new System.Drawing.Size(495, 29);
            this.opHeadCompany.TabIndex = 71;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Dock = System.Windows.Forms.DockStyle.Top;
            this.label4.Location = new System.Drawing.Point(0, 344);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(203, 25);
            this.label4.TabIndex = 70;
            this.label4.Text = "Группа компаний:";
            // 
            // cmbRadioStationGroup
            // 
            this.cmbRadioStationGroup.AccessibleRole = System.Windows.Forms.AccessibleRole.MenuBar;
            this.cmbRadioStationGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbRadioStationGroup.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbRadioStationGroup.IsNullable = false;
            this.cmbRadioStationGroup.Location = new System.Drawing.Point(0, 311);
            this.cmbRadioStationGroup.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.cmbRadioStationGroup.Name = "cmbRadioStationGroup";
            this.cmbRadioStationGroup.SelectedIndex = -1;
            this.cmbRadioStationGroup.SelectedValue = null;
            this.cmbRadioStationGroup.Size = new System.Drawing.Size(495, 33);
            this.cmbRadioStationGroup.TabIndex = 54;
            this.cmbRadioStationGroup.SelectedItemChanged += new System.EventHandler(this.lookUpGroups_SelectedItemChanged);
            // 
            // lbl
            // 
            this.lbl.AutoSize = true;
            this.lbl.Dock = System.Windows.Forms.DockStyle.Top;
            this.lbl.Location = new System.Drawing.Point(0, 286);
            this.lbl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbl.Name = "lbl";
            this.lbl.Size = new System.Drawing.Size(73, 25);
            this.lbl.TabIndex = 56;
            this.lbl.Text = "Группа:";
            // 
            // checkBoxShowWhite
            // 
            this.checkBoxShowWhite.AutoSize = true;
            this.checkBoxShowWhite.Checked = true;
            this.checkBoxShowWhite.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxShowWhite.Dock = System.Windows.Forms.DockStyle.Top;
            this.checkBoxShowWhite.Location = new System.Drawing.Point(0, 257);
            this.checkBoxShowWhite.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxShowWhite.Name = "checkBoxShowWhite";
            this.checkBoxShowWhite.Size = new System.Drawing.Size(495, 29);
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
            this.checkBoxShowBlack.Location = new System.Drawing.Point(0, 228);
            this.checkBoxShowBlack.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.checkBoxShowBlack.Name = "checkBoxShowBlack";
            this.checkBoxShowBlack.Size = new System.Drawing.Size(495, 29);
            this.checkBoxShowBlack.TabIndex = 64;
            this.checkBoxShowBlack.Text = "Показывать без оплаты";
            this.checkBoxShowBlack.UseVisualStyleBackColor = true;
            // 
            // opAdvertType
            // 
            this.opAdvertType.Dock = System.Windows.Forms.DockStyle.Top;
            this.opAdvertType.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.opAdvertType.Location = new System.Drawing.Point(0, 197);
            this.opAdvertType.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.opAdvertType.Name = "opAdvertType";
            this.opAdvertType.Size = new System.Drawing.Size(495, 31);
            this.opAdvertType.TabIndex = 68;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Dock = System.Windows.Forms.DockStyle.Top;
            this.label3.Location = new System.Drawing.Point(0, 172);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(166, 25);
            this.label3.TabIndex = 69;
            this.label3.Text = "Предмет рекламы:";
            // 
            // objectPickerFirm
            // 
            this.objectPickerFirm.Dock = System.Windows.Forms.DockStyle.Top;
            this.objectPickerFirm.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.objectPickerFirm.Location = new System.Drawing.Point(0, 143);
            this.objectPickerFirm.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.objectPickerFirm.Name = "objectPickerFirm";
            this.objectPickerFirm.Size = new System.Drawing.Size(495, 29);
            this.objectPickerFirm.TabIndex = 65;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Dock = System.Windows.Forms.DockStyle.Top;
            this.label2.Location = new System.Drawing.Point(0, 118);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(72, 25);
            this.label2.TabIndex = 67;
            this.label2.Text = "Фирма:";
            // 
            // objectPickerUser
            // 
            this.objectPickerUser.Dock = System.Windows.Forms.DockStyle.Top;
            this.objectPickerUser.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.objectPickerUser.Location = new System.Drawing.Point(0, 87);
            this.objectPickerUser.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
            this.objectPickerUser.Name = "objectPickerUser";
            this.objectPickerUser.Size = new System.Drawing.Size(495, 31);
            this.objectPickerUser.TabIndex = 62;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Dock = System.Windows.Forms.DockStyle.Top;
            this.label1.Location = new System.Drawing.Point(0, 62);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(103, 25);
            this.label1.TabIndex = 66;
            this.label1.Text = "Менеджер:";
            // 
            // dtFinish
            // 
            this.dtFinish.Dock = System.Windows.Forms.DockStyle.Top;
            this.dtFinish.Location = new System.Drawing.Point(0, 31);
            this.dtFinish.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dtFinish.Name = "dtFinish";
            this.dtFinish.Size = new System.Drawing.Size(495, 31);
            this.dtFinish.TabIndex = 61;
            // 
            // dtStart
            // 
            this.dtStart.Dock = System.Windows.Forms.DockStyle.Top;
            this.dtStart.Location = new System.Drawing.Point(0, 0);
            this.dtStart.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dtStart.Name = "dtStart";
            this.dtStart.Size = new System.Drawing.Size(495, 31);
            this.dtStart.TabIndex = 60;
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
            this.grdActions.Size = new System.Drawing.Size(495, 361);
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
            // RollerStatisticForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
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
        private System.Windows.Forms.Label label4;
        private FogSoft.WinForm.Controls.ObjectPicker2 opHeadCompany;
    }
}