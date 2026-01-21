using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export.MSExcel;
using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class PriceCalculatorForm : Form
    {
        private decimal _lastTotalBeforePackageDiscount;
        private decimal _lastPackageDiscount;
        private decimal _lastTotalAfterPackage;
        private readonly string[] _columnsWithMoney = new[]
        {
            "Цена прайм будни",
            "Цена не-прайм будни",
            "Цена прайм выходные",
            "Цена не-прайм выходные",
            "Цена кампания",
            "Итог"
        };

        public PriceCalculatorForm()
        {
            InitializeComponent();
            templateEditor.CalculateButton.Click += CalculateButton_Click;
            templateEditor.ExcelButton.Click += (s, e) => ExportSelectedToExcel();
            templateEditor.ManagerDiscountNum.ValueChanged += (s, e) => grdPriceCalculator.SetManagerDiscount(templateEditor.ManagerDiscount);

            templateEditor.PositionChanged += (s, e) =>
                grdPriceCalculator.SetDefaultPosition(templateEditor.SelectedPosition);

            // пересчитываем при смене дневных/выходных количеств
            templateEditor.SpotsSettingsChanged += (s, e) => RecalculateFromTemplateInputs();
            templateEditor.DurationChanged += (s, e) => RecalculateFromTemplateInputs();
            grdPriceCalculator.SummaryUpdater = UpdateSummary;
        }

        private void RecalculateFromTemplateInputs()
        {
            if (grdPriceCalculator.SummaryTable == null) return;

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
                templateEditor.ManagerDiscount
            );

            // оставить позицию из шаблона и актуальный SummaryUpdater
            //grdPriceCalculator.SetDefaultPosition(templateEditor.SelectedPosition);
            UpdateSummary();
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            if (!templateEditor.IsDaysOfWeekValid())
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation("Выберите хотя бы один день недели");
                return;
            }

            if (templateEditor.DateFrom > templateEditor.DateTo)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation("Дата начала периода акции не может быть больше даты окончания");
                return;
            }

            try
            {
                UseWaitCursor = true;
                Cursor.Current = Cursors.WaitCursor;
                Application.DoEvents();

                grdPriceCalculator.LoadData(templateEditor.MassmediaGroupId, templateEditor.DateFrom, templateEditor.DateTo);

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
                    templateEditor.ManagerDiscount
                );

                // ✅ протянуть позицию из шаблона в таблицу грида после пересоздания данных
                grdPriceCalculator.SetDefaultPosition(templateEditor.SelectedPosition);

                
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
            var swTotal = Stopwatch.StartNew();
            var swStep = Stopwatch.StartNew();
            Debug.WriteLine("[PriceCalc] UpdateSummary start");

            try
            {
                var selectedRows = grdPriceCalculator.GetSelectedRadiostations();
                Debug.WriteLine($"[PriceCalc] GetSelectedRadiostations: {swStep.ElapsedMilliseconds} ms");

                swStep.Restart();
                decimal totalBeforePackageDiscount = grdPriceCalculator.GetSelectedTotalWithManagerDiscount();
                Debug.WriteLine($"[PriceCalc] GetSelectedTotalWithManagerDiscount: {swStep.ElapsedMilliseconds} ms");

                swStep.Restart();
                decimal packageDiscount = GetPackageDiscount(
                    startDate: templateEditor.DateFrom,
                    priceTotal: totalBeforePackageDiscount,
                    selectedRows: selectedRows
                );
                Debug.WriteLine($"[PriceCalc] GetPackageDiscount: {swStep.ElapsedMilliseconds} ms");

                decimal totalAfterPackage = totalBeforePackageDiscount * packageDiscount;

                swStep.Restart();
                grdPriceCalculator.ApplyPackageTotals(packageDiscount);
                Debug.WriteLine($"[PriceCalc] ApplyPackageTotals: {swStep.ElapsedMilliseconds} ms");

                swStep.Restart();
                DisplayTotal(totalBeforePackageDiscount, packageDiscount, totalAfterPackage);
                Debug.WriteLine($"[PriceCalc] DisplayTotal: {swStep.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                Debug.WriteLine($"[PriceCalc] UpdateSummary total: {swTotal.ElapsedMilliseconds} ms");
            }
        }

        private decimal GetPackageDiscount(DateTime startDate, decimal priceTotal, List<DataRowView> selectedRows)
        {
            var sw = Stopwatch.StartNew();
            Debug.WriteLine($"[PriceCalc] GetPackageDiscount start, selected={selectedRows.Count}, priceTotal={priceTotal}");

            try
            {
                if (selectedRows.Count == 0) return 1m;

                var tvpTable = BuildSelectedMassmediaTvp(selectedRows);

                var p = DataAccessor.CreateParametersDictionary();
                p["startDate"] = startDate;
                p["campaignTypeID"] = (byte)1;
                p["priceTotal"] = priceTotal;
                p["sel"] = new FogSoft.WinForm.DataAccess.TvpValue(tvpTable, "dbo.pc_SelectedMassmedia");

                p["discountValue"] = null;               // OUT
                p["packageDiscountPriceListID"] = null;  // OUT (если нужно показать какой пакет)

                var swDb = Stopwatch.StartNew();
                DataAccessor.ExecuteNonQuery("pc_PackageDiscountCalculateModel", p, 30, false);
                Debug.WriteLine($"[PriceCalc] GetPackageDiscount ExecuteNonQuery: {swDb.ElapsedMilliseconds} ms");

                var dv = p["discountValue"];
                return (dv == null || dv == DBNull.Value) ? 1m : Convert.ToDecimal(dv);
            }
            finally
            {
                Debug.WriteLine($"[PriceCalc] GetPackageDiscount total: {sw.ElapsedMilliseconds} ms");
            }
        }

        private DataTable BuildSelectedMassmediaTvp(IEnumerable<DataRowView> selectedRows)
        {
            var sw = Stopwatch.StartNew();
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

            Debug.WriteLine($"[PriceCalc] BuildSelectedMassmediaTvp rows={count} in {sw.ElapsedMilliseconds} ms");
            return dt;
        }

        private void DisplayTotal(decimal totalBeforePackageDiscount, decimal packageDiscount, decimal totalAfterPackage)
        {
            // cache last calculated totals for later export
            _lastTotalBeforePackageDiscount = totalBeforePackageDiscount;
            _lastPackageDiscount = packageDiscount;
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

        /// <summary>
        /// Экспорт отмеченных строк PriceCalculatorGrid в Excel.
        /// Привяжите вызов к вашей кнопке/меню.
        /// </summary>
        public void ExportSelectedToExcel()
        {
            var dt = grdPriceCalculator.BuildSelectedExportTable();
            if (dt == null || dt.Rows.Count == 0)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowInformation("Нет отмеченных строк для экспорта.");
                return;
            }

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

                sheet.SetCellValue(currentRow++, 1, "Город: " + templateEditor.MassmediaGroupName);
                sheet.SetCellValue(currentRow++, 1, string.Format("Период: {0:d} - {1:d}", templateEditor.DateFrom, templateEditor.DateTo));
                sheet.SetCellValue(currentRow++, 1, "График: " + BuildScheduleDescription());
                
                sheet.SetCellValue(currentRow++, 1, "Количество рекламных выпусков:" + grdPriceCalculator.GetTotalSpots());
                sheet.SetCellValue(currentRow++, 1, "Хронометраж эфирного времени:" + grdPriceCalculator.GetTotalSeconds());

                // Теперь таблица с данными
                currentRow += 1; // одна пустая строка
                // заголовки
                var cols = dt.Columns.Cast<DataColumn>().ToList();
                int headerRow = currentRow;
                for (int c = 0; c < cols.Count; c++)
                {
                    var name = cols[c].ColumnName;
                    var header = headers != null && headers.ContainsKey(name) ? headers[name] : name;
                    sheet.SetCellValue(headerRow, c + 1, header);
                    if (_columnsWithMoney.Contains(header))
                        sheet.SetColumnNumberFormat(c + 1, "#,##0.00");
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
                sheet.SetCellValue(totalRow, cols.Count-1, "Итог: ");
                sheet.SetCellValue(totalRow, cols.Count,  _lastTotalAfterPackage);
                sheet.SetBoldForRange(totalRow, cols.Count, totalRow, cols.Count);
                currentRow++;

                int lastRow = currentRow - 1;

                // перенос строк и автоподбор ширины по данным (без удлинения заголовками)
                sheet.SetWrapText(1, 3, lastRow, cols.Count, true);
                sheet.SetAutoFitCells(1, cols.Count);

                sheet.SetAutoFitCells(); // keep global autofit if needed for other sheets
                doc.FinishExport();
            }
            finally
            {
                doc.OnAppQuit();
            }
        }
    }
}