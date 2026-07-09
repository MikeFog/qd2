using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class FrmTemplate3 : Form
    {
        private const string ColSelected = "colSelected";
        private const string ColRollerName = "colRollerName";
        private const string ColAdvertSubject = "colAdvertSubject";
        private const string ColDuration = "colDuration";
        private const string ColQuantity = "colQuantity";

        private readonly IssueTemplate _template;
        private readonly IList<int> _massmediaIds;
        private readonly RollerPositions _position;

        public FrmTemplate3()
        {
            InitializeComponent();
            InitRollersGrid();
        }

        public FrmTemplate3(Firm firm, IssueTemplate template, IList<int> massmediaIds, RollerPositions position) : this()
        {
            _position = position;
            if (template != null && template.StartTime != DateTime.MinValue && template.FinishTime != DateTime.MinValue)
                _template = template;
            else
            {
                var (startDate, finishDate) = GetDefaultPeriod();
                _template = new IssueTemplate(startDate, finishDate,
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 6, 0, 0),
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 21, 0, 0),
                    2)
                {
                    IsModeAdd = true,
                    IgnoreWindowsWithTheSameFirmIssue = true
                };
                for (int i = 0; i < _template.WeekDays.Length; i++)
                    _template.WeekDays[i] = true;
            }
            _massmediaIds = massmediaIds;
            dgvRollers.DataSource = firm.GetRollers();
        }

        private void InitRollersGrid()
        {
            dgvRollers.AutoGenerateColumns = false;
            dgvRollers.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = ColSelected,
                HeaderText = string.Empty,
                Width = 40
            });
            dgvRollers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColRollerName,
                HeaderText = "Ролик",
                DataPropertyName = "name",
                ReadOnly = true,
                Width = 250
            });
            dgvRollers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColAdvertSubject,
                HeaderText = "Предмет рекламы",
                DataPropertyName = "advertTypeName",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
            });
            dgvRollers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColDuration,
                HeaderText = "Продолжительность",
                DataPropertyName = "durationString",
                ReadOnly = true,
                Width = 130
            });
            dgvRollers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColQuantity,
                HeaderText = "Количество",
                Width = 110
            });

            // Чекбокс должен коммититься сразу, иначе CellValueChanged сработает только после ухода с ячейки
            dgvRollers.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvRollers.IsCurrentCellDirty && dgvRollers.CurrentCell is DataGridViewCheckBoxCell)
                    dgvRollers.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            dgvRollers.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex < 0)
                    return;
                string columnName = dgvRollers.Columns[e.ColumnIndex].Name;

                // При вводе количества автоматически отмечаем ролик галочкой
                if (columnName == ColQuantity)
                {
                    DataGridViewRow row = dgvRollers.Rows[e.RowIndex];
                    if (int.TryParse(row.Cells[ColQuantity].Value?.ToString(), out int quantity) && quantity > 0)
                        row.Cells[ColSelected].Value = true;
                }

                if (columnName == ColSelected || columnName == ColQuantity)
                    RecalculateStats();
            };
        }

        private void RecalculateStats()
        {
            // "Общее количество выходов" — не сумма по гриду, а то, что задают настройки
            // интервала/дней недели/чёт-нечёт и количества в день (перекочевали из Шаблона 2).
            // Разбивка по роликам в гриде должна СОВПАСТЬ с этим числом — проверяется при расчёте цены,
            // а расхождение видно сразу по цвету строки "Сумма по гриду роликов".
            int expectedTotalQuantity = GetExpectedTotalQuantity();
            lblTotalQuantityValue.Text = expectedTotalQuantity.ToString();

            var (gridTotalQuantity, totalDurationSeconds) = GetCheckedRollerAggregates();
            lblGridQuantityValue.Text = gridTotalQuantity.ToString();
            lblGridQuantityValue.ForeColor = gridTotalQuantity == expectedTotalQuantity ? Color.Green : Color.Red;

            lblTotalDurationValue.Text = FormatDuration(totalDurationSeconds);
        }

        private int GetExpectedTotalQuantity()
        {
            SaveData2Template();
            _template.Reset();
            int dailyQuantity = cbSplitPrime.Checked
                ? (int)numQuantityPrime.Value + (int)numQuantityNonPrime.Value
                : (int)txtQuantity.Value;
            return dailyQuantity * _template.DaysCount;
        }

        // Сумма "Количество" и суммарный хронометраж (duration * quantity) по отмеченным роликам грида
        private (int TotalQuantity, long TotalDurationSeconds) GetCheckedRollerAggregates()
        {
            int totalQuantity = 0;
            long totalDurationSeconds = 0;
            foreach (DataGridViewRow row in dgvRollers.Rows)
            {
                if (row.IsNewRow || row.DataBoundItem == null)
                    continue;
                if (!(row.Cells[ColSelected].Value is bool isSelected) || !isSelected)
                    continue;

                int.TryParse(row.Cells[ColQuantity].Value?.ToString(), out int quantity);
                DataRow dataRow = ((DataRowView)row.DataBoundItem).Row;
                int duration = dataRow["duration"] == DBNull.Value ? 0 : Convert.ToInt32(dataRow["duration"]);

                totalQuantity += quantity;
                totalDurationSeconds += (long)duration * quantity;
            }
            return (totalQuantity, totalDurationSeconds);
        }

        private static string FormatDuration(long totalSeconds)
        {
            long hours = totalSeconds / 3600;
            int minutes = (int)(totalSeconds % 3600) / 60;
            int seconds = (int)(totalSeconds % 60);
            return $"{hours:00}:{minutes:00}:{seconds:00}";
        }

        // Черновая оценка цены до реального размещения: переиспользует PriceCalculatorGrid
        // (тот же движок, что в калькуляторе цены). Длительность ролика — среднее по отмеченным
        // роликам, взвешенное по их количеству выходов (роликов с большим количеством выходов
        // в среднем учитывается больше).
        private void btnEstimatePrice_Click(object sender, EventArgs e)
        {
            var (gridTotalQuantity, totalDurationSeconds) = GetCheckedRollerAggregates();
            if (gridTotalQuantity == 0)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation("Отметьте хотя бы один ролик и укажите для него количество");
                return;
            }

            SaveData2Template();
            if (_template.StartDate > _template.FinishDate || _template.StartTime >= _template.FinishTime)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation("Проверьте даты и время интервала перед расчётом");
                return;
            }

            int expectedTotalQuantity = GetExpectedTotalQuantity();
            if (gridTotalQuantity != expectedTotalQuantity)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(
                    $"Сумма количества по роликам ({gridTotalQuantity}) не совпадает с общим количеством выходов по настройкам интервала ({expectedTotalQuantity})");
                return;
            }

            List<DateTime> selectedDates = BuildTemplateDates();
            if (selectedDates.Count == 0)
            {
                lblCampaignPriceValue.Text = lblTotalBeforePackageValue.Text = 0m.ToString("c");
                lblCompanyDiscountValue.Text = 1m.ToString("N2");
                return;
            }

            // Точная дробная длительность идёт прямо в PriceCalculatorGrid — она сама теперь считает
            // с decimal durationSec (единственное место с этой математикой), без ручного пересчёта пропорцией.
            decimal durationSec = (decimal)totalDurationSeconds / gridTotalQuantity;
            int primePerDay = cbSplitPrime.Checked ? (int)numQuantityPrime.Value : 0;
            int nonPrimePerDay = cbSplitPrime.Checked ? (int)numQuantityNonPrime.Value : (int)txtQuantity.Value;

            using (var calcGrid = new PriceCalculatorGrid())
            {
                calcGrid.LoadData(0, selectedDates.Min(), selectedDates.Max());

                // Позиция ролика (наценка первый/второй/последний) — берём из тарифного грида формы кампании
                foreach (DataRow row in calcGrid.SummaryTable.Rows)
                    row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.Position)] = (int)_position;

                calcGrid.ApplyCalculation(
                    selectedDates,
                    durationSec,
                    primePerDay, nonPrimePerDay,
                    primePerDay, nonPrimePerDay,
                    1m, true, 0);

                var targetRows = calcGrid.SummaryTable.AsEnumerable()
                    .Where(row => _massmediaIds.Contains(Convert.ToInt32(row["massmediaID"])))
                    .ToList();

                decimal totalAmount = targetRows.Sum(row => Convert.ToDecimal(row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.TotalAmount)]));
                decimal totalWithDiscount = targetRows.Sum(row => Convert.ToDecimal(row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.TotalWithDiscount)]));
                decimal totalBeforePackage = targetRows.Sum(row => Convert.ToDecimal(row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.TotalBeforePackage)]));
                decimal companyDiscount = totalAmount > 0 ? totalWithDiscount / totalAmount : 1m;

                lblCampaignPriceValue.Text = totalAmount.ToString("c");
                lblCompanyDiscountValue.Text = companyDiscount.ToString("N2");
                lblTotalBeforePackageValue.Text = totalBeforePackage.ToString("c");

                // Пакетная скидка — актуальна прежде всего для веерной кампании (несколько СМИ разом),
                // но считаем тем же способом, что и реальный Калькулятор цены, независимо от их числа.
                decimal packageDiscount = GetPackageDiscount(selectedDates.Min(), totalWithDiscount, targetRows, durationSec);
                decimal grandTotal = totalBeforePackage * packageDiscount;

                lblPackageDiscountValue.Text = packageDiscount.ToString("N2");
                lblGrandTotalValue.Text = grandTotal.ToString("c");

                // ВРЕМЕННО: отладка расхождения предварительной цены с реальной после генерации.
                // Показываем только для линейной кампании (одна СМИ) — для веерной цифр по станциям несколько.
                if (targetRows.Count == 1)
                {
                    DataRow priceRow = targetRows[0];
                    string mode = cbSplitPrime.Checked
                        ? "с разбивкой прайм/офф-прайм"
                        : "БЕЗ разбивки — всё количество считается по цене офф-прайм";
                    lblDebugInfo.Text =
                        $"[Отладка] Средняя продолжительность ролика: {durationSec:0.00} сек (без округления)\n" +
                        $"Режим: {mode}\n" +
                        $"Будни:    прайм {Convert.ToDecimal(priceRow["PrimePricePerSecWeekday"]):0.####} / " +
                        $"офф-прайм {Convert.ToDecimal(priceRow["NonPrimePricePerSecWeekday"]):0.####} за сек\n" +
                        $"Выходные: прайм {Convert.ToDecimal(priceRow["PrimePricePerSecWeekend"]):0.####} / " +
                        $"офф-прайм {Convert.ToDecimal(priceRow["NonPrimePricePerSecWeekend"]):0.####} за сек";
                }
                else
                {
                    lblDebugInfo.Text = "[Отладка] доступно только для линейной кампании (одна СМИ)";
                }
            }
        }

        // Тот же вызов, что делает PriceCalculatorForm.GetPackageDiscount — берём напрямую по SP,
        // так как сам метод формы приватный и недоступен отсюда. TVP-тип pc_SelectedMassmedia в SQL
        // хранит durationSec как INT — округляем только на входе в эту процедуру, это ограничение TVP,
        // не PriceCalculatorGrid.
        private static decimal GetPackageDiscount(DateTime startDate, decimal priceTotal, List<DataRow> rows, decimal durationSec)
        {
            if (rows.Count == 0)
                return 1m;

            var tvpTable = new DataTable();
            tvpTable.Columns.Add("massmediaID", typeof(short));
            tvpTable.Columns.Add("durationSec", typeof(int));
            foreach (DataRow row in rows)
            {
                int massmediaId = Convert.ToInt32(row["massmediaID"]);
                int primeWd = Convert.ToInt32(row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.PrimeTotalSpotsWeekday)]);
                int nonPrimeWd = Convert.ToInt32(row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.NonPrimeTotalSpotsWeekday)]);
                int primeWe = Convert.ToInt32(row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.PrimeTotalSpotsWeekend)]);
                int nonPrimeWe = Convert.ToInt32(row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.NonPrimeTotalSpotsWeekend)]);
                int totalSpots = primeWd + nonPrimeWd + primeWe + nonPrimeWe;
                tvpTable.Rows.Add((short)massmediaId, (int)Math.Round(durationSec * totalSpots));
            }

            var p = DataAccessor.CreateParametersDictionary();
            p["startDate"] = startDate;
            p["campaignTypeID"] = (byte)1;
            p["priceTotal"] = priceTotal;
            p["sel"] = new TvpValue(tvpTable, "dbo.pc_SelectedMassmedia");
            p["discountValue"] = null;
            p["packageDiscountPriceListID"] = null;

            DataAccessor.ExecuteNonQuery("pc_PackageDiscountCalculateModel", p, 30, false);

            object dv = p["discountValue"];
            return (dv == null || dv == DBNull.Value) ? 1m : Convert.ToDecimal(dv);
        }

        private List<DateTime> BuildTemplateDates()
        {
            var dates = new List<DateTime>();
            _template.Reset();
            while (_template.MoveNext())
                dates.Add(_template.CurrentDate.Date);
            _template.Reset();
            return dates;
        }

        // Пары (rollerID, количество) для роликов, отмеченных в гриде с Quantity > 0.
        // Пока не используется генератором выпусков — только UI-заготовка для нового бизнес-процесса.
        public IList<(int RollerId, string Name, int Quantity)> SelectedRollers
        {
            get
            {
                var result = new List<(int, string, int)>();
                foreach (DataGridViewRow row in dgvRollers.Rows)
                {
                    if (row.IsNewRow || row.DataBoundItem == null)
                        continue;
                    if (!(row.Cells[ColSelected].Value is bool isSelected) || !isSelected)
                        continue;
                    if (!int.TryParse(row.Cells[ColQuantity].Value?.ToString(), out int quantity) || quantity <= 0)
                        continue;

                    DataRow dataRow = ((DataRowView)row.DataBoundItem).Row;
                    result.Add(((int)dataRow["rollerID"], (string)dataRow["name"], quantity));
                }
                return result;
            }
        }

        private static (DateTime start, DateTime end) GetDefaultPeriod()
        {
            DateTime tomorrow = DateTime.Today.AddDays(1);
            int daysToSunday = (7 - (int)tomorrow.DayOfWeek) % 7;
            DateTime endOfWeek = tomorrow.AddDays(daysToSunday);
            return (tomorrow, endOfWeek);
        }

        private static void SetDateTimeValue(DateTimePicker control, DateTime date)
        {
            if (control.MinDate < date && control.MaxDate > date)
                control.Value = date;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetDateTimeValue(dtStartDate, _template.StartDate);
            SetDateTimeValue(dtFinishDate, _template.FinishDate);
            SetDateTimeValue(dtStartTime, _template.StartTime);
            SetDateTimeValue(dtFinishTime, _template.FinishTime);
            if (_template.Quantity > 0)
                txtQuantity.Value = _template.Quantity;

            clbWeekDays.Items.Clear();
            for (int i = 0; i < DateTimeUtils.WeekDayNames.Length; i++)
            {
                clbWeekDays.Items.Add(DateTimeUtils.WeekDayNames[i], _template.WeekDays[i]);
            }

            cbSplitPrime.Checked = _template.Quantity == 0;
            numQuantityPrime.Value = _template.QuantityPrime;
            numQuantityNonPrime.Value = _template.QuantityNonPrime;
            cbIgnoreWindows.Checked = _template.IgnoreWindowsWithTheSameFirmIssue;

            rbDays.Checked = _template.Day2AddMode == Day2AddMode.WeekDays;
            rbNumber.Checked = _template.Day2AddMode == Day2AddMode.OddEvenDays;
            rbOdd.Checked = _template.IsOdd;
            rbEven.Checked = !_template.IsOdd;
            SetEnabled();

            this.rbDays.Click += new System.EventHandler(this.groupButton_CheckChanged);
            this.rbNumber.Click += new System.EventHandler(this.groupButton_CheckChanged);

            // Пересчёт "Общего количества выходов" при изменении настроек интервала/количества в день
            dtStartDate.ValueChanged += (s, args) => RecalculateStats();
            dtFinishDate.ValueChanged += (s, args) => RecalculateStats();
            txtQuantity.ValueChanged += (s, args) => RecalculateStats();
            numQuantityPrime.ValueChanged += (s, args) => RecalculateStats();
            numQuantityNonPrime.ValueChanged += (s, args) => RecalculateStats();
            rbOdd.CheckedChanged += (s, args) => RecalculateStats();
            rbEven.CheckedChanged += (s, args) => RecalculateStats();

            RecalculateStats();
        }

        private void groupButton_CheckChanged(object sender, EventArgs e)
        {
            RadioButton senderRB = (RadioButton)sender;
            senderRB.Checked = !senderRB.Checked;
            if (sender == rbNumber)
                rbDays.Checked = !senderRB.Checked;
            else if (sender == rbDays)
                rbNumber.Checked = !senderRB.Checked;
            _template.IsOdd = rbOdd.Checked;
            _template.Day2AddMode = (rbNumber.Checked) ? Day2AddMode.OddEvenDays : Day2AddMode.WeekDays;
            SetEnabled();
            RecalculateStats();
        }

        public IssueTemplate Template
        {
            get => _template;
        }

        private void SaveData2Template()
        {
            _template.StartDate = dtStartDate.Value.Date;
            _template.FinishDate = dtFinishDate.Value.Date;
            _template.StartTime = dtStartTime.Value;
            _template.FinishTime = dtFinishTime.Value;
            _template.Quantity = cbSplitPrime.Checked ? 0 : ((int)txtQuantity.Value);
            _template.QuantityPrime = (int)numQuantityPrime.Value;
            _template.QuantityNonPrime = (int)numQuantityNonPrime.Value;

            // FIX: сохраняем выбор чёт/нечёт и режим шаблона при OK
            _template.IsOdd = rbOdd.Checked;
            _template.Day2AddMode = rbNumber.Checked ? Day2AddMode.OddEvenDays : Day2AddMode.WeekDays;

            _template.TemplateType = IssueTemplateType.TimePeriod;
            _template.IgnoreWindowsWithTheSameFirmIssue = cbIgnoreWindows.Checked;
        }

        private void clbWeekDays_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            _template.WeekDays[e.Index] = e.NewValue == CheckState.Checked;
            RecalculateStats();
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SaveData2Template();
            bool errorFlag = false;

            if(cbSplitPrime.Checked && _template.QuantityNonPrime + _template.QuantityPrime == 0)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.PrimeAndNonPrimeQuantityMissing);
                errorFlag = true;
            }

            
            if(_template.StartDate > _template.FinishDate)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("TemplateStartFinishDateError"));
                errorFlag=true;
            }
            if (_template.StartTime >= _template.FinishTime)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("TemplateStartFinishTimeError"));
                errorFlag = true;
            }
            if (errorFlag)
                DialogResult = DialogResult.None;
        }

        private void cbSplitPrime_CheckedChanged(object sender, EventArgs e)
        {
            numQuantityPrime.Enabled = numQuantityNonPrime.Enabled = cbSplitPrime.Checked;
            txtQuantity.Enabled = !cbSplitPrime.Checked;
            RecalculateStats();
        }

        private void SetEnabled()
        {
            rbEven.Enabled = rbNumber.Checked;
            rbOdd.Enabled = rbNumber.Checked;
            clbWeekDays.Enabled = rbDays.Checked;
        }

        private void FrmTemplate3_Load(object sender, EventArgs e)
        {

        }
    }
}
