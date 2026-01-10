
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
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
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
            ((System.ComponentModel.ISupportInitialize)(this.txtQuantity)).BeginInit();
            this.gbWeekDays.SuspendLayout();
            this.SuspendLayout();
            // 
            // dtFinishDate
            // 
            this.dtFinishDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtFinishDate.Location = new System.Drawing.Point(223, 90);
            this.dtFinishDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtFinishDate.Name = "dtFinishDate";
            this.dtFinishDate.Size = new System.Drawing.Size(222, 31);
            this.dtFinishDate.TabIndex = 17;
            // 
            // dtStartDate
            // 
            this.dtStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStartDate.Location = new System.Drawing.Point(223, 59);
            this.dtStartDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStartDate.Name = "dtStartDate";
            this.dtStartDate.Size = new System.Drawing.Size(222, 31);
            this.dtStartDate.TabIndex = 16;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(10, 90);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(197, 24);
            this.label2.TabIndex = 15;
            this.label2.Text = "Окончание  интервала:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(10, 59);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(164, 24);
            this.label1.TabIndex = 14;
            this.label1.Text = "Начало интервала:";
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(230, 501);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(85, 30);
            this.btnCancel.TabIndex = 29;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(132, 501);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(85, 30);
            this.btnOk.TabIndex = 28;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 187);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(262, 24);
            this.label3.TabIndex = 21;
            this.label3.Text = "Окончание  интервала (время):";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(10, 157);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(229, 24);
            this.label4.TabIndex = 20;
            this.label4.Text = "Начало интервала (время):";
            // 
            // dtFinishTime
            // 
            this.dtFinishTime.CustomFormat = "HH:mm";
            this.dtFinishTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtFinishTime.Location = new System.Drawing.Point(223, 179);
            this.dtFinishTime.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtFinishTime.Name = "dtFinishTime";
            this.dtFinishTime.ShowUpDown = true;
            this.dtFinishTime.Size = new System.Drawing.Size(222, 31);
            this.dtFinishTime.TabIndex = 23;
            // 
            // dtStartTime
            // 
            this.dtStartTime.CustomFormat = "HH:mm";
            this.dtStartTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtStartTime.Location = new System.Drawing.Point(223, 149);
            this.dtStartTime.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStartTime.Name = "dtStartTime";
            this.dtStartTime.ShowUpDown = true;
            this.dtStartTime.Size = new System.Drawing.Size(222, 31);
            this.dtStartTime.TabIndex = 22;
            // 
            // txtQuantity
            // 
            this.txtQuantity.Location = new System.Drawing.Point(223, 232);
            this.txtQuantity.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtQuantity.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.txtQuantity.Name = "txtQuantity";
            this.txtQuantity.Size = new System.Drawing.Size(222, 31);
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
            this.label5.Location = new System.Drawing.Point(10, 235);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(240, 24);
            this.label5.TabIndex = 25;
            this.label5.Text = "Количество выходов в день:";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(10, 25);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(64, 24);
            this.label6.TabIndex = 30;
            this.label6.Text = "Ролик:";
            // 
            // lblRollerName
            // 
            this.lblRollerName.AutoSize = true;
            this.lblRollerName.Location = new System.Drawing.Point(220, 25);
            this.lblRollerName.Name = "lblRollerName";
            this.lblRollerName.Size = new System.Drawing.Size(105, 24);
            this.lblRollerName.TabIndex = 31;
            this.lblRollerName.Text = "Roller Name";
            // 
            // gbWeekDays
            // 
            this.gbWeekDays.Controls.Add(this.rbDays);
            this.gbWeekDays.Controls.Add(this.clbWeekDays);
            this.gbWeekDays.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.gbWeekDays.Location = new System.Drawing.Point(13, 268);
            this.gbWeekDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.gbWeekDays.Name = "gbWeekDays";
            this.gbWeekDays.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.gbWeekDays.Size = new System.Drawing.Size(432, 221);
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
            this.clbWeekDays.Size = new System.Drawing.Size(386, 168);
            this.clbWeekDays.TabIndex = 10;
            this.clbWeekDays.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbWeekDays_ItemCheck);
            // 
            // FrmTemplate2
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(461, 544);
            this.Controls.Add(this.gbWeekDays);
            this.Controls.Add(this.lblRollerName);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.txtQuantity);
            this.Controls.Add(this.dtFinishTime);
            this.Controls.Add(this.dtStartTime);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.dtFinishDate);
            this.Controls.Add(this.dtStartDate);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
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
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.DateTimePicker dtFinishDate;
        private System.Windows.Forms.DateTimePicker dtStartDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
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
    }
}