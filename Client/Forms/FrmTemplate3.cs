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
        private readonly Campaign _campaign;
        // Веерная кампания идёт без конкретной _campaign (см. CampaignForm.GetTemplateMassmediaIds) —
        // акцию в этом случае берёт вызывающий код из TariffWithRangeGrid.Action и передаёт сюда
        // готовой, а не через _campaign.Action. Для линейной кампании это просто _campaign.Action.
        private readonly ActionOnMassmedia _action;

        // Существующее (до этого запуска шаблона) состояние кампании, по каждому СМИ отдельно —
        // и объёмная, и пакетная скидка считаются по-станционно (см. hlp_CompanyDiscountCalculate,
        // pc_PackageDiscountCalculateModel), поэтому агрегат тут не годится. Заполняется один раз
        // в OnLoad (см. LoadExistingCampaignState), переиспользуется в btnEstimatePrice_Click.
        private Dictionary<int, decimal> _existingTariffPriceByMassmedia;
        private Dictionary<int, int> _existingIssuesDurationByMassmedia;

        // Агрегаты по всем станциям сразу — базис для блока "Статистика": то, что там показано
        // при открытии формы и после любого сброса (см. DisplayBaselinePrices).
        private decimal _baselineTariffSum;
        private decimal _baselineDiscountedSum;
        private decimal _baselineManagerDiscount;
        private decimal _baselinePackageDiscount;

        // Веерная: объёмная скидка — коэффициент по станциям (единого числа на всю акцию нет),
        // показываем значением или "разные" (см. FormatPerStationValue). Цена с учётом объёмной
        // скидки — сумма цен по кампаниям станций (_baselineDiscountedSum), а не по-станционное поле.
        // Список инициализирован пустым не случайно: из-за порядка в OnLoad DisplayBaselinePrices
        // может сработать ДО LoadExistingCampaignState (риск #9) — пустой список даёт "—", а не NRE.
        private List<decimal> _baselineStationCompanyDiscounts = new List<decimal>();

        // Веерная: менеджерская скидка — тоже коэффициент по станциям, а не общий на акцию
        // (ActionRecalculate пишет managerDiscount отдельно в каждую строку Campaign — на
        // практике часто различается: у каждой станции своя история 0→не 0 переходов количества
        // выпусков). Раньше бралось значение ПЕРВОЙ найденной кампании с выпусками как будто
        // общее — баг, найден тестировщиками (см. LoadExistingCampaignState). Собираются только
        // кампании с IssuesCount > 0 — у остальных ManagerDiscount голая единица по DEFAULT
        // (см. GetEffectiveManagerDiscount), сравнивать её было бы бессмысленно.
        private List<decimal> _baselineStationManagerDiscounts = new List<decimal>();

        // Признак "есть реальные выпуски хоть у одной кампании акции" и её менеджерский коэффициент —
        // источник для GetEffectiveManagerDiscount, единый для линейной (сама _campaign) и веерной
        // (первая кампания акции с выпусками) кампании. Заполняется в LoadExistingCampaignState.
        private bool _hasExistingIssues;
        private decimal _existingManagerDiscount;

        public FrmTemplate3()
        {
            InitializeComponent();
            InitRollersGrid();
        }

        internal FrmTemplate3(Firm firm, IssueTemplate template, IList<int> massmediaIds, RollerPositions position, Campaign campaign, ActionOnMassmedia action) : this()
        {
            _position = position;
            _campaign = campaign;
            _action = action;
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
            // DisplayedCells (не None): с фиксированным размером строк/колонок тематический CheckBoxRenderer
            // иногда не рисует галочку колонки ColSelected над RDP-сессией — глиф не помещается в статично
            // рассчитанную под локальный DPI ячейку и не рисуется вообще, без ошибки. При DisplayedCells
            // размер ячейки постоянно подгоняется под фактический контент при любом DPI.
            dgvRollers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgvRollers.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
            dgvRollers.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = ColSelected,
                HeaderText = string.Empty,
                Width = 60
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
            dgvRollers.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = ColAdvertSubject,
                HeaderText = "Предмет рекламы",
                DataPropertyName = "advertTypeName",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
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
            // Веерная (несколько СМИ) размещает одни и те же выпуски на КАЖДУЮ станцию — показываем
            // "на одной станции / суммарно по всем станциям" через слэш. Линейная — одно число.
            int stationCount = _massmediaIds.Count;

            int expectedTotalQuantity = GetExpectedTotalQuantity();
            lblTotalQuantityValue.Text = stationCount > 1
                ? $"{expectedTotalQuantity} / {expectedTotalQuantity * stationCount}"
                : expectedTotalQuantity.ToString();

            var (gridTotalQuantity, totalDurationSeconds) = GetCheckedRollerAggregates();
            lblGridQuantityValue.Text = stationCount > 1
                ? $"{gridTotalQuantity} / {gridTotalQuantity * stationCount}"
                : gridTotalQuantity.ToString();
            // Цвет сравнивает по-станционную сумму по гриду с ожидаемой (обе — на одну станцию).
            lblGridQuantityValue.ForeColor = gridTotalQuantity == expectedTotalQuantity ? Color.Green : Color.Red;

            lblTotalDurationValue.Text = stationCount > 1
                ? $"{FormatDuration(totalDurationSeconds)} / {FormatDuration(totalDurationSeconds * stationCount)}"
                : FormatDuration(totalDurationSeconds);

            // Любое изменение параметров делает предыдущий расчёт цены неактуальным —
            // сбрасываем его, чтобы не ввести пользователя в заблуждение устаревшими цифрами.
            ResetPriceEstimate();
        }

        // Любое изменение параметров возвращает блок статистики к текущему, реальному состоянию
        // кампании — пользователю важна финальная сумма, а не то, сколько именно принесёт шаблон,
        // поэтому отдельной "оценки добавки" не показываем, только конечный результат.
        private void ResetPriceEstimate()
        {
            DisplayBaselinePrices();
        }

        private void DisplayBaselinePrices()
        {
            lblCampaignPriceValue.Text = _baselineTariffSum.ToString("c");
            lblPackageDiscountValue.Text = _baselinePackageDiscount.ToString("N2");

            if (_massmediaIds.Count == 1)
            {
                lblCompanyDiscountValue.Text = (_baselineTariffSum > 0 ? _baselineDiscountedSum / _baselineTariffSum : 1m).ToString("N2");
                lblTotalBeforePackageValue.Text = _baselineDiscountedSum.ToString("c");
                lblManagerDiscountValue.Text = _baselineManagerDiscount.ToString("N2");
                lblGrandTotalValue.Text = (_baselineDiscountedSum * _baselineManagerDiscount * _baselinePackageDiscount).ToString("c");
            }
            else
            {
                // Объёмная и менеджерская скидка — коэффициенты по станциям: одинаковые у всех →
                // значение, иначе → "разные" (см. GetEffectiveManagerDiscount за тем, почему список
                // менеджерской может быть пуст — все кампании веера новые, без выпусков). Цена с
                // учётом объёмной скидки — сумма цен по кампаниям станций. Итого — готовое с Action.
                lblCompanyDiscountValue.Text = FormatPerStationValue(_baselineStationCompanyDiscounts, "N2");
                lblTotalBeforePackageValue.Text = _baselineDiscountedSum.ToString("c");
                lblManagerDiscountValue.Text = _baselineStationManagerDiscounts.Count > 0
                    ? FormatPerStationValue(_baselineStationManagerDiscounts, "N2")
                    : _baselineManagerDiscount.ToString("N2");
                lblGrandTotalValue.Text = _action.TotalPrice.ToString("c");
            }
        }

        // Веерная кампания: коэффициент объёмной скидки по станциям. Если во всех станциях значение
        // совпадает (в том виде, в каком показывается — сравнение по отформатированной строке, чтобы
        // незначимые разряды не давали ложное "разные"), возвращаем само значение, иначе "разные".
        // Пустой набор → "—".
        private static string FormatPerStationValue(IEnumerable<decimal> values, string format)
        {
            var distinct = values.Select(v => v.ToString(format)).Distinct().ToList();
            if (distinct.Count == 0)
                return "—";
            return distinct.Count == 1 ? distinct[0] : "разные";
        }

        // Campaign.managerDiscount у НОВОЙ кампании (нет ни одного выпуска) — это ещё не реальный
        // коэффициент, а голый DEFAULT 1 из Campaign.sql: ActionRecalculate вызывает
        // fn_GetMaxUserDiscount и пишет managerDiscount только на переходе количества выпусков
        // 0 → не 0 (см. ActionRecalculate.sql, Phase 1E), а для кампании, которая этот переход
        // ни разу не проходила, поле так и остаётся равно 1. Реальный Калькулятор для такого
        // гипотетического случая коэффициент из кампании не читает вовсе — считает его через
        // SecurityManager.User.GetDiscount(startDate, finishDate) на выбранный период, с
        // исключением для админов (см. TemplateEditorControl.SetManagerDiscount). Для кампании
        // с реальными выпусками управленческий коэффициент уже корректно посчитан — используем
        // его (_hasExistingIssues/_existingManagerDiscount, см. LoadExistingCampaignState), без
        // обращения к SP.
        private decimal GetEffectiveManagerDiscount(DateTime startDate, DateTime finishDate)
        {
            if (_hasExistingIssues)
                return _existingManagerDiscount;

            SecurityManager.User user = SecurityManager.LoggedUser;
            return user.IsAdmin ? 1m : user.GetDiscount(startDate, finishDate);
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
                DisplayBaselinePrices();
                return;
            }

            // Точная дробная длительность идёт прямо в PriceCalculatorGrid — она сама теперь считает
            // с decimal durationSec (единственное место с этой математикой), без ручного пересчёта пропорцией.
            decimal durationSec = (decimal)totalDurationSeconds / gridTotalQuantity;
            int primePerDay = cbSplitPrime.Checked ? (int)numQuantityPrime.Value : 0;
            int nonPrimePerDay = cbSplitPrime.Checked ? (int)numQuantityNonPrime.Value : (int)txtQuantity.Value;

            // На реальном периоде шаблона, а не на первоначальном (см. GetEffectiveManagerDiscount) —
            // пользователь мог поменять даты в диалоге перед нажатием "Рассчитать".
            decimal managerDiscount = GetEffectiveManagerDiscount(selectedDates.Min(), selectedDates.Max());

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
                    managerDiscount, true, 0);

                var targetRows = calcGrid.SummaryTable.AsEnumerable()
                    .Where(row => _massmediaIds.Contains(Convert.ToInt32(row["massmediaID"])))
                    .ToList();

                // Показываем не "сколько добавит шаблон", а финальное состояние кампании целиком —
                // существующее (см. LoadExistingCampaignState) плюс то, что добавляют эти ролики.
                // Объёмная скидка считается hlp_CompanyDiscountCalculate по ПОЛНОЙ сумме кампании
                // (см. ActionRecalculate), поэтому к сумме каждой станции прибавляется её существующая
                // tariffPrice до вызова; наивный TotalWithDiscount/TotalBeforePackage из calcGrid
                // (посчитанные без учёта существующей суммы) не используются вовсе.
                decimal combinedTariffSum = 0m;
                decimal combinedDiscountedSum = 0m;
                var stationCompanyDiscounts = new List<decimal>();
                foreach (DataRow row in targetRows)
                {
                    int massmediaId = Convert.ToInt32(row["massmediaID"]);
                    decimal rowAmount = Convert.ToDecimal(row[nameof(PriceCalculatorGrid.PriceCalculatorColumn.TotalAmount)]);

                    decimal existingTariffPrice = _existingTariffPriceByMassmedia.TryGetValue(massmediaId, out decimal existing) ? existing : 0m;
                    decimal newStationTariffSum = existingTariffPrice + rowAmount;
                    decimal stationDiscount = GetCompanyDiscount(massmediaId, selectedDates.Min(), newStationTariffSum);

                    combinedTariffSum += newStationTariffSum;
                    combinedDiscountedSum += newStationTariffSum * stationDiscount;
                    stationCompanyDiscounts.Add(stationDiscount);
                }

                lblCampaignPriceValue.Text = combinedTariffSum.ToString("c");

                // Пакетная скидка — актуальна прежде всего для веерной кампании (несколько СМИ разом),
                // но считаем тем же способом, что и реальный Калькулятор цены, независимо от их числа.
                // priceTotal и durationSec-и в TVP — с учётом уже существующих данных кампании (см. выше),
                // иначе порог пакета подбирался бы только по добавляемым роликам, без контекста акции.
                decimal packageDiscount = GetPackageDiscount(selectedDates.Min(), combinedDiscountedSum, targetRows, durationSec, _existingIssuesDurationByMassmedia);
                decimal grandTotal = combinedDiscountedSum * managerDiscount * packageDiscount;

                lblPackageDiscountValue.Text = packageDiscount.ToString("N2");
                lblGrandTotalValue.Text = grandTotal.ToString("c");

                if (_massmediaIds.Count == 1)
                {
                    decimal companyDiscount = combinedTariffSum > 0 ? combinedDiscountedSum / combinedTariffSum : 1m;
                    lblCompanyDiscountValue.Text = companyDiscount.ToString("N2");
                    lblTotalBeforePackageValue.Text = combinedDiscountedSum.ToString("c");
                    lblManagerDiscountValue.Text = managerDiscount.ToString("N2");
                }
                else
                {
                    // Объёмная скидка — коэффициент по станциям: одинаковый → значение, разный → "разные".
                    // Цена с учётом объёмной скидки — сумма по станциям (combinedDiscountedSum).
                    // Менеджерская скидка — тоже по станциям; реальные значения (кампании с выпусками)
                    // не зависят от дат шаблона, поэтому переиспользуем список из LoadExistingCampaignState
                    // (managerDiscount выше — единственный ПРЕДСТАВИТЕЛЬ для формулы grandTotal, не для
                    // сравнения на UI).
                    lblCompanyDiscountValue.Text = FormatPerStationValue(stationCompanyDiscounts, "N2");
                    lblTotalBeforePackageValue.Text = combinedDiscountedSum.ToString("c");
                    lblManagerDiscountValue.Text = _baselineStationManagerDiscounts.Count > 0
                        ? FormatPerStationValue(_baselineStationManagerDiscounts, "N2")
                        : managerDiscount.ToString("N2");
                }
            }
        }

        // Тот же вызов, что делает PriceCalculatorGrid.GetCompanyDiscount — недоступен оттуда (private),
        // дублируем напрямую по SP. В отличие от PriceCalculatorGrid (который вызывает её с tariffPrice
        // только новых роликов, без контекста кампании), вызывающий код здесь передаёт уже существующую
        // сумму по тарифам кампании + добавляемую — ровно как считает ActionRecalculate при реальном
        // пересчёте (hlp_CompanyDiscountCalculate там тоже вызывается с полной, а не только новой, суммой).
        private static decimal GetCompanyDiscount(int massmediaId, DateTime startDate, decimal tariffPrice)
        {
            var p = DataAccessor.CreateParametersDictionary();
            p["massMediaID"] = (short)massmediaId;
            p["campaignTypeID"] = (byte)1;
            p["startDate"] = startDate;
            p["tariffPrice"] = tariffPrice;
            p["discountValue"] = null;

            DataAccessor.ExecuteNonQuery("hlp_CompanyDiscountCalculate", p, 30, false);

            object dv = p["discountValue"];
            return (dv == null || dv == DBNull.Value) ? 1m : Convert.ToDecimal(dv);
        }

        // Тот же вызов, что делает PriceCalculatorForm.GetPackageDiscount — берём напрямую по SP,
        // так как сам метод формы приватный и недоступен отсюда. TVP-тип pc_SelectedMassmedia в SQL
        // хранит durationSec как INT — округляем только на входе в эту процедуру, это ограничение TVP,
        // не PriceCalculatorGrid. durationSec в TVP — существующий хронометраж станции (Campaign.issuesDuration)
        // ПЛЮС добавляемый, иначе порог eachVolume сравнивался бы со средней длительностью только по
        // добавляемым роликам, без остальной акции (см. hlp_ActionDiscountCalculate).
        private static decimal GetPackageDiscount(DateTime startDate, decimal priceTotal, List<DataRow> rows, decimal durationSec,
            Dictionary<int, int> existingDurationByMassmedia)
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
                int addedDurationSec = (int)Math.Round(durationSec * totalSpots);
                int existingDurationSec = existingDurationByMassmedia.TryGetValue(massmediaId, out int existing) ? existing : 0;
                tvpTable.Rows.Add((short)massmediaId, existingDurationSec + addedDurationSec);
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

        // Существующие сумма по тарифам, цена с учётом объёмной скидки и хронометраж кампании до
        // запуска этого шаблона, по каждому СМИ отдельно. tariffPrice нужен для объёмной скидки
        // (hlp_CompanyDiscountCalculate), issuesDuration — для порога пакетной скидки (eachVolume
        // в pc_PackageDiscountCalculateModel сравнивается со средней длительностью по всей акции,
        // а не только с добавляемой). Campaign.Price — вычисляемая колонка (tariffPrice*discount,
        // см. Campaign.sql) — уже готовая "цена с учётом объёмной скидки", пересчитывать не нужно.
        // Для линейной — это просто _campaign.*. Для веерной аналогично GetTemplateMassmediaIds
        // в CampaignForm: перебираем все кампании акции (Action.Campaigns()).
        private void LoadExistingCampaignState()
        {
            _existingTariffPriceByMassmedia = new Dictionary<int, decimal>();
            _existingIssuesDurationByMassmedia = new Dictionary<int, int>();
            _baselineTariffSum = 0m;
            _baselineDiscountedSum = 0m;
            _baselineStationCompanyDiscounts = new List<decimal>();
            _baselineStationManagerDiscounts = new List<decimal>();

            if (_massmediaIds.Count == 1)
            {
                // Линейная — _campaign гарантированно не null (см. CampaignForm.GetTemplateMassmediaIds).
                _existingTariffPriceByMassmedia[_massmediaIds[0]] = _campaign.TariffPrice;
                _existingIssuesDurationByMassmedia[_massmediaIds[0]] = _campaign.IssuesDuration;
                _baselineTariffSum = _campaign.TariffPrice;
                _baselineDiscountedSum = _campaign.Price;
                // Одна кампания в акции — ActionRecalculate жёстко пишет discount=1
                // (@campaignCount<=1, без обращения к hlp_ActionDiscountCalculate).
                _baselinePackageDiscount = 1m;
                _hasExistingIssues = _campaign.IssuesCount > 0;
                _existingManagerDiscount = _campaign.ManagerDiscount;
                _baselineManagerDiscount = GetEffectiveManagerDiscount(dtStartDate.Value, dtFinishDate.Value);
                return;
            }
            else
            {
                // Веерная — _campaign == null, акция и её кампании берутся через _action
                // (переданный вызывающим кодом, см. CampaignForm.tbbTemplate3_Click).
                foreach (DataRow row in _action.Campaigns().Rows)
                {
                    int campaignId = Convert.ToInt32(row[Campaign.ParamNames.CampaignId]);
                    Campaign campaign = Campaign.GetCampaignById(campaignId);
                    int massmediaId = Convert.ToInt32(campaign[Campaign.ParamNames.MassmediaId]);
                    _existingTariffPriceByMassmedia[massmediaId] = campaign.TariffPrice;
                    _existingIssuesDurationByMassmedia[massmediaId] = campaign.IssuesDuration;
                    _baselineTariffSum += campaign.TariffPrice;
                    // Цена с учётом объёмной скидки веерной акции — сумма Campaign.Price по станциям
                    // (Campaign.Price = tariffPrice*discount, уже готовое поле, см. Campaign.sql).
                    _baselineDiscountedSum += campaign.Price;

                    // Объёмная скидка по станции — коэффициент Price/TariffPrice; выводится значением
                    // или "разные" в веере (см. DisplayBaselinePrices/FormatPerStationValue).
                    _baselineStationCompanyDiscounts.Add(campaign.TariffPrice > 0 ? campaign.Price / campaign.TariffPrice : 1m);

                    // Менеджерская скидка станции — реальна только у кампании с выпусками (иначе
                    // голая единица по DEFAULT, см. GetEffectiveManagerDiscount) — собираем для
                    // сравнения "значение или разные" в UI (FormatPerStationValue).
                    if (campaign.IssuesCount > 0)
                    {
                        _baselineStationManagerDiscounts.Add(campaign.ManagerDiscount);
                        // Представитель для формулы grandTotal — GetEffectiveManagerDiscount должен
                        // вернуть ОДНО число; первая найденная кампания с выпусками, как и раньше.
                        if (!_hasExistingIssues)
                        {
                            _hasExistingIssues = true;
                            _existingManagerDiscount = campaign.ManagerDiscount;
                        }
                    }
                }

                // Пакетная скидка веерной акции — то же поле, что показывает EditIssuesForm
                // (см. ActionOnMassmedia.DisplayData): сервер уже посчитал и хранит его на Action,
                // пересчитывать самим не нужно.
                _baselinePackageDiscount = _action.Discount;

                // Менеджерская скидка веерной акции теперь выводится (единый коэффициент на
                // пользователя/период) — считаем один раз, как для линейной.
                _baselineManagerDiscount = GetEffectiveManagerDiscount(dtStartDate.Value, dtFinishDate.Value);
            }
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

            // Состояние кампании на момент открытия формы — до RecalculateStats(), т.к. он вызывает
            // ResetPriceEstimate(), который показывает именно эти базовые значения (см. DisplayBaselinePrices).
            _action.Refresh();
            LoadExistingCampaignState();

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
