namespace Merlin.Forms
{
    partial class ManagerDiscountForm
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
            this.label1 = new System.Windows.Forms.Label();
            this.lblTariffPrice = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lblPackDiscount = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.txtRatio = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.txtFinalPrice = new System.Windows.Forms.NumericUpDown();
            this.chkCurrentDate = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanelMain = new System.Windows.Forms.TableLayoutPanel();
            this.dtCurrentDate = new System.Windows.Forms.DateTimePicker();
            this.buttonPanel = new System.Windows.Forms.Panel();
            ((System.ComponentModel.ISupportInitialize)(this.txtRatio)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFinalPrice)).BeginInit();
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
            this.btnOK.Click += new System.EventHandler(this.btnOk_Click);
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
            // label1
            // 
            this.label1.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 18);
            this.label1.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(159, 25);
            this.label1.TabIndex = 0;
            this.label1.Text = "Цена по тарифам:";
            // 
            // lblTariffPrice
            // 
            this.lblTariffPrice.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblTariffPrice.AutoSize = true;
            this.lblTariffPrice.Location = new System.Drawing.Point(339, 18);
            this.lblTariffPrice.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.lblTariffPrice.Name = "lblTariffPrice";
            this.lblTariffPrice.Size = new System.Drawing.Size(46, 25);
            this.lblTariffPrice.TabIndex = 1;
            this.lblTariffPrice.Text = "0,00";
            // 
            // label2
            // 
            this.label2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(15, 55);
            this.label2.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 25);
            this.label2.TabIndex = 2;
            this.label2.Text = "Скидка:";
            // 
            // lblDiscount
            // 
            this.lblDiscount.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDiscount.AutoSize = true;
            this.lblDiscount.Location = new System.Drawing.Point(339, 55);
            this.lblDiscount.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.lblDiscount.Name = "lblDiscount";
            this.lblDiscount.Size = new System.Drawing.Size(46, 25);
            this.lblDiscount.TabIndex = 3;
            this.lblDiscount.Text = "0,00";
            // 
            // label6
            // 
            this.label6.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(15, 92);
            this.label6.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(151, 25);
            this.label6.TabIndex = 4;
            this.label6.Text = "Пакетная скидка:";
            // 
            // lblPackDiscount
            // 
            this.lblPackDiscount.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPackDiscount.AutoSize = true;
            this.lblPackDiscount.Location = new System.Drawing.Point(339, 92);
            this.lblPackDiscount.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.lblPackDiscount.Name = "lblPackDiscount";
            this.lblPackDiscount.Size = new System.Drawing.Size(46, 25);
            this.lblPackDiscount.TabIndex = 5;
            this.lblPackDiscount.Text = "0,00";
            // 
            // label3
            // 
            this.label3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(15, 132);
            this.label3.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(198, 25);
            this.label3.TabIndex = 6;
            this.label3.Text = "Менеджерская скидка:";
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
            this.txtRatio.Location = new System.Drawing.Point(339, 129);
            this.txtRatio.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtRatio.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.txtRatio.Name = "txtRatio";
            this.txtRatio.Size = new System.Drawing.Size(274, 31);
            this.txtRatio.TabIndex = 7;
            this.txtRatio.Leave += new System.EventHandler(this.txtRatio_Leave);
            // 
            // label4
            // 
            this.label4.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(15, 175);
            this.label4.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(185, 25);
            this.label4.TabIndex = 8;
            this.label4.Text = "Окончательная цена:";
            // 
            // txtFinalPrice
            // 
            this.txtFinalPrice.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFinalPrice.DecimalPlaces = 2;
            this.txtFinalPrice.Location = new System.Drawing.Point(339, 172);
            this.txtFinalPrice.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.txtFinalPrice.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.txtFinalPrice.Name = "txtFinalPrice";
            this.txtFinalPrice.Size = new System.Drawing.Size(274, 31);
            this.txtFinalPrice.TabIndex = 9;
            this.txtFinalPrice.Leave += new System.EventHandler(this.txtFinalPrice_Leave);
            // 
            // chkCurrentDate
            // 
            this.chkCurrentDate.AutoSize = true;
            this.chkCurrentDate.Location = new System.Drawing.Point(15, 212);
            this.chkCurrentDate.Name = "chkCurrentDate";
            this.chkCurrentDate.Size = new System.Drawing.Size(318, 29);
            this.chkCurrentDate.TabIndex = 10;
            this.chkCurrentDate.Text = "Изменить цену акции в прошлом:";
            this.chkCurrentDate.UseVisualStyleBackColor = true;
            this.chkCurrentDate.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // tableLayoutPanelMain
            // 
            this.tableLayoutPanelMain.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tableLayoutPanelMain.ColumnCount = 2;
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelMain.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanelMain.Controls.Add(this.label1, 0, 0);
            this.tableLayoutPanelMain.Controls.Add(this.lblTariffPrice, 1, 0);
            this.tableLayoutPanelMain.Controls.Add(this.label2, 0, 1);
            this.tableLayoutPanelMain.Controls.Add(this.lblDiscount, 1, 1);
            this.tableLayoutPanelMain.Controls.Add(this.label6, 0, 2);
            this.tableLayoutPanelMain.Controls.Add(this.lblPackDiscount, 1, 2);
            this.tableLayoutPanelMain.Controls.Add(this.label3, 0, 3);
            this.tableLayoutPanelMain.Controls.Add(this.txtRatio, 1, 3);
            this.tableLayoutPanelMain.Controls.Add(this.label4, 0, 4);
            this.tableLayoutPanelMain.Controls.Add(this.txtFinalPrice, 1, 4);
            this.tableLayoutPanelMain.Controls.Add(this.chkCurrentDate, 0, 5);
            this.tableLayoutPanelMain.Controls.Add(this.dtCurrentDate, 1, 5);
            this.tableLayoutPanelMain.Dock = System.Windows.Forms.DockStyle.Top;
            this.tableLayoutPanelMain.GrowStyle = System.Windows.Forms.TableLayoutPanelGrowStyle.FixedSize;
            this.tableLayoutPanelMain.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanelMain.Name = "tableLayoutPanelMain";
            this.tableLayoutPanelMain.Padding = new System.Windows.Forms.Padding(12);
            this.tableLayoutPanelMain.RowCount = 7;
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelMain.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 3F));
            this.tableLayoutPanelMain.Size = new System.Drawing.Size(628, 254);
            this.tableLayoutPanelMain.TabIndex = 12;
            // 
            // dtCurrentDate
            // 
            this.dtCurrentDate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.dtCurrentDate.Enabled = false;
            this.dtCurrentDate.Location = new System.Drawing.Point(339, 212);
            this.dtCurrentDate.Name = "dtCurrentDate";
            this.dtCurrentDate.Size = new System.Drawing.Size(274, 31);
            this.dtCurrentDate.TabIndex = 11;
            // 
            // buttonPanel
            // 
            this.buttonPanel.Controls.Add(this.btnCancel);
            this.buttonPanel.Controls.Add(this.btnOK);
            this.buttonPanel.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.buttonPanel.Location = new System.Drawing.Point(0, 252);
            this.buttonPanel.Name = "buttonPanel";
            this.buttonPanel.Padding = new System.Windows.Forms.Padding(0, 8, 12, 12);
            this.buttonPanel.Size = new System.Drawing.Size(628, 52);
            this.buttonPanel.TabIndex = 13;
            // 
            // ManagerDiscountForm
            // 
            this.AcceptButton = this.btnOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(628, 304);
            this.Controls.Add(this.tableLayoutPanelMain);
            this.Controls.Add(this.buttonPanel);
            this.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManagerDiscountForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Менеджерская скидка";
            ((System.ComponentModel.ISupportInitialize)(this.txtRatio)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtFinalPrice)).EndInit();
            this.tableLayoutPanelMain.ResumeLayout(false);
            this.tableLayoutPanelMain.PerformLayout();
            this.buttonPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblTariffPrice;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblDiscount;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblPackDiscount;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown txtRatio;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.NumericUpDown txtFinalPrice;
        private System.Windows.Forms.CheckBox chkCurrentDate;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelMain;
        private System.Windows.Forms.Panel buttonPanel;
        private System.Windows.Forms.DateTimePicker dtCurrentDate;
    }
}
