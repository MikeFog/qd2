// TemplateEditorControl.Designer.cs (with "Шаблон выходов": days vs even/odd)
// .NET Framework WinForms
namespace Merlin.Controls
{
    partial class TemplateEditorControl
    {
        private System.ComponentModel.IContainer components = null;
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
            this.tlTemplate = new System.Windows.Forms.TableLayoutPanel();
            this.rbEvenOdd = new System.Windows.Forms.RadioButton();
            this.lblTotalAfterPackageDiscount = new System.Windows.Forms.Label();
            this.nmManagerDiscount = new System.Windows.Forms.NumericUpDown();
            this.btnCalculate = new System.Windows.Forms.Button();
            this.lblDiscount = new System.Windows.Forms.Label();
            this.lblPos = new System.Windows.Forms.Label();
            this.cbPosition = new System.Windows.Forms.ComboBox();
            this.btnExcel = new System.Windows.Forms.Button();
            this.nudDuration = new System.Windows.Forms.NumericUpDown();
            this.lblDuration = new System.Windows.Forms.Label();
            this.flpOdds = new System.Windows.Forms.FlowLayoutPanel();
            this.rbEvenDays = new System.Windows.Forms.RadioButton();
            this.rbOddDays = new System.Windows.Forms.RadioButton();
            this.cmbCapmaighType = new System.Windows.Forms.ComboBox();
            this.flpDays = new System.Windows.Forms.FlowLayoutPanel();
            this.chkMon = new System.Windows.Forms.CheckBox();
            this.chkTue = new System.Windows.Forms.CheckBox();
            this.chkWed = new System.Windows.Forms.CheckBox();
            this.chkThu = new System.Windows.Forms.CheckBox();
            this.chkFri = new System.Windows.Forms.CheckBox();
            this.chkSat = new System.Windows.Forms.CheckBox();
            this.chkSun = new System.Windows.Forms.CheckBox();
            this.dtEnd = new System.Windows.Forms.DateTimePicker();
            this.rbDaysOfWeek = new System.Windows.Forms.RadioButton();
            this.dtStart = new System.Windows.Forms.DateTimePicker();
            this.numNonPrimeWeekend = new System.Windows.Forms.NumericUpDown();
            this.label3 = new System.Windows.Forms.Label();
            this.numPrimeWeekend = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblCity = new System.Windows.Forms.Label();
            this.cbCity = new System.Windows.Forms.ComboBox();
            this.lblPrime = new System.Windows.Forms.Label();
            this.nudPrimeWeekday = new System.Windows.Forms.NumericUpDown();
            this.lblNonPrime = new System.Windows.Forms.Label();
            this.nudNonPrimeWeekday = new System.Windows.Forms.NumericUpDown();
            this.label4 = new System.Windows.Forms.Label();
            this.tlTemplate.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmManagerDiscount)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).BeginInit();
            this.flpOdds.SuspendLayout();
            this.flpDays.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numNonPrimeWeekend)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPrimeWeekend)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPrimeWeekday)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNonPrimeWeekday)).BeginInit();
            this.SuspendLayout();
            // 
            // tlTemplate
            // 
            this.tlTemplate.AutoSize = true;
            this.tlTemplate.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlTemplate.ColumnCount = 8;
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tlTemplate.Controls.Add(this.rbEvenOdd, 0, 6);
            this.tlTemplate.Controls.Add(this.lblTotalAfterPackageDiscount, 7, 0);
            this.tlTemplate.Controls.Add(this.nmManagerDiscount, 6, 2);
            this.tlTemplate.Controls.Add(this.btnCalculate, 4, 6);
            this.tlTemplate.Controls.Add(this.lblDiscount, 5, 2);
            this.tlTemplate.Controls.Add(this.lblPos, 5, 1);
            this.tlTemplate.Controls.Add(this.cbPosition, 6, 1);
            this.tlTemplate.Controls.Add(this.btnExcel, 3, 6);
            this.tlTemplate.Controls.Add(this.nudDuration, 6, 0);
            this.tlTemplate.Controls.Add(this.lblDuration, 5, 0);
            this.tlTemplate.Controls.Add(this.flpOdds, 1, 6);
            this.tlTemplate.Controls.Add(this.cmbCapmaighType, 1, 1);
            this.tlTemplate.Controls.Add(this.flpDays, 1, 4);
            this.tlTemplate.Controls.Add(this.dtEnd, 2, 2);
            this.tlTemplate.Controls.Add(this.rbDaysOfWeek, 0, 4);
            this.tlTemplate.Controls.Add(this.dtStart, 1, 2);
            this.tlTemplate.Controls.Add(this.numNonPrimeWeekend, 4, 3);
            this.tlTemplate.Controls.Add(this.label3, 3, 3);
            this.tlTemplate.Controls.Add(this.numPrimeWeekend, 4, 2);
            this.tlTemplate.Controls.Add(this.label2, 3, 2);
            this.tlTemplate.Controls.Add(this.label1, 0, 1);
            this.tlTemplate.Controls.Add(this.label5, 0, 3);
            this.tlTemplate.Controls.Add(this.lblCity, 0, 0);
            this.tlTemplate.Controls.Add(this.cbCity, 1, 0);
            this.tlTemplate.Controls.Add(this.lblPrime, 3, 0);
            this.tlTemplate.Controls.Add(this.nudPrimeWeekday, 4, 0);
            this.tlTemplate.Controls.Add(this.lblNonPrime, 3, 1);
            this.tlTemplate.Controls.Add(this.nudNonPrimeWeekday, 4, 1);
            this.tlTemplate.Controls.Add(this.label4, 0, 2);
            this.tlTemplate.Dock = System.Windows.Forms.DockStyle.Top;
            this.tlTemplate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.tlTemplate.Location = new System.Drawing.Point(0, 0);
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
            this.tlTemplate.Size = new System.Drawing.Size(1200, 413);
            this.tlTemplate.TabIndex = 0;
            // 
            // rbEvenOdd
            // 
            this.rbEvenOdd.AutoSize = true;
            this.rbEvenOdd.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbEvenOdd.Location = new System.Drawing.Point(3, 236);
            this.rbEvenOdd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbEvenOdd.Name = "rbEvenOdd";
            this.rbEvenOdd.Size = new System.Drawing.Size(185, 29);
            this.rbEvenOdd.TabIndex = 1;
            this.rbEvenOdd.Text = "Чётные/нечётные:";
            this.rbEvenOdd.UseVisualStyleBackColor = true;
            // 
            // lblTotalAfterPackageDiscount
            // 
            this.lblTotalAfterPackageDiscount.AutoSize = true;
            this.lblTotalAfterPackageDiscount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblTotalAfterPackageDiscount.Location = new System.Drawing.Point(1140, 0);
            this.lblTotalAfterPackageDiscount.Name = "lblTotalAfterPackageDiscount";
            this.lblTotalAfterPackageDiscount.Size = new System.Drawing.Size(57, 25);
            this.lblTotalAfterPackageDiscount.TabIndex = 2;
            this.lblTotalAfterPackageDiscount.Text = "Итог:";
            // 
            // nmManagerDiscount
            // 
            this.nmManagerDiscount.DecimalPlaces = 2;
            this.nmManagerDiscount.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
            this.nmManagerDiscount.Location = new System.Drawing.Point(1067, 85);
            this.nmManagerDiscount.Name = "nmManagerDiscount";
            this.nmManagerDiscount.Size = new System.Drawing.Size(67, 31);
            this.nmManagerDiscount.TabIndex = 36;
            this.nmManagerDiscount.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nmManagerDiscount.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            // 
            // btnCalculate
            // 
            this.btnCalculate.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.btnCalculate.Location = new System.Drawing.Point(726, 232);
            this.btnCalculate.Margin = new System.Windows.Forms.Padding(0);
            this.btnCalculate.Name = "btnCalculate";
            this.btnCalculate.Size = new System.Drawing.Size(120, 38);
            this.btnCalculate.TabIndex = 1;
            this.btnCalculate.Text = "Рассчитать";
            this.btnCalculate.UseVisualStyleBackColor = true;
            // 
            // lblDiscount
            // 
            this.lblDiscount.AutoSize = true;
            this.lblDiscount.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblDiscount.Location = new System.Drawing.Point(849, 82);
            this.lblDiscount.Name = "lblDiscount";
            this.lblDiscount.Size = new System.Drawing.Size(212, 25);
            this.lblDiscount.TabIndex = 29;
            this.lblDiscount.Text = "Сезонный коэффициент:";
            // 
            // lblPos
            // 
            this.lblPos.AutoSize = true;
            this.lblPos.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblPos.Location = new System.Drawing.Point(849, 41);
            this.lblPos.Name = "lblPos";
            this.lblPos.Size = new System.Drawing.Size(88, 25);
            this.lblPos.TabIndex = 31;
            this.lblPos.Text = "Позиция:";
            // 
            // cbPosition
            // 
            this.cbPosition.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPosition.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbPosition.FormattingEnabled = true;
            this.cbPosition.IntegralHeight = false;
            this.cbPosition.Location = new System.Drawing.Point(1067, 45);
            this.cbPosition.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.cbPosition.Name = "cbPosition";
            this.cbPosition.Size = new System.Drawing.Size(67, 33);
            this.cbPosition.TabIndex = 32;
            // 
            // btnExcel
            // 
            this.btnExcel.Location = new System.Drawing.Point(537, 232);
            this.btnExcel.Margin = new System.Windows.Forms.Padding(0);
            this.btnExcel.Name = "btnExcel";
            this.btnExcel.Size = new System.Drawing.Size(120, 40);
            this.btnExcel.TabIndex = 42;
            this.btnExcel.Text = "Excel";
            this.btnExcel.UseVisualStyleBackColor = true;
            // 
            // nudDuration
            // 
            this.nudDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudDuration.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudDuration.Location = new System.Drawing.Point(1067, 5);
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
            this.nudDuration.Size = new System.Drawing.Size(67, 31);
            this.nudDuration.TabIndex = 23;
            this.nudDuration.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.nudDuration.ThousandsSeparator = true;
            this.nudDuration.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // lblDuration
            // 
            this.lblDuration.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblDuration.AutoSize = true;
            this.lblDuration.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblDuration.Location = new System.Drawing.Point(849, 8);
            this.lblDuration.Name = "lblDuration";
            this.lblDuration.Size = new System.Drawing.Size(110, 25);
            this.lblDuration.TabIndex = 22;
            this.lblDuration.Text = "Ролик (сек.):";
            // 
            // flpOdds
            // 
            this.tlTemplate.SetColumnSpan(this.flpOdds, 2);
            this.flpOdds.Controls.Add(this.rbEvenDays);
            this.flpOdds.Controls.Add(this.rbOddDays);
            this.flpOdds.Location = new System.Drawing.Point(194, 235);
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
            // cmbCapmaighType
            // 
            this.tlTemplate.SetColumnSpan(this.cmbCapmaighType, 2);
            this.cmbCapmaighType.FormattingEnabled = true;
            this.cmbCapmaighType.Location = new System.Drawing.Point(194, 44);
            this.cmbCapmaighType.Name = "cmbCapmaighType";
            this.cmbCapmaighType.Size = new System.Drawing.Size(283, 33);
            this.cmbCapmaighType.TabIndex = 6;
            // 
            // flpDays
            // 
            this.flpDays.AutoSize = true;
            this.flpDays.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.tlTemplate.SetColumnSpan(this.flpDays, 2);
            this.flpDays.Controls.Add(this.chkMon);
            this.flpDays.Controls.Add(this.chkTue);
            this.flpDays.Controls.Add(this.chkWed);
            this.flpDays.Controls.Add(this.chkThu);
            this.flpDays.Controls.Add(this.chkFri);
            this.flpDays.Controls.Add(this.chkSat);
            this.flpDays.Controls.Add(this.chkSun);
            this.flpDays.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.flpDays.Location = new System.Drawing.Point(191, 158);
            this.flpDays.Margin = new System.Windows.Forms.Padding(0);
            this.flpDays.Name = "flpDays";
            this.flpDays.Size = new System.Drawing.Size(285, 74);
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
            this.chkFri.Location = new System.Drawing.Point(0, 41);
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
            this.chkSat.Location = new System.Drawing.Point(71, 41);
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
            this.chkSun.Location = new System.Drawing.Point(143, 41);
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
            this.dtEnd.Location = new System.Drawing.Point(321, 86);
            this.dtEnd.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtEnd.Name = "dtEnd";
            this.dtEnd.Size = new System.Drawing.Size(99, 31);
            this.dtEnd.TabIndex = 5;
            // 
            // rbDaysOfWeek
            // 
            this.rbDaysOfWeek.AutoSize = true;
            this.rbDaysOfWeek.Checked = true;
            this.rbDaysOfWeek.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.rbDaysOfWeek.Location = new System.Drawing.Point(3, 162);
            this.rbDaysOfWeek.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbDaysOfWeek.Name = "rbDaysOfWeek";
            this.rbDaysOfWeek.Size = new System.Drawing.Size(135, 29);
            this.rbDaysOfWeek.TabIndex = 0;
            this.rbDaysOfWeek.TabStop = true;
            this.rbDaysOfWeek.Text = "Дни недели:";
            this.rbDaysOfWeek.UseVisualStyleBackColor = true;
            // 
            // dtStart
            // 
            this.dtStart.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.dtStart.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStart.Location = new System.Drawing.Point(194, 86);
            this.dtStart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStart.Name = "dtStart";
            this.dtStart.Size = new System.Drawing.Size(121, 31);
            this.dtStart.TabIndex = 3;
            // 
            // numNonPrimeWeekend
            // 
            this.numNonPrimeWeekend.Location = new System.Drawing.Point(729, 124);
            this.numNonPrimeWeekend.Name = "numNonPrimeWeekend";
            this.numNonPrimeWeekend.Size = new System.Drawing.Size(80, 31);
            this.numNonPrimeWeekend.TabIndex = 41;
            this.numNonPrimeWeekend.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numNonPrimeWeekend.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(540, 121);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(183, 25);
            this.label3.TabIndex = 39;
            this.label3.Text = "Выходные не прайм ";
            // 
            // numPrimeWeekend
            // 
            this.numPrimeWeekend.Location = new System.Drawing.Point(729, 85);
            this.numPrimeWeekend.Name = "numPrimeWeekend";
            this.numPrimeWeekend.Size = new System.Drawing.Size(80, 31);
            this.numPrimeWeekend.TabIndex = 40;
            this.numPrimeWeekend.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.numPrimeWeekend.Value = new decimal(new int[] {
            3,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(540, 82);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(154, 25);
            this.label2.TabIndex = 38;
            this.label2.Text = "Выходные прайм";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 41);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(153, 25);
            this.label1.TabIndex = 6;
            this.label1.Text = "Тип размещения:";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(3, 121);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(156, 25);
            this.label5.TabIndex = 44;
            this.label5.Text = "Шаблон выходов";
            // 
            // lblCity
            // 
            this.lblCity.AutoSize = true;
            this.lblCity.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblCity.Location = new System.Drawing.Point(3, 0);
            this.lblCity.Name = "lblCity";
            this.lblCity.Size = new System.Drawing.Size(73, 25);
            this.lblCity.TabIndex = 0;
            this.lblCity.Text = "Группа:";
            // 
            // cbCity
            // 
            this.tlTemplate.SetColumnSpan(this.cbCity, 2);
            this.cbCity.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbCity.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cbCity.FormattingEnabled = true;
            this.cbCity.IntegralHeight = false;
            this.cbCity.Location = new System.Drawing.Point(194, 4);
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
            this.lblPrime.Location = new System.Drawing.Point(540, 8);
            this.lblPrime.Name = "lblPrime";
            this.lblPrime.Size = new System.Drawing.Size(123, 25);
            this.lblPrime.TabIndex = 27;
            this.lblPrime.Text = "Будни прайм:";
            // 
            // nudPrimeWeekday
            // 
            this.nudPrimeWeekday.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudPrimeWeekday.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudPrimeWeekday.Location = new System.Drawing.Point(729, 5);
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
            // lblNonPrime
            // 
            this.lblNonPrime.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblNonPrime.AutoSize = true;
            this.lblNonPrime.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblNonPrime.Location = new System.Drawing.Point(540, 49);
            this.lblNonPrime.Name = "lblNonPrime";
            this.lblNonPrime.Size = new System.Drawing.Size(147, 25);
            this.lblNonPrime.TabIndex = 33;
            this.lblNonPrime.Text = "Будни не прайм:";
            // 
            // nudNonPrimeWeekday
            // 
            this.nudNonPrimeWeekday.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.nudNonPrimeWeekday.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.nudNonPrimeWeekday.Location = new System.Drawing.Point(729, 46);
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
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(3, 82);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(134, 25);
            this.label4.TabIndex = 43;
            this.label4.Text = "Период акции:";
            // 
            // TemplateEditorControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.tlTemplate);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(0);
            this.MinimumSize = new System.Drawing.Size(800, 0);
            this.Name = "TemplateEditorControl";
            this.Size = new System.Drawing.Size(800, 238);
            this.tlTemplate.ResumeLayout(false);
            this.tlTemplate.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nmManagerDiscount)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudDuration)).EndInit();
            this.flpOdds.ResumeLayout(false);
            this.flpOdds.PerformLayout();
            this.flpDays.ResumeLayout(false);
            this.flpDays.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numNonPrimeWeekend)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numPrimeWeekend)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudPrimeWeekday)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nudNonPrimeWeekday)).EndInit();
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
        private System.Windows.Forms.Label lblTotalAfterPackageDiscount;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cmbCapmaighType;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
    }
}
