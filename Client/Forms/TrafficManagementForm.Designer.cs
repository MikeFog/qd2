using Merlin.Controls;

namespace Merlin.Forms {
	partial class TrafficManagementForm {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private void InitializeComponent() {
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tbbRefresh = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.tsbJump = new System.Windows.Forms.ToolStripButton();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.chkDate = new System.Windows.Forms.CheckBox();
            this.chkFixed = new System.Windows.Forms.CheckBox();
            this.grdMassmedia = new FogSoft.WinForm.Controls.SmartGrid();
            this.cmbRadioStationGroup = new FogSoft.WinForm.LookUp();
            this.grdSelectedCellIssues = new FogSoft.WinForm.Controls.SmartGrid();
            this.grdTariffWindow = new Merlin.Controls.TrafficGrid();
            this.toolStrip1.SuspendLayout();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbbRefresh,
            this.toolStripSeparator1,
            this.tsbJump});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(2223, 57);
            this.toolStrip1.TabIndex = 0;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tbbRefresh
            // 
            this.tbbRefresh.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tbbRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbRefresh.Name = "tbbRefresh";
            this.tbbRefresh.Size = new System.Drawing.Size(34, 52);
            this.tbbRefresh.ToolTipText = "Обновить информацию";
            this.tbbRefresh.Click += new System.EventHandler(this.tbbRefresh_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 57);
            // 
            // tsbJump
            // 
            this.tsbJump.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.tsbJump.Image = global::Merlin.Properties.Resources.calendar;
            this.tsbJump.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tsbJump.Name = "tsbJump";
            this.tsbJump.Size = new System.Drawing.Size(34, 52);
            this.tsbJump.Text = "Переход к дате";
            this.tsbJump.ToolTipText = "Переход к выбранной дате";
            this.tsbJump.Click += new System.EventHandler(this.tsbJump_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 86);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.grdTariffWindow);
            this.splitContainer1.Size = new System.Drawing.Size(2223, 1257);
            this.splitContainer1.SplitterDistance = 694;
            this.splitContainer1.TabIndex = 1;
            this.splitContainer1.Text = "splitContainer1";
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.grdMassmedia);
            this.splitContainer2.Panel1.Controls.Add(this.cmbRadioStationGroup);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.grdSelectedCellIssues);
            this.splitContainer2.Panel2.Controls.Add(this.chkDate);
            this.splitContainer2.Size = new System.Drawing.Size(694, 1257);
            this.splitContainer2.SplitterDistance = 685;
            this.splitContainer2.TabIndex = 0;
            this.splitContainer2.Text = "splitContainer2";
            // 
            // chkDate
            // 
            this.chkDate.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkDate.AutoSize = true;
            this.chkDate.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkDate.Dock = System.Windows.Forms.DockStyle.Top;
            this.chkDate.Location = new System.Drawing.Point(0, 0);
            this.chkDate.Name = "chkDate";
            this.chkDate.Size = new System.Drawing.Size(694, 23);
            this.chkDate.TabIndex = 0;
            this.chkDate.Text = " ";
            // 
            // chkFixed
            // 
            this.chkFixed.Appearance = System.Windows.Forms.Appearance.Button;
            this.chkFixed.AutoSize = true;
            this.chkFixed.CheckAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.chkFixed.Dock = System.Windows.Forms.DockStyle.Top;
            this.chkFixed.Location = new System.Drawing.Point(0, 0);
            this.chkFixed.Name = "chkFixed";
            this.chkFixed.Size = new System.Drawing.Size(227, 23);
            this.chkFixed.TabIndex = 0;
            this.chkFixed.Text = " ";
            // 
            // grdMassmedia
            // 
            this.grdMassmedia.Caption = "Радиостанции";
            this.grdMassmedia.CaptionVisible = true;
            this.grdMassmedia.CheckBoxes = false;
            this.grdMassmedia.ColumnNameHighlight = null;
            this.grdMassmedia.DataSource = null;
            this.grdMassmedia.DependantGrid = null;
            this.grdMassmedia.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdMassmedia.Entity = null;
            this.grdMassmedia.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdMassmedia.IsHighlightInvertColor = false;
            this.grdMassmedia.IsNeedHighlight = false;
            this.grdMassmedia.Location = new System.Drawing.Point(0, 29);
            this.grdMassmedia.MenuEnabled = false;
            this.grdMassmedia.Name = "grdMassmedia";
            this.grdMassmedia.QuickSearchVisible = false;
            this.grdMassmedia.SelectedObject = null;
            this.grdMassmedia.Size = new System.Drawing.Size(1041, 984);
            this.grdMassmedia.TabIndex = 18;
            // 
            // cmbRadioStationGroup
            // 
            this.cmbRadioStationGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbRadioStationGroup.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbRadioStationGroup.IsNullable = false;
            this.cmbRadioStationGroup.Location = new System.Drawing.Point(0, 0);
            this.cmbRadioStationGroup.Name = "cmbRadioStationGroup";
            this.cmbRadioStationGroup.SelectedIndex = -1;
            this.cmbRadioStationGroup.SelectedValue = null;
            this.cmbRadioStationGroup.Size = new System.Drawing.Size(694, 29);
            this.cmbRadioStationGroup.TabIndex = 17;
            this.cmbRadioStationGroup.SelectedItemChanged += new System.EventHandler(this.cmbRadioStationGroup_SelectedItemChanged);
            // 
            // grdSelectedCellIssues
            // 
            this.grdSelectedCellIssues.Caption = "Выпуски";
            this.grdSelectedCellIssues.CaptionVisible = true;
            this.grdSelectedCellIssues.CheckBoxes = false;
            this.grdSelectedCellIssues.ColumnNameHighlight = null;
            this.grdSelectedCellIssues.DataSource = null;
            this.grdSelectedCellIssues.DependantGrid = null;
            this.grdSelectedCellIssues.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdSelectedCellIssues.Entity = null;
            this.grdSelectedCellIssues.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdSelectedCellIssues.IsHighlightInvertColor = false;
            this.grdSelectedCellIssues.IsNeedHighlight = false;
            this.grdSelectedCellIssues.Location = new System.Drawing.Point(0, 23);
            this.grdSelectedCellIssues.MenuEnabled = false;
            this.grdSelectedCellIssues.Name = "grdSelectedCellIssues";
            this.grdSelectedCellIssues.QuickSearchVisible = false;
            this.grdSelectedCellIssues.SelectedObject = null;
            this.grdSelectedCellIssues.Size = new System.Drawing.Size(694, 545);
            this.grdSelectedCellIssues.TabIndex = 1;
            // 
            // grdTariffWindow
            // 
            this.grdTariffWindow.Campaign = null;
            this.grdTariffWindow.CurrentDate = new System.DateTime(2006, 6, 17, 0, 0, 0, 0);
            this.grdTariffWindow.Cursor = System.Windows.Forms.Cursors.Default;
            this.grdTariffWindow.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdTariffWindow.EditMode = Merlin.Controls.EditMode.View;
            this.grdTariffWindow.ExcludeSpecialTariffs = false;
            this.grdTariffWindow.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdTariffWindow.Grantor = null;
            this.grdTariffWindow.Location = new System.Drawing.Point(0, 0);
            this.grdTariffWindow.Massmedia = null;
            this.grdTariffWindow.Module = null;
            this.grdTariffWindow.Name = "grdTariffWindow";
            this.grdTariffWindow.Pricelist = null;
            this.grdTariffWindow.Roller = null;
            this.grdTariffWindow.RollerPosition = Merlin.RollerPositions.Undefined;
            this.grdTariffWindow.ShowTrafficWindows = false;
            this.grdTariffWindow.ShowUnconfirmed = false;
            this.grdTariffWindow.Size = new System.Drawing.Size(1525, 1257);
            this.grdTariffWindow.TabIndex = 0;
            // 
            // TrafficManagementForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1482, 895);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Name = "TrafficManagementForm";
            this.Text = "Трафик-менеджмент";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.SplitContainer splitContainer1;
		private System.Windows.Forms.SplitContainer splitContainer2;
		private System.Windows.Forms.CheckBox chkDate;
		private FogSoft.WinForm.Controls.SmartGrid grdSelectedCellIssues;
		private System.Windows.Forms.ToolStripButton tbbRefresh;
    private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
    private System.Windows.Forms.CheckBox chkFixed;
		private TrafficGrid grdTariffWindow;
		private System.Windows.Forms.ToolStripButton tsbJump;
        private FogSoft.WinForm.LookUp cmbRadioStationGroup;
        private FogSoft.WinForm.Controls.SmartGrid grdMassmedia;
    }
}