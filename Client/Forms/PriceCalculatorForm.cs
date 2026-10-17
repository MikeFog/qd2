using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export.MSExcel;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using Merlin.Controls;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm.Forms;
using log4net;

namespace Merlin.Forms
{
    public partial class PriceCalculatorForm : Form
    {
        private static readonly ILog Log = LogManager.GetLogger(typeof(PriceCalculatorForm));

        private readonly List<CampaignCalcSnapshot> _saved = new List<CampaignCalcSnapshot>();
        private decimal _lastTotalAfterPackage;
        
        // ✅ Добавляем поле для отслеживания редактируемого варианта
        private CampaignCalcSnapshot _editingSnapshot = null;

        // идёт восстановление варианта: события полей шаблона не пересчитывают и не перечитывают по одному
        private bool _restoringVariant;

        // перечитывание модели после смены периода, группы или менеджера — через паузу, а не на каждую цифру даты,
        // шаг календаря или стрелку по списку; всё, что считает по модели, сначала доделывает отложенное (FlushPendingReload)
        private readonly System.Windows.Forms.Timer _reloadTimer = new System.Windows.Forms.Timer { Interval = 400 };
        
        // по именам колонок таблицы, а не по заголовкам: заголовки цен меняются при слиянии колонок, и прежний
        // список заголовков с ними не совпадал — формат денег к ценам не применялся (К-12)
        private readonly string[] _columnsWithMoney = new[]
        {
            "colPrimeWeekday",
            "colNonPrimeWeekday",
            "colPrimeWeekend",
            "colNonPrimeWeekend",
            "colTotalAmount",
            "colTotalBeforePackage",
            "colTotalAfterPackage"
        };

        private readonly string[] _columnsWithNumeric = new[]
        {
            "colSeasonCoeff",
            "colCompanyDiscount",
            "colPackageDiscount",
        };

        public PriceCalculatorForm()
        {
            InitializeComponent();
            templateEditor.SaveButton.Click += (s, e) => SaveVariant();
            templateEditor.CalculateButton.Click += CalculateButton_Click;
            templateEditor.ExcelButton.Click += (s, e) => ExportSelectedToExcel();
            templateEditor.ManagerDiscountNum.ValueChanged += (s, e) =>
            {
                if (_restoringVariant) return;
                grdPriceCalculator.SetManagerDiscount(templateEditor.ManagerDiscount);
            };

            templateEditor.PositionChanged += (s, e) =>
            {
                if (_restoringVariant) return;
                grdPriceCalculator.SetDefaultPosition(templateEditor.SelectedPosition);
            };

            // пересчитываем при смене дневных/выходных количеств
            templateEditor.SpotsSettingsChanged += (s, e) => RecalculateFromTemplateInputs();
            templateEditor.DurationChanged += (s, e) => RecalculateFromTemplateInputs();
            templateEditor.ManagerDiscountModeChanged += (s, e) => RecalculateFromTemplateInputs();
            // периоды коэффициента читаются по выбранному менеджеру — смена менеджера перечитывает модель
            templateEditor.CurrentUserChanged += (s, e) => ScheduleReload();
            templateEditor.ModelParametersChanged += (s, e) => ScheduleReload();
            _reloadTimer.Tick += (s, e) =>
            {
                _reloadTimer.Stop();
                ReloadModel();
            };
            FormClosed += (s, e) => _reloadTimer.Dispose();

            // новый авто-пересчёт по датам/дням/чётности
            templateEditor.ScheduleChanged += (s, e) => RecalculateFromTemplateInputs();

            grdPriceCalculator.SummaryUpdater = UpdateSummary;
            flpSaved.SizeChanged += FlpSaved_SizeChanged;
            chkAll.CheckedChanged += ChkAll_CheckedChanged;
            btnDeleteAllChecked.Click += BtnDeleteAllChecked_Click;
            LoadAgencies();
            Log.Info("PriceCalculator opened");
        }

        private void FlpSaved_SizeChanged(object sender, EventArgs e)
        {
            ApplySnapshotCardWidths();
        }

        private void ChkAll_CheckedChanged(object sender, EventArgs e)
        {
            SetSnapshotSelection(chkAll.Checked);
        }

        private void BtnDeleteAllChecked_Click(object sender, EventArgs e)
        {
            DeleteCheckedSnapshots();
        }

        private void LoadAgencies()
        {
            // твой метод, который возвращает DataView
            DataView dv = Agency.LoadAgencies(true).Tables[0].DefaultView;

            cmbAgency.BeginUpdate();
            try
            {
                cmbAgency.DataSource = dv;

                // ВАЖНО: поставь реальные имена колонок.
                // Если колонки без имён — см. блок ниже (как узнать имена).
                cmbAgency.ValueMember = Agency.ParamNames.AgencyId;
                cmbAgency.DisplayMember = Constants.Parameters.Name;

                // без выбора: прежняя 1 — несуществующее агентство, выбор так же сбрасывался (К-13)
                cmbAgency.SelectedIndex = -1;
            }
            finally
            {
                cmbAgency.EndUpdate();
            }
        }

        private void RecalculateFromTemplateInputs()
        {
            if (_restoringVariant) return;
            if (grdPriceCalculator.SummaryTable == null) return;
            // отложенное перечитывание уже посчитало по текущим полям; без него этот пересчёт пошёл бы по новым датам на
            // старой модели (К-2)
            if (FlushPendingReload()) return;

            var selectedDates = BuildSelectedDates(
                templateEditor.DateFrom, templateEditor.DateTo,
                templateEditor.UseDaysOfWeek,
                templateEditor.DaysOfWeekChecked,
                templateEditor.EvenDaysSelected
            );

            grdPriceCalculator.ApplyCalculation(
                selectedDates,
                templateEditor.DurationSec,
                templateEditor.PrimePerDayWeekday,
                templateEditor.NonPrimePerDayWeekday,
                templateEditor.PrimePerDayWeekend,
                templateEditor.NonPrimePerDayWeekend,
                templateEditor.ManagerDiscount,
                templateEditor.ManagerDiscountModeSingle,
                templateEditor.CurrentUserId
            );

            // оставить позицию из шаблона и актуальный SummaryUpdater
            //grdPriceCalculator.SetDefaultPosition(templateEditor.SelectedPosition);
            UpdateSummary();
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            // ✅ При новом расчете сбрасываем режим редактирования
            _editingSnapshot = null;
            
            if (!templateEditor.IsDaysOfWeekValid())
            {
                UserMessage.ShowExclamation("Выберите хотя бы один день недели");
                return;
            }

            if (templateEditor.DateFrom > templateEditor.DateTo)
            {
                UserMessage.ShowExclamation("Дата начала периода акции не может быть больше даты окончания");
                return;
            }

            try
            {
                UseWaitCursor = true;
                Cursor.Current = Cursors.WaitCursor;
                Application.DoEvents();
                templateEditor.TotalAfterPackageDiscount.Text = "Итог: ";

                var sw = Stopwatch.StartNew();
                LoadAndCalculate(keepRowPositions: false);
                Log.Info($"PriceCalculator calculate completed in {sw.ElapsedMilliseconds}ms group={templateEditor.MassmediaGroupId}" +
                    $" period={templateEditor.DateFrom:dd.MM.yyyy}-{templateEditor.DateTo:dd.MM.yyyy} manager={templateEditor.CurrentUserId}" +
                    $" stations={grdPriceCalculator.SummaryTable?.Rows.Count ?? 0}");
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                UseWaitCursor = false;
                Cursor.Current = Cursors.Default;
            }
        }

        /// <param name="keepRowPositions">false — «Рассчитать»: всем строкам позиция шаблона, как раньше; true — перечитывание
        /// после смены периода, группы или менеджера: позиции строк остаются, новым станциям — позиция шаблона.</param>
        private void LoadAndCalculate(bool keepRowPositions)
        {
            // отложенное перечитывание больше не нужно — модель читается сейчас; останавливать только здесь: «Рассчитать»,
            // отказавшее на проверке дат или дней, должно оставить его в силе
            _reloadTimer.Stop();
            grdPriceCalculator.LoadData(templateEditor.MassmediaGroupId, templateEditor.DateFrom, templateEditor.DateTo,
                templateEditor.CurrentUserId);

            // позиция — до расчёта, и строки считаются один раз (прежде SetDefaultPosition после расчёта считал их второй раз)
            grdPriceCalculator.PresetPositions(templateEditor.SelectedPosition, onlyMissing: keepRowPositions);

            var selectedDates = BuildSelectedDates(
                templateEditor.DateFrom, templateEditor.DateTo,
                templateEditor.UseDaysOfWeek,
                templateEditor.DaysOfWeekChecked,
                templateEditor.EvenDaysSelected
            );

            grdPriceCalculator.ApplyCalculation(
                selectedDates,
                templateEditor.DurationSec,
                templateEditor.PrimePerDayWeekday,
                templateEditor.NonPrimePerDayWeekday,
                templateEditor.PrimePerDayWeekend,
                templateEditor.NonPrimePerDayWeekend,
                templateEditor.ManagerDiscount,
                templateEditor.ManagerDiscountModeSingle,
                templateEditor.CurrentUserId
            );

            // итог и кнопки — всегда: если отмеченных строк не осталось (сменилась группа), «Итог:», «Сохранить», «Excel»
            // оставались от прошлого расчёта (К-25)
            UpdateSummary();
        }

        private void ScheduleReload()
        {
            if (_restoringVariant || grdPriceCalculator.SummaryTable == null) return;
            _reloadTimer.Stop();
            _reloadTimer.Start();
        }

        // true — перечитывание было отложено и сделано сейчас (вместе с пересчётом по текущим полям)
        private bool FlushPendingReload()
        {
            if (!_reloadTimer.Enabled) return false;
            _reloadTimer.Stop();
            ReloadModel();
            return true;
        }

        // Период, группа или менеджер поменялись после расчёта: модель цен (сегменты прайс-листов, периоды
        // коэффициента) перечитывается — иначе следующий пересчёт шёл по новым датам на старых сегментах, и дни вне
        // загруженного диапазона считались по 0 (К-2). До первого «Рассчитать» модели нет — ничего не делаем.
        private void ReloadModel()
        {
            if (_restoringVariant) return;
            if (grdPriceCalculator.SummaryTable == null) return;

            // период перевёрнут — читать нечего: пересчёт с пустым набором дат обнулит строки и закроет «Сохранить» и
            // «Excel». Выйти из этого состояния можно только сменой даты, а она перечитывает. Дни недели модель не
            // меняют — без них перечитываем как обычно (пустой набор дней так же обнулит строки), иначе модель отставала
            // бы от периода, группы и менеджера, пока дни не отметят снова
            if (templateEditor.DateFrom > templateEditor.DateTo)
            {
                RecalculateFromTemplateInputs();
                return;
            }

            try
            {
                Cursor.Current = Cursors.WaitCursor;
                LoadAndCalculate(keepRowPositions: true);
            }
            catch (Exception ex)
            {
                // таблица после сбоя наполовину пересчитана — сохранять и выгружать её нельзя до следующего расчёта
                templateEditor.SaveButton.Enabled = templateEditor.ExcelButton.Enabled = false;
                ErrorManager.PublishError(ex);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private static List<DateTime> BuildSelectedDates(
            DateTime dateFrom,
            DateTime dateTo,
            bool useDaysOfWeek,
            bool[] daysOfWeekChecked, // 7 элементов: Mon..Sun
            bool evenDays // если useDaysOfWeek=false: true=чётные, false=нечётные
        )
        {
            var res = new List<DateTime>();
            for (var d = dateFrom.Date; d <= dateTo.Date; d = d.AddDays(1))
            {
                bool ok;
                if (useDaysOfWeek)
                {
                    // C# DayOfWeek: Sunday=0 ... Saturday=6
                    int idx = ((int)d.DayOfWeek + 6) % 7; // Mon=0..Sun=6
                    ok = daysOfWeekChecked[idx];
                }
                else
                {
                    ok = evenDays ? (d.Day % 2 == 0) : (d.Day % 2 == 1);
                }

                if (ok) res.Add(d);
            }
            return res;
        }

        private void UpdateSummary()
        {
            try
            {
                var selectedRows = grdPriceCalculator.GetSelectedRadiostations();
                var selectedDates = grdPriceCalculator.GetSelectedDates();
                // без выбранных дней сохранять нечего: снимок брал первый и последний день из пустого списка (К-9)
                templateEditor.SaveButton.Enabled = templateEditor.ExcelButton.Enabled =
                    selectedRows.Count > 0 && selectedDates.Count > 0;

                decimal totalwithCampaignDiscount = grdPriceCalculator.GetSelectedTotalWithCampaignDiscount();
                decimal packageDiscount = GetPackageDiscount(
                    // первый выбранный день, как у объёмной скидки (К-5); система берёт первый день выхода кампании — он позже,
                    // только если в первые выбранные дни выходов нет (например, будни 0/0 при периоде с четверга)
                    startDate: selectedDates.Count > 0 ? selectedDates[0] : templateEditor.DateFrom,
                    priceTotal: totalwithCampaignDiscount,
                    selectedRows: selectedRows
                );

                decimal totalAfterPackage = grdPriceCalculator.ApplyPackageTotals(packageDiscount);
                DisplayTotal(totalAfterPackage);
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        private decimal GetPackageDiscount(DateTime startDate, decimal priceTotal, List<DataRowView> selectedRows)
        {
            if (selectedRows.Count == 0) return 1m;

            var tvpTable = BuildSelectedMassmediaTvp(selectedRows);

            var p = DataAccessor.CreateParametersDictionary();
            p["startDate"] = startDate;
            p["campaignTypeID"] = (byte)1;
            // decimal(18,2) параметра отсекает дробные копейки, а не округляет — округляем сами (К-29)
            p["priceTotal"] = Math.Round(priceTotal, 2, MidpointRounding.AwayFromZero);
            p["sel"] = new FogSoft.WinForm.DataAccess.TvpValue(tvpTable, "dbo.pc_SelectedMassmedia");

            p["discountValue"] = null;               // OUT
            p["packageDiscountPriceListID"] = null;  // OUT (если нужно показать какой пакет)

            DataAccessor.ExecuteNonQuery("pc_PackageDiscountCalculateModel", p, 30, false);

            var dv = p["discountValue"];
            return (dv == null || dv == DBNull.Value) ? 1m : Convert.ToDecimal(dv);
        }

        private DataTable BuildSelectedMassmediaTvp(IEnumerable<DataRowView> selectedRows)
        {
            int count = 0;

            var dt = new DataTable();
            dt.Columns.Add("massmediaID", typeof(short));
            dt.Columns.Add("durationSec", typeof(int)); // issuesDuration (секунды)

            foreach (var drv in selectedRows)
            {
                int massmediaId = Convert.ToInt32(drv["massmediaID"]);

                int primeWd = Convert.ToInt32(drv["PrimeTotalSpotsWeekday"]);
                int nonPrimeWd = Convert.ToInt32(drv["NonPrimeTotalSpotsWeekday"]);

                int primeWe = Convert.ToInt32(drv["PrimeTotalSpotsWeekend"]);
                int nonPrimeWe = Convert.ToInt32(drv["NonPrimeTotalSpotsWeekend"]);

                int rollDuration = Convert.ToInt32(drv["RollerDuration"]);

                int totalSpots =
                    primeWd + nonPrimeWd +
                    primeWe + nonPrimeWe;

                int issuesDurationSec = rollDuration * totalSpots;

                dt.Rows.Add((short)massmediaId, issuesDurationSec);
                count++;
            }

            return dt;
        }

        private void DisplayTotal(decimal totalAfterPackage)
        {
            // cache last calculated totals for later export
            _lastTotalAfterPackage = totalAfterPackage;
            templateEditor.TotalAfterPackageDiscount.Text = "Итог: " + totalAfterPackage.ToString("c");
        }

        private string BuildScheduleDescription()
        {
            if (templateEditor.UseDaysOfWeek)
            {
                var names = new[] { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" };
                var checkedDays = templateEditor.DaysOfWeekChecked;
                var active = names.Where((n, i) => checkedDays[i]).ToList();
                return active.Count > 0 ? string.Join(", ", active) : "—";
            }

            return templateEditor.EvenDaysSelected ? "Чётные дни" : "Нечётные дни";
        }

        // Города отмеченных станций, как в КП: «Показать все» или пустое комбо в «Город:» ничего не говорили (К-13)
        private string BuildCityDescription()
        {
            var groups = grdPriceCalculator.GetSelectedRadiostations()
                .Select(r => r.Row.Table.Columns.Contains("groupName") ? Convert.ToString(r["groupName"]).Trim() : null)
                .Where(g => !string.IsNullOrEmpty(g))
                .Distinct()
                .OrderBy(g => g)
                .ToList();
            return string.Join(", ", groups);
        }

        // часы не переносятся в сутки: 25 ч 50 мин — «25:50:00», а не «01:50:00» (К-8)
        private static string FormatDuration(int totalSeconds)
        {
            return $"{totalSeconds / 3600:00}:{totalSeconds % 3600 / 60:00}:{totalSeconds % 60:00}";
        }

        /// <summary>
        /// Экспорт отмеченных строк PriceCalculatorGrid в Excel.
        /// Привяжите вызов к вашей кнопке/меню.
        /// </summary>
        public void ExportSelectedToExcel()
        {
            if (FlushPendingReload() && !templateEditor.ExcelButton.Enabled) return;
            var dt = grdPriceCalculator.BuildSelectedExportTable();
            if (dt == null || dt.Rows.Count == 0)
            {
                UserMessage.ShowInformation("Нет отмеченных строк для экспорта.");
                return;
            }

            Log.Info($"PriceCalculator excel rows={dt.Rows.Count} total={_lastTotalAfterPackage}");
            var headers = dt.ExtendedProperties["Headers"] as Dictionary<string, string>;
            var doc = new MSExportDocument();
            try
            {
                doc.StartExport();
                var sheet = doc.GetNewSheet("Отмеченные", "Arial", 10);

                int currentRow = 1;
                sheet.SetCellValue(currentRow, 1, "Параметры расчета:");
                sheet.SetBoldForRange(currentRow, 1, currentRow, 1);
                currentRow++;

                sheet.SetCellValue(currentRow++, 1, "Город: " + BuildCityDescription());
                sheet.SetCellValue(currentRow++, 1, string.Format("Период: {0:d} - {1:d}", templateEditor.DateFrom, templateEditor.DateTo));
                sheet.SetCellValue(currentRow++, 1, "График: " + BuildScheduleDescription());

                sheet.SetCellValue(currentRow++, 1, "Количество рекламных выпусков:" + grdPriceCalculator.GetTotalSpots());
                sheet.SetCellValue(currentRow++, 1, "Хронометраж эфирного времени:" + FormatDuration(grdPriceCalculator.GetTotalSeconds()));
                // Теперь таблица с данными
                currentRow += 1; // одна пустая строка
                // заголовки
                var cols = dt.Columns.Cast<DataColumn>().ToList();
                var columnHeaders = new List<string>(cols.Count);
                int headerRow = currentRow;
                for (int c = 0; c < cols.Count; c++)
                {
                    var name = cols[c].ColumnName;
                    var header = headers != null && headers.ContainsKey(name) ? headers[name] : name;
                    columnHeaders.Add(header);
                    sheet.SetCellValue(headerRow, c + 1, header);
                    if (_columnsWithMoney.Contains(name))
                        sheet.SetColumnNumberFormat(c + 1, "#,##0.00");
                    else if (_columnsWithNumeric.Contains(name))
                        sheet.SetColumnNumberFormat(c + 1, "0.00");
                }
                currentRow = headerRow + 1;

                // данные
                for (int r = 0; r < dt.Rows.Count; r++)
                {
                    var row = dt.Rows[r];
                    for (int c = 0; c < cols.Count; c++)
                    {
                        sheet.SetCellValue(currentRow, c + 1, row[c]);
                    }
                    currentRow++;
                }

                sheet.SetBoldForRange(headerRow, 1, headerRow, cols.Count);
                // выделяем жирным всю последнюю колонку (заголовок + данные)
                sheet.SetBoldForRange(headerRow, cols.Count, headerRow + dt.Rows.Count, cols.Count);
                sheet.SetBordersStyles(headerRow, 1, headerRow + dt.Rows.Count, cols.Count, true);
                int totalRow = currentRow;
                sheet.SetCellValue(totalRow, cols.Count - 1, "Итог: ");
                sheet.SetCellValue(totalRow, cols.Count, _lastTotalAfterPackage);
                sheet.SetBoldForRange(totalRow, cols.Count, totalRow, cols.Count);
                currentRow++;

                int lastRow = currentRow - 1;

                // перенос строк для заголовков и данных и автоподбор ширины, затем корректировка минимальной ширины по самому длинному слову заголовка
                sheet.SetWrapText(headerRow, 1, lastRow, cols.Count, true);
                sheet.SetAutoFitCells(1, cols.Count);

                for (int c = 0; c < cols.Count; c++)
                {
                    var headerText = columnHeaders[c];
                    if (string.IsNullOrWhiteSpace(headerText))
                        continue;

                    var longestWordLength = headerText
                        .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .DefaultIfEmpty(string.Empty)
                        .Max(w => w.Length);

                    if (longestWordLength == 0)
                        continue;

                    var currentWidth = sheet.GetColumnWidth(c + 1);
                    // Увеличиваем минимальную ширину с запасом, чтобы слова не дробились (учтём ширину кириллицы)
                    var minWidth = Math.Max(currentWidth, Math.Ceiling(longestWordLength * 1.2) + 2);
                    sheet.SetColumnWidth(c + 1, minWidth);
                }

                sheet.SetAutoFitRows(headerRow, lastRow);

                //sheet.SetAutoFitCells(); // keep global autofit if needed for other sheets
                doc.FinishExport();
            }
            finally
            {
                doc.OnAppQuit();
            }
        }

        private void SaveVariant()
        {
            // перечитывание могло снять все отметки (другая группа) или обнулить строки — тогда кнопка уже закрыта
            if (FlushPendingReload() && !templateEditor.SaveButton.Enabled) return;
            // кнопка без выбранных дней закрыта (UpdateSummary); снимок без дней не построить (К-9)
            if (grdPriceCalculator.GetSelectedDates().Count == 0) return;

            var snapshot = BuildSnapshot();
            Log.Info($"PriceCalculator save variant stations={snapshot.Rows.Count} total={snapshot.GrandTotal} editing={_editingSnapshot != null}");
            
            // ✅ Проверяем, редактируем ли мы существующий вариант
            if (_editingSnapshot != null)
            {
                PCSaveVariantQuestion form = new PCSaveVariantQuestion();
                if(form.ShowDialog() == DialogResult.Cancel)
                {
                    return; // Пользователь отменил сохранение
                }

                // Спрашиваем пользователя, что делать

                if (form.OverrideRadioButton.Checked)
                {
                    // Перезаписываем существующий вариант
                    int index = _saved.IndexOf(_editingSnapshot);
                    if (index >= 0)
                    {
                        _saved[index] = snapshot;
                        _editingSnapshot = null; // Сбрасываем флаг редактирования
                        RenderSavedVariants(_saved);
                        return;
                    }
                    else
                    {
                        // Если вдруг не нашли (удалили пока редактировали), сохраняем как новый
                        _editingSnapshot = null;
                    }
                }
                else 
                {
                    // Сохраняем как новый вариант
                    _editingSnapshot = null; // Сбрасываем флаг редактирования
                    // Продолжаем обычную логику сохранения ниже
                }
            }

            // Обычная логика сохранения нового варианта
            if (_saved.Contains(snapshot))
            {
                UserMessage.ShowInformation("Данный вариант уже добавлен.");
                return;
            }
            
            _saved.Add(snapshot);
            RenderSavedVariants(_saved);
        }

        public CampaignCalcSnapshot BuildSnapshot()
        {
            var snap = new CampaignCalcSnapshot
            {
                DateFrom = grdPriceCalculator.GetSelectedDates().Min(),
                DateTo = grdPriceCalculator.GetSelectedDates().Max(),
                PeriodFrom = templateEditor.DateFrom,
                PeriodTo = templateEditor.DateTo,
                TotalDays = grdPriceCalculator.GetSelectedDates().Count,
                TotalDuration = grdPriceCalculator.GetTotalSeconds(),

                GrandTotal = _lastTotalAfterPackage,

                // Template editor settings
                MassmediaGroupId = templateEditor.MassmediaGroupId,
                DurationSec = templateEditor.DurationSec,
                PrimePerDayWeekday = templateEditor.PrimePerDayWeekday,
                NonPrimePerDayWeekday = templateEditor.NonPrimePerDayWeekday,
                PrimePerDayWeekend = templateEditor.PrimePerDayWeekend,
                NonPrimePerDayWeekend = templateEditor.NonPrimePerDayWeekend,
                ManagerDiscountValue = templateEditor.ManagerDiscount,
                ManagerDiscountModeSingle = templateEditor.ManagerDiscountModeSingle,
                CurrentUserId = templateEditor.CurrentUserId,
                PositionValue = (int)templateEditor.SelectedPosition,

                // Schedule settings
                UseDaysOfWeek = templateEditor.UseDaysOfWeek,
                EvenDaysSelected = templateEditor.EvenDaysSelected,
                DaysOfWeekChecked = templateEditor.DaysOfWeekChecked,

                Rows = new List<CampaignCalcRow>()
            };

            var selectedRows = grdPriceCalculator.GetSelectedRadiostations();
            foreach (var rv in selectedRows)
            {
                if (rv == null) continue;

                var r = rv.Row;
                if (r == null) continue;
                if (r.RowState == DataRowState.Deleted) continue;

                var row = new CampaignCalcRow
                {
                    StationName = Convert.ToString(r["name"]),
                    MassmediaId = Convert.ToInt32(r["massmediaID"]),
                    GroupName = r.Table.Columns.Contains("groupName") ? Convert.ToString(r["groupName"]) : null,
                    ShortName = r.Table.Columns.Contains("shortName") ? Convert.ToString(r["shortName"]) : null,

                    // calc columns
                    PrimeTotalSpotsWeekday = GetInt(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.PrimeTotalSpotsWeekday)),
                    NonPrimeTotalSpotsWeekday = GetInt(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.NonPrimeTotalSpotsWeekday)),
                    PrimeTotalSpotsWeekend = GetInt(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.PrimeTotalSpotsWeekend)),
                    NonPrimeTotalSpotsWeekend = GetInt(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.NonPrimeTotalSpotsWeekend)),

                    RollerDuration = GetInt(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.RollerDuration)),
                    Position = GetInt(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.Position)),

                    TotalBeforePackage = GetDecimal(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.TotalBeforePackage)),
                    PackageDiscount = GetDecimal(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.PackageDiscount)),
                    TotalAfterPackage = GetDecimal(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.TotalAfterPackage)),

                    CompanyDiscount = GetDecimal(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.CompanyDiscount)),
                    TotalWithDiscount = GetDecimal(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.TotalWithDiscount)),

                    ManagerDiscount = GetDecimal(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.ManagerDiscount)),
                    TotalAmount = GetDecimal(r, ColumnName(PriceCalculatorGrid.PriceCalculatorColumn.TotalAmount))
                };

                snap.Rows.Add(row);
            }

            return snap;
        }

        private static string ColumnName(PriceCalculatorGrid.PriceCalculatorColumn column) => column.ToString();

        private static int GetInt(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col)) return 0;
            object v = r[col];
            return v == DBNull.Value ? 0 : Convert.ToInt32(v);
        }

        private static decimal GetDecimal(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col)) return 0m;
            object v = r[col];
            return v == DBNull.Value ? 0m : Convert.ToDecimal(v);
        }

        private static bool GetBool(DataRow r, string col)
        {
            if (!r.Table.Columns.Contains(col)) return false;
            object v = r[col];
            return v != DBNull.Value && Convert.ToBoolean(v);
        }

        private void RenderSavedVariants(IEnumerable<CampaignCalcSnapshot> items)
        {
            flpSaved.SuspendLayout();
            try
            {
                var cards = flpSaved.Controls.OfType<Panel>().ToList();
                foreach (var card in cards)
                    flpSaved.Controls.Remove(card);

                foreach (var snap in items)
                {
                    var card = CreateSnapshotCard(snap);
                    flpSaved.Controls.Add(card);
                }

                ApplySnapshotCardWidths();
                SetSnapshotSelection(chkAll.Checked);
            }
            finally
            {
                flpSaved.ResumeLayout(true);
            }
        }

        private Control CreateSnapshotCard(CampaignCalcSnapshot snap)
        {
            var card = new Panel
            {
                AutoSize = false,
                Width = GetSnapshotCardWidth(),
                Padding = new Padding(0, 8, 8, 8),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = snap
            };

            var chk = new CheckBox
            {
                AutoSize = true,
                Location = new Point(7, 8), // ✅ 7 вместо 8 (компенсация border)
                Name = "chkSelect",
                Checked = true
            };

            var title = new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(29, 8), // ✅ 29 вместо 30 (для консистентности)
                Text = BuildTitle(snap),
                Name = "lblTitle"
            };

            var details = new Label
            {
                AutoSize = true,
                Location = new Point(29, 30), // ✅ 29 вместо 30
                MaximumSize = new Size(card.Width - 40, 0),
                Text = BuildDetails(snap),
                Name = "lblDetails"
            };
            
            var btnApply = new Button
            {
                Text = "Редактировать",
                Width = 100,
                Height = 33,
                Location = new Point(29, details.Bottom + 8), // ✅ 29 вместо 30
                Tag = snap,
                Name = "btnApply"
            };
            btnApply.Click += (s, e) => ApplySnapshot((CampaignCalcSnapshot)((Button)s).Tag);
            
            var btnDelete = new Button
            {
                Text = "Удалить",
                Width = 100,
                Height = 33,
                Location = new Point(29, details.Bottom + 8),
                Tag = snap,
                Name = "btnDelete"
            };
            btnDelete.Click += (s, e) => DeleteSnapshot((CampaignCalcSnapshot)((Button)s).Tag);

            // Клик по карточке/тексту — переключает чекбокс
            title.Click += (s, e) => chk.Checked = !chk.Checked;
            details.Click += (s, e) => chk.Checked = !chk.Checked;

            // лёгкая подсветка
            card.MouseEnter += (s, e) => card.BackColor = Color.AliceBlue;
            card.MouseLeave += (s, e) => card.BackColor = SystemColors.Control;

            card.Controls.Add(chk);
            card.Controls.Add(title);
            card.Controls.Add(details);
            card.Controls.Add(btnApply);
            card.Controls.Add(btnDelete);

            LayoutSnapshotCard(card);

            return card;
        }

        private void ApplySnapshotCardWidths()
        {
            int width = GetSnapshotCardWidth();
            foreach (Control control in flpSaved.Controls)
            {
                if (control is Panel panel)
                {
                    panel.Width = width;
                    LayoutSnapshotCard(panel);
                }
            }
        }

        private void SetSnapshotSelection(bool isChecked)
        {
            foreach (Control control in flpSaved.Controls)
            {
                var chk = control.Controls["chkSelect"] as CheckBox;
                if (chk != null)
                    chk.Checked = isChecked;
            }
        }

        private void LayoutSnapshotCard(Panel card)
        {
            var title = card.Controls["lblTitle"] as Label;
            var details = card.Controls["lblDetails"] as Label;
            var btnApply = card.Controls["btnApply"] as Button;
            var btnDelete = card.Controls["btnDelete"] as Button;

            if (title == null || details == null)
                return;

            details.MaximumSize = new Size(card.Width - 40, 0);
            details.Size = details.PreferredSize;

            if (btnApply != null)
            {
                btnApply.Location = new Point(29, details.Bottom + 8); // ✅ 29
            }

            if (btnDelete != null)
            {
                int leftPosition = btnApply != null ? btnApply.Right + 8 : 29;
                btnDelete.Location = new Point(leftPosition, details.Bottom + 8);
            }

            int bottomMostButton = Math.Max(
                btnApply?.Bottom ?? 0,
                btnDelete?.Bottom ?? 0
            );

            card.Height = bottomMostButton + 8;
        }

        private int GetSnapshotCardWidth()
        {
            int width = flpSaved.DisplayRectangle.Width - flpSaved.Padding.Horizontal;
            if (flpSaved.VerticalScroll.Visible)
                width -= SystemInformation.VerticalScrollBarWidth;

            return Math.Max(100, width - 8);
        }

        private string BuildTitle(CampaignCalcSnapshot snap)
        {
            // Можно добавить название, если введёшь поле Name
            return string.Format("Период: с {0:dd.MM.yyyy} до {1:dd.MM.yyyy}", snap.DateFrom, snap.DateTo);
        }

        private string BuildDetails(CampaignCalcSnapshot snap)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("Радиостанции: {0}", snap.GetRadiostationsList()));
            builder.AppendLine(string.Format("Количество дней рекламной акции: {0}", snap.TotalDays));
            builder.AppendLine(string.Format("Продолжительность ролика: {0}", snap.DurationSec));
            builder.AppendLine(string.Format("Суммарный хронометраж эфирного времени: {0}", FormatDuration(snap.TotalDuration)));
            builder.AppendLine(string.Format("Итог: {0}", snap.GrandTotal.ToString("c")));
            return builder.ToString();
        }

        private List<CampaignCalcSnapshot> GetCheckedSnapshots()
        {
            var result = new List<CampaignCalcSnapshot>();

            foreach (Control c in flpSaved.Controls)
            {
                var snap = c.Tag as CampaignCalcSnapshot;
                if (snap == null) continue;

                // ищем чекбокс внутри карточки
                var chk = c.Controls.OfType<CheckBox>().FirstOrDefault();
                if (chk != null && chk.Checked)
                    result.Add(snap);
            }

            return result;
        }

        private void ApplySnapshot(CampaignCalcSnapshot snap)
        {
            // ✅ Устанавливаем флаг редактирования
            _editingSnapshot = snap;
            LoadVariant(snap);
            // ✅ Переключаемся на закладку с расчетами
            if (tabControl1 != null && tpCalc != null)
            {
                tabControl1.SelectedTab = tpCalc;
            }

            // Устанавливаем фокус на редактор шаблона
            templateEditor.Focus();

        }

        // ✅ Добавляем метод для сброса режима редактирования
        private void ClearEditingMode()
        {
            _editingSnapshot = null;
        }

        public void LoadVariant(CampaignCalcSnapshot variant)
        {
            if (variant == null) return;

            // Введённый период, а не первый и последний выбранный день: с ними сдвигались дата пакетной скидки и диапазон
            // коэффициента, и итог расходился с сохранённым (К-26). У вариантов без введённого периода — как раньше.
            DateTime periodFrom = variant.PeriodFrom != default(DateTime) ? variant.PeriodFrom : variant.DateFrom;
            DateTime periodTo = variant.PeriodTo != default(DateTime) ? variant.PeriodTo : variant.DateTo;
            _reloadTimer.Stop(); // модель варианта читается ниже

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                // 1) Restore template editor settings. Менеджер — первым: его смена ставит в поле коэффициента значение
                // по его UserDiscount и затирала восстановленное (К-14). События полей на это время не пересчитывают и не
                // перечитывают таблицу — расчёт ниже один, по варианту.
                _restoringVariant = true;
                try
                {
                    templateEditor.CurrentUserId = variant.CurrentUserId;
                    templateEditor.SetMassmediaGroupId(variant.MassmediaGroupId);
                    templateEditor.SetDateRange(periodFrom, periodTo);
                    templateEditor.SetDurationSec(variant.DurationSec);
                    templateEditor.SetSpotsSettings(
                        variant.PrimePerDayWeekday,
                        variant.NonPrimePerDayWeekday,
                        variant.PrimePerDayWeekend,
                        variant.NonPrimePerDayWeekend);
                    templateEditor.SetManagerDiscountSettings(variant.ManagerDiscountValue, variant.ManagerDiscountModeSingle);
                    templateEditor.SetPosition(variant.PositionValue);
                    templateEditor.SetSchedulePattern(
                        variant.UseDaysOfWeek,
                        variant.EvenDaysSelected,
                        variant.DaysOfWeekChecked);
                }
                finally
                {
                    _restoringVariant = false;
                }

                // 2) Reload grid data with the variant's massmedia group and date range
                grdPriceCalculator.LoadData(variant.MassmediaGroupId, periodFrom, periodTo, variant.CurrentUserId);
                // строкам — позиция шаблона варианта, как при «Рассчитать»; строки варианта получат свои ниже
                grdPriceCalculator.PresetPositions((RollerPositions)variant.PositionValue, onlyMissing: false);

                // 3) Build selected dates based on schedule pattern
                var selectedDates = BuildSelectedDates(
                    periodFrom, periodTo,
                    variant.UseDaysOfWeek,
                    variant.DaysOfWeekChecked,
                    variant.EvenDaysSelected
                );

                // 4) Select radiostations from the saved variant
                SelectRadiostationsFromVariant(variant);

                // 5) Apply calculation to restore calculated values (with template defaults)
                grdPriceCalculator.ApplyCalculation(
                    selectedDates,
                    variant.DurationSec,
                    variant.PrimePerDayWeekday,
                    variant.NonPrimePerDayWeekday,
                    variant.PrimePerDayWeekend,
                    variant.NonPrimePerDayWeekend,
                    variant.ManagerDiscountValue,
                    variant.ManagerDiscountModeSingle,
                    variant.CurrentUserId
                );

                // ✅ 6) Restore individual row values from saved snapshot (overrides template defaults)
                grdPriceCalculator.RestoreRowDetails(variant.Rows);

                // 8) Update summary
                UpdateSummary();
                
                // ✅ 9) Switch to calculation tab
                if (tabControl1 != null && tpCalc != null)
                {
                    tabControl1.SelectedTab = tpCalc;
                }
                
                templateEditor.Focus();
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private void SelectRadiostationsFromVariant(CampaignCalcSnapshot variant)
        {
            if (grdPriceCalculator.SummaryTable == null || variant.Rows == null) return;

            // Get the MassmediaIds from the saved variant
            var selectedIds = new HashSet<int>(variant.Rows.Select(r => r.MassmediaId));

            // Mark rows in SummaryTable as selected based on MassmediaId
            string isSelectedColumn = "IsSelected";
            if (!grdPriceCalculator.SummaryTable.Columns.Contains(isSelectedColumn))
                return;

            foreach (DataRow row in grdPriceCalculator.SummaryTable.Rows)
            {
                if (row.RowState == DataRowState.Deleted) continue;

                int massmediaId = (row["MassmediaID"] == DBNull.Value) ? 0 : Convert.ToInt32(row["MassmediaID"]);
                row[isSelectedColumn] = (massmediaId != 0 && selectedIds.Contains(massmediaId));
            }
        }

        private void DeleteSnapshot(CampaignCalcSnapshot snap)
        {
            // ✅ Если удаляем редактируемый вариант, сбрасываем флаг
            if (_editingSnapshot == snap)
            {
                _editingSnapshot = null;
            }
            
            _saved.Remove(snap);
            RenderSavedVariants(_saved);
        }

        private void DeleteCheckedSnapshots()
        {
            var snapshots = GetCheckedSnapshots();
            if (snapshots.Count == 0)
                return;

            foreach (var snap in snapshots)
            {
                // ✅ Если удаляем редактируемый вариант, сбрасываем флаг
                if (_editingSnapshot == snap)
                {
                    _editingSnapshot = null;
                }
                _saved.Remove(snap);
            }

            RenderSavedVariants(_saved);
        }

        private void btnCreaateProposal_Click(object sender, EventArgs e)
        {
            try
            {
                var list = GetCheckedSnapshots();
                if (!list.Any())
                {
                    UserMessage.ShowExclamation("Нет отмеченных вариантов для создания коммерческого предложения.");
                    return;
                }

                if (cmbAgency.SelectedValue == null)
                {
                    UserMessage.ShowExclamation("Пожалуйста, выберите агентство.");
                    return;
                }

                var agency = Agency.GetAgencyByID(Convert.ToInt32(cmbAgency.SelectedValue));
                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationUtil.ProposalTemplateFolder, agency[Agency.ParamNames.Path2ProposalTemplate].ToString());
                if (!File.Exists(templatePath))
                {
                    UserMessage.ShowExclamation($"Шаблон коммерческого предложения для агентства «{agency.Name}» не найден по пути:\n{templatePath}");
                    return;
                }

                var outFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CommercialOffers");
                Directory.CreateDirectory(outFolder);

                var fileName = string.IsNullOrEmpty(txtFirmName.Text) ? $"КП_{DateTime.Now:dd.MM.yyyy-HH-mm-ss}.docx" : $"КП для {txtFirmName.Text} {DateTime.Now:dd.MM.yyyy-HH-mm-ss}.docx";
                var invalidChars = Path.GetInvalidFileNameChars();
                fileName = string.Concat(fileName.Select(ch => invalidChars.Contains(ch) ? '_' : ch));

                var outPath = Path.Combine(outFolder, fileName);
                SecurityManager.User user = SecurityManager.GetUser(templateEditor.CurrentUserId);
                Log.Info($"PriceCalculator proposal variants={list.Count} agency={cmbAgency.SelectedValue} manager={templateEditor.CurrentUserId}");

                var result = Cp.CpOneDocGenerator.GenerateOneDoc(
                    list,
                    templatePath,
                    outPath,
                    clientName: txtFirmName.Text,
                    docDate: DateTime.Today,
                    contactName: user.FullName,
                    contactEmail: user.Email,
                    contactPhone: user.Phone
                );

                Process.Start(new ProcessStartInfo(result) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        private void btnSelectFirm_Click(object sender, EventArgs e)
        {
            try
            {
                Firm firm = Firm.SelectFirm(this);
                if (firm == null) return;
                txtFirmName.Text = firm.PrefixWithName;
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            txtFirmName.Text = string.Empty;
        }
    }
}