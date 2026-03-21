namespace FogSoft.WinForm.Passport.Forms {
  public partial class TabbedForm {
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
            this.pbFake = new System.Windows.Forms.PictureBox();
            this.tabPassport = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).BeginInit();
            this.tabPassport.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // pbFake
            // 
            this.pbFake.Image = global::FogSoft.WinForm.Properties.Resources.New;
            this.pbFake.Location = new System.Drawing.Point(3, 624);
            this.pbFake.Margin = new System.Windows.Forms.Padding(4);
            this.pbFake.Name = "pbFake";
            this.pbFake.Size = new System.Drawing.Size(48, 48);
            this.pbFake.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbFake.TabIndex = 13;
            this.pbFake.TabStop = false;
            this.pbFake.Visible = false;
            // 
            // tabPassport
            // 
            this.tabPassport.Controls.Add(this.tabPage1);
            this.tabPassport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabPassport.Location = new System.Drawing.Point(4, 4);
            this.tabPassport.Margin = new System.Windows.Forms.Padding(4);
            this.tabPassport.Name = "tabPassport";
            this.tabPassport.SelectedIndex = 0;
            this.tabPassport.Size = new System.Drawing.Size(797, 702);
            this.tabPassport.TabIndex = 14;
            // 
            // tabPage1
            // 
            this.tabPage1.Location = new System.Drawing.Point(4, 33);
            this.tabPage1.Margin = new System.Windows.Forms.Padding(4);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Size = new System.Drawing.Size(789, 665);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "tabPage1";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.Controls.Add(this.btnApply);
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnOk);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 713);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Padding = new System.Windows.Forms.Padding(5);
            this.flowLayoutPanel1.Size = new System.Drawing.Size(799, 51);
            this.flowLayoutPanel1.TabIndex = 15;
            this.flowLayoutPanel1.WrapContents = false;
            // 
            // btnApply
            // 
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApply.Enabled = false;
            this.btnApply.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnApply.Location = new System.Drawing.Point(669, 5);
            this.btnApply.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(120, 33);
            this.btnApply.TabIndex = 15;
            this.btnApply.Text = "Применить";
            this.btnApply.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(543, 5);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(120, 33);
            this.btnCancel.TabIndex = 14;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(417, 5);
            this.btnOk.Margin = new System.Windows.Forms.Padding(6, 0, 0, 0);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(120, 33);
            this.btnOk.TabIndex = 13;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tabPassport, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(805, 767);
            this.tableLayoutPanel1.TabIndex = 16;
            // 
            // TabbedForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(805, 767);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Controls.Add(this.pbFake);
            this.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(558, 680);
            this.Name = "TabbedForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "TabbedForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.tabPassport.ResumeLayout(false);
            this.flowLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.ResumeLayout(false);

    }

    #endregion
    protected System.Windows.Forms.PictureBox pbFake;
        protected System.Windows.Forms.TabControl tabPassport;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        protected System.Windows.Forms.Button btnApply;
        protected System.Windows.Forms.Button btnCancel;
        protected System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
    }
}