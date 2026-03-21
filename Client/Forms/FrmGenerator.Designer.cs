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
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.grdSuccess = new FogSoft.WinForm.Controls.SmartGrid();
            this.grdFail = new FogSoft.WinForm.Controls.SmartGrid();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnOk = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbbExcel,
            this.pbProgress});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(764, 32);
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
            this.pbProgress.Size = new System.Drawing.Size(300, 29);
            this.pbProgress.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.grdFail, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.grdSuccess, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 2);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 32);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(764, 755);
            this.tableLayoutPanel1.TabIndex = 6;
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
            this.grdSuccess.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdSuccess.IsHighlightInvertColor = false;
            this.grdSuccess.IsNeedHighlight = false;
            this.grdSuccess.Location = new System.Drawing.Point(4, 3);
            this.grdSuccess.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdSuccess.MenuEnabled = true;
            this.grdSuccess.Name = "grdSuccess";
            this.grdSuccess.QuickSearchVisible = false;
            this.grdSuccess.SelectedObject = null;
            this.grdSuccess.ShowMultiselectColumn = true;
            this.grdSuccess.Size = new System.Drawing.Size(756, 420);
            this.grdSuccess.TabIndex = 4;
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
            this.grdFail.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdFail.IsHighlightInvertColor = false;
            this.grdFail.IsNeedHighlight = false;
            this.grdFail.Location = new System.Drawing.Point(4, 429);
            this.grdFail.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdFail.MenuEnabled = true;
            this.grdFail.Name = "grdFail";
            this.grdFail.QuickSearchVisible = false;
            this.grdFail.SelectedObject = null;
            this.grdFail.ShowMultiselectColumn = true;
            this.grdFail.Size = new System.Drawing.Size(756, 278);
            this.grdFail.TabIndex = 5;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.btnOk);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 713);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(758, 39);
            this.flowLayoutPanel1.TabIndex = 6;
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(655, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 33);
            this.btnOk.TabIndex = 6;
            this.btnOk.Text = "Ок";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // FrmGenerator
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(764, 787);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.toolStrip1);
            this.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmGenerator";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Внесение роликов по шаблону";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tbbExcel;
        private System.Windows.Forms.ToolStripProgressBar pbProgress;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private FogSoft.WinForm.Controls.SmartGrid grdFail;
        private FogSoft.WinForm.Controls.SmartGrid grdSuccess;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnOk;
    }
}