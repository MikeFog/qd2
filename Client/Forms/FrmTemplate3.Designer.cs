
namespace Merlin.Forms
{
    partial class FrmTemplate3
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
            this.dgvRollers = new System.Windows.Forms.DataGridView();
            this.gbRollerStats = new System.Windows.Forms.GroupBox();
            this.lblTotalQuantityCaption = new System.Windows.Forms.Label();
            this.lblTotalQuantityValue = new System.Windows.Forms.Label();
            this.lblGridQuantityCaption = new System.Windows.Forms.Label();
            this.lblGridQuantityValue = new System.Windows.Forms.Label();
            this.lblTotalDurationCaption = new System.Windows.Forms.Label();
            this.lblTotalDurationValue = new System.Windows.Forms.Label();
            this.lblCampaignPriceCaption = new System.Windows.Forms.Label();
            this.lblCampaignPriceValue = new System.Windows.Forms.Label();
            this.lblCompanyDiscountCaption = new System.Windows.Forms.Label();
            this.lblCompanyDiscountValue = new System.Windows.Forms.Label();
            this.lblTotalBeforePackageCaption = new System.Windows.Forms.Label();
            this.lblTotalBeforePackageValue = new System.Windows.Forms.Label();
            this.lblPackageDiscountCaption = new System.Windows.Forms.Label();
            this.lblPackageDiscountValue = new System.Windows.Forms.Label();
            this.lblGrandTotalCaption = new System.Windows.Forms.Label();
            this.lblGrandTotalValue = new System.Windows.Forms.Label();
            this.lblManagerDiscountCaption = new System.Windows.Forms.Label();
            this.lblManagerDiscountValue = new System.Windows.Forms.Label();
            this.btnEstimatePrice = new System.Windows.Forms.Button();
            this.clbWeekDays = new System.Windows.Forms.CheckedListBox();
            this.cbIgnoreWindows = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.pnlOddEven = new System.Windows.Forms.Panel();
            this.rbOdd = new System.Windows.Forms.RadioButton();
            this.rbEven = new System.Windows.Forms.RadioButton();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnOk = new System.Windows.Forms.Button();
            this.label7 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.numQuantityPrime = new System.Windows.Forms.NumericUpDown();
            this.numQuantityNonPrime = new System.Windows.Forms.NumericUpDown();
            this.cbSplitPrime = new System.Windows.Forms.CheckBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.rbNumber = new System.Windows.Forms.RadioButton();
            this.rbDays = new System.Windows.Forms.RadioButton();
            ((System.ComponentModel.ISupportInitialize)(this.txtQuantity)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRollers)).BeginInit();
            this.gbRollerStats.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.pnlOddEven.SuspendLayout();
            this.flowLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numQuantityPrime)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numQuantityNonPrime)).BeginInit();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // dtFinishDate
            // 
            this.dtFinishDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtFinishDate.Location = new System.Drawing.Point(317, 415);
            this.dtFinishDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtFinishDate.Name = "dtFinishDate";
            this.dtFinishDate.Size = new System.Drawing.Size(234, 31);
            this.dtFinishDate.TabIndex = 17;
            // 
            // dtStartDate
            // 
            this.dtStartDate.Format = System.Windows.Forms.DateTimePickerFormat.Short;
            this.dtStartDate.Location = new System.Drawing.Point(317, 376);
            this.dtStartDate.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStartDate.Name = "dtStartDate";
            this.dtStartDate.Size = new System.Drawing.Size(234, 31);
            this.dtStartDate.TabIndex = 16;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 411);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(200, 24);
            this.label2.TabIndex = 15;
            this.label2.Text = "Окончание  интервала:";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(3, 372);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(164, 24);
            this.label1.TabIndex = 14;
            this.label1.Text = "Начало интервала:";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(631, 411);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(267, 24);
            this.label3.TabIndex = 21;
            this.label3.Text = "Окончание  интервала (время):";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(631, 372);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(231, 24);
            this.label4.TabIndex = 20;
            this.label4.Text = "Начало интервала (время):";
            // 
            // dtFinishTime
            // 
            this.dtFinishTime.CustomFormat = "HH:mm";
            this.dtFinishTime.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
            this.dtFinishTime.Location = new System.Drawing.Point(945, 415);
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
            this.dtStartTime.Location = new System.Drawing.Point(945, 376);
            this.dtStartTime.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dtStartTime.Name = "dtStartTime";
            this.dtStartTime.ShowUpDown = true;
            this.dtStartTime.Size = new System.Drawing.Size(234, 31);
            this.dtStartTime.TabIndex = 22;
            // 
            // txtQuantity
            // 
            this.txtQuantity.Location = new System.Drawing.Point(317, 454);
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
            this.label5.Location = new System.Drawing.Point(3, 450);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(245, 24);
            this.label5.TabIndex = 25;
            this.label5.Text = "Количество выходов в день:";
            // 
            // dgvRollers
            // 
            this.dgvRollers.AllowUserToAddRows = false;
            this.dgvRollers.AllowUserToDeleteRows = false;
            this.dgvRollers.AllowUserToResizeRows = false;
            this.dgvRollers.ColumnHeadersHeight = 34;
            this.tableLayoutPanel1.SetColumnSpan(this.dgvRollers, 4);
            this.dgvRollers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRollers.Location = new System.Drawing.Point(3, 3);
            this.dgvRollers.Name = "dgvRollers";
            this.dgvRollers.RowHeadersVisible = false;
            this.dgvRollers.RowHeadersWidth = 62;
            this.dgvRollers.Size = new System.Drawing.Size(1252, 150);
            this.dgvRollers.TabIndex = 30;
            // 
            // gbRollerStats
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.gbRollerStats, 4);
            this.gbRollerStats.Controls.Add(this.lblTotalQuantityCaption);
            this.gbRollerStats.Controls.Add(this.lblTotalQuantityValue);
            this.gbRollerStats.Controls.Add(this.lblGridQuantityCaption);
            this.gbRollerStats.Controls.Add(this.lblGridQuantityValue);
            this.gbRollerStats.Controls.Add(this.lblTotalDurationCaption);
            this.gbRollerStats.Controls.Add(this.lblTotalDurationValue);
            this.gbRollerStats.Controls.Add(this.lblCampaignPriceCaption);
            this.gbRollerStats.Controls.Add(this.lblCampaignPriceValue);
            this.gbRollerStats.Controls.Add(this.lblCompanyDiscountCaption);
            this.gbRollerStats.Controls.Add(this.lblCompanyDiscountValue);
            this.gbRollerStats.Controls.Add(this.lblTotalBeforePackageCaption);
            this.gbRollerStats.Controls.Add(this.lblTotalBeforePackageValue);
            this.gbRollerStats.Controls.Add(this.lblPackageDiscountCaption);
            this.gbRollerStats.Controls.Add(this.lblPackageDiscountValue);
            this.gbRollerStats.Controls.Add(this.lblGrandTotalCaption);
            this.gbRollerStats.Controls.Add(this.lblGrandTotalValue);
            this.gbRollerStats.Controls.Add(this.lblManagerDiscountCaption);
            this.gbRollerStats.Controls.Add(this.lblManagerDiscountValue);
            this.gbRollerStats.Controls.Add(this.btnEstimatePrice);
            this.gbRollerStats.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbRollerStats.Location = new System.Drawing.Point(3, 159);
            this.gbRollerStats.Name = "gbRollerStats";
            this.gbRollerStats.Size = new System.Drawing.Size(1252, 210);
            this.gbRollerStats.TabIndex = 41;
            this.gbRollerStats.TabStop = false;
            this.gbRollerStats.Text = "Статистика";
            // 
            // lblTotalQuantityCaption
            // 
            this.lblTotalQuantityCaption.AutoSize = true;
            this.lblTotalQuantityCaption.Location = new System.Drawing.Point(10, 26);
            this.lblTotalQuantityCaption.Name = "lblTotalQuantityCaption";
            this.lblTotalQuantityCaption.Size = new System.Drawing.Size(248, 24);
            this.lblTotalQuantityCaption.TabIndex = 0;
            this.lblTotalQuantityCaption.Text = "Общее количество выходов:";
            // 
            // lblTotalQuantityValue
            // 
            this.lblTotalQuantityValue.AutoSize = true;
            this.lblTotalQuantityValue.Location = new System.Drawing.Point(290, 26);
            this.lblTotalQuantityValue.Name = "lblTotalQuantityValue";
            this.lblTotalQuantityValue.Size = new System.Drawing.Size(20, 24);
            this.lblTotalQuantityValue.TabIndex = 1;
            this.lblTotalQuantityValue.Text = "0";
            // 
            // lblGridQuantityCaption
            // 
            this.lblGridQuantityCaption.AutoSize = true;
            this.lblGridQuantityCaption.Location = new System.Drawing.Point(10, 54);
            this.lblGridQuantityCaption.Name = "lblGridQuantityCaption";
            this.lblGridQuantityCaption.Size = new System.Drawing.Size(164, 24);
            this.lblGridQuantityCaption.TabIndex = 10;
            this.lblGridQuantityCaption.Text = "Выбрано роликов:";
            // 
            // lblGridQuantityValue
            // 
            this.lblGridQuantityValue.AutoSize = true;
            this.lblGridQuantityValue.Location = new System.Drawing.Point(290, 54);
            this.lblGridQuantityValue.Name = "lblGridQuantityValue";
            this.lblGridQuantityValue.Size = new System.Drawing.Size(20, 24);
            this.lblGridQuantityValue.TabIndex = 11;
            this.lblGridQuantityValue.Text = "0";
            // 
            // lblTotalDurationCaption
            // 
            this.lblTotalDurationCaption.AutoSize = true;
            this.lblTotalDurationCaption.Location = new System.Drawing.Point(10, 82);
            this.lblTotalDurationCaption.Name = "lblTotalDurationCaption";
            this.lblTotalDurationCaption.Size = new System.Drawing.Size(191, 24);
            this.lblTotalDurationCaption.TabIndex = 2;
            this.lblTotalDurationCaption.Text = "Общий хронометраж:";
            // 
            // lblTotalDurationValue
            // 
            this.lblTotalDurationValue.AutoSize = true;
            this.lblTotalDurationValue.Location = new System.Drawing.Point(290, 82);
            this.lblTotalDurationValue.Name = "lblTotalDurationValue";
            this.lblTotalDurationValue.Size = new System.Drawing.Size(78, 24);
            this.lblTotalDurationValue.TabIndex = 3;
            this.lblTotalDurationValue.Text = "00:00:00";
            // 
            // lblCampaignPriceCaption
            // 
            this.lblCampaignPriceCaption.AutoSize = true;
            this.lblCampaignPriceCaption.Location = new System.Drawing.Point(650, 26);
            this.lblCampaignPriceCaption.Name = "lblCampaignPriceCaption";
            this.lblCampaignPriceCaption.Size = new System.Drawing.Size(140, 24);
            this.lblCampaignPriceCaption.TabIndex = 4;
            this.lblCampaignPriceCaption.Text = "Цена по тарифам:";
            // 
            // lblCampaignPriceValue
            // 
            this.lblCampaignPriceValue.AutoSize = true;
            this.lblCampaignPriceValue.Location = new System.Drawing.Point(930, 26);
            this.lblCampaignPriceValue.Name = "lblCampaignPriceValue";
            this.lblCampaignPriceValue.Size = new System.Drawing.Size(28, 24);
            this.lblCampaignPriceValue.TabIndex = 5;
            this.lblCampaignPriceValue.Text = "—";
            // 
            // lblCompanyDiscountCaption
            // 
            this.lblCompanyDiscountCaption.AutoSize = true;
            this.lblCompanyDiscountCaption.Location = new System.Drawing.Point(650, 54);
            this.lblCompanyDiscountCaption.Name = "lblCompanyDiscountCaption";
            this.lblCompanyDiscountCaption.Size = new System.Drawing.Size(159, 24);
            this.lblCompanyDiscountCaption.TabIndex = 6;
            this.lblCompanyDiscountCaption.Text = "Объёмная скидка:";
            // 
            // lblCompanyDiscountValue
            // 
            this.lblCompanyDiscountValue.AutoSize = true;
            this.lblCompanyDiscountValue.Location = new System.Drawing.Point(930, 54);
            this.lblCompanyDiscountValue.Name = "lblCompanyDiscountValue";
            this.lblCompanyDiscountValue.Size = new System.Drawing.Size(28, 24);
            this.lblCompanyDiscountValue.TabIndex = 7;
            this.lblCompanyDiscountValue.Text = "—";
            // 
            // lblTotalBeforePackageCaption
            // 
            this.lblTotalBeforePackageCaption.AutoSize = true;
            this.lblTotalBeforePackageCaption.Location = new System.Drawing.Point(650, 82);
            this.lblTotalBeforePackageCaption.Name = "lblTotalBeforePackageCaption";
            this.lblTotalBeforePackageCaption.Size = new System.Drawing.Size(280, 24);
            this.lblTotalBeforePackageCaption.TabIndex = 8;
            this.lblTotalBeforePackageCaption.Text = "Цена с учётом объёмной скидки:";
            // 
            // lblTotalBeforePackageValue
            // 
            this.lblTotalBeforePackageValue.AutoSize = true;
            this.lblTotalBeforePackageValue.Location = new System.Drawing.Point(930, 82);
            this.lblTotalBeforePackageValue.Name = "lblTotalBeforePackageValue";
            this.lblTotalBeforePackageValue.Size = new System.Drawing.Size(28, 24);
            this.lblTotalBeforePackageValue.TabIndex = 9;
            this.lblTotalBeforePackageValue.Text = "—";
            // 
            // lblPackageDiscountCaption
            // 
            this.lblPackageDiscountCaption.AutoSize = true;
            this.lblPackageDiscountCaption.Location = new System.Drawing.Point(650, 110);
            this.lblPackageDiscountCaption.Name = "lblPackageDiscountCaption";
            this.lblPackageDiscountCaption.Size = new System.Drawing.Size(149, 24);
            this.lblPackageDiscountCaption.TabIndex = 10;
            this.lblPackageDiscountCaption.Text = "Пакетная скидка:";
            // 
            // lblPackageDiscountValue
            // 
            this.lblPackageDiscountValue.AutoSize = true;
            this.lblPackageDiscountValue.Location = new System.Drawing.Point(930, 110);
            this.lblPackageDiscountValue.Name = "lblPackageDiscountValue";
            this.lblPackageDiscountValue.Size = new System.Drawing.Size(28, 24);
            this.lblPackageDiscountValue.TabIndex = 11;
            this.lblPackageDiscountValue.Text = "—";
            // 
            // lblGrandTotalCaption
            // 
            this.lblGrandTotalCaption.AutoSize = true;
            this.lblGrandTotalCaption.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Bold);
            this.lblGrandTotalCaption.Location = new System.Drawing.Point(650, 166);
            this.lblGrandTotalCaption.Name = "lblGrandTotalCaption";
            this.lblGrandTotalCaption.Size = new System.Drawing.Size(290, 24);
            this.lblGrandTotalCaption.TabIndex = 12;
            this.lblGrandTotalCaption.Text = "Цена с учётом всех скидок:";
            // 
            // lblGrandTotalValue
            // 
            this.lblGrandTotalValue.AutoSize = true;
            this.lblGrandTotalValue.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Bold);
            this.lblGrandTotalValue.Location = new System.Drawing.Point(930, 166);
            this.lblGrandTotalValue.Name = "lblGrandTotalValue";
            this.lblGrandTotalValue.Size = new System.Drawing.Size(28, 24);
            this.lblGrandTotalValue.TabIndex = 13;
            this.lblGrandTotalValue.Text = "—";
            //
            // lblManagerDiscountCaption
            //
            this.lblManagerDiscountCaption.AutoSize = true;
            this.lblManagerDiscountCaption.Location = new System.Drawing.Point(650, 138);
            this.lblManagerDiscountCaption.Name = "lblManagerDiscountCaption";
            this.lblManagerDiscountCaption.Size = new System.Drawing.Size(196, 24);
            this.lblManagerDiscountCaption.TabIndex = 16;
            this.lblManagerDiscountCaption.Text = "Менеджерская скидка:";
            //
            // lblManagerDiscountValue
            //
            this.lblManagerDiscountValue.AutoSize = true;
            this.lblManagerDiscountValue.Location = new System.Drawing.Point(930, 138);
            this.lblManagerDiscountValue.Name = "lblManagerDiscountValue";
            this.lblManagerDiscountValue.Size = new System.Drawing.Size(28, 24);
            this.lblManagerDiscountValue.TabIndex = 17;
            this.lblManagerDiscountValue.Text = "—";
            //
            // btnEstimatePrice
            // 
            this.btnEstimatePrice.Location = new System.Drawing.Point(10, 130);
            this.btnEstimatePrice.Name = "btnEstimatePrice";
            this.btnEstimatePrice.Size = new System.Drawing.Size(160, 30);
            this.btnEstimatePrice.TabIndex = 14;
            this.btnEstimatePrice.Text = "Рассчитать";
            this.btnEstimatePrice.UseVisualStyleBackColor = true;
            this.btnEstimatePrice.Click += new System.EventHandler(this.btnEstimatePrice_Click);
            // 
            // clbWeekDays
            // 
            this.clbWeekDays.BackColor = System.Drawing.SystemColors.Control;
            this.clbWeekDays.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.clbWeekDays.CheckOnClick = true;
            this.clbWeekDays.FormattingEnabled = true;
            this.clbWeekDays.Location = new System.Drawing.Point(3, 604);
            this.clbWeekDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.clbWeekDays.Name = "clbWeekDays";
            this.clbWeekDays.Size = new System.Drawing.Size(289, 196);
            this.clbWeekDays.TabIndex = 10;
            this.clbWeekDays.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.clbWeekDays_ItemCheck);
            // 
            // cbIgnoreWindows
            // 
            this.cbIgnoreWindows.AutoSize = true;
            this.cbIgnoreWindows.Checked = true;
            this.cbIgnoreWindows.CheckState = System.Windows.Forms.CheckState.Checked;
            this.tableLayoutPanel1.SetColumnSpan(this.cbIgnoreWindows, 4);
            this.cbIgnoreWindows.Location = new System.Drawing.Point(3, 807);
            this.cbIgnoreWindows.Name = "cbIgnoreWindows";
            this.cbIgnoreWindows.Size = new System.Drawing.Size(481, 28);
            this.cbIgnoreWindows.TabIndex = 33;
            this.cbIgnoreWindows.Text = "Не использовать окна где есть ролики данной фирмы";
            this.cbIgnoreWindows.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 4;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.clbWeekDays, 0, 7);
            this.tableLayoutPanel1.Controls.Add(this.cbIgnoreWindows, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this.pnlOddEven, 1, 7);
            this.tableLayoutPanel1.Controls.Add(this.dtStartDate, 1, 1);
            this.tableLayoutPanel1.Controls.Add(this.dgvRollers, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.gbRollerStats, 0, 9);
            this.tableLayoutPanel1.Controls.Add(this.label2, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.dtFinishDate, 1, 2);
            this.tableLayoutPanel1.Controls.Add(this.label4, 2, 1);
            this.tableLayoutPanel1.Controls.Add(this.txtQuantity, 1, 3);
            this.tableLayoutPanel1.Controls.Add(this.label5, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.dtStartTime, 3, 1);
            this.tableLayoutPanel1.Controls.Add(this.label3, 2, 2);
            this.tableLayoutPanel1.Controls.Add(this.dtFinishTime, 3, 2);
            this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 10);
            this.tableLayoutPanel1.Controls.Add(this.label7, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.label8, 2, 5);
            this.tableLayoutPanel1.Controls.Add(this.numQuantityPrime, 1, 5);
            this.tableLayoutPanel1.Controls.Add(this.numQuantityNonPrime, 3, 5);
            this.tableLayoutPanel1.Controls.Add(this.cbSplitPrime, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 6);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 11;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 156F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 216F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1258, 883);
            this.tableLayoutPanel1.TabIndex = 34;
            // 
            // pnlOddEven
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.pnlOddEven, 3);
            this.pnlOddEven.Controls.Add(this.rbOdd);
            this.pnlOddEven.Controls.Add(this.rbEven);
            this.pnlOddEven.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlOddEven.Location = new System.Drawing.Point(317, 603);
            this.pnlOddEven.Name = "pnlOddEven";
            this.pnlOddEven.Size = new System.Drawing.Size(938, 198);
            this.pnlOddEven.TabIndex = 9;
            // 
            // rbOdd
            // 
            this.rbOdd.AutoSize = true;
            this.rbOdd.Dock = System.Windows.Forms.DockStyle.Top;
            this.rbOdd.Enabled = false;
            this.rbOdd.Location = new System.Drawing.Point(0, 28);
            this.rbOdd.Name = "rbOdd";
            this.rbOdd.Size = new System.Drawing.Size(938, 28);
            this.rbOdd.TabIndex = 13;
            this.rbOdd.TabStop = true;
            this.rbOdd.Text = "Нечётные дни";
            this.rbOdd.UseVisualStyleBackColor = true;
            // 
            // rbEven
            // 
            this.rbEven.AutoSize = true;
            this.rbEven.Checked = true;
            this.rbEven.Dock = System.Windows.Forms.DockStyle.Top;
            this.rbEven.Enabled = false;
            this.rbEven.Location = new System.Drawing.Point(0, 0);
            this.rbEven.Name = "rbEven";
            this.rbEven.Size = new System.Drawing.Size(938, 28);
            this.rbEven.TabIndex = 12;
            this.rbEven.TabStop = true;
            this.rbEven.Text = "Чётные дни";
            this.rbEven.UseVisualStyleBackColor = true;
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 4);
            this.flowLayoutPanel1.Controls.Add(this.btnCancel);
            this.flowLayoutPanel1.Controls.Add(this.btnOk);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 841);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(1252, 39);
            this.flowLayoutPanel1.TabIndex = 34;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
            this.btnCancel.Location = new System.Drawing.Point(1149, 3);
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
            this.btnOk.Location = new System.Drawing.Point(1043, 3);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(100, 33);
            this.btnOk.TabIndex = 28;
            this.btnOk.Text = "Ок";
            this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(3, 523);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(160, 24);
            this.label7.TabIndex = 35;
            this.label7.Text = "Выходов в прайм:";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(631, 523);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(200, 24);
            this.label8.TabIndex = 36;
            this.label8.Text = "Выходов в офф прайм:";
            // 
            // numQuantityPrime
            // 
            this.numQuantityPrime.Enabled = false;
            this.numQuantityPrime.Location = new System.Drawing.Point(317, 526);
            this.numQuantityPrime.Name = "numQuantityPrime";
            this.numQuantityPrime.Size = new System.Drawing.Size(234, 31);
            this.numQuantityPrime.TabIndex = 37;
            // 
            // numQuantityNonPrime
            // 
            this.numQuantityNonPrime.Enabled = false;
            this.numQuantityNonPrime.Location = new System.Drawing.Point(945, 526);
            this.numQuantityNonPrime.Name = "numQuantityNonPrime";
            this.numQuantityNonPrime.Size = new System.Drawing.Size(234, 31);
            this.numQuantityNonPrime.TabIndex = 38;
            // 
            // cbSplitPrime
            // 
            this.cbSplitPrime.AutoSize = true;
            this.tableLayoutPanel1.SetColumnSpan(this.cbSplitPrime, 4);
            this.cbSplitPrime.Location = new System.Drawing.Point(3, 492);
            this.cbSplitPrime.Name = "cbSplitPrime";
            this.cbSplitPrime.Size = new System.Drawing.Size(335, 28);
            this.cbSplitPrime.TabIndex = 39;
            this.cbSplitPrime.Text = "С разбивкой на прайм и офф прайм";
            this.cbSplitPrime.UseVisualStyleBackColor = true;
            this.cbSplitPrime.CheckedChanged += new System.EventHandler(this.cbSplitPrime_CheckedChanged);
            // 
            // panel1
            // 
            this.tableLayoutPanel1.SetColumnSpan(this.panel1, 4);
            this.panel1.Controls.Add(this.rbNumber);
            this.panel1.Controls.Add(this.rbDays);
            this.panel1.Location = new System.Drawing.Point(3, 563);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(632, 34);
            this.panel1.TabIndex = 40;
            // 
            // rbNumber
            // 
            this.rbNumber.AutoCheck = false;
            this.rbNumber.AutoSize = true;
            this.rbNumber.Location = new System.Drawing.Point(142, 3);
            this.rbNumber.Name = "rbNumber";
            this.rbNumber.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.rbNumber.Size = new System.Drawing.Size(187, 28);
            this.rbNumber.TabIndex = 15;
            this.rbNumber.TabStop = true;
            this.rbNumber.Text = "Четный/Нечетный";
            this.rbNumber.UseVisualStyleBackColor = true;
            // 
            // rbDays
            // 
            this.rbDays.AutoCheck = false;
            this.rbDays.AutoSize = true;
            this.rbDays.Checked = true;
            this.rbDays.Location = new System.Drawing.Point(4, 4);
            this.rbDays.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.rbDays.Name = "rbDays";
            this.rbDays.Padding = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.rbDays.Size = new System.Drawing.Size(132, 28);
            this.rbDays.TabIndex = 12;
            this.rbDays.TabStop = true;
            this.rbDays.Text = "Дни недели";
            this.rbDays.UseVisualStyleBackColor = true;
            // 
            // FrmTemplate3
            // 
            this.AcceptButton = this.btnOk;
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 24F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(1258, 883);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmTemplate3";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Шаблон №3 для внесения роликов";
            this.Load += new System.EventHandler(this.FrmTemplate3_Load);
            ((System.ComponentModel.ISupportInitialize)(this.txtQuantity)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRollers)).EndInit();
            this.gbRollerStats.ResumeLayout(false);
            this.gbRollerStats.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.pnlOddEven.ResumeLayout(false);
            this.pnlOddEven.PerformLayout();
            this.flowLayoutPanel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.numQuantityPrime)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numQuantityNonPrime)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
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
        private System.Windows.Forms.DataGridView dgvRollers;
        private System.Windows.Forms.GroupBox gbRollerStats;
        private System.Windows.Forms.Label lblTotalQuantityCaption;
        private System.Windows.Forms.Label lblTotalQuantityValue;
        private System.Windows.Forms.Label lblGridQuantityCaption;
        private System.Windows.Forms.Label lblGridQuantityValue;
        private System.Windows.Forms.Label lblTotalDurationCaption;
        private System.Windows.Forms.Label lblTotalDurationValue;
        private System.Windows.Forms.Label lblCampaignPriceCaption;
        private System.Windows.Forms.Label lblCampaignPriceValue;
        private System.Windows.Forms.Label lblCompanyDiscountCaption;
        private System.Windows.Forms.Label lblCompanyDiscountValue;
        private System.Windows.Forms.Label lblTotalBeforePackageCaption;
        private System.Windows.Forms.Label lblTotalBeforePackageValue;
        private System.Windows.Forms.Label lblPackageDiscountCaption;
        private System.Windows.Forms.Label lblPackageDiscountValue;
        private System.Windows.Forms.Label lblGrandTotalCaption;
        private System.Windows.Forms.Label lblGrandTotalValue;
        private System.Windows.Forms.Label lblManagerDiscountCaption;
        private System.Windows.Forms.Label lblManagerDiscountValue;
        private System.Windows.Forms.Button btnEstimatePrice;
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
        private System.Windows.Forms.Panel pnlOddEven;
        private System.Windows.Forms.RadioButton rbOdd;
        private System.Windows.Forms.RadioButton rbEven;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.RadioButton rbNumber;
        private System.Windows.Forms.RadioButton rbDays;
    }
}