namespace Merlin.Forms
{
    partial class ActionFinalPriceForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.txtFinalPrice = new System.Windows.Forms.NumericUpDown();
            this.txtRatio = new System.Windows.Forms.NumericUpDown();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.dtCurrentDate = new System.Windows.Forms.DateTimePicker();
            this.chkCurrentDate = new System.Windows.Forms.CheckBox();
            this.rbRatio = new System.Windows.Forms.RadioButton();
            this.buttonPanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.txtFinalPrice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRatio)).BeginInit();
            this.tableLayoutPanelMain.SuspendLayout();
            this.buttonPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOK.Location = new System.Drawing.Point(450, 8);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(80, 32);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "Ок";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.BtnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(536, 8);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 32);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Отмена";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // txtFinalPrice
            // 
            this.txtFinalPrice.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFinalPrice.DecimalPlaces = 2;
            this.txtFinalPrice.Enabled = false;
            this.txtFinalPrice.Location = new System.Drawing.Point(285, 61);
            this.txtFinalPrice.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtFinalPrice.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.txtFinalPrice.Name = "txtFinalPrice";
            this.txtFinalPrice.Size = new System.Drawing.Size(328, 31);
            this.txtFinalPrice.TabIndex = 14;
            // 
            // txtRatio
            // 
            this.txtRatio.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtRatio.DecimalPlaces = 2;
            this.txtRatio.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.txtRatio.Location = new System.Drawing.Point(285, 18);
            this.txtRatio.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtRatio.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.txtRatio.Name = "txtRatio";
            this.txtRatio.Size = new System.Drawing.Size(328, 31);
            this.txtRatio.TabIndex = 22;
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.AutoSize = true;
            this.tableLayoutPanelMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelMain.ColumnCount = 2;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelMain.Controls.Add(this.radioButton2, 0, 1);
            this.tableLayoutPanelMain.Controls.Add(this.txtRatio, 1, 0);
            this.tableLayoutPanelMain.Controls.Add(this.txtFinalPrice, 1, 1);
            this.tableLayoutPanelMain.Controls.Add(this.dtCurrentDate, 1, 2);
            this.tableLayoutPanelMain.Controls.Add(this.chkCurrentDate, 0, 2);
            this.tableLayoutPanelMain.Controls.Add(this.rbRatio, 0, 0);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelMain.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.Padding = new System.Windows.Forms.Padding(12);
            this.tableLayoutPanelMain.RowCount = 4;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(628, 157);
            this.tableLayoutPanelMain.TabIndex = 23;
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(15, 58);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(207, 29);
            this.radioButton2.TabIndex = 26;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "Итоговая стоимость:";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.CheckedChanged);
            // 
            // dtCurrentDate
            // 
            this.dtCurrentDate.Enabled = false;
            this.dtCurrentDate.Location = new System.Drawing.Point(285, 101);
            this.dtCurrentDate.Name = "dtCurrentDate";
            this.dtCurrentDate.Size = new System.Drawing.Size(171, 31);
            this.dtCurrentDate.TabIndex = 23;
            // 
            // chkCurrentDate
            // 
            this.chkCurrentDate.AutoSize = true;
            this.chkCurrentDate.Location = new System.Drawing.Point(15, 101);
            this.chkCurrentDate.Name = "chkCurrentDate";
            this.chkCurrentDate.Size = new System.Drawing.Size(264, 29);
            this.chkCurrentDate.TabIndex = 24;
            this.chkCurrentDate.Text = "Изменить цену в прошлом:";
            this.chkCurrentDate.UseVisualStyleBackColor = true;
            this.chkCurrentDate.CheckedChanged += new System.EventHandler(this.chkCurrentDate_CheckedChanged);
            // 
            // rbRatio
            // 
            this.rbRatio.AutoSize = true;
            this.rbRatio.Checked = true;
            this.rbRatio.Location = new System.Drawing.Point(15, 15);
            this.rbRatio.Name = "rbRatio";
            this.rbRatio.Size = new System.Drawing.Size(223, 29);
            this.rbRatio.TabIndex = 25;
            this.rbRatio.TabStop = true;
            this.rbRatio.Text = "Менеджерская скидка:";
            this.rbRatio.UseVisualStyleBackColor = true;
            this.rbRatio.CheckedChanged += new System.EventHandler(this.CheckedChanged);
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnOK);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(0, 157);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 8, 12, 12);
            this.buttonPanel.Size = new System.Drawing.Size(628, 52);
            this.buttonPanel.TabIndex = 24;
            // 
            // ActionFinalPriceForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(628, 209);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Controls.Add(this.buttonPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ActionFinalPriceForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Итоговая стоимость";
            ((System.ComponentModel.ISupportInitialize)(this.txtFinalPrice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRatio)).EndInit();
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelMain.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.NumericUpDown txtFinalPrice;
        private System.Windows.Forms.NumericUpDown txtRatio;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.DateTimePicker dtCurrentDate;
        private System.Windows.Forms.CheckBox chkCurrentDate;
        private System.Windows.Forms.RadioButton rbRatio;
        private System.Windows.Forms.RadioButton radioButton2;
    }
}
