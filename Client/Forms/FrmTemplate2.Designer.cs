
namespace Merlin.Forms
{
    partial class FrmTemplate2
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
            this.dtFinishDate = new System.Windows.Forms.DateTimePicker();
            this.dtStartDate = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.dtFinishTime = new System.Windows.Forms.DateTimePicker();
            this.dtStartTime = new System.Windows.Forms.DateTimePicker();
            this.txtQuantity = new System.Windows.Forms.NumericUpDown();
            this.label5 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.lblRollerName = new System.Windows.Forms.Label();
            this.gbWeekDays = new System.Windows.Forms.GroupBox();
            this.rbDays = new System.Windows.Forms.RadioButton();
            this.clbWeekDays = new System.Windows.Forms.CheckedListBox();
            this.cbIgnoreWindows = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.numQuantityPrime = new System.Windows.Forms.NumericUpDown();
            this.numQuantityNonPrime = new System.Windows.Forms.NumericUpDown();
            this.cbSplitPrime = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.txtQuantity)).BeginInit();
            this.gbWeekDays.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numQuantityPrime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numQuantityNonPrime)).BeginInit();
            this.SuspendLayout();
            // 
            // dtFinishDate
            // 
            this.dtFinishDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtFinishDate.Location = new System.Drawing.Point(330, 67);
            this.dtFinishDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtFinishDate.Name = "dtFinishDate";
            this.dtFinishDate.Size = new System.Drawing.Size(234, 31);
            this.dtFinishDate.TabIndex = 17;
            // 
            // dtStartDate
            // 
            this.dtStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStartDate.Location = new System.Drawing.Point(330, 28);
            this.dtStartDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStartDate.Name = "dtStartDate";
            this.dtStartDate.Size = new System.Drawing.Size(234, 31);
            this.dtStartDate.TabIndex = 16;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 63);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(200, 24);
            this.label2.TabIndex = 15;
            this.label2.Text = "Окончание  интервала:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 24);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(164, 24);
            this.label1.TabIndex = 14;
            this.label1.Text = "Начало интервала:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(3, 141);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(267, 24);
            this.label3.TabIndex = 21;
            this.label3.Text = "Окончание  интервала (время):";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 102);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(231, 24);
            this.label4.TabIndex = 20;
            this.label4.Text = "Начало интервала (время):";
            // 
            // dtFinishTime
            // 
            this.dtFinishTime.CustomFormat = "HH:mm";
            this.dtFinishTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtFinishTime.Location = new System.Drawing.Point(330, 145);
            this.dtFinishTime.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtFinishTime.Name = "dtFinishTime";
            this.dtFinishTime.ShowUpDown = true;
            this.dtFinishTime.Size = new System.Drawing.Size(234, 31);
            this.dtFinishTime.TabIndex = 23;
            // 
            // dtStartTime
            // 
            this.dtStartTime.CustomFormat = "HH:mm";
            this.dtStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtStartTime.Location = new System.Drawing.Point(330, 106);
            this.dtStartTime.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStartTime.Name = "dtStartTime";
            this.dtStartTime.ShowUpDown = true;
            this.dtStartTime.Size = new System.Drawing.Size(234, 31);
            this.dtStartTime.TabIndex = 22;
            // 
            // txtQuantity
            // 
            this.txtQuantity.Location = new System.Drawing.Point(330, 184);
            this.txtQuantity.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtQuantity.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtQuantity.Name = "txtQuantity";
            this.txtQuantity.Size = new System.Drawing.Size(234, 31);
            this.txtQuantity.TabIndex = 24;
            this.txtQuantity.Value = new decimal(new int[] {
            2,
            0,
            0,
            0});
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 180);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(245, 24);
            this.label5.TabIndex = 25;
            this.label5.Text = "Количество выходов в день:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(3, 0);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(63, 24);
            this.label6.TabIndex = 30;
            this.label6.Text = "Ролик:";
            // 
            // lblRollerName
            // 
            this.lblRollerName.AutoSize = true;
            this.lblRollerName.Location = new System.Drawing.Point(330, 0);
            this.lblRollerName.Name = "lblRollerName";
            this.lblRollerName.Size = new System.Drawing.Size(106, 24);
            this.lblRollerName.TabIndex = 31;
            this.lblRollerName.Text = "Roller Name";
            // 
            // gbWeekDays
            // 
            this.gbWeekDays.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.gbWeekDays, 2);
            this.gbWeekDays.Controls.Add(this.rbDays);
            this.gbWeekDays.Controls.Add(this.clbWeekDays);
            this.gbWeekDays.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.gbWeekDays.Location = new System.Drawing.Point(3, 331);
            this.gbWeekDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.gbWeekDays.Name = "gbWeekDays";
            this.gbWeekDays.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.gbWeekDays.Size = new System.Drawing.Size(399, 290);
            this.gbWeekDays.TabIndex = 32;
            this.gbWeekDays.TabStop = false;
            // 
            // rbDays
            // 
            this.rbDays.AutoCheck = false;
            this.rbDays.AutoSize = true;
            this.rbDays.Checked = true;
            this.rbDays.Location = new System.Drawing.Point(7, 0);
            this.rbDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbDays.Name = "rbDays";
            this.rbDays.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.rbDays.Size = new System.Drawing.Size(132, 28);
            this.rbDays.TabIndex = 11;
            this.rbDays.TabStop = true;
            this.rbDays.Text = "Дни недели";
            this.rbDays.UseVisualStyleBackColor = true;
            // 
            // clbWeekDays
            // 
            this.clbWeekDays.BackColor = System.Drawing.SystemColors.Control;
            this.clbWeekDays.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.clbWeekDays.CheckOnClick = true;
            this.clbWeekDays.FormattingEnabled = true;
            this.clbWeekDays.Location = new System.Drawing.Point(7, 39);
            this.clbWeekDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.clbWeekDays.Name = "clbWeekDays";
            this.clbWeekDays.Size = new System.Drawing.Size(386, 252);
            this.clbWeekDays.TabIndex = 10;
            this.clbWeekDays.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbWeekDays_ItemCheck);
            // 
            // cbIgnoreWindows
            // 
            this.cbIgnoreWindows.AutoSize = true;
            this.cbIgnoreWindows.Checked = true;
            this.cbIgnoreWindows.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tableLayoutPanel1.SetColumnSpan(this.cbIgnoreWindows, 2);
            this.cbIgnoreWindows.Location = new System.Drawing.Point(3, 628);
            this.cbIgnoreWindows.Name = "cbIgnoreWindows";
            this.cbIgnoreWindows.Size = new System.Drawing.Size(481, 28);
            this.cbIgnoreWindows.TabIndex = 33;
            this.cbIgnoreWindows.Text = "Не использовать окна где есть ролики данной фирмы";
            this.cbIgnoreWindows.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.cbIgnoreWindows, 0, 10);
            this.tableLayoutPanel1.Controls.Add(this.dtStartDate, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblRollerName, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.gbWeekDays, 0, 9);
            this.tableLayoutPanel1.Controls.Add(this.label6, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.dtFinishDate, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label4, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.txtQuantity, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.dtStartTime, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label3, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.dtFinishTime, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 11);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.label8, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this.numQuantityPrime, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.numQuantityNonPrime, 1, 8);
            this.tableLayoutPanel1.Controls.Add(this.cbSplitPrime, 0, 6);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 12;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(654, 704);
            this.tableLayoutPanel1.TabIndex = 34;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 2);
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnOk);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 662);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(648, 39);
            this.flowLayoutPanel1.TabIndex = 34;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(545, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 33);
            this.btnCancel.TabIndex = 29;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(439, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 33);
            this.btnOk.TabIndex = 28;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 253);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(160, 24);
            this.label7.TabIndex = 35;
            this.label7.Text = "Выходов в прайм:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(3, 290);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(184, 24);
            this.label8.TabIndex = 36;
            this.label8.Text = "Выходов в офф прайм:";
            // 
            // numQuantityPrime
            // 
            this.numQuantityPrime.Enabled = false;
            this.numQuantityPrime.Location = new System.Drawing.Point(330, 256);
            this.numQuantityPrime.Name = "numQuantityPrime";
            this.numQuantityPrime.Size = new System.Drawing.Size(234, 31);
            this.numQuantityPrime.TabIndex = 37;
            // 
            // numQuantityNonPrime
            // 
            this.numQuantityNonPrime.Enabled = false;
            this.numQuantityNonPrime.Location = new System.Drawing.Point(330, 293);
            this.numQuantityNonPrime.Name = "numQuantityNonPrime";
            this.numQuantityNonPrime.Size = new System.Drawing.Size(234, 31);
            this.numQuantityNonPrime.TabIndex = 38;
            // 
            // cbSplitPrime
            // 
            this.cbSplitPrime.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.cbSplitPrime, 2);
            this.cbSplitPrime.Location = new System.Drawing.Point(3, 222);
            this.cbSplitPrime.Name = "cbSplitPrime";
            this.cbSplitPrime.Size = new System.Drawing.Size(319, 28);
            this.cbSplitPrime.TabIndex = 39;
            this.cbSplitPrime.Text = "С разбивкой на прайм и офф прайм";
            this.cbSplitPrime.UseVisualStyleBackColor = true;
            this.cbSplitPrime.CheckedChanged += new System.EventHandler(this.cbSplitPrime_CheckedChanged);
            // 
            // FrmTemplate2
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(654, 704);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmTemplate2";
            this.ShowInTaskbar = false;
            this.Text = "Шаблон для внесения роликов";
            this.Load += new System.EventHandler(this.FrmTemplate2_Load);
            ((System.ComponentModel.ISupportInitialize)(this.txtQuantity)).EndInit();
            this.gbWeekDays.ResumeLayout(false);
            this.gbWeekDays.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numQuantityPrime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numQuantityNonPrime)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtFinishDate;
        private System.Windows.Forms.DateTimePicker dtStartDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.DateTimePicker dtFinishTime;
        private System.Windows.Forms.DateTimePicker dtStartTime;
        private System.Windows.Forms.NumericUpDown txtQuantity;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Label lblRollerName;
        private System.Windows.Forms.GroupBox gbWeekDays;
        private System.Windows.Forms.RadioButton rbDays;
        private System.Windows.Forms.CheckedListBox clbWeekDays;
        private System.Windows.Forms.CheckBox cbIgnoreWindows;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.NumericUpDown numQuantityPrime;
        private System.Windows.Forms.NumericUpDown numQuantityNonPrime;
        private System.Windows.Forms.CheckBox cbSplitPrime;
    }
}