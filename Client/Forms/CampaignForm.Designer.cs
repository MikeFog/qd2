namespace Merlin.Forms {
  partial class CampaignForm {
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
        if (mediaControl != null)
            mediaControl.Stop();
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(CampaignForm));
            this.tsCampaign = new System.Windows.Forms.ToolStrip();
            this.tbbRefresh = new System.Windows.Forms.ToolStripButton();
            this.tbbStart = new System.Windows.Forms.ToolStripButton();
            this.tsbMuteRoller = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator6 = new System.Windows.Forms.ToolStripSeparator();
            this.tbbPosition = new System.Windows.Forms.ToolStripDropDownButton();
            this.miShowAllPositions = new System.Windows.Forms.ToolStripMenuItem();
            this.miShowFirst = new System.Windows.Forms.ToolStripMenuItem();
            this.miShowSecond = new System.Windows.Forms.ToolStripMenuItem();
            this.miShowLast = new System.Windows.Forms.ToolStripMenuItem();
            this.tbbAdvertType = new System.Windows.Forms.ToolStripDropDownButton();
            this.miShowAllAdvertTypes = new System.Windows.Forms.ToolStripMenuItem();
            this.miShowlAdvertTypeExist = new System.Windows.Forms.ToolStripMenuItem();
            this.miShowlAdvertTypeNotExist = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator4 = new System.Windows.Forms.ToolStripSeparator();
            this.tbbModules = new System.Windows.Forms.ToolStripDropDownButton();
            this.tbbShowUnconfirmed = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tbbPlay = new System.Windows.Forms.ToolStripButton();
            this.tsbStop = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator3 = new System.Windows.Forms.ToolStripSeparator();
            this.tbbJump = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator5 = new System.Windows.Forms.ToolStripSeparator();
            this.tbbTemplate = new System.Windows.Forms.ToolStripButton();
            this.tbbTemplate2 = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.tbbExcel = new System.Windows.Forms.ToolStripButton();
            this.toolStripButtonGrantor = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.grdRollers = new FogSoft.WinForm.Controls.SmartGrid();
            this.splitContainer3 = new System.Windows.Forms.SplitContainer();
            this.lstStat = new System.Windows.Forms.ListBox();
            this.splitContainer4 = new System.Windows.Forms.SplitContainer();
            this.grdIssues = new FogSoft.WinForm.Controls.SmartGrid();
            this.grdCurrentCampaignIssues = new FogSoft.WinForm.Controls.SmartGrid();
            this.splitContainer5 = new System.Windows.Forms.SplitContainer();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabelSelected = new System.Windows.Forms.ToolStripStatusLabel();
            this.tsCampaign.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.splitContainer3.Panel1.SuspendLayout();
            this.splitContainer3.Panel2.SuspendLayout();
            this.splitContainer3.SuspendLayout();
            this.splitContainer4.Panel1.SuspendLayout();
            this.splitContainer4.Panel2.SuspendLayout();
            this.splitContainer4.SuspendLayout();
            this.splitContainer5.SuspendLayout();
            this.SuspendLayout();
            // 
            // tsCampaign
            // 
            this.tsCampaign.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.tsCampaign.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbbRefresh,
            this.tbbStart,
            this.tsbMuteRoller,
            this.toolStripSeparator6,
            this.tbbPosition,
            this.tbbAdvertType,
            this.toolStripSeparator4,
            this.tbbModules,
            this.tbbShowUnconfirmed,
            this.toolStripSeparator1,
            this.tbbPlay,
            this.tsbStop,
            this.toolStripSeparator3,
            this.tbbJump,
            this.toolStripSeparator5,
            this.tbbTemplate,
            this.tbbTemplate2,
            this.toolStripSeparator2,
            this.tbbExcel,
            this.toolStripButtonGrantor});
            this.tsCampaign.Location = new System.Drawing.Point(0, 0);
            this.tsCampaign.Name = "tsCampaign";
            this.tsCampaign.Size = new System.Drawing.Size(1592, 27);
            this.tsCampaign.TabIndex = 0;
            // 
            // tbbRefresh
            // 
            this.tbbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbRefresh.Name = "tbbRefresh";
            this.tbbRefresh.Size = new System.Drawing.Size(23, 24);
            this.tbbRefresh.Text = "Refresh";
            this.tbbRefresh.ToolTipText = "Обновить информацию";
            this.tbbRefresh.Click += new System.EventHandler(this.tbbRefresh_Click);
            // 
            // tbbStart
            // 
            this.tbbStart.CheckOnClick = true;
            this.tbbStart.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbbStart.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbStart.Name = "tbbStart";
            this.tbbStart.Size = new System.Drawing.Size(23, 24);
            this.tbbStart.Text = "Start";
            this.tbbStart.ToolTipText = "Режим добавления";
            this.tbbStart.CheckedChanged += new System.EventHandler(this.tbbStart_CheckedChanged);
            // 
            // tsbMuteRoller
            // 
            this.tsbMuteRoller.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbMuteRoller.Image = global::Merlin.Properties.Resources.mute_roller;
            this.tsbMuteRoller.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbMuteRoller.Name = "tsbMuteRoller";
            this.tsbMuteRoller.Size = new System.Drawing.Size(24, 24);
            this.tsbMuteRoller.Text = "Добавить ролик - пустышку";
            this.tsbMuteRoller.Click += new System.EventHandler(this.tsbMuteRoller_Click);
            // 
            // toolStripSeparator6
            // 
            this.toolStripSeparator6.Name = "toolStripSeparator6";
            this.toolStripSeparator6.Size = new System.Drawing.Size(6, 27);
            // 
            // tbbPosition
            // 
            this.tbbPosition.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbPosition.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miShowAllPositions,
            this.miShowFirst,
            this.miShowSecond,
            this.miShowLast});
            this.tbbPosition.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbPosition.Name = "tbbPosition";
            this.tbbPosition.Size = new System.Drawing.Size(129, 24);
            this.tbbPosition.Text = "Позиционирование";
            this.tbbPosition.ToolTipText = "Позиция в блоке";
            this.tbbPosition.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tbbPosition_DropDownItemClicked);
            // 
            // miShowAllPositions
            // 
            this.miShowAllPositions.Name = "miShowAllPositions";
            this.miShowAllPositions.Size = new System.Drawing.Size(201, 22);
            this.miShowAllPositions.Tag = "0";
            this.miShowAllPositions.Text = "Показывать всё";
            // 
            // miShowFirst
            // 
            this.miShowFirst.Name = "miShowFirst";
            this.miShowFirst.Size = new System.Drawing.Size(201, 22);
            this.miShowFirst.Tag = "-20";
            this.miShowFirst.Text = "Показывать первые";
            // 
            // miShowSecond
            // 
            this.miShowSecond.Name = "miShowSecond";
            this.miShowSecond.Size = new System.Drawing.Size(201, 22);
            this.miShowSecond.Tag = "-10";
            this.miShowSecond.Text = "Показывать вторые";
            // 
            // miShowLast
            // 
            this.miShowLast.Name = "miShowLast";
            this.miShowLast.Size = new System.Drawing.Size(201, 22);
            this.miShowLast.Tag = "10";
            this.miShowLast.Text = "Показывать последние";
            // 
            // tbbAdvertType
            // 
            this.tbbAdvertType.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbAdvertType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miShowAllAdvertTypes,
            this.miShowlAdvertTypeExist,
            this.miShowlAdvertTypeNotExist});
            this.tbbAdvertType.Image = ((System.Drawing.Image)(resources.GetObject("tbbAdvertType.Image")));
            this.tbbAdvertType.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbAdvertType.Name = "tbbAdvertType";
            this.tbbAdvertType.Size = new System.Drawing.Size(130, 24);
            this.tbbAdvertType.Text = "Предметы рекламы";
            this.tbbAdvertType.ToolTipText = "Предметы рекламы";
            this.tbbAdvertType.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tbbAdvertType_DropDownItemClicked);
            // 
            // miShowAllAdvertTypes
            // 
            this.miShowAllAdvertTypes.Name = "miShowAllAdvertTypes";
            this.miShowAllAdvertTypes.Size = new System.Drawing.Size(309, 22);
            this.miShowAllAdvertTypes.Tag = "0";
            this.miShowAllAdvertTypes.Text = "Показывать всё";
            // 
            // miShowlAdvertTypeExist
            // 
            this.miShowlAdvertTypeExist.Name = "miShowlAdvertTypeExist";
            this.miShowlAdvertTypeExist.Size = new System.Drawing.Size(309, 22);
            this.miShowlAdvertTypeExist.Tag = "5";
            this.miShowlAdvertTypeExist.Text = "Показывать все где есть предмет рекламы";
            // 
            // miShowlAdvertTypeNotExist
            // 
            this.miShowlAdvertTypeNotExist.Name = "miShowlAdvertTypeNotExist";
            this.miShowlAdvertTypeNotExist.Size = new System.Drawing.Size(309, 22);
            this.miShowlAdvertTypeNotExist.Tag = "10";
            this.miShowlAdvertTypeNotExist.Text = "Показывать все где нет предмета рекламы";
            // 
            // toolStripSeparator4
            // 
            this.toolStripSeparator4.Name = "toolStripSeparator4";
            this.toolStripSeparator4.Size = new System.Drawing.Size(6, 27);
            // 
            // tbbModules
            // 
            this.tbbModules.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbModules.Name = "tbbModules";
            this.tbbModules.Size = new System.Drawing.Size(101, 24);
            this.tbbModules.Text = "Выбор модуля";
            this.tbbModules.Visible = false;
            // 
            // tbbShowUnconfirmed
            // 
            this.tbbShowUnconfirmed.Checked = true;
            this.tbbShowUnconfirmed.CheckOnClick = true;
            this.tbbShowUnconfirmed.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tbbShowUnconfirmed.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
            this.tbbShowUnconfirmed.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbShowUnconfirmed.Name = "tbbShowUnconfirmed";
            this.tbbShowUnconfirmed.Size = new System.Drawing.Size(113, 24);
            this.tbbShowUnconfirmed.Text = "Учитывать макеты";
            this.tbbShowUnconfirmed.ToolTipText = "Показывать неподтвержденные выпуски";
            this.tbbShowUnconfirmed.Click += new System.EventHandler(this.tbbShowUnconfirmed_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 27);
            // 
            // tbbPlay
            // 
            this.tbbPlay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbbPlay.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbPlay.Name = "tbbPlay";
            this.tbbPlay.Size = new System.Drawing.Size(23, 24);
            this.tbbPlay.Text = "Прослушать ролик";
            this.tbbPlay.Click += new System.EventHandler(this.tbbPlay_Click);
            // 
            // tsbStop
            // 
            this.tsbStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbStop.Enabled = false;
            this.tsbStop.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbStop.Name = "tsbStop";
            this.tsbStop.Size = new System.Drawing.Size(23, 24);
            this.tsbStop.Text = "Остановить прослушивание";
            this.tsbStop.ToolTipText = "Остановить прослушивание";
            this.tsbStop.Click += new System.EventHandler(this.tsbStop_Click);
            // 
            // toolStripSeparator3
            // 
            this.toolStripSeparator3.Name = "toolStripSeparator3";
            this.toolStripSeparator3.Size = new System.Drawing.Size(6, 27);
            // 
            // tbbJump
            // 
            this.tbbJump.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbbJump.Image = global::Merlin.Properties.Resources.calendar;
            this.tbbJump.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbJump.Name = "tbbJump";
            this.tbbJump.Size = new System.Drawing.Size(24, 24);
            this.tbbJump.Text = "Переход к выбранной дате";
            this.tbbJump.Click += new System.EventHandler(this.tbbJump_Click);
            // 
            // toolStripSeparator5
            // 
            this.toolStripSeparator5.Name = "toolStripSeparator5";
            this.toolStripSeparator5.Size = new System.Drawing.Size(6, 27);
            // 
            // tbbTemplate
            // 
            this.tbbTemplate.CheckOnClick = true;
            this.tbbTemplate.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbbTemplate.Image = ((System.Drawing.Image)(resources.GetObject("tbbTemplate.Image")));
            this.tbbTemplate.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbTemplate.Name = "tbbTemplate";
            this.tbbTemplate.Size = new System.Drawing.Size(24, 24);
            this.tbbTemplate.Text = "Шаблон для внесения роликов";
            this.tbbTemplate.ToolTipText = "Шаблон для внесения роликов";
            this.tbbTemplate.Click += new System.EventHandler(this.tbbTemplate_Click);
            // 
            // tbbTemplate2
            // 
            this.tbbTemplate2.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbbTemplate2.Image = ((System.Drawing.Image)(resources.GetObject("tbbTemplate2.Image")));
            this.tbbTemplate2.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbTemplate2.Name = "tbbTemplate2";
            this.tbbTemplate2.Size = new System.Drawing.Size(24, 24);
            this.tbbTemplate2.Text = "Шаблон для внесения роликов";
            this.tbbTemplate2.ToolTipText = "Шаблон для внесения роликов на интервале времени";
            this.tbbTemplate2.Click += new System.EventHandler(this.tbbTemplate2_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(6, 27);
            // 
            // tbbExcel
            // 
            this.tbbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbExcel.Name = "tbbExcel";
            this.tbbExcel.Size = new System.Drawing.Size(56, 24);
            this.tbbExcel.Text = "Экспорт";
            this.tbbExcel.ToolTipText = "Экспорт таблицы";
            this.tbbExcel.Click += new System.EventHandler(this.tbbExcel_Click);
            // 
            // toolStripButtonGrantor
            // 
            this.toolStripButtonGrantor.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.toolStripButtonGrantor.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButtonGrantor.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButtonGrantor.Name = "toolStripButtonGrantor";
            this.toolStripButtonGrantor.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.toolStripButtonGrantor.Size = new System.Drawing.Size(23, 24);
            this.toolStripButtonGrantor.Text = "Grant";
            this.toolStripButtonGrantor.ToolTipText = "Присоединить привилегированного пользователя.";
            this.toolStripButtonGrantor.Click += new System.EventHandler(this.toolStripButtonGrantor_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 27);
            this.splitContainer1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer5);
            this.splitContainer1.Panel2.Controls.Add(this.statusStrip1);
            this.splitContainer1.Size = new System.Drawing.Size(1592, 1034);
            this.splitContainer1.SplitterDistance = 691;
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
            this.splitContainer2.Panel1.Controls.Add(this.grdRollers);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.splitContainer3);
            this.splitContainer2.Size = new System.Drawing.Size(691, 1034);
            this.splitContainer2.SplitterDistance = 259;
            this.splitContainer2.SplitterWidth = 5;
            this.splitContainer2.TabIndex = 0;
            this.splitContainer2.Text = "splitContainer2";
            // 
            // grdRollers
            // 
            this.grdRollers.Caption = "Ролики";
            this.grdRollers.CaptionVisible = true;
            this.grdRollers.CheckBoxes = false;
            this.grdRollers.ColumnNameHighlight = null;
            this.grdRollers.DataSource = null;
            this.grdRollers.DependantGrid = null;
            this.grdRollers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdRollers.Entity = null;
            this.grdRollers.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdRollers.IsHighlightInvertColor = false;
            this.grdRollers.IsNeedHighlight = false;
            this.grdRollers.Location = new System.Drawing.Point(0, 0);
            this.grdRollers.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdRollers.MenuEnabled = true;
            this.grdRollers.Name = "grdRollers";
            this.grdRollers.QuickSearchVisible = false;
            this.grdRollers.SelectedObject = null;
            this.grdRollers.Size = new System.Drawing.Size(691, 259);
            this.grdRollers.TabIndex = 0;
            this.grdRollers.ObjectSelected += new FogSoft.WinForm.ObjectDelegate(this.grdRollers_ObjectSelected);
            // 
            // splitContainer3
            // 
            this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer3.Location = new System.Drawing.Point(0, 0);
            this.splitContainer3.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer3.Name = "splitContainer3";
            this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer3.Panel1
            // 
            this.splitContainer3.Panel1.Controls.Add(this.lstStat);
            // 
            // splitContainer3.Panel2
            // 
            this.splitContainer3.Panel2.Controls.Add(this.splitContainer4);
            this.splitContainer3.Size = new System.Drawing.Size(691, 770);
            this.splitContainer3.SplitterDistance = 148;
            this.splitContainer3.SplitterWidth = 5;
            this.splitContainer3.TabIndex = 0;
            this.splitContainer3.Text = "splitContainer3";
            // 
            // lstStat
            // 
            this.lstStat.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lstStat.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lstStat.FormattingEnabled = true;
            this.lstStat.ItemHeight = 15;
            this.lstStat.Location = new System.Drawing.Point(0, 0);
            this.lstStat.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.lstStat.Name = "lstStat";
            this.lstStat.Size = new System.Drawing.Size(691, 148);
            this.lstStat.TabIndex = 5;
            // 
            // splitContainer4
            // 
            this.splitContainer4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer4.Location = new System.Drawing.Point(0, 0);
            this.splitContainer4.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer4.Name = "splitContainer4";
            this.splitContainer4.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer4.Panel1
            // 
            this.splitContainer4.Panel1.Controls.Add(this.grdIssues);
            // 
            // splitContainer4.Panel2
            // 
            this.splitContainer4.Panel2.Controls.Add(this.grdCurrentCampaignIssues);
            this.splitContainer4.Size = new System.Drawing.Size(691, 617);
            this.splitContainer4.SplitterDistance = 299;
            this.splitContainer4.SplitterWidth = 5;
            this.splitContainer4.TabIndex = 1;
            // 
            // grdIssues
            // 
            this.grdIssues.Caption = "Все выходы в эфир";
            this.grdIssues.CaptionVisible = true;
            this.grdIssues.CheckBoxes = false;
            this.grdIssues.ColumnNameHighlight = null;
            this.grdIssues.DataSource = null;
            this.grdIssues.DependantGrid = null;
            this.grdIssues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdIssues.Entity = null;
            this.grdIssues.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdIssues.IsHighlightInvertColor = false;
            this.grdIssues.IsNeedHighlight = false;
            this.grdIssues.Location = new System.Drawing.Point(0, 0);
            this.grdIssues.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdIssues.MenuEnabled = false;
            this.grdIssues.Name = "grdIssues";
            this.grdIssues.QuickSearchVisible = false;
            this.grdIssues.SelectedObject = null;
            this.grdIssues.Size = new System.Drawing.Size(691, 299);
            this.grdIssues.TabIndex = 2;
            // 
            // grdCurrentCampaignIssues
            // 
            this.grdCurrentCampaignIssues.Caption = "Выходы в эфир этой кампании";
            this.grdCurrentCampaignIssues.CaptionVisible = true;
            this.grdCurrentCampaignIssues.CheckBoxes = false;
            this.grdCurrentCampaignIssues.ColumnNameHighlight = null;
            this.grdCurrentCampaignIssues.DataSource = null;
            this.grdCurrentCampaignIssues.DependantGrid = null;
            this.grdCurrentCampaignIssues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdCurrentCampaignIssues.Entity = null;
            this.grdCurrentCampaignIssues.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdCurrentCampaignIssues.IsHighlightInvertColor = false;
            this.grdCurrentCampaignIssues.IsNeedHighlight = false;
            this.grdCurrentCampaignIssues.Location = new System.Drawing.Point(0, 0);
            this.grdCurrentCampaignIssues.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdCurrentCampaignIssues.MenuEnabled = true;
            this.grdCurrentCampaignIssues.Name = "grdCurrentCampaignIssues";
            this.grdCurrentCampaignIssues.QuickSearchVisible = false;
            this.grdCurrentCampaignIssues.SelectedObject = null;
            this.grdCurrentCampaignIssues.Size = new System.Drawing.Size(691, 313);
            this.grdCurrentCampaignIssues.TabIndex = 1;
            this.grdCurrentCampaignIssues.ObjectDeleted += new FogSoft.WinForm.ObjectDelegate(this.grdCurrentCampaignIssues_ObjectDeleted);
            this.grdCurrentCampaignIssues.ObjectChanged += new FogSoft.WinForm.ObjectDelegate(this.grdCurrentCampaignIssues_ObjectChanged);
            // 
            // splitContainer5
            // 
            this.splitContainer5.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer5.Location = new System.Drawing.Point(0, 0);
            this.splitContainer5.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.splitContainer5.Name = "splitContainer5";
            this.splitContainer5.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.splitContainer5.Size = new System.Drawing.Size(894, 1009);
            this.splitContainer5.SplitterDistance = 517;
            this.splitContainer5.SplitterWidth = 5;
            this.splitContainer5.TabIndex = 1;
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(36, 36);
            this.statusStrip1.Location = new System.Drawing.Point(0, 1012);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Padding = new System.Windows.Forms.Padding(1, 0, 16, 0);
            this.statusStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.Professional;
            this.statusStrip1.Size = new System.Drawing.Size(896, 22);
            this.statusStrip1.TabIndex = 0;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabelSelected
            // 
            this.toolStripStatusLabelSelected.Name = "toolStripStatusLabelSelected";
            this.toolStripStatusLabelSelected.Size = new System.Drawing.Size(95, 17);
            this.toolStripStatusLabelSelected.Text = "Окно не выбрано";
            // 
            // CampaignForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1592, 1061);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.tsCampaign);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "CampaignForm";
            this.ShowInTaskbar = false;
            this.Text = "Рекламная кампания";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.CampaignForm_FormClosing);
            this.Load += new System.EventHandler(this.CampaignForm_Load);
            this.tsCampaign.ResumeLayout(false);
            this.tsCampaign.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.Panel2.PerformLayout();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.ResumeLayout(false);
            this.splitContainer3.Panel1.ResumeLayout(false);
            this.splitContainer3.Panel2.ResumeLayout(false);
            this.splitContainer3.ResumeLayout(false);
            this.splitContainer4.Panel1.ResumeLayout(false);
            this.splitContainer4.Panel2.ResumeLayout(false);
            this.splitContainer4.ResumeLayout(false);
            this.splitContainer5.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.ToolStrip tsCampaign;
    private System.Windows.Forms.SplitContainer splitContainer1;
    private System.Windows.Forms.SplitContainer splitContainer2;
	private FogSoft.WinForm.Controls.SmartGrid grdRollers;
    private System.Windows.Forms.ToolStripButton tbbRefresh;
    private System.Windows.Forms.ToolStripButton tbbStart;
    private System.Windows.Forms.ToolStripDropDownButton tbbModules;
    private System.Windows.Forms.SplitContainer splitContainer3;
    protected System.Windows.Forms.ListBox lstStat;
    private System.Windows.Forms.ToolStripDropDownButton tbbPosition;
    private System.Windows.Forms.ToolStripButton tbbShowUnconfirmed;
    private System.Windows.Forms.ToolStripMenuItem miShowAllPositions;
    private System.Windows.Forms.ToolStripMenuItem miShowFirst;
    private System.Windows.Forms.ToolStripMenuItem miShowSecond;
    private System.Windows.Forms.ToolStripMenuItem miShowLast;
    protected System.Windows.Forms.SplitContainer splitContainer4;
	protected FogSoft.WinForm.Controls.SmartGrid grdIssues;
	protected FogSoft.WinForm.Controls.SmartGrid grdCurrentCampaignIssues;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.ToolStripButton tbbPlay;
    private System.Windows.Forms.ToolStripButton tbbJump;
    protected System.Windows.Forms.ToolStripButton tbbTemplate;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
    private System.Windows.Forms.ToolStripButton tbbExcel;
    private System.Windows.Forms.ToolStripButton tsbStop;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator3;
	private System.Windows.Forms.ToolStripButton toolStripButtonGrantor;
	private System.Windows.Forms.StatusStrip statusStrip1;
	private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabelSelected;
	private System.Windows.Forms.SplitContainer splitContainer5;
	protected System.Windows.Forms.ToolStripButton tsbMuteRoller;
    protected System.Windows.Forms.ToolStripButton tbbTemplate2;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator5;
    private System.Windows.Forms.ToolStripDropDownButton tbbAdvertType;
    private System.Windows.Forms.ToolStripMenuItem miShowAllAdvertTypes;
    private System.Windows.Forms.ToolStripMenuItem miShowlAdvertTypeExist;
    private System.Windows.Forms.ToolStripMenuItem miShowlAdvertTypeNotExist;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator6;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator4;
    }
}