// TemplateEditorControl.Designer.cs (with "Шаблон выходов": days vs even/odd)
// .NET Framework WinForms
namespace Merlin.Controls
{
    partial class TemplateEditorControl
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.GroupBox gbTemplate;
        private System.Windows.Forms.TableLayoutPanel tlTemplate;

        private System.Windows.Forms.Label lblCity;
        private System.Windows.Forms.ComboBox cbCity;

        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.DateTimePicker dtStart;
        private System.Windows.Forms.DateTimePicker dtEnd;
        private System.Windows.Forms.Button btnCalculate;

        private System.Windows.Forms.Label lblDuration;
        private System.Windows.Forms.NumericUpDown nudDuration;

        private System.Windows.Forms.Label lblPrime;
        private System.Windows.Forms.NumericUpDown nudPrimeWeekday;

        private System.Windows.Forms.Label lblDiscount;

        private System.Windows.Forms.Label lblPos;
        private System.Windows.Forms.ComboBox cbPosition;

        private System.Windows.Forms.Label lblNonPrime;
        private System.Windows.Forms.NumericUpDown nudNonPrimeWeekday;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.gbTemplate = new System.Windows.Forms.GroupBox();
            this.tlTemplate = new System.Windows.Forms.TableLayoutPanel();
            this.gbSchedulePattern = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.rbEvenDays = new System.Windows.Forms.RadioButton();
            this.rbOddDays = new System.Windows.Forms.RadioButton();
            this.rbDaysOfWeek = new System.Windows.Forms.RadioButton();
            this.flpDays = new System.Windows.Forms.FlowLayoutPanel();
            this.chkMon = new System.Windows.Forms.CheckBox();
            this.chkTue = new System.Windows.Forms.CheckBox();
            this.chkWed = new System.Windows.Forms.CheckBox();
            this.chkThu = new System.Windows.Forms.CheckBox();
            this.chkFri = new System.Windows.Forms.CheckBox();
            this.chkSat = new System.Windows.Forms.CheckBox();
            this.chkSun = new System.Windows.Forms.CheckBox();
            this.rbEvenOdd = new System.Windows.Forms.RadioButton();
            this.lblCity = new System.Windows.Forms.Label();
            this.cbCity = new System.Windows.Forms.ComboBox();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.lblDuration = new System.Windows.Forms.Label();
            this.nudDuration = new System.Windows.Forms.NumericUpDown();
            this.lblPrime = new System.Windows.Forms.Label();
            this.nudPrimeWeekday = new System.Windows.Forms.NumericUpDown();
            this.lblNonPrime = new System.Windows.Forms.Label();
            this.nudNonPrimeWeekday = new System.Windows.Forms.NumericUpDown();
            this.lblFrom = new System.Windows.Forms.Label();
            this.dtStart = new System.Windows.Forms.DateTimePicker();
            this.dtEnd = new System.Windows.Forms.DateTimePicker();
            this.lblPos = new System.Windows.Forms.Label();
            this.cbPosition = new System.Windows.Forms.ComboBox();
            this.nmManagerDiscount = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.numPrimeWeekend = new System.Windows.Forms.NumericUpDown();
            this.numNonPrimeWeekend = new System.Windows.Forms.NumericUpDown();
            this.lblTotalBeforePackageDiscount = new System.Windows.Forms.Label();
            this.lblPackageDiscount = new System.Windows.Forms.Label();
            this.lblTotalAfterPackageDiscount = new System.Windows.Forms.Label();
            this.btnCalculate = new System.Windows.Forms.Button();
            this.gbTemplate.SuspendLayout();
            this.tlTemplate.SuspendLayout();
            this.gbSchedulePattern.SuspendLayout();
            this.groupBox1.SuspendLayout();
            this.flpDays.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPrimeWeekday)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNonPrimeWeekday)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmManagerDiscount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPrimeWeekend)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNonPrimeWeekend)).BeginInit();
            this.SuspendLayout();
            // 
            // gbTemplate
            // 
            this.gbTemplate.Controls.Add(this.tlTemplate);
            this.gbTemplate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbTemplate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbTemplate.Location = new System.Drawing.Point(0, 0);
            this.gbTemplate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.gbTemplate.Name = "gbTemplate";
            this.gbTemplate.Padding = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this.gbTemplate.Size = new System.Drawing.Size(1320, 407);
            this.gbTemplate.TabIndex = 0;
            this.gbTemplate.TabStop = false;
            this.gbTemplate.Text = "Общие параметры размещения";
            // 
            // tlTemplate
            // 
            this.tlTemplate.ColumnCount = 8;
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 169F));
            this.tlTemplate.Controls.Add(this.gbSchedulePattern, 0, 2);
            this.tlTemplate.Controls.Add(this.lblCity, 0, 0);
            this.tlTemplate.Controls.Add(this.cbCity, 1, 0);
            this.tlTemplate.Controls.Add(this.lblDiscount, 0, 6);
            this.tlTemplate.Controls.Add(this.lblDuration, 0, 4);
            this.tlTemplate.Controls.Add(this.nudDuration, 1, 4);
            this.tlTemplate.Controls.Add(this.lblPrime, 0, 3);
            this.tlTemplate.Controls.Add(this.nudPrimeWeekday, 1, 3);
            this.tlTemplate.Controls.Add(this.lblNonPrime, 2, 3);
            this.tlTemplate.Controls.Add(this.nudNonPrimeWeekday, 3, 3);
            this.tlTemplate.Controls.Add(this.lblFrom, 0, 1);
            this.tlTemplate.Controls.Add(this.dtStart, 1, 1);
            this.tlTemplate.Controls.Add(this.dtEnd, 2, 1);
            this.tlTemplate.Controls.Add(this.lblPos, 2, 4);
            this.tlTemplate.Controls.Add(this.cbPosition, 3, 4);
            this.tlTemplate.Controls.Add(this.nmManagerDiscount, 1, 6);
            this.tlTemplate.Controls.Add(this.label2, 4, 3);
            this.tlTemplate.Controls.Add(this.label3, 6, 3);
            this.tlTemplate.Controls.Add(this.numPrimeWeekend, 5, 3);
            this.tlTemplate.Controls.Add(this.numNonPrimeWeekend, 7, 3);
            this.tlTemplate.Controls.Add(this.lblTotalBeforePackageDiscount, 4, 0);
            this.tlTemplate.Controls.Add(this.lblPackageDiscount, 4, 1);
            this.tlTemplate.Controls.Add(this.lblTotalAfterPackageDiscount, 4, 2);
            this.tlTemplate.Controls.Add(this.btnCalculate, 2, 6);
            this.tlTemplate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlTemplate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tlTemplate.Location = new System.Drawing.Point(9, 34);
            this.tlTemplate.Margin = new System.Windows.Forms.Padding(0);
            this.tlTemplate.Name = "tlTemplate";
            this.tlTemplate.RowCount = 7;
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.Size = new System.Drawing.Size(1302, 363);
            this.tlTemplate.TabIndex = 0;
            // 
            // gbSchedulePattern
            // 
            this.gbSchedulePattern.AutoSize = true;
            this.gbSchedulePattern.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlTemplate.SetColumnSpan(this.gbSchedulePattern, 4);
            this.gbSchedulePattern.Controls.Add(this.groupBox1);
            this.gbSchedulePattern.Controls.Add(this.rbDaysOfWeek);
            this.gbSchedulePattern.Controls.Add(this.flpDays);
            this.gbSchedulePattern.Controls.Add(this.rbEvenOdd);
            this.gbSchedulePattern.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbSchedulePattern.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbSchedulePattern.Location = new System.Drawing.Point(3, 82);
            this.gbSchedulePattern.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.gbSchedulePattern.Name = "gbSchedulePattern";
            this.gbSchedulePattern.Padding = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this.gbSchedulePattern.Size = new System.Drawing.Size(869, 145);
            this.gbSchedulePattern.TabIndex = 35;
            this.gbSchedulePattern.TabStop = false;
            this.gbSchedulePattern.Text = "Шаблон выходов";
            // 
            // groupBox1
            // 
            this.groupBox1.BackColor = System.Drawing.SystemColors.Control;
            this.groupBox1.Controls.Add(this.rbEvenDays);
            this.groupBox1.Controls.Add(this.rbOddDays);
            this.groupBox1.Location = new System.Drawing.Point(216, 59);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(295, 49);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            // 
            // rbEvenDays
            // 
            this.rbEvenDays.AutoSize = true;
            this.rbEvenDays.Checked = true;
            this.rbEvenDays.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbEvenDays.Location = new System.Drawing.Point(0, 10);
            this.rbEvenDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbEvenDays.Name = "rbEvenDays";
            this.rbEvenDays.Size = new System.Drawing.Size(97, 29);
            this.rbEvenDays.TabIndex = 4;
            this.rbEvenDays.TabStop = true;
            this.rbEvenDays.Text = "Чётные";
            this.rbEvenDays.UseVisualStyleBackColor = true;
            // 
            // rbOddDays
            // 
            this.rbOddDays.AutoSize = true;
            this.rbOddDays.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbOddDays.Location = new System.Drawing.Point(105, 10);
            this.rbOddDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbOddDays.Name = "rbOddDays";
            this.rbOddDays.Size = new System.Drawing.Size(117, 29);
            this.rbOddDays.TabIndex = 5;
            this.rbOddDays.Text = "Нечётные";
            this.rbOddDays.UseVisualStyleBackColor = true;
            // 
            // rbDaysOfWeek
            // 
            this.rbDaysOfWeek.AutoSize = true;
            this.rbDaysOfWeek.Checked = true;
            this.rbDaysOfWeek.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbDaysOfWeek.Location = new System.Drawing.Point(11, 28);
            this.rbDaysOfWeek.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbDaysOfWeek.Name = "rbDaysOfWeek";
            this.rbDaysOfWeek.Size = new System.Drawing.Size(135, 29);
            this.rbDaysOfWeek.TabIndex = 0;
            this.rbDaysOfWeek.TabStop = true;
            this.rbDaysOfWeek.Text = "Дни недели:";
            this.rbDaysOfWeek.UseVisualStyleBackColor = true;
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
            this.flpDays.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.flpDays.Location = new System.Drawing.Point(216, 22);
            this.flpDays.Margin = new System.Windows.Forms.Padding(0);
            this.flpDays.Name = "flpDays";
            this.flpDays.Size = new System.Drawing.Size(578, 31);
            this.flpDays.TabIndex = 1;
            this.flpDays.WrapContents = false;
            // 
            // chkMon
            // 
            this.chkMon.AutoSize = true;
            this.chkMon.Checked = true;
            this.chkMon.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkMon.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chkMon.Location = new System.Drawing.Point(0, 4);
            this.chkMon.Margin = new System.Windows.Forms.Padding(0, 4, 13, 4);
            this.chkMon.Name = "chkMon";
            this.chkMon.Size = new System.Drawing.Size(61, 29);
            this.chkMon.TabIndex = 0;
            this.chkMon.Text = "Пн";
            // 
            // chkTue
            // 
            this.chkTue.AutoSize = true;
            this.chkTue.Checked = true;
            this.chkTue.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkTue.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chkTue.Location = new System.Drawing.Point(74, 4);
            this.chkTue.Margin = new System.Windows.Forms.Padding(0, 4, 13, 4);
            this.chkTue.Name = "chkTue";
            this.chkTue.Size = new System.Drawing.Size(55, 29);
            this.chkTue.TabIndex = 1;
            this.chkTue.Text = "Вт";
            // 
            // chkWed
            // 
            this.chkWed.AutoSize = true;
            this.chkWed.Checked = true;
            this.chkWed.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkWed.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chkWed.Location = new System.Drawing.Point(142, 4);
            this.chkWed.Margin = new System.Windows.Forms.Padding(0, 4, 13, 4);
            this.chkWed.Name = "chkWed";
            this.chkWed.Size = new System.Drawing.Size(60, 29);
            this.chkWed.TabIndex = 2;
            this.chkWed.Text = "Ср";
            // 
            // chkThu
            // 
            this.chkThu.AutoSize = true;
            this.chkThu.Checked = true;
            this.chkThu.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkThu.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chkThu.Location = new System.Drawing.Point(215, 4);
            this.chkThu.Margin = new System.Windows.Forms.Padding(0, 4, 13, 4);
            this.chkThu.Name = "chkThu";
            this.chkThu.Size = new System.Drawing.Size(57, 29);
            this.chkThu.TabIndex = 3;
            this.chkThu.Text = "Чт";
            // 
            // chkFri
            // 
            this.chkFri.AutoSize = true;
            this.chkFri.Checked = true;
            this.chkFri.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkFri.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chkFri.Location = new System.Drawing.Point(285, 4);
            this.chkFri.Margin = new System.Windows.Forms.Padding(0, 4, 13, 4);
            this.chkFri.Name = "chkFri";
            this.chkFri.Size = new System.Drawing.Size(58, 29);
            this.chkFri.TabIndex = 4;
            this.chkFri.Text = "Пт";
            // 
            // chkSat
            // 
            this.chkSat.AutoSize = true;
            this.chkSat.Checked = true;
            this.chkSat.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSat.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chkSat.Location = new System.Drawing.Point(356, 4);
            this.chkSat.Margin = new System.Windows.Forms.Padding(0, 4, 13, 4);
            this.chkSat.Name = "chkSat";
            this.chkSat.Size = new System.Drawing.Size(59, 29);
            this.chkSat.TabIndex = 5;
            this.chkSat.Text = "Сб";
            // 
            // chkSun
            // 
            this.chkSun.AutoSize = true;
            this.chkSun.Checked = true;
            this.chkSun.CheckState = System.Windows.Forms.CheckState.Checked;
            this.chkSun.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.chkSun.Location = new System.Drawing.Point(428, 4);
            this.chkSun.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.chkSun.Name = "chkSun";
            this.chkSun.Size = new System.Drawing.Size(56, 29);
            this.chkSun.TabIndex = 6;
            this.chkSun.Text = "Вс";
            // 
            // rbEvenOdd
            // 
            this.rbEvenOdd.AutoSize = true;
            this.rbEvenOdd.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbEvenOdd.Location = new System.Drawing.Point(11, 70);
            this.rbEvenOdd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbEvenOdd.Name = "rbEvenOdd";
            this.rbEvenOdd.Size = new System.Drawing.Size(185, 29);
            this.rbEvenOdd.TabIndex = 1;
            this.rbEvenOdd.Text = "Чётные/нечётные:";
            this.rbEvenOdd.UseVisualStyleBackColor = true;
            // 
            // lblCity
            // 
            this.lblCity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCity.AutoSize = true;
            this.lblCity.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblCity.Location = new System.Drawing.Point(3, 8);
            this.lblCity.Name = "lblCity";
            this.lblCity.Size = new System.Drawing.Size(73, 25);
            this.lblCity.TabIndex = 0;
            this.lblCity.Text = "Группа:";
            // 
            // cbCity
            // 
            this.cbCity.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cbCity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCity.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbCity.FormattingEnabled = true;
            this.cbCity.IntegralHeight = false;
            this.cbCity.Location = new System.Drawing.Point(221, 4);
            this.cbCity.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cbCity.Name = "cbCity";
            this.cbCity.Size = new System.Drawing.Size(261, 33);
            this.cbCity.TabIndex = 1;
            // 
            // lblDiscount
            // 
            this.lblDiscount.AutoSize = true;
            this.lblDiscount.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDiscount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblDiscount.Location = new System.Drawing.Point(3, 320);
            this.lblDiscount.Name = "lblDiscount";
            this.lblDiscount.Size = new System.Drawing.Size(212, 25);
            this.lblDiscount.TabIndex = 29;
            this.lblDiscount.Text = "Сезонный коэффициент:";
            this.lblDiscount.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblDuration
            // 
            this.lblDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDuration.AutoSize = true;
            this.lblDuration.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblDuration.Location = new System.Drawing.Point(3, 287);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(110, 25);
            this.lblDuration.TabIndex = 22;
            this.lblDuration.Text = "Ролик (сек.):";
            // 
            // nudDuration
            // 
            this.nudDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudDuration.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudDuration.Location = new System.Drawing.Point(221, 284);
            this.nudDuration.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.nudDuration.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.nudDuration.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            0});
            this.nudDuration.Name = "nudDuration";
            this.nudDuration.Size = new System.Drawing.Size(78, 31);
            this.nudDuration.TabIndex = 23;
            this.nudDuration.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudDuration.ThousandsSeparator = true;
            this.nudDuration.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // lblPrime
            // 
            this.lblPrime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPrime.AutoSize = true;
            this.lblPrime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPrime.Location = new System.Drawing.Point(3, 241);
            this.lblPrime.Name = "lblPrime";
            this.lblPrime.Size = new System.Drawing.Size(126, 25);
            this.lblPrime.TabIndex = 27;
            this.lblPrime.Text = "Прайм будни:";
            // 
            // nudPrimeWeekday
            // 
            this.nudPrimeWeekday.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudPrimeWeekday.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudPrimeWeekday.Location = new System.Drawing.Point(221, 238);
            this.nudPrimeWeekday.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.nudPrimeWeekday.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudPrimeWeekday.Name = "nudPrimeWeekday";
            this.nudPrimeWeekday.Size = new System.Drawing.Size(78, 31);
            this.nudPrimeWeekday.TabIndex = 28;
            this.nudPrimeWeekday.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudPrimeWeekday.ThousandsSeparator = true;
            this.nudPrimeWeekday.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // lblNonPrime
            // 
            this.lblNonPrime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblNonPrime.AutoSize = true;
            this.lblNonPrime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblNonPrime.Location = new System.Drawing.Point(488, 241);
            this.lblNonPrime.Name = "lblNonPrime";
            this.lblNonPrime.Size = new System.Drawing.Size(155, 25);
            this.lblNonPrime.TabIndex = 33;
            this.lblNonPrime.Text = "Не прайм  будни:";
            // 
            // nudNonPrimeWeekday
            // 
            this.nudNonPrimeWeekday.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudNonPrimeWeekday.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudNonPrimeWeekday.Location = new System.Drawing.Point(746, 238);
            this.nudNonPrimeWeekday.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.nudNonPrimeWeekday.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudNonPrimeWeekday.Name = "nudNonPrimeWeekday";
            this.nudNonPrimeWeekday.Size = new System.Drawing.Size(78, 31);
            this.nudNonPrimeWeekday.TabIndex = 34;
            this.nudNonPrimeWeekday.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudNonPrimeWeekday.ThousandsSeparator = true;
            this.nudNonPrimeWeekday.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // lblFrom
            // 
            this.lblFrom.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblFrom.AutoSize = true;
            this.lblFrom.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblFrom.Location = new System.Drawing.Point(3, 48);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(134, 25);
            this.lblFrom.TabIndex = 2;
            this.lblFrom.Text = "Период акции:";
            // 
            // dtStart
            // 
            this.dtStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtStart.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.dtStart.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStart.Location = new System.Drawing.Point(221, 45);
            this.dtStart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStart.Name = "dtStart";
            this.dtStart.Size = new System.Drawing.Size(261, 31);
            this.dtStart.TabIndex = 3;
            // 
            // dtEnd
            // 
            this.dtEnd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtEnd.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.dtEnd.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtEnd.Location = new System.Drawing.Point(488, 45);
            this.dtEnd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtEnd.Name = "dtEnd";
            this.dtEnd.Size = new System.Drawing.Size(252, 31);
            this.dtEnd.TabIndex = 5;
            // 
            // lblPos
            // 
            this.lblPos.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPos.AutoSize = true;
            this.lblPos.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPos.Location = new System.Drawing.Point(488, 287);
            this.lblPos.Name = "lblPos";
            this.lblPos.Size = new System.Drawing.Size(88, 25);
            this.lblPos.TabIndex = 31;
            this.lblPos.Text = "Позиция:";
            // 
            // cbPosition
            // 
            this.cbPosition.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbPosition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPosition.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbPosition.FormattingEnabled = true;
            this.cbPosition.IntegralHeight = false;
            this.cbPosition.Location = new System.Drawing.Point(746, 283);
            this.cbPosition.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cbPosition.Name = "cbPosition";
            this.cbPosition.Size = new System.Drawing.Size(126, 33);
            this.cbPosition.TabIndex = 32;
            // 
            // nmManagerDiscount
            // 
            this.nmManagerDiscount.DecimalPlaces = 2;
            this.nmManagerDiscount.Dock = System.Windows.Forms.DockStyle.Fill;
            this.nmManagerDiscount.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.nmManagerDiscount.Location = new System.Drawing.Point(221, 323);
            this.nmManagerDiscount.Name = "nmManagerDiscount";
            this.nmManagerDiscount.Size = new System.Drawing.Size(261, 31);
            this.nmManagerDiscount.TabIndex = 36;
            this.nmManagerDiscount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(878, 229);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 50);
            this.label2.TabIndex = 38;
            this.label2.Text = "Прайм выходные";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(1118, 229);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 50);
            this.label3.TabIndex = 39;
            this.label3.Text = "Не прайм выходные";
            // 
            // numPrimeWeekend
            // 
            this.numPrimeWeekend.Location = new System.Drawing.Point(998, 232);
            this.numPrimeWeekend.Name = "numPrimeWeekend";
            this.numPrimeWeekend.Size = new System.Drawing.Size(114, 31);
            this.numPrimeWeekend.TabIndex = 40;
            this.numPrimeWeekend.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // numNonPrimeWeekend
            // 
            this.numNonPrimeWeekend.Location = new System.Drawing.Point(1238, 232);
            this.numNonPrimeWeekend.Name = "numNonPrimeWeekend";
            this.numNonPrimeWeekend.Size = new System.Drawing.Size(120, 31);
            this.numNonPrimeWeekend.TabIndex = 41;
            this.numNonPrimeWeekend.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // lblTotalBeforePackageDiscount
            // 
            this.lblTotalBeforePackageDiscount.AutoSize = true;
            this.tlTemplate.SetColumnSpan(this.lblTotalBeforePackageDiscount, 2);
            this.lblTotalBeforePackageDiscount.Location = new System.Drawing.Point(878, 0);
            this.lblTotalBeforePackageDiscount.Name = "lblTotalBeforePackageDiscount";
            this.lblTotalBeforePackageDiscount.Size = new System.Drawing.Size(173, 25);
            this.lblTotalBeforePackageDiscount.TabIndex = 42;
            this.lblTotalBeforePackageDiscount.Text = "По радиостанциям:";
            // 
            // lblPackageDiscount
            // 
            this.lblPackageDiscount.AutoSize = true;
            this.tlTemplate.SetColumnSpan(this.lblPackageDiscount, 2);
            this.lblPackageDiscount.Location = new System.Drawing.Point(878, 41);
            this.lblPackageDiscount.Name = "lblPackageDiscount";
            this.lblPackageDiscount.Size = new System.Drawing.Size(151, 25);
            this.lblPackageDiscount.TabIndex = 43;
            this.lblPackageDiscount.Text = "Пакетная скидка:";
            // 
            // lblTotalAfterPackageDiscount
            // 
            this.lblTotalAfterPackageDiscount.AutoSize = true;
            this.tlTemplate.SetColumnSpan(this.lblTotalAfterPackageDiscount, 2);
            this.lblTotalAfterPackageDiscount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblTotalAfterPackageDiscount.Location = new System.Drawing.Point(878, 80);
            this.lblTotalAfterPackageDiscount.Name = "lblTotalAfterPackageDiscount";
            this.lblTotalAfterPackageDiscount.Size = new System.Drawing.Size(57, 25);
            this.lblTotalAfterPackageDiscount.TabIndex = 44;
            this.lblTotalAfterPackageDiscount.Text = "Итог:";
            // 
            // btnCalculate
            // 
            this.btnCalculate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCalculate.Location = new System.Drawing.Point(494, 324);
            this.btnCalculate.Margin = new System.Windows.Forms.Padding(9, 4, 0, 4);
            this.btnCalculate.Name = "btnCalculate";
            this.btnCalculate.Size = new System.Drawing.Size(249, 38);
            this.btnCalculate.TabIndex = 1;
            this.btnCalculate.Text = "Рассчитать";
            this.btnCalculate.UseVisualStyleBackColor = true;
            // 
            // TemplateEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.gbTemplate);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(0);
            this.Name = "TemplateEditorControl";
            this.Size = new System.Drawing.Size(1320, 407);
            this.gbTemplate.ResumeLayout(false);
            this.tlTemplate.ResumeLayout(false);
            this.tlTemplate.PerformLayout();
            this.gbSchedulePattern.ResumeLayout(false);
            this.gbSchedulePattern.PerformLayout();
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.flpDays.ResumeLayout(false);
            this.flpDays.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPrimeWeekday)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNonPrimeWeekday)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmManagerDiscount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPrimeWeekend)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNonPrimeWeekend)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.GroupBox gbSchedulePattern;
        private System.Windows.Forms.RadioButton rbDaysOfWeek;
        private System.Windows.Forms.FlowLayoutPanel flpDays;
        private System.Windows.Forms.CheckBox chkMon;
        private System.Windows.Forms.CheckBox chkTue;
        private System.Windows.Forms.CheckBox chkWed;
        private System.Windows.Forms.CheckBox chkThu;
        private System.Windows.Forms.CheckBox chkFri;
        private System.Windows.Forms.CheckBox chkSat;
        private System.Windows.Forms.CheckBox chkSun;
        private System.Windows.Forms.RadioButton rbEvenOdd;
        private System.Windows.Forms.NumericUpDown nmManagerDiscount;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numPrimeWeekend;
        private System.Windows.Forms.NumericUpDown numNonPrimeWeekend;
        private System.Windows.Forms.Label lblTotalBeforePackageDiscount;
        private System.Windows.Forms.Label lblPackageDiscount;
        private System.Windows.Forms.Label lblTotalAfterPackageDiscount;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton rbEvenDays;
        private System.Windows.Forms.RadioButton rbOddDays;
    }
}
