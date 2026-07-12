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
            this.dtFinishDate = new System.Windows.Forms.DateTimePicker();
            this.dtStartDate = new System.Windows.Forms.DateTimePicker();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.rbModeAdd = new System.Windows.Forms.RadioButton();
            this.rbModeDelete = new System.Windows.Forms.RadioButton();
            this.gbOddEven = new System.Windows.Forms.GroupBox();
            this.rbNumber = new System.Windows.Forms.RadioButton();
            this.rbOdd = new System.Windows.Forms.RadioButton();
            this.rbEven = new System.Windows.Forms.RadioButton();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cbIgnoreWindows = new System.Windows.Forms.CheckBox();
            this.gbWeekDays.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            this.gbOddEven.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(537, 3);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 33);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Отмена";
            // 
            // btnOk
            // 
            this.btnOk.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnOk.Location = new System.Drawing.Point(431, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 33);
            this.btnOk.TabIndex = 6;
            this.btnOk.Text = "ОК";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // gbWeekDays
            // 
            this.gbWeekDays.AutoSize = true;
            this.gbWeekDays.Controls.Add(this.rbDays);
            this.gbWeekDays.Controls.Add(this.clbWeekDays);
            this.gbWeekDays.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.gbWeekDays.Location = new System.Drawing.Point(6, 128);
            this.gbWeekDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.gbWeekDays.Name = "gbWeekDays";
            this.gbWeekDays.Padding = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.gbWeekDays.Size = new System.Drawing.Size(302, 280);
            this.gbWeekDays.TabIndex = 8;
            this.gbWeekDays.TabStop = false;
            // 
            // rbDays
            // 
            this.rbDays.AutoCheck = false;
            this.rbDays.AutoSize = true;
            this.rbDays.Checked = true;
            this.rbDays.Location = new System.Drawing.Point(11, 0);
            this.rbDays.Name = "rbDays";
            this.rbDays.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.rbDays.Size = new System.Drawing.Size(132, 28);
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
            this.clbWeekDays.Location = new System.Drawing.Point(11, 53);
            this.clbWeekDays.Name = "clbWeekDays";
            this.clbWeekDays.Size = new System.Drawing.Size(285, 196);
            this.clbWeekDays.TabIndex = 10;
            this.clbWeekDays.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbWeekDays_ItemCheck);
            // 
            // dtFinishDate
            // 
            this.dtFinishDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtFinishDate.Location = new System.Drawing.Point(329, 46);
            this.dtFinishDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtFinishDate.Name = "dtFinishDate";
            this.dtFinishDate.Size = new System.Drawing.Size(234, 31);
            this.dtFinishDate.TabIndex = 13;
            // 
            // dtStartDate
            // 
            this.dtStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStartDate.Location = new System.Drawing.Point(329, 7);
            this.dtStartDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStartDate.Name = "dtStartDate";
            this.dtStartDate.Size = new System.Drawing.Size(234, 31);
            this.dtStartDate.TabIndex = 12;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 42);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(197, 24);
            this.label2.TabIndex = 11;
            this.label2.Text = "Окончание  интервала:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 3);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(164, 24);
            this.label1.TabIndex = 10;
            this.label1.Text = "Начало интервала:";
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 2);
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnOk);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(6, 590);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(640, 39);
            this.flowLayoutPanel1.TabIndex = 14;
            // 
            // rbModeAdd
            // 
            this.rbModeAdd.AutoSize = true;
            this.rbModeAdd.Location = new System.Drawing.Point(3, 3);
            this.rbModeAdd.Name = "rbModeAdd";
            this.rbModeAdd.Size = new System.Drawing.Size(214, 28);
            this.rbModeAdd.TabIndex = 0;
            this.rbModeAdd.TabStop = true;
            this.rbModeAdd.Text = "Добавить выбранные ";
            this.rbModeAdd.UseVisualStyleBackColor = true;
            // 
            // rbModeDelete
            // 
            this.rbModeDelete.AutoSize = true;
            this.rbModeDelete.Location = new System.Drawing.Point(226, 3);
            this.rbModeDelete.Name = "rbModeDelete";
            this.rbModeDelete.Size = new System.Drawing.Size(202, 28);
            this.rbModeDelete.TabIndex = 1;
            this.rbModeDelete.TabStop = true;
            this.rbModeDelete.Text = "Удалить  выбранные";
            this.rbModeDelete.UseVisualStyleBackColor = true;
            // 
            // gbOddEven
            // 
            this.gbOddEven.Controls.Add(this.rbNumber);
            this.gbOddEven.Controls.Add(this.rbOdd);
            this.gbOddEven.Controls.Add(this.rbEven);
            this.gbOddEven.Location = new System.Drawing.Point(329, 127);
            this.gbOddEven.Name = "gbOddEven";
            this.gbOddEven.Size = new System.Drawing.Size(260, 257);
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
            this.rbNumber.Size = new System.Drawing.Size(187, 28);
            this.rbNumber.TabIndex = 14;
            this.rbNumber.TabStop = true;
            this.rbNumber.Text = "Четный/Нечетный";
            this.rbNumber.UseVisualStyleBackColor = true;
            this.rbNumber.Click += new System.EventHandler(this.groupButton_CheckChanged);
            // 
            // rbOdd
            // 
            this.rbOdd.AutoSize = true;
            this.rbOdd.Location = new System.Drawing.Point(12, 83);
            this.rbOdd.Name = "rbOdd";
            this.rbOdd.Size = new System.Drawing.Size(149, 28);
            this.rbOdd.TabIndex = 13;
            this.rbOdd.TabStop = true;
            this.rbOdd.Text = "Нечетные дни";
            this.rbOdd.UseVisualStyleBackColor = true;
            // 
            // rbEven
            // 
            this.rbEven.AutoSize = true;
            this.rbEven.Location = new System.Drawing.Point(12, 44);
            this.rbEven.Name = "rbEven";
            this.rbEven.Size = new System.Drawing.Size(129, 28);
            this.rbEven.TabIndex = 12;
            this.rbEven.TabStop = true;
            this.rbEven.Text = "Четные дни";
            this.rbEven.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.dtStartDate, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.dtFinishDate, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.gbWeekDays, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.gbOddEven, 1, 4);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.cbIgnoreWindows, 0, 5);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(3);
            this.tableLayoutPanel1.RowCount = 7;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(652, 635);
            this.tableLayoutPanel1.TabIndex = 34;
            // 
            // panel1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panel1, 2);
            this.panel1.Controls.Add(this.rbModeAdd);
            this.panel1.Controls.Add(this.rbModeDelete);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(6, 84);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(640, 37);
            this.panel1.TabIndex = 40;
            // 
            // cbIgnoreWindows
            // 
            this.cbIgnoreWindows.AutoSize = true;
            this.cbIgnoreWindows.Checked = true;
            this.cbIgnoreWindows.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tableLayoutPanel1.SetColumnSpan(this.cbIgnoreWindows, 2);
            this.cbIgnoreWindows.Location = new System.Drawing.Point(6, 556);
            this.cbIgnoreWindows.Name = "cbIgnoreWindows";
            this.cbIgnoreWindows.Size = new System.Drawing.Size(477, 28);
            this.cbIgnoreWindows.TabIndex = 41;
            this.cbIgnoreWindows.Text = "Не использовать окна где есть ролики данной фирмы";
            this.cbIgnoreWindows.UseVisualStyleBackColor = true;
            // 
            // FrmTemplate
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(652, 635);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmTemplate";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Шаблон №1 для внесения роликов";
            this.Load += new System.EventHandler(this.FrmTemplate_Load);
            this.gbWeekDays.ResumeLayout(false);
            this.gbWeekDays.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.gbOddEven.ResumeLayout(false);
            this.gbOddEven.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.GroupBox gbWeekDays;
        private System.Windows.Forms.CheckedListBox clbWeekDays;
        private System.Windows.Forms.DateTimePicker dtFinishDate;
        private System.Windows.Forms.DateTimePicker dtStartDate;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rbDays;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.RadioButton rbModeAdd;
        private System.Windows.Forms.RadioButton rbModeDelete;
        private System.Windows.Forms.GroupBox gbOddEven;
        private System.Windows.Forms.RadioButton rbNumber;
        private System.Windows.Forms.RadioButton rbOdd;
        private System.Windows.Forms.RadioButton rbEven;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox cbIgnoreWindows;
    }

}
