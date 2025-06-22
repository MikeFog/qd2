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
            this.grdSuccess = new FogSoft.WinForm.Controls.SmartGrid();
            this.grdFail = new FogSoft.WinForm.Controls.SmartGrid();
            this.btnOk = new System.Windows.Forms.Button();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // toolStrip1
            // 
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbbExcel,
            this.pbProgress});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(574, 25);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // tbbExcel
            // 
            this.tbbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.tbbExcel.Name = "tbbExcel";
            this.tbbExcel.Size = new System.Drawing.Size(56, 22);
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
            // grdSuccess
            // 
            this.grdSuccess.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdSuccess.Caption = "Добавленные выпуски";
            this.grdSuccess.CaptionVisible = true;
            this.grdSuccess.CheckBoxes = false;
            this.grdSuccess.ColumnNameHighlight = null;
            this.grdSuccess.DataSource = null;
            this.grdSuccess.DependantGrid = null;
            this.grdSuccess.Entity = null;
            this.grdSuccess.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdSuccess.IsHighlightInvertColor = false;
            this.grdSuccess.IsNeedHighlight = false;
            this.grdSuccess.Location = new System.Drawing.Point(1, 28);
            this.grdSuccess.MenuEnabled = true;
            this.grdSuccess.Name = "grdSuccess";
            this.grdSuccess.QuickSearchVisible = false;
            this.grdSuccess.SelectedObject = null;
            this.grdSuccess.Size = new System.Drawing.Size(573, 207);
            this.grdSuccess.TabIndex = 2;
            // 
            // grdFail
            // 
            this.grdFail.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdFail.Caption = "Ошибки (не добавлены в сетку вещания)";
            this.grdFail.CaptionVisible = true;
            this.grdFail.CheckBoxes = false;
            this.grdFail.ColumnNameHighlight = null;
            this.grdFail.DataSource = null;
            this.grdFail.DependantGrid = null;
            this.grdFail.Entity = null;
            this.grdFail.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdFail.IsHighlightInvertColor = false;
            this.grdFail.IsNeedHighlight = false;
            this.grdFail.Location = new System.Drawing.Point(1, 237);
            this.grdFail.MenuEnabled = true;
            this.grdFail.Name = "grdFail";
            this.grdFail.QuickSearchVisible = false;
            this.grdFail.SelectedObject = null;
            this.grdFail.Size = new System.Drawing.Size(573, 217);
            this.grdFail.TabIndex = 3;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(320, 283);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(75, 23);
            this.btnOk.TabIndex = 4;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // FrmGenerator
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.CancelButton = this.btnOk;
            this.ClientSize = new System.Drawing.Size(574, 454);
            this.Controls.Add(this.grdFail);
            this.Controls.Add(this.grdSuccess);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.btnOk);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmGenerator";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Внесение роликов по шаблону";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.ToolStripButton tbbExcel;
        private System.Windows.Forms.ToolStripProgressBar pbProgress;
			private FogSoft.WinForm.Controls.SmartGrid grdSuccess;
			private FogSoft.WinForm.Controls.SmartGrid grdFail;
        private System.Windows.Forms.Button btnOk;

    }
}