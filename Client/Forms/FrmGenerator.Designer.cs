namespace Merlin.Forms
{
    partial class FrmGenerator
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
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.tbbExcel = new System.Windows.Forms.ToolStripButton();
            this.pbProgress = new System.Windows.Forms.ToolStripProgressBar();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.grdSuccess = new FogSoft.WinForm.Controls.SmartGrid();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.grdFail = new FogSoft.WinForm.Controls.SmartGrid();
            this.btnOk = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
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
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbbExcel,
            this.pbProgress});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(767, 34);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tbbExcel
            // 
            this.tbbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbExcel.Name = "tbbExcel";
            this.tbbExcel.Size = new System.Drawing.Size(83, 29);
            this.tbbExcel.Text = "Экспорт";
            this.tbbExcel.Click += new System.EventHandler(this.tbbExcel_Click);
            // 
            // pbProgress
            // 
            this.pbProgress.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.pbProgress.AutoSize = false;
            this.pbProgress.Margin = new System.Windows.Forms.Padding(1, 2, 5, 1);
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(300, 11);
            this.pbProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 34);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.grdSuccess);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.splitContainer2);
            this.splitContainer1.Size = new System.Drawing.Size(767, 705);
            this.splitContainer1.SplitterDistance = 400;
            this.splitContainer1.TabIndex = 5;
            // 
            // grdSuccess
            // 
            this.grdSuccess.Caption = "Добавленные выпуски";
            this.grdSuccess.CaptionVisible = true;
            this.grdSuccess.CheckBoxes = false;
            this.grdSuccess.ColumnNameHighlight = null;
            this.grdSuccess.DataSource = null;
            this.grdSuccess.DependantGrid = null;
            this.grdSuccess.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdSuccess.Entity = null;
            this.grdSuccess.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdSuccess.IsHighlightInvertColor = false;
            this.grdSuccess.IsNeedHighlight = false;
            this.grdSuccess.Location = new System.Drawing.Point(0, 0);
            this.grdSuccess.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdSuccess.MenuEnabled = true;
            this.grdSuccess.Name = "grdSuccess";
            this.grdSuccess.QuickSearchVisible = false;
            this.grdSuccess.SelectedObject = null;
            this.grdSuccess.ShowMultiselectColumn = true;
            this.grdSuccess.Size = new System.Drawing.Size(767, 400);
            this.grdSuccess.TabIndex = 3;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.FixedPanel = System.Windows.Forms.FixedPanel.Panel2;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            this.splitContainer2.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.grdFail);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.btnOk);
            this.splitContainer2.Size = new System.Drawing.Size(767, 301);
            this.splitContainer2.SplitterDistance = 239;
            this.splitContainer2.TabIndex = 0;
            // 
            // grdFail
            // 
            this.grdFail.Caption = "Ошибки (не добавлены в сетку вещания)";
            this.grdFail.CaptionVisible = true;
            this.grdFail.CheckBoxes = false;
            this.grdFail.ColumnNameHighlight = null;
            this.grdFail.DataSource = null;
            this.grdFail.DependantGrid = null;
            this.grdFail.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdFail.Entity = null;
            this.grdFail.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdFail.IsHighlightInvertColor = false;
            this.grdFail.IsNeedHighlight = false;
            this.grdFail.Location = new System.Drawing.Point(0, 0);
            this.grdFail.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdFail.MenuEnabled = true;
            this.grdFail.Name = "grdFail";
            this.grdFail.QuickSearchVisible = false;
            this.grdFail.SelectedObject = null;
            this.grdFail.ShowMultiselectColumn = true;
            this.grdFail.Size = new System.Drawing.Size(767, 239);
            this.grdFail.TabIndex = 4;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(680, 23);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 5;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // FrmGenerator
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(767, 739);
            this.Controls.Add(this.splitContainer1);
            this.Controls.Add(this.toolStrip1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmGenerator";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Внесение роликов по шаблону";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tbbExcel;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.ToolStripProgressBar pbProgress;
        private FogSoft.WinForm.Controls.SmartGrid grdSuccess;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private FogSoft.WinForm.Controls.SmartGrid grdFail;
        private System.Windows.Forms.Button btnOk;
    }
}