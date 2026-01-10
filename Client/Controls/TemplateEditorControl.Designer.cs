namespace Merlin.Controls
{
    partial class TemplateEditorControl
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.gbSchedulePattern = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbOddDays = new System.Windows.Forms.RadioButton();
            this.rbEvenDays = new System.Windows.Forms.RadioButton();
            this.flpDays = new System.Windows.Forms.FlowLayoutPanel();
            this.chkMon = new System.Windows.Forms.CheckBox();
            this.chkTue = new System.Windows.Forms.CheckBox();
            this.chkWed = new System.Windows.Forms.CheckBox();
            this.chkThu = new System.Windows.Forms.CheckBox();
            this.chkFri = new System.Windows.Forms.CheckBox();
            this.chkSat = new System.Windows.Forms.CheckBox();
            this.chkSun = new System.Windows.Forms.CheckBox();
            this.rbEvenOdd = new System.Windows.Forms.RadioButton();
            this.rbDaysOfWeek = new System.Windows.Forms.RadioButton();
            this.gbSchedulePattern.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.flpDays.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbSchedulePattern
            // 
            this.gbSchedulePattern.Controls.Add(this.rbEvenOdd);
            this.gbSchedulePattern.Controls.Add(this.rbDaysOfWeek);
            this.gbSchedulePattern.Controls.Add(this.groupBox1);
            this.gbSchedulePattern.Controls.Add(this.flpDays);
            this.gbSchedulePattern.Location = new System.Drawing.Point(3, 3);
            this.gbSchedulePattern.Name = "gbSchedulePattern";
            this.gbSchedulePattern.Size = new System.Drawing.Size(400, 250);
            this.gbSchedulePattern.TabIndex = 0;
            this.gbSchedulePattern.TabStop = false;
            this.gbSchedulePattern.Text = "Шаблон выходов";
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.rbOddDays);
            this.groupBox1.Controls.Add(this.rbEvenDays);
            this.groupBox1.Location = new System.Drawing.Point(220, 45);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(170, 80);
            this.groupBox1.TabIndex = 3;
            this.groupBox1.TabStop = false;
            // 
            // rbOddDays
            // 
            this.rbOddDays.AutoSize = true;
            this.rbOddDays.Location = new System.Drawing.Point(10, 45);
            this.rbOddDays.Name = "rbOddDays";
            this.rbOddDays.Size = new System.Drawing.Size(98, 17);
            this.rbOddDays.TabIndex = 1;
            this.rbOddDays.Text = "Нечётные дни";
            this.rbOddDays.UseVisualStyleBackColor = true;
            // 
            // rbEvenDays
            // 
            this.rbEvenDays.AutoSize = true;
            this.rbEvenDays.Checked = true;
            this.rbEvenDays.Location = new System.Drawing.Point(10, 20);
            this.rbEvenDays.Name = "rbEvenDays";
            this.rbEvenDays.Size = new System.Drawing.Size(86, 17);
            this.rbEvenDays.TabIndex = 0;
            this.rbEvenDays.TabStop = true;
            this.rbEvenDays.Text = "Чётные дни";
            this.rbEvenDays.UseVisualStyleBackColor = true;
            // 
            // flpDays
            // 
            this.flpDays.Controls.Add(this.chkMon);
            this.flpDays.Controls.Add(this.chkTue);
            this.flpDays.Controls.Add(this.chkWed);
            this.flpDays.Controls.Add(this.chkThu);
            this.flpDays.Controls.Add(this.chkFri);
            this.flpDays.Controls.Add(this.chkSat);
            this.flpDays.Controls.Add(this.chkSun);
            this.flpDays.FlowDirection = System.Windows.Forms.FlowDirection.TopDown;
            this.flpDays.Location = new System.Drawing.Point(20, 45);
            this.flpDays.Name = "flpDays";
            this.flpDays.Size = new System.Drawing.Size(180, 190);
            this.flpDays.TabIndex = 2;
            // 
            // chkMon
            // 
            this.chkMon.AutoSize = true;
            this.chkMon.Checked = true;
            this.chkMon.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMon.Location = new System.Drawing.Point(3, 3);
            this.chkMon.Name = "chkMon";
            this.chkMon.Size = new System.Drawing.Size(105, 17);
            this.chkMon.TabIndex = 0;
            this.chkMon.Text = "Понедельник";
            this.chkMon.UseVisualStyleBackColor = true;
            // 
            // chkTue
            // 
            this.chkTue.AutoSize = true;
            this.chkTue.Checked = true;
            this.chkTue.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTue.Location = new System.Drawing.Point(3, 26);
            this.chkTue.Name = "chkTue";
            this.chkTue.Size = new System.Drawing.Size(71, 17);
            this.chkTue.TabIndex = 1;
            this.chkTue.Text = "Вторник";
            this.chkTue.UseVisualStyleBackColor = true;
            // 
            // chkWed
            // 
            this.chkWed.AutoSize = true;
            this.chkWed.Checked = true;
            this.chkWed.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkWed.Location = new System.Drawing.Point(3, 49);
            this.chkWed.Name = "chkWed";
            this.chkWed.Size = new System.Drawing.Size(61, 17);
            this.chkWed.TabIndex = 2;
            this.chkWed.Text = "Среда";
            this.chkWed.UseVisualStyleBackColor = true;
            // 
            // chkThu
            // 
            this.chkThu.AutoSize = true;
            this.chkThu.Checked = true;
            this.chkThu.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkThu.Location = new System.Drawing.Point(3, 72);
            this.chkThu.Name = "chkThu";
            this.chkThu.Size = new System.Drawing.Size(67, 17);
            this.chkThu.TabIndex = 3;
            this.chkThu.Text = "Четверг";
            this.chkThu.UseVisualStyleBackColor = true;
            // 
            // chkFri
            // 
            this.chkFri.AutoSize = true;
            this.chkFri.Checked = true;
            this.chkFri.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFri.Location = new System.Drawing.Point(3, 95);
            this.chkFri.Name = "chkFri";
            this.chkFri.Size = new System.Drawing.Size(72, 17);
            this.chkFri.TabIndex = 4;
            this.chkFri.Text = "Пятница";
            this.chkFri.UseVisualStyleBackColor = true;
            // 
            // chkSat
            // 
            this.chkSat.AutoSize = true;
            this.chkSat.Checked = true;
            this.chkSat.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSat.Location = new System.Drawing.Point(3, 118);
            this.chkSat.Name = "chkSat";
            this.chkSat.Size = new System.Drawing.Size(67, 17);
            this.chkSat.TabIndex = 5;
            this.chkSat.Text = "Суббота";
            this.chkSat.UseVisualStyleBackColor = true;
            // 
            // chkSun
            // 
            this.chkSun.AutoSize = true;
            this.chkSun.Checked = true;
            this.chkSun.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSun.Location = new System.Drawing.Point(3, 141);
            this.chkSun.Name = "chkSun";
            this.chkSun.Size = new System.Drawing.Size(97, 17);
            this.chkSun.TabIndex = 6;
            this.chkSun.Text = "Воскресенье";
            this.chkSun.UseVisualStyleBackColor = true;
            // 
            // rbEvenOdd
            // 
            this.rbEvenOdd.AutoSize = true;
            this.rbEvenOdd.Location = new System.Drawing.Point(220, 22);
            this.rbEvenOdd.Name = "rbEvenOdd";
            this.rbEvenOdd.Size = new System.Drawing.Size(90, 17);
            this.rbEvenOdd.TabIndex = 1;
            this.rbEvenOdd.Text = "Чёт/нечёт";
            this.rbEvenOdd.UseVisualStyleBackColor = true;
            this.rbEvenOdd.CheckedChanged += new System.EventHandler(this.rbEvenOdd_CheckedChanged);
            // 
            // rbDaysOfWeek
            // 
            this.rbDaysOfWeek.AutoSize = true;
            this.rbDaysOfWeek.Checked = true;
            this.rbDaysOfWeek.Location = new System.Drawing.Point(20, 22);
            this.rbDaysOfWeek.Name = "rbDaysOfWeek";
            this.rbDaysOfWeek.Size = new System.Drawing.Size(88, 17);
            this.rbDaysOfWeek.TabIndex = 0;
            this.rbDaysOfWeek.TabStop = true;
            this.rbDaysOfWeek.Text = "Дни недели";
            this.rbDaysOfWeek.UseVisualStyleBackColor = true;
            this.rbDaysOfWeek.CheckedChanged += new System.EventHandler(this.rbDaysOfWeek_CheckedChanged);
            // 
            // TemplateEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.gbSchedulePattern);
            this.Name = "TemplateEditorControl";
            this.Size = new System.Drawing.Size(410, 260);
            this.Load += new System.EventHandler(this.TemplateEditorControl_Load);
            this.gbSchedulePattern.ResumeLayout(false);
            this.gbSchedulePattern.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.flpDays.ResumeLayout(false);
            this.flpDays.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbSchedulePattern;
        private System.Windows.Forms.RadioButton rbDaysOfWeek;
        private System.Windows.Forms.RadioButton rbEvenOdd;
        private System.Windows.Forms.FlowLayoutPanel flpDays;
        private System.Windows.Forms.CheckBox chkMon;
        private System.Windows.Forms.CheckBox chkTue;
        private System.Windows.Forms.CheckBox chkWed;
        private System.Windows.Forms.CheckBox chkThu;
        private System.Windows.Forms.CheckBox chkFri;
        private System.Windows.Forms.CheckBox chkSat;
        private System.Windows.Forms.CheckBox chkSun;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbEvenDays;
        private System.Windows.Forms.RadioButton rbOddDays;
    }
}
