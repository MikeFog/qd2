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
        private System.Windows.Forms.DateTimePicker dtStart;
        private System.Windows.Forms.DateTimePicker dtEnd;
        private System.Windows.Forms.Button btnCalculate;

        private System.Windows.Forms.Label lblDuration;
        private System.Windows.Forms.NumericUpDown nudDuration;

        private System.Windows.Forms.Label lblPrime;
        private System.Windows.Forms.NumericUpDown nudPrimeWeekday;

        private System.Windows.Forms.Label lblDiscount;

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
            this.lblTotalAfterPackageDiscount = new System.Windows.Forms.Label();
            this.lblPackageDiscount = new System.Windows.Forms.Label();
            this.lblTotalBeforePackageDiscount = new System.Windows.Forms.Label();
            this.flpDays = new System.Windows.Forms.FlowLayoutPanel();
            this.chkMon = new System.Windows.Forms.CheckBox();
            this.chkTue = new System.Windows.Forms.CheckBox();
            this.chkWed = new System.Windows.Forms.CheckBox();
            this.chkThu = new System.Windows.Forms.CheckBox();
            this.chkFri = new System.Windows.Forms.CheckBox();
            this.chkSat = new System.Windows.Forms.CheckBox();
            this.chkSun = new System.Windows.Forms.CheckBox();
            this.dtEnd = new System.Windows.Forms.DateTimePicker();
            this.dtStart = new System.Windows.Forms.DateTimePicker();
            this.lblCity = new System.Windows.Forms.Label();
            this.rbDaysOfWeek = new System.Windows.Forms.RadioButton();
            this.rbEvenOdd = new System.Windows.Forms.RadioButton();
            this.cbCity = new System.Windows.Forms.ComboBox();
            this.lblPrime = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.nudPrimeWeekday = new System.Windows.Forms.NumericUpDown();
            this.numPrimeWeekend = new System.Windows.Forms.NumericUpDown();
            this.lblNonPrime = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.nudNonPrimeWeekday = new System.Windows.Forms.NumericUpDown();
            this.numNonPrimeWeekend = new System.Windows.Forms.NumericUpDown();
            this.lblDuration = new System.Windows.Forms.Label();
            this.nudDuration = new System.Windows.Forms.NumericUpDown();
            this.nmManagerDiscount = new System.Windows.Forms.NumericUpDown();
            this.cbPosition = new System.Windows.Forms.ComboBox();
            this.lblPos = new System.Windows.Forms.Label();
            this.btnCalculate = new System.Windows.Forms.Button();
            this.btnExcel = new System.Windows.Forms.Button();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.flpOdds = new System.Windows.Forms.FlowLayoutPanel();
            this.rbEvenDays = new System.Windows.Forms.RadioButton();
            this.rbOddDays = new System.Windows.Forms.RadioButton();
            this.gbTemplate.SuspendLayout();
            this.tlTemplate.SuspendLayout();
            this.flpDays.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPrimeWeekday)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPrimeWeekend)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNonPrimeWeekday)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNonPrimeWeekend)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmManagerDiscount)).BeginInit();
            this.flpOdds.SuspendLayout();
            this.SuspendLayout();
            // 
            // gbTemplate
            // 
            this.gbTemplate.AutoSize = true;
            this.gbTemplate.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.gbTemplate.Controls.Add(this.tlTemplate);
            this.gbTemplate.Dock = System.Windows.Forms.DockStyle.Top;
            this.gbTemplate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.gbTemplate.Location = new System.Drawing.Point(0, 0);
            this.gbTemplate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.gbTemplate.MinimumSize = new System.Drawing.Size(1200, 0);
            this.gbTemplate.Name = "gbTemplate";
            this.gbTemplate.Padding = new System.Windows.Forms.Padding(9, 10, 9, 10);
            this.gbTemplate.Size = new System.Drawing.Size(1400, 274);
            this.gbTemplate.TabIndex = 0;
            this.gbTemplate.TabStop = false;
            this.gbTemplate.Text = "Общие параметры размещения";
            // 
            // tlTemplate
            // 
            this.tlTemplate.AutoSize = true;
            this.tlTemplate.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlTemplate.ColumnCount = 8;
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 100F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 380F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 5F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 180F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 120F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 150F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 50F));
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.Controls.Add(this.lblTotalAfterPackageDiscount, 7, 2);
            this.tlTemplate.Controls.Add(this.lblPackageDiscount, 7, 1);
            this.tlTemplate.Controls.Add(this.lblTotalBeforePackageDiscount, 7, 0);
            this.tlTemplate.Controls.Add(this.flpDays, 1, 2);
            this.tlTemplate.Controls.Add(this.dtEnd, 1, 1);
            this.tlTemplate.Controls.Add(this.dtStart, 0, 1);
            this.tlTemplate.Controls.Add(this.lblCity, 0, 0);
            this.tlTemplate.Controls.Add(this.rbDaysOfWeek, 0, 2);
            this.tlTemplate.Controls.Add(this.rbEvenOdd, 0, 3);
            this.tlTemplate.Controls.Add(this.cbCity, 1, 0);
            this.tlTemplate.Controls.Add(this.lblPrime, 3, 0);
            this.tlTemplate.Controls.Add(this.label2, 5, 0);
            this.tlTemplate.Controls.Add(this.nudPrimeWeekday, 4, 0);
            this.tlTemplate.Controls.Add(this.numPrimeWeekend, 6, 0);
            this.tlTemplate.Controls.Add(this.lblNonPrime, 3, 1);
            this.tlTemplate.Controls.Add(this.label3, 5, 1);
            this.tlTemplate.Controls.Add(this.nudNonPrimeWeekday, 4, 1);
            this.tlTemplate.Controls.Add(this.numNonPrimeWeekend, 6, 1);
            this.tlTemplate.Controls.Add(this.lblDuration, 3, 2);
            this.tlTemplate.Controls.Add(this.nudDuration, 4, 2);
            this.tlTemplate.Controls.Add(this.nmManagerDiscount, 4, 3);
            this.tlTemplate.Controls.Add(this.cbPosition, 4, 4);
            this.tlTemplate.Controls.Add(this.lblPos, 3, 4);
            this.tlTemplate.Controls.Add(this.btnCalculate, 5, 2);
            this.tlTemplate.Controls.Add(this.btnExcel, 5, 3);
            this.tlTemplate.Controls.Add(this.lblDiscount, 3, 3);
            this.tlTemplate.Controls.Add(this.flpOdds, 1, 3);
            this.tlTemplate.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlTemplate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tlTemplate.Location = new System.Drawing.Point(9, 34);
            this.tlTemplate.Margin = new System.Windows.Forms.Padding(0);
            this.tlTemplate.MinimumSize = new System.Drawing.Size(1200, 0);
            this.tlTemplate.Name = "tlTemplate";
            this.tlTemplate.RowCount = 5;
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tlTemplate.Size = new System.Drawing.Size(1382, 230);
            this.tlTemplate.TabIndex = 0;
            // 
            // lblTotalAfterPackageDiscount
            // 
            this.lblTotalAfterPackageDiscount.AutoSize = true;
            this.lblTotalAfterPackageDiscount.Location = new System.Drawing.Point(988, 100);
            this.lblTotalAfterPackageDiscount.Name = "lblTotalAfterPackageDiscount";
            this.lblTotalAfterPackageDiscount.Size = new System.Drawing.Size(54, 25);
            this.lblTotalAfterPackageDiscount.TabIndex = 2;
            this.lblTotalAfterPackageDiscount.Text = "Итог:";
            // 
            // lblPackageDiscount
            // 
            this.lblPackageDiscount.AutoSize = true;
            this.lblPackageDiscount.Location = new System.Drawing.Point(988, 50);
            this.lblPackageDiscount.Name = "lblPackageDiscount";
            this.lblPackageDiscount.Size = new System.Drawing.Size(151, 25);
            this.lblPackageDiscount.TabIndex = 2;
            this.lblPackageDiscount.Text = "Пакетная скидка:";
            // 
            // lblTotalBeforePackageDiscount
            // 
            this.lblTotalBeforePackageDiscount.AutoSize = true;
            this.lblTotalBeforePackageDiscount.Location = new System.Drawing.Point(988, 0);
            this.lblTotalBeforePackageDiscount.Name = "lblTotalBeforePackageDiscount";
            this.lblTotalBeforePackageDiscount.Size = new System.Drawing.Size(173, 25);
            this.lblTotalBeforePackageDiscount.TabIndex = 43;
            this.lblTotalBeforePackageDiscount.Text = "По радиостанциям:";
            // 
            // flpDays
            // 
            this.flpDays.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.flpDays.BackColor = System.Drawing.SystemColors.Control;
            this.flpDays.Controls.Add(this.chkMon);
            this.flpDays.Controls.Add(this.chkTue);
            this.flpDays.Controls.Add(this.chkWed);
            this.flpDays.Controls.Add(this.chkThu);
            this.flpDays.Controls.Add(this.chkFri);
            this.flpDays.Controls.Add(this.chkSat);
            this.flpDays.Controls.Add(this.chkSun);
            this.flpDays.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.flpDays.Location = new System.Drawing.Point(100, 100);
            this.flpDays.Margin = new System.Windows.Forms.Padding(0);
            this.flpDays.Name = "flpDays";
            this.flpDays.Size = new System.Drawing.Size(380, 33);
            this.flpDays.TabIndex = 1;
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
            this.chkSat.Location = new System.Drawing.Point(0, 41);
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
            this.chkSun.Location = new System.Drawing.Point(72, 41);
            this.chkSun.Margin = new System.Windows.Forms.Padding(0, 4, 0, 4);
            this.chkSun.Name = "chkSun";
            this.chkSun.Size = new System.Drawing.Size(56, 29);
            this.chkSun.TabIndex = 6;
            this.chkSun.Text = "Вс";
            // 
            // dtEnd
            // 
            this.dtEnd.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.dtEnd.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtEnd.Location = new System.Drawing.Point(103, 54);
            this.dtEnd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtEnd.Name = "dtEnd";
            this.dtEnd.Size = new System.Drawing.Size(99, 31);
            this.dtEnd.TabIndex = 5;
            // 
            // dtStart
            // 
            this.dtStart.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.dtStart.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStart.Location = new System.Drawing.Point(3, 54);
            this.dtStart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStart.Name = "dtStart";
            this.dtStart.Size = new System.Drawing.Size(87, 31);
            this.dtStart.TabIndex = 3;
            // 
            // lblCity
            // 
            this.lblCity.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblCity.AutoSize = true;
            this.lblCity.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblCity.Location = new System.Drawing.Point(3, 12);
            this.lblCity.Name = "lblCity";
            this.lblCity.Size = new System.Drawing.Size(73, 25);
            this.lblCity.TabIndex = 0;
            this.lblCity.Text = "Группа:";
            // 
            // rbDaysOfWeek
            // 
            this.rbDaysOfWeek.AutoSize = true;
            this.rbDaysOfWeek.Checked = true;
            this.rbDaysOfWeek.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbDaysOfWeek.Location = new System.Drawing.Point(3, 104);
            this.rbDaysOfWeek.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbDaysOfWeek.Name = "rbDaysOfWeek";
            this.rbDaysOfWeek.Size = new System.Drawing.Size(94, 29);
            this.rbDaysOfWeek.TabIndex = 0;
            this.rbDaysOfWeek.TabStop = true;
            this.rbDaysOfWeek.Text = "Дни недели:";
            this.rbDaysOfWeek.UseVisualStyleBackColor = true;
            // 
            // rbEvenOdd
            // 
            this.rbEvenOdd.AutoSize = true;
            this.rbEvenOdd.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbEvenOdd.Location = new System.Drawing.Point(3, 143);
            this.rbEvenOdd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbEvenOdd.Name = "rbEvenOdd";
            this.rbEvenOdd.Size = new System.Drawing.Size(94, 29);
            this.rbEvenOdd.TabIndex = 1;
            this.rbEvenOdd.Text = "Чётные/нечётные:";
            this.rbEvenOdd.UseVisualStyleBackColor = true;
            // 
            // cbCity
            // 
            this.tlTemplate.SetColumnSpan(this.cbCity, 2);
            this.cbCity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCity.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbCity.FormattingEnabled = true;
            this.cbCity.IntegralHeight = false;
            this.cbCity.Location = new System.Drawing.Point(103, 4);
            this.cbCity.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cbCity.Name = "cbCity";
            this.cbCity.Size = new System.Drawing.Size(283, 33);
            this.cbCity.TabIndex = 1;
            // 
            // lblPrime
            // 
            this.lblPrime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblPrime.AutoSize = true;
            this.lblPrime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPrime.Location = new System.Drawing.Point(488, 12);
            this.lblPrime.Name = "lblPrime";
            this.lblPrime.Size = new System.Drawing.Size(126, 25);
            this.lblPrime.TabIndex = 27;
            this.lblPrime.Text = "Прайм будни:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(788, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(96, 50);
            this.label2.TabIndex = 38;
            this.label2.Text = "Прайм выходные";
            // 
            // nudPrimeWeekday
            // 
            this.nudPrimeWeekday.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudPrimeWeekday.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudPrimeWeekday.Location = new System.Drawing.Point(668, 9);
            this.nudPrimeWeekday.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.nudPrimeWeekday.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudPrimeWeekday.Name = "nudPrimeWeekday";
            this.nudPrimeWeekday.Size = new System.Drawing.Size(80, 31);
            this.nudPrimeWeekday.TabIndex = 28;
            this.nudPrimeWeekday.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudPrimeWeekday.ThousandsSeparator = true;
            this.nudPrimeWeekday.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // numPrimeWeekend
            // 
            this.numPrimeWeekend.Location = new System.Drawing.Point(938, 3);
            this.numPrimeWeekend.Name = "numPrimeWeekend";
            this.numPrimeWeekend.Size = new System.Drawing.Size(44, 31);
            this.numPrimeWeekend.TabIndex = 40;
            this.numPrimeWeekend.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPrimeWeekend.Value = new decimal(new int[] {
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
            this.lblNonPrime.Location = new System.Drawing.Point(488, 62);
            this.lblNonPrime.Name = "lblNonPrime";
            this.lblNonPrime.Size = new System.Drawing.Size(155, 25);
            this.lblNonPrime.TabIndex = 33;
            this.lblNonPrime.Text = "Не прайм  будни:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(788, 50);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(97, 50);
            this.label3.TabIndex = 39;
            this.label3.Text = "Не прайм выходные";
            // 
            // nudNonPrimeWeekday
            // 
            this.nudNonPrimeWeekday.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudNonPrimeWeekday.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudNonPrimeWeekday.Location = new System.Drawing.Point(668, 59);
            this.nudNonPrimeWeekday.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.nudNonPrimeWeekday.Maximum = new decimal(new int[] {
            200,
            0,
            0,
            0});
            this.nudNonPrimeWeekday.Name = "nudNonPrimeWeekday";
            this.nudNonPrimeWeekday.Size = new System.Drawing.Size(80, 31);
            this.nudNonPrimeWeekday.TabIndex = 34;
            this.nudNonPrimeWeekday.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudNonPrimeWeekday.ThousandsSeparator = true;
            this.nudNonPrimeWeekday.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // numNonPrimeWeekend
            // 
            this.numNonPrimeWeekend.Location = new System.Drawing.Point(938, 53);
            this.numNonPrimeWeekend.Name = "numNonPrimeWeekend";
            this.numNonPrimeWeekend.Size = new System.Drawing.Size(44, 31);
            this.numNonPrimeWeekend.TabIndex = 41;
            this.numNonPrimeWeekend.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numNonPrimeWeekend.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // lblDuration
            // 
            this.lblDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDuration.AutoSize = true;
            this.lblDuration.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblDuration.Location = new System.Drawing.Point(488, 107);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(110, 25);
            this.lblDuration.TabIndex = 22;
            this.lblDuration.Text = "Ролик (сек.):";
            // 
            // nudDuration
            // 
            this.nudDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudDuration.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudDuration.Location = new System.Drawing.Point(668, 104);
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
            this.nudDuration.Size = new System.Drawing.Size(80, 31);
            this.nudDuration.TabIndex = 23;
            this.nudDuration.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudDuration.ThousandsSeparator = true;
            this.nudDuration.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // nmManagerDiscount
            // 
            this.nmManagerDiscount.DecimalPlaces = 2;
            this.nmManagerDiscount.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.nmManagerDiscount.Location = new System.Drawing.Point(668, 142);
            this.nmManagerDiscount.Name = "nmManagerDiscount";
            this.nmManagerDiscount.Size = new System.Drawing.Size(80, 31);
            this.nmManagerDiscount.TabIndex = 36;
            this.nmManagerDiscount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nmManagerDiscount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // cbPosition
            // 
            this.cbPosition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPosition.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbPosition.FormattingEnabled = true;
            this.cbPosition.IntegralHeight = false;
            this.cbPosition.Location = new System.Drawing.Point(668, 193);
            this.cbPosition.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cbPosition.Name = "cbPosition";
            this.cbPosition.Size = new System.Drawing.Size(114, 33);
            this.cbPosition.TabIndex = 32;
            // 
            // lblPos
            // 
            this.lblPos.AutoSize = true;
            this.lblPos.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPos.Location = new System.Drawing.Point(488, 189);
            this.lblPos.Name = "lblPos";
            this.lblPos.Size = new System.Drawing.Size(88, 25);
            this.lblPos.TabIndex = 31;
            this.lblPos.Text = "Позиция:";
            // 
            // btnCalculate
            // 
            this.btnCalculate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCalculate.Location = new System.Drawing.Point(785, 100);
            this.btnCalculate.Margin = new System.Windows.Forms.Padding(0);
            this.btnCalculate.Name = "btnCalculate";
            this.btnCalculate.Size = new System.Drawing.Size(120, 38);
            this.btnCalculate.TabIndex = 1;
            this.btnCalculate.Text = "Рассчитать";
            this.btnCalculate.UseVisualStyleBackColor = true;
            // 
            // btnExcel
            // 
            this.btnExcel.Location = new System.Drawing.Point(785, 139);
            this.btnExcel.Margin = new System.Windows.Forms.Padding(0);
            this.btnExcel.Name = "btnExcel";
            this.btnExcel.Size = new System.Drawing.Size(120, 40);
            this.btnExcel.TabIndex = 42;
            this.btnExcel.Text = "Excel";
            this.btnExcel.UseVisualStyleBackColor = true;
            // 
            // lblDiscount
            // 
            this.lblDiscount.AutoSize = true;
            this.lblDiscount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblDiscount.Location = new System.Drawing.Point(488, 139);
            this.lblDiscount.Name = "lblDiscount";
            this.lblDiscount.Size = new System.Drawing.Size(125, 50);
            this.lblDiscount.TabIndex = 29;
            this.lblDiscount.Text = "Сезонный коэффициент:";
            // 
            // flpOdds
            // 
            this.flpOdds.Controls.Add(this.rbEvenDays);
            this.flpOdds.Controls.Add(this.rbOddDays);
            this.flpOdds.Location = new System.Drawing.Point(103, 142);
            this.flpOdds.Name = "flpOdds";
            this.flpOdds.Size = new System.Drawing.Size(230, 37);
            this.flpOdds.TabIndex = 1;
            // 
            // rbEvenDays
            // 
            this.rbEvenDays.AutoSize = true;
            this.rbEvenDays.Checked = true;
            this.rbEvenDays.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbEvenDays.Location = new System.Drawing.Point(3, 4);
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
            this.rbOddDays.Location = new System.Drawing.Point(106, 4);
            this.rbOddDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbOddDays.Name = "rbOddDays";
            this.rbOddDays.Size = new System.Drawing.Size(117, 29);
            this.rbOddDays.TabIndex = 5;
            this.rbOddDays.Text = "Нечётные";
            this.rbOddDays.UseVisualStyleBackColor = true;
            // 
            // TemplateEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.gbTemplate);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(0);
            this.MinimumSize = new System.Drawing.Size(1400, 0);
            this.Size = new System.Drawing.Size(1400, 0);
            this.gbTemplate.ResumeLayout(false);
            this.gbTemplate.PerformLayout();
            this.tlTemplate.ResumeLayout(false);
            this.tlTemplate.PerformLayout();
            this.flpDays.ResumeLayout(false);
            this.flpDays.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudPrimeWeekday)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPrimeWeekend)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNonPrimeWeekday)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numNonPrimeWeekend)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nmManagerDiscount)).EndInit();
            this.flpOdds.ResumeLayout(false);
            this.flpOdds.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
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
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.NumericUpDown numPrimeWeekend;
        private System.Windows.Forms.NumericUpDown numNonPrimeWeekend;
        private System.Windows.Forms.RadioButton rbEvenDays;
        private System.Windows.Forms.RadioButton rbOddDays;
        private System.Windows.Forms.Button btnExcel;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox cbPosition;
        private System.Windows.Forms.Label lblPos;
        private System.Windows.Forms.FlowLayoutPanel flpOdds;
        private System.Windows.Forms.Label lblTotalBeforePackageDiscount;
        private System.Windows.Forms.Label lblPackageDiscount;
        private System.Windows.Forms.Label lblTotalAfterPackageDiscount;
    }
}
