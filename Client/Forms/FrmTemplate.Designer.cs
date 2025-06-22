namespace Merlin.Forms
{
    partial class FrmTemplate
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.gbWeekDays = new System.Windows.Forms.GroupBox();
            this.rbDays = new System.Windows.Forms.RadioButton();
            this.clbWeekDays = new System.Windows.Forms.CheckedListBox();
            this.gbOddEven = new System.Windows.Forms.GroupBox();
            this.rbNumber = new System.Windows.Forms.RadioButton();
            this.rbOdd = new System.Windows.Forms.RadioButton();
            this.rbEven = new System.Windows.Forms.RadioButton();
            this.dtFinishDate = new System.Windows.Forms.DateTimePicker();
            this.dtStartDate = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.gbWeekDays.SuspendLayout();
            this.gbOddEven.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(275, 252);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 22);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(187, 252);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 22);
            this.btnOk.TabIndex = 6;
            this.btnOk.Text = "Ок";
            // 
            // gbWeekDays
            // 
            this.gbWeekDays.Controls.Add(this.rbDays);
            this.gbWeekDays.Controls.Add(this.clbWeekDays);
            this.gbWeekDays.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.gbWeekDays.Location = new System.Drawing.Point(12, 74);
            this.gbWeekDays.Name = "gbWeekDays";
            this.gbWeekDays.Size = new System.Drawing.Size(181, 159);
            this.gbWeekDays.TabIndex = 8;
            this.gbWeekDays.TabStop = false;
            // 
            // rbDays
            // 
            this.rbDays.AutoCheck = false;
            this.rbDays.AutoSize = true;
            this.rbDays.Checked = true;
            this.rbDays.Location = new System.Drawing.Point(6, 0);
            this.rbDays.Name = "rbDays";
            this.rbDays.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.rbDays.Size = new System.Drawing.Size(88, 17);
            this.rbDays.TabIndex = 11;
            this.rbDays.TabStop = true;
            this.rbDays.Text = "Дни недели";
            this.rbDays.UseVisualStyleBackColor = true;
            this.rbDays.Click += new System.EventHandler(this.groupButton_CheckChanged);
            // 
            // clbWeekDays
            // 
            this.clbWeekDays.BackColor = System.Drawing.SystemColors.Control;
            this.clbWeekDays.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.clbWeekDays.CheckOnClick = true;
            this.clbWeekDays.FormattingEnabled = true;
            this.clbWeekDays.Location = new System.Drawing.Point(6, 31);
            this.clbWeekDays.Name = "clbWeekDays";
            this.clbWeekDays.Size = new System.Drawing.Size(169, 112);
            this.clbWeekDays.TabIndex = 10;
            this.clbWeekDays.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbWeekDays_ItemCheck);
            // 
            // gbOddEven
            // 
            this.gbOddEven.Controls.Add(this.rbNumber);
            this.gbOddEven.Controls.Add(this.rbOdd);
            this.gbOddEven.Controls.Add(this.rbEven);
            this.gbOddEven.Location = new System.Drawing.Point(199, 75);
            this.gbOddEven.Name = "gbOddEven";
            this.gbOddEven.Size = new System.Drawing.Size(156, 159);
            this.gbOddEven.TabIndex = 9;
            this.gbOddEven.TabStop = false;
            // 
            // rbNumber
            // 
            this.rbNumber.AutoCheck = false;
            this.rbNumber.AutoSize = true;
            this.rbNumber.Location = new System.Drawing.Point(4, -2);
            this.rbNumber.Name = "rbNumber";
            this.rbNumber.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.rbNumber.Size = new System.Drawing.Size(122, 17);
            this.rbNumber.TabIndex = 14;
            this.rbNumber.TabStop = true;
            this.rbNumber.Text = "Четный/Нечетный";
            this.rbNumber.UseVisualStyleBackColor = true;
            this.rbNumber.Click += new System.EventHandler(this.groupButton_CheckChanged);
            // 
            // rbOdd
            // 
            this.rbOdd.AutoSize = true;
            this.rbOdd.Location = new System.Drawing.Point(7, 48);
            this.rbOdd.Name = "rbOdd";
            this.rbOdd.Size = new System.Drawing.Size(98, 17);
            this.rbOdd.TabIndex = 13;
            this.rbOdd.TabStop = true;
            this.rbOdd.Text = "Нечётные дни";
            this.rbOdd.UseVisualStyleBackColor = true;
            // 
            // rbEven
            // 
            this.rbEven.AutoSize = true;
            this.rbEven.Location = new System.Drawing.Point(7, 25);
            this.rbEven.Name = "rbEven";
            this.rbEven.Size = new System.Drawing.Size(86, 17);
            this.rbEven.TabIndex = 12;
            this.rbEven.TabStop = true;
            this.rbEven.Text = "Чётные дни";
            this.rbEven.UseVisualStyleBackColor = true;
            // 
            // dtFinishDate
            // 
            this.dtFinishDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtFinishDate.Location = new System.Drawing.Point(148, 33);
            this.dtFinishDate.Name = "dtFinishDate";
            this.dtFinishDate.Size = new System.Drawing.Size(200, 21);
            this.dtFinishDate.TabIndex = 13;
            // 
            // dtStartDate
            // 
            this.dtStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStartDate.Location = new System.Drawing.Point(148, 9);
            this.dtStartDate.Name = "dtStartDate";
            this.dtStartDate.Size = new System.Drawing.Size(200, 21);
            this.dtStartDate.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(9, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(127, 13);
            this.label2.TabIndex = 11;
            this.label2.Text = "Окончание  интервала:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(9, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(105, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Начало интервала:";
            // 
            // FrmTemplate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(369, 294);
            this.Controls.Add(this.dtFinishDate);
            this.Controls.Add(this.dtStartDate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.gbOddEven);
            this.Controls.Add(this.gbWeekDays);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmTemplate";
            this.ShowInTaskbar = false;
            this.Text = "Шаблон для внесения роликов";
            this.Load += new System.EventHandler(this.FrmTemplate_Load);
            this.gbWeekDays.ResumeLayout(false);
            this.gbWeekDays.PerformLayout();
            this.gbOddEven.ResumeLayout(false);
            this.gbOddEven.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.GroupBox gbWeekDays;
		private System.Windows.Forms.GroupBox gbOddEven;
        private System.Windows.Forms.CheckedListBox clbWeekDays;
        private System.Windows.Forms.RadioButton rbOdd;
        private System.Windows.Forms.RadioButton rbEven;
        private System.Windows.Forms.DateTimePicker dtFinishDate;
        private System.Windows.Forms.DateTimePicker dtStartDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
		private System.Windows.Forms.RadioButton rbDays;
		private System.Windows.Forms.RadioButton rbNumber;
    }
}