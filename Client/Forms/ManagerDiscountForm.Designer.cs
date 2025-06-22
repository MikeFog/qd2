namespace Merlin.Forms
{
	partial class ManagerDiscountForm
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
			if(disposing && (components != null))
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.txtFinalPrice = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblTariffPrice = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.txtRatio = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lblPackDiscount = new System.Windows.Forms.Label();
            this.panel = new System.Windows.Forms.Panel();
            this.chkCurrentDate = new System.Windows.Forms.CheckBox();
            this.dtCurrentDate = new System.Windows.Forms.DateTimePicker();
            ((System.ComponentModel.ISupportInitialize)(this.txtFinalPrice)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRatio)).BeginInit();
            this.panel.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(276, 185);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(112, 34);
            this.btnCancel.TabIndex = 15;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Location = new System.Drawing.Point(154, 185);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(112, 34);
            this.btnOk.TabIndex = 14;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // txtFinalPrice
            // 
            this.txtFinalPrice.DecimalPlaces = 2;
            this.txtFinalPrice.Location = new System.Drawing.Point(226, 59);
            this.txtFinalPrice.Margin = new System.Windows.Forms.Padding(4);
            this.txtFinalPrice.Maximum = new decimal(new int[] {
            9999999,
            0,
            0,
            0});
            this.txtFinalPrice.Name = "txtFinalPrice";
            this.txtFinalPrice.Size = new System.Drawing.Size(324, 31);
            this.txtFinalPrice.TabIndex = 13;
            this.txtFinalPrice.ValueChanged += new System.EventHandler(this.txtFinalPrice_TextChanged);
            this.txtFinalPrice.Leave += new System.EventHandler(this.txtFinalPrice_Leave);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(20, 65);
            this.label4.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(185, 25);
            this.label4.TabIndex = 12;
            this.label4.Text = "Окончательная цена:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(20, 22);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(198, 25);
            this.label3.TabIndex = 10;
            this.label3.Text = "Менеджерская скидка:";
            // 
            // lblTariffPrice
            // 
            this.lblTariffPrice.AutoSize = true;
            this.lblTariffPrice.Location = new System.Drawing.Point(222, 14);
            this.lblTariffPrice.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblTariffPrice.Name = "lblTariffPrice";
            this.lblTariffPrice.Size = new System.Drawing.Size(57, 25);
            this.lblTariffPrice.TabIndex = 9;
            this.lblTariffPrice.Text = "0.00р";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(18, 14);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(159, 25);
            this.label1.TabIndex = 8;
            this.label1.Text = "Цена по тарифам:";
            // 
            // txtRatio
            // 
            this.txtRatio.DecimalPlaces = 2;
            this.txtRatio.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.txtRatio.Location = new System.Drawing.Point(226, 16);
            this.txtRatio.Margin = new System.Windows.Forms.Padding(4);
            this.txtRatio.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.txtRatio.Name = "txtRatio";
            this.txtRatio.Size = new System.Drawing.Size(324, 31);
            this.txtRatio.TabIndex = 11;
            this.txtRatio.ValueChanged += new System.EventHandler(this.txtRatio_TextChanged);
            this.txtRatio.Leave += new System.EventHandler(this.txtRatio_Leave);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(18, 54);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(74, 25);
            this.label2.TabIndex = 16;
            this.label2.Text = "Скидка:";
            // 
            // lblDiscount
            // 
            this.lblDiscount.AutoSize = true;
            this.lblDiscount.Location = new System.Drawing.Point(222, 54);
            this.lblDiscount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDiscount.Name = "lblDiscount";
            this.lblDiscount.Size = new System.Drawing.Size(46, 25);
            this.lblDiscount.TabIndex = 17;
            this.lblDiscount.Text = "0,00";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(18, 94);
            this.label6.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(151, 25);
            this.label6.TabIndex = 18;
            this.label6.Text = "Пакетная скидка:";
            // 
            // lblPackDiscount
            // 
            this.lblPackDiscount.AutoSize = true;
            this.lblPackDiscount.Location = new System.Drawing.Point(222, 94);
            this.lblPackDiscount.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblPackDiscount.Name = "lblPackDiscount";
            this.lblPackDiscount.Size = new System.Drawing.Size(46, 25);
            this.lblPackDiscount.TabIndex = 19;
            this.lblPackDiscount.Text = "0,00";
            // 
            // panel
            // 
            this.panel.Controls.Add(this.chkCurrentDate);
            this.panel.Controls.Add(this.dtCurrentDate);
            this.panel.Controls.Add(this.label3);
            this.panel.Controls.Add(this.txtRatio);
            this.panel.Controls.Add(this.label4);
            this.panel.Controls.Add(this.txtFinalPrice);
            this.panel.Controls.Add(this.btnOk);
            this.panel.Controls.Add(this.btnCancel);
            this.panel.Location = new System.Drawing.Point(0, 138);
            this.panel.Margin = new System.Windows.Forms.Padding(4);
            this.panel.Name = "panel";
            this.panel.Size = new System.Drawing.Size(568, 229);
            this.panel.TabIndex = 20;
            // 
            // chkCurrentDate
            // 
            this.chkCurrentDate.AutoSize = true;
            this.chkCurrentDate.Location = new System.Drawing.Point(20, 100);
            this.chkCurrentDate.Name = "chkCurrentDate";
            this.chkCurrentDate.Size = new System.Drawing.Size(411, 29);
            this.chkCurrentDate.TabIndex = 18;
            this.chkCurrentDate.Text = "Изменить цену акции в прошедшем периоде";
            this.chkCurrentDate.UseVisualStyleBackColor = true;
            this.chkCurrentDate.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // dtCurrentDate
            // 
            this.dtCurrentDate.Enabled = false;
            this.dtCurrentDate.Location = new System.Drawing.Point(226, 134);
            this.dtCurrentDate.Name = "dtCurrentDate";
            this.dtCurrentDate.Size = new System.Drawing.Size(324, 31);
            this.dtCurrentDate.TabIndex = 16;
            // 
            // ManagerDiscountForm
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(568, 373);
            this.Controls.Add(this.panel);
            this.Controls.Add(this.lblPackDiscount);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.lblDiscount);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.lblTariffPrice);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ManagerDiscountForm";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Менеджерская скидка";
            ((System.ComponentModel.ISupportInitialize)(this.txtFinalPrice)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtRatio)).EndInit();
            this.panel.ResumeLayout(false);
            this.panel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.Button btnOk;
		private System.Windows.Forms.NumericUpDown txtFinalPrice;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label lblTariffPrice;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown txtRatio;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblDiscount;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblPackDiscount;
        private System.Windows.Forms.Panel panel;
        private System.Windows.Forms.DateTimePicker dtCurrentDate;
        private System.Windows.Forms.CheckBox chkCurrentDate;
    }
}