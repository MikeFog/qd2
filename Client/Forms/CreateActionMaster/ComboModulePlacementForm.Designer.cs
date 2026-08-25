namespace Merlin.Forms.CreateActionMaster
{
	partial class ComboModulePlacementForm
	{
		private System.ComponentModel.IContainer components = null;

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
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.tbbRefresh = new System.Windows.Forms.ToolStripButton();
			this.tbbStart = new System.Windows.Forms.ToolStripButton();
			this.tbbJump = new System.Windows.Forms.ToolStripButton();
			this.tbbSeparator0 = new System.Windows.Forms.ToolStripSeparator();
			this.tsbMuteRoller = new System.Windows.Forms.ToolStripButton();
			this.tbbPlay = new System.Windows.Forms.ToolStripButton();
			this.tsbStop = new System.Windows.Forms.ToolStripButton();
			this.tbbSeparator0b = new System.Windows.Forms.ToolStripSeparator();
			this.tbbPosition = new System.Windows.Forms.ToolStripDropDownButton();
			this.miPositionAny = new System.Windows.Forms.ToolStripMenuItem();
			this.miPositionFirst = new System.Windows.Forms.ToolStripMenuItem();
			this.miPositionSecond = new System.Windows.Forms.ToolStripMenuItem();
			this.miPositionLast = new System.Windows.Forms.ToolStripMenuItem();
			this.tbbAdvertType = new System.Windows.Forms.ToolStripDropDownButton();
			this.miShowAllAdvertTypes = new System.Windows.Forms.ToolStripMenuItem();
			this.miShowAdvertTypeExist = new System.Windows.Forms.ToolStripMenuItem();
			this.miShowAdvertTypeNotExist = new System.Windows.Forms.ToolStripMenuItem();
			this.tbbSeparator1 = new System.Windows.Forms.ToolStripSeparator();
			this.tbbPeriodMode = new System.Windows.Forms.ToolStripDropDownButton();
			this.miPeriodWeek = new System.Windows.Forms.ToolStripMenuItem();
			this.miPeriodMonth = new System.Windows.Forms.ToolStripMenuItem();
			this.tbbSeparator2 = new System.Windows.Forms.ToolStripSeparator();
			this.tbbShowUnconfirmed = new System.Windows.Forms.ToolStripButton();
			this.tbbSeparator3 = new System.Windows.Forms.ToolStripSeparator();
			this.tbbExcel = new System.Windows.Forms.ToolStripButton();
			this.splitContainer1 = new System.Windows.Forms.SplitContainer();
			this.splitContainer2 = new System.Windows.Forms.SplitContainer();
			this.splitContainer3 = new System.Windows.Forms.SplitContainer();
			this.grdRollers = new FogSoft.WinForm.Controls.SmartGrid();
			this.lstStat = new System.Windows.Forms.ListBox();
			this.grdAddedIssues = new FogSoft.WinForm.Controls.SmartGrid();
			this.comboModuleGrid = new Merlin.Controls.ComboModuleGrid();
			this.toolStrip1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
			this.splitContainer1.Panel1.SuspendLayout();
			this.splitContainer1.Panel2.SuspendLayout();
			this.splitContainer1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
			this.splitContainer2.Panel1.SuspendLayout();
			this.splitContainer2.Panel2.SuspendLayout();
			this.splitContainer2.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).BeginInit();
			this.splitContainer3.Panel1.SuspendLayout();
			this.splitContainer3.Panel2.SuspendLayout();
			this.splitContainer3.SuspendLayout();
			this.SuspendLayout();
			//
			// tbbRefresh
			//
			this.tbbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tbbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbRefresh.Name = "tbbRefresh";
			this.tbbRefresh.Size = new System.Drawing.Size(34, 33);
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
			this.tbbStart.Size = new System.Drawing.Size(34, 33);
			this.tbbStart.Text = "Режим добавления";
			this.tbbStart.ToolTipText = "Режим добавления";
			this.tbbStart.CheckedChanged += new System.EventHandler(this.tbbStart_CheckedChanged);
			//
			// tbbJump
			//
			this.tbbJump.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tbbJump.Image = global::Merlin.Properties.Resources.calendar;
			this.tbbJump.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbJump.Name = "tbbJump";
			this.tbbJump.Size = new System.Drawing.Size(34, 33);
			this.tbbJump.Text = "Переход к выбранной дате";
			this.tbbJump.ToolTipText = "Переход к выбранной дате";
			this.tbbJump.Click += new System.EventHandler(this.tbbJump_Click);
			//
			// tbbSeparator0
			//
			this.tbbSeparator0.Name = "tbbSeparator0";
			//
			// tsbMuteRoller
			//
			this.tsbMuteRoller.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsbMuteRoller.Image = global::Merlin.Properties.Resources.mute_roller;
			this.tsbMuteRoller.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbMuteRoller.Name = "tsbMuteRoller";
			this.tsbMuteRoller.Size = new System.Drawing.Size(34, 33);
			this.tsbMuteRoller.Text = "Добавить ролик - пустышку";
			this.tsbMuteRoller.ToolTipText = "Добавить ролик - пустышку";
			this.tsbMuteRoller.Click += new System.EventHandler(this.tsbMuteRoller_Click);
			//
			// tbbPlay
			//
			this.tbbPlay.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tbbPlay.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbPlay.Name = "tbbPlay";
			this.tbbPlay.Size = new System.Drawing.Size(34, 33);
			this.tbbPlay.Text = "Прослушать ролик";
			this.tbbPlay.ToolTipText = "Прослушать ролик";
			this.tbbPlay.Click += new System.EventHandler(this.tbbPlay_Click);
			//
			// tsbStop
			//
			this.tsbStop.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tsbStop.Enabled = false;
			this.tsbStop.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tsbStop.Name = "tsbStop";
			this.tsbStop.Size = new System.Drawing.Size(34, 33);
			this.tsbStop.Text = "Остановить прослушивание";
			this.tsbStop.ToolTipText = "Остановить прослушивание";
			this.tsbStop.Click += new System.EventHandler(this.tsbStop_Click);
			//
			// tbbSeparator0b
			//
			this.tbbSeparator0b.Name = "tbbSeparator0b";
			//
			// tbbPosition
			//
			this.tbbPosition.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbPosition.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
				this.miPositionAny,
				this.miPositionFirst,
				this.miPositionSecond,
				this.miPositionLast});
			this.tbbPosition.Name = "tbbPosition";
			this.tbbPosition.Text = "Позиционирование";
			this.tbbPosition.ToolTipText = "Позиция в блоке";
			this.tbbPosition.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tbbPosition_DropDownItemClicked);
			//
			// miPositionAny
			//
			this.miPositionAny.Name = "miPositionAny";
			this.miPositionAny.Tag = "0";
			this.miPositionAny.Text = "Показывать всё";
			//
			// miPositionFirst
			//
			this.miPositionFirst.Name = "miPositionFirst";
			this.miPositionFirst.Tag = "-20";
			this.miPositionFirst.Text = "Показывать первые";
			//
			// miPositionSecond
			//
			this.miPositionSecond.Name = "miPositionSecond";
			this.miPositionSecond.Tag = "-10";
			this.miPositionSecond.Text = "Показывать вторые";
			//
			// miPositionLast
			//
			this.miPositionLast.Name = "miPositionLast";
			this.miPositionLast.Tag = "10";
			this.miPositionLast.Text = "Показывать последние";
			//
			// tbbAdvertType
			//
			this.tbbAdvertType.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbAdvertType.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
				this.miShowAllAdvertTypes,
				this.miShowAdvertTypeExist,
				this.miShowAdvertTypeNotExist});
			this.tbbAdvertType.Name = "tbbAdvertType";
			this.tbbAdvertType.Text = "Предметы рекламы";
			this.tbbAdvertType.ToolTipText = "Предметы рекламы";
			this.tbbAdvertType.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tbbAdvertType_DropDownItemClicked);
			//
			// miShowAllAdvertTypes
			//
			this.miShowAllAdvertTypes.Name = "miShowAllAdvertTypes";
			this.miShowAllAdvertTypes.Tag = "0";
			this.miShowAllAdvertTypes.Text = "Показывать всё";
			//
			// miShowAdvertTypeExist
			//
			this.miShowAdvertTypeExist.Name = "miShowAdvertTypeExist";
			this.miShowAdvertTypeExist.Tag = "5";
			this.miShowAdvertTypeExist.Text = "Показывать все где есть предмет рекламы";
			//
			// miShowAdvertTypeNotExist
			//
			this.miShowAdvertTypeNotExist.Name = "miShowAdvertTypeNotExist";
			this.miShowAdvertTypeNotExist.Tag = "10";
			this.miShowAdvertTypeNotExist.Text = "Показывать все где нет предмета рекламы";
			//
			// tbbSeparator1
			//
			this.tbbSeparator1.Name = "tbbSeparator1";
			//
			// tbbPeriodMode
			//
			this.tbbPeriodMode.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbPeriodMode.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
				this.miPeriodWeek,
				this.miPeriodMonth});
			this.tbbPeriodMode.Name = "tbbPeriodMode";
			this.tbbPeriodMode.Text = "Неделя";
			this.tbbPeriodMode.ToolTipText = "Показывать неделю или месяц";
			this.tbbPeriodMode.DropDownItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.tbbPeriodMode_DropDownItemClicked);
			//
			// miPeriodWeek
			//
			this.miPeriodWeek.Name = "miPeriodWeek";
			this.miPeriodWeek.Tag = "Week";
			this.miPeriodWeek.Text = "Неделя";
			//
			// miPeriodMonth
			//
			this.miPeriodMonth.Name = "miPeriodMonth";
			this.miPeriodMonth.Tag = "Month";
			this.miPeriodMonth.Text = "Месяц";
			//
			// tbbSeparator2
			//
			this.tbbSeparator2.Name = "tbbSeparator2";
			//
			// tbbShowUnconfirmed
			//
			this.tbbShowUnconfirmed.CheckOnClick = true;
			this.tbbShowUnconfirmed.Checked = true;
			this.tbbShowUnconfirmed.CheckState = System.Windows.Forms.CheckState.Checked;
			this.tbbShowUnconfirmed.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
			this.tbbShowUnconfirmed.Name = "tbbShowUnconfirmed";
			this.tbbShowUnconfirmed.Text = "Учитывать макеты";
			this.tbbShowUnconfirmed.Click += new System.EventHandler(this.tbbShowUnconfirmed_Click);
			//
			// tbbSeparator3
			//
			this.tbbSeparator3.Name = "tbbSeparator3";
			//
			// tbbExcel
			//
			this.tbbExcel.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this.tbbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbExcel.Name = "tbbExcel";
			this.tbbExcel.Size = new System.Drawing.Size(34, 33);
			this.tbbExcel.Text = "Экспорт";
			this.tbbExcel.ToolTipText = "Экспорт таблицы";
			this.tbbExcel.Click += new System.EventHandler(this.tbbExcel_Click);
			//
			// toolStrip1
			//
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
				this.tbbRefresh,
				this.tbbStart,
				this.tbbJump,
				this.tbbSeparator0,
				this.tsbMuteRoller,
				this.tbbPlay,
				this.tsbStop,
				this.tbbSeparator0b,
				this.tbbPosition,
				this.tbbAdvertType,
				this.tbbSeparator1,
				this.tbbPeriodMode,
				this.tbbSeparator2,
				this.tbbShowUnconfirmed,
				this.tbbSeparator3,
				this.tbbExcel});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(1200, 25);
			this.toolStrip1.TabIndex = 0;
			//
			// grdRollers
			//
			this.grdRollers.Caption = "Ролики";
			this.grdRollers.CaptionVisible = true;
			this.grdRollers.ColumnNameHighlight = null;
			this.grdRollers.DataSource = null;
			this.grdRollers.DependantGrid = null;
			this.grdRollers.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grdRollers.Entity = null;
			this.grdRollers.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdRollers.IsHighlightInvertColor = false;
			this.grdRollers.IsNeedHighlight = false;
			this.grdRollers.MenuEnabled = false;
			this.grdRollers.Name = "grdRollers";
			this.grdRollers.QuickSearchVisible = false;
			this.grdRollers.SelectedObject = null;
			this.grdRollers.TabIndex = 0;
			//
			// lstStat
			//
			this.lstStat.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lstStat.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.lstStat.FormattingEnabled = true;
			this.lstStat.IntegralHeight = false;
			this.lstStat.Name = "lstStat";
			this.lstStat.TabIndex = 0;
			//
			// grdAddedIssues
			//
			this.grdAddedIssues.Caption = "Добавленные выпуски";
			this.grdAddedIssues.CaptionVisible = true;
			this.grdAddedIssues.ColumnNameHighlight = null;
			this.grdAddedIssues.DataSource = null;
			this.grdAddedIssues.DependantGrid = null;
			this.grdAddedIssues.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grdAddedIssues.Entity = null;
			this.grdAddedIssues.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grdAddedIssues.IsHighlightInvertColor = false;
			this.grdAddedIssues.IsNeedHighlight = false;
			this.grdAddedIssues.MenuEnabled = true;
			this.grdAddedIssues.Name = "grdAddedIssues";
			this.grdAddedIssues.QuickSearchVisible = false;
			this.grdAddedIssues.SelectedObject = null;
			this.grdAddedIssues.TabIndex = 0;
			//
			// splitContainer3
			//
			this.splitContainer3.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer3.Location = new System.Drawing.Point(0, 0);
			this.splitContainer3.Name = "splitContainer3";
			this.splitContainer3.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.splitContainer3.Panel1.Controls.Add(this.lstStat);
			this.splitContainer3.Panel2.Controls.Add(this.grdAddedIssues);
			this.splitContainer3.Size = new System.Drawing.Size(400, 500);
			this.splitContainer3.SplitterDistance = 200;
			this.splitContainer3.TabIndex = 0;
			//
			// splitContainer2
			//
			this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer2.Location = new System.Drawing.Point(0, 0);
			this.splitContainer2.Name = "splitContainer2";
			this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
			this.splitContainer2.Panel1.Controls.Add(this.grdRollers);
			this.splitContainer2.Panel2.Controls.Add(this.splitContainer3);
			this.splitContainer2.Size = new System.Drawing.Size(400, 700);
			this.splitContainer2.SplitterDistance = 200;
			this.splitContainer2.TabIndex = 0;
			//
			// comboModuleGrid
			//
			this.comboModuleGrid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.comboModuleGrid.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.comboModuleGrid.Location = new System.Drawing.Point(0, 0);
			this.comboModuleGrid.Name = "comboModuleGrid";
			this.comboModuleGrid.TabIndex = 0;
			//
			// splitContainer1
			//
			this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.splitContainer1.Location = new System.Drawing.Point(0, 25);
			this.splitContainer1.Name = "splitContainer1";
			this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
			this.splitContainer1.Panel2.Controls.Add(this.comboModuleGrid);
			this.splitContainer1.Size = new System.Drawing.Size(1200, 700);
			this.splitContainer1.SplitterDistance = 400;
			this.splitContainer1.TabIndex = 1;
			//
			// ComboModulePlacementForm
			//
			this.ClientSize = new System.Drawing.Size(1200, 725);
			this.Controls.Add(this.splitContainer1);
			this.Controls.Add(this.toolStrip1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.MinimizeBox = false;
			this.Name = "ComboModulePlacementForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Размещение комбо-модулями";
			this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
			this.toolStrip1.ResumeLayout(false);
			this.splitContainer1.Panel1.ResumeLayout(false);
			this.splitContainer1.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
			this.splitContainer1.ResumeLayout(false);
			this.splitContainer2.Panel1.ResumeLayout(false);
			this.splitContainer2.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
			this.splitContainer2.ResumeLayout(false);
			this.splitContainer3.Panel1.ResumeLayout(false);
			this.splitContainer3.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)(this.splitContainer3)).EndInit();
			this.splitContainer3.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();
		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton tbbRefresh;
		private System.Windows.Forms.ToolStripButton tbbStart;
		private System.Windows.Forms.ToolStripButton tbbJump;
		private System.Windows.Forms.ToolStripSeparator tbbSeparator0;
		private System.Windows.Forms.ToolStripButton tsbMuteRoller;
		private System.Windows.Forms.ToolStripButton tbbPlay;
		private System.Windows.Forms.ToolStripButton tsbStop;
		private System.Windows.Forms.ToolStripSeparator tbbSeparator0b;
		private System.Windows.Forms.ToolStripDropDownButton tbbPosition;
		private System.Windows.Forms.ToolStripMenuItem miPositionAny;
		private System.Windows.Forms.ToolStripMenuItem miPositionFirst;
		private System.Windows.Forms.ToolStripMenuItem miPositionSecond;
		private System.Windows.Forms.ToolStripMenuItem miPositionLast;
		private System.Windows.Forms.ToolStripDropDownButton tbbAdvertType;
		private System.Windows.Forms.ToolStripMenuItem miShowAllAdvertTypes;
		private System.Windows.Forms.ToolStripMenuItem miShowAdvertTypeExist;
		private System.Windows.Forms.ToolStripMenuItem miShowAdvertTypeNotExist;
		private System.Windows.Forms.ToolStripSeparator tbbSeparator1;
		private System.Windows.Forms.ToolStripDropDownButton tbbPeriodMode;
		private System.Windows.Forms.ToolStripMenuItem miPeriodWeek;
		private System.Windows.Forms.ToolStripMenuItem miPeriodMonth;
		private System.Windows.Forms.ToolStripSeparator tbbSeparator2;
		private System.Windows.Forms.ToolStripButton tbbShowUnconfirmed;
		private System.Windows.Forms.ToolStripSeparator tbbSeparator3;
		private System.Windows.Forms.ToolStripButton tbbExcel;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.SplitContainer splitContainer3;
		private FogSoft.WinForm.Controls.SmartGrid grdRollers;
		private System.Windows.Forms.ListBox lstStat;
		private FogSoft.WinForm.Controls.SmartGrid grdAddedIssues;
		private Merlin.Controls.ComboModuleGrid comboModuleGrid;
	}
}
