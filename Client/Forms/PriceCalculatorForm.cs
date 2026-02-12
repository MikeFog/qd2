using DocumentFormat.OpenXml.Bibliography;
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

namespace Merlin.Forms
{
    public partial class PriceCalculatorForm : Form
    {
        private readonly List<CampaignCalcSnapshot> _saved = new List<CampaignCalcSnapshot>();
        private decimal _lastTotalAfterPackage;
        
        // ✅ Добавляем поле для отслеживания редактируемого варианта
        private CampaignCalcSnapshot _editingSnapshot = null;
        
        private readonly string[] _columnsWithMoney = new[]
        {
            "Цена прайм будни",
            "Цена не-прайм будни",
            "Цена прайм выходные",
            "Цена не-прайм выходные",
            "Цена кампании",
            "Цена до пакетной скидки",
            "Итог"
        };

        private readonly string[] _columnsWithNumeric = new[]
        {
            "Сезонный коэфф.",
            "Объёмная скидка",
            "Пакетная скидка",
        };

        public PriceCalculatorForm()
        {
            InitializeComponent();
            templateEditor.SaveButton.Click += (s, e) => SaveVariant();
            templateEditor.CalculateButton.Click += CalculateButton_Click;
            templateEditor.ExcelButton.Click += (s, e) => ExportSelectedToExcel();
            templateEditor.ManagerDiscountNum.ValueChanged += (s, e) => grdPriceCalculator.SetManagerDiscount(templateEditor.ManagerDiscount);

            templateEditor.PositionChanged += (s, e) => grdPriceCalculator.SetDefaultPosition(templateEditor.SelectedPosition);

            // пересчитываем при смене дневных/выходных количеств
            templateEditor.SpotsSettingsChanged += (s, e) => RecalculateFromTemplateInputs();
            templateEditor.DurationChanged += (s, e) => RecalculateFromTemplateInputs();
            templateEditor.ManagerDiscountModeChanged += (s, e) => RecalculateFromTemplateInputs();

            // новый авто-пересчёт по датам/дням/чётности
            templateEditor.ScheduleChanged += (s, e) => RecalculateFromTemplateInputs();

            grdPriceCalculator.SummaryUpdater = UpdateSummary;
            flpSaved.SizeChanged += FlpSaved_SizeChanged;
            chkAll.CheckedChanged += ChkAll_CheckedChanged;
            btnDeleteAllChecked.Click += BtnDeleteAllChecked_Click;
            LoadAgencies();
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

                cmbAgency.SelectedValue = 1;
            }
            finally
            {
                cmbAgency.EndUpdate();
            }
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
                templateEditor.ManagerDiscount,
                templateEditor.ManagerDiscountModeSingle
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
                templateEditor.TotalAfterPackageDiscount.Text = "Итог: ";

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
                    templateEditor.ManagerDiscount,
                    templateEditor.ManagerDiscountModeSingle
                );

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
            try
            {
                var selectedRows = grdPriceCalculator.GetSelectedRadiostations();
                templateEditor.SaveButton.Enabled = templateEditor.ExcelButton.Enabled = selectedRows.Count >0;

                decimal totalwithCampaignDiscount = grdPriceCalculator.GetSelectedTotalWithCampaignDiscount();
                decimal packageDiscount = GetPackageDiscount(
                    startDate: templateEditor.DateFrom,
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
            p["priceTotal"] = priceTotal;
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
                var totalSeconds = grdPriceCalculator.GetTotalSeconds();
                var duration = TimeSpan.FromSeconds(totalSeconds);
                sheet.SetCellValue(currentRow++, 1, "Хронометраж эфирного времени:" + duration.ToString(@"hh\:mm\:ss"));
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
                    if (_columnsWithMoney.Contains(header))
                        sheet.SetColumnNumberFormat(c + 1, "#,##0.00");
                    else if (_columnsWithNumeric.Contains(header))
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
            var snapshot = BuildSnapshot();
            
            // ✅ Проверяем, редактируем ли мы существующий вариант
            if (_editingSnapshot != null)
            {
                // Спрашиваем пользователя, что делать
                var result = MessageBox.Show(
                    "Вы редактируете существующий вариант.\n\n" +
                    "Да - перезаписать существующий вариант\n" +
                    "Нет - сохранить как новый вариант\n" +
                    "Отмена - отменить сохранение",
                    "Сохранение варианта",
                    MessageBoxButtons.YesNoCancel,
                    MessageBoxIcon.Question);

                if (result == DialogResult.Cancel)
                {
                    return; // Отменяем сохранение
                }
                else if (result == DialogResult.Yes)
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
                else // DialogResult.No
                {
                    // Сохраняем как новый вариант
                    _editingSnapshot = null; // Сбрасываем флаг редактирования
                    // Продолжаем обычную логику сохранения ниже
                }
            }

            // Обычная логика сохранения нового варианта
            if (_saved.Contains(snapshot))
            {
                MessageBox.Show("Данный вариант уже добавлен.", "Сохранение не требуется", MessageBoxButtons.OK, MessageBoxIcon.Stop);
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
                flpSaved.Controls.Clear();

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
                Padding = new Padding(8),
                BorderStyle = BorderStyle.FixedSingle,
                Tag = snap
            };

            var chk = new CheckBox
            {
                AutoSize = true,
                Location = new Point(8, 8),
                Name = "chkSelect",
                Checked = true
            };

            var title = new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Location = new Point(30, 8),
                Text = BuildTitle(snap),
                Name = "lblTitle"
            };

            var details = new Label
            {
                AutoSize = true,
                Location = new Point(30, 30),
                MaximumSize = new Size(card.Width - 40, 0),
                Text = BuildDetails(snap),
                Name = "lblDetails"
            };
            var btnApply = new Button
            {
                Text = "Редактировать",
                Width = 100,
                Height = 26,
                Location = new Point(30, details.Bottom + 8),
                Tag = snap,
                Name = "btnApply"
            };
            btnApply.Click += (s, e) => ApplySnapshot((CampaignCalcSnapshot)((Button)s).Tag);
            var btnDelete = new Button
            {
                Text = "Удалить",
                Width = 90,
                Height = 26,
                Location = new Point(30, details.Bottom + 8),
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

            // Position buttons side by side
            if (btnApply != null)
            {
                btnApply.Location = new Point(30, details.Bottom + 8);
            }

            if (btnDelete != null)
            {
                int leftPosition = btnApply != null ? btnApply.Right + 8 : 30;
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
            var duration = TimeSpan.FromSeconds(snap.TotalDuration);
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format("Радиостанции: {0}", snap.GetRadiostationsList()));
            builder.AppendLine(string.Format("Количество дней рекламной акции: {0}", snap.TotalDays));
            builder.AppendLine(string.Format("Суммарный хронометраж эфирного времени: {0}", duration.ToString(@"hh\:mm\:ss")));
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

            try
            {
                Cursor.Current = Cursors.WaitCursor;

                // 1) Restore template editor settings
                templateEditor.SetMassmediaGroupId(variant.MassmediaGroupId);
                templateEditor.SetDateRange(variant.DateFrom, variant.DateTo);
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

                // 2) Reload grid data with the variant's massmedia group and date range
                grdPriceCalculator.LoadData(variant.MassmediaGroupId, variant.DateFrom, variant.DateTo);

                // 3) Build selected dates based on schedule pattern
                var selectedDates = BuildSelectedDates(
                    variant.DateFrom, variant.DateTo,
                    variant.UseDaysOfWeek,
                    variant.DaysOfWeekChecked,
                    variant.EvenDaysSelected
                );

                // 4) Select radiostations from the saved variant
                SelectRadiostationsFromVariant(variant);

                // 5) Apply calculation to restore calculated values
                grdPriceCalculator.ApplyCalculation(
                    selectedDates,
                    variant.DurationSec,
                    variant.PrimePerDayWeekday,
                    variant.NonPrimePerDayWeekday,
                    variant.PrimePerDayWeekend,
                    variant.NonPrimePerDayWeekend,
                    variant.ManagerDiscountValue,
                    variant.ManagerDiscountModeSingle
                );

                // 6) Set position
                grdPriceCalculator.SetDefaultPosition((RollerPositions)variant.PositionValue);

                // 7) Update summary
                UpdateSummary();
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
                    MessageBox.Show("Нет отмеченных вариантов для создания коммерческого предложения.", "Создание КП", MessageBoxButtons.OK, MessageBoxIcon.Information); 
                    return;
                }

                if (cmbAgency.SelectedValue == null)
                {
                    MessageBox.Show("Пожалуйста, выберите агентство.", "Создание КП", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var agency = Agency.GetAgencyByID(Convert.ToInt32(cmbAgency.SelectedValue));
                var templatePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigurationUtil.ProposalTemplateFolder, agency[Agency.ParamNames.Path2ProposalTemplate].ToString());
                if (!File.Exists(templatePath))
                {
                    MessageBox.Show($"Шаблон коммерческого предложения для агентства «{agency.Name}» не найден по пути:\n{templatePath}", "Создание КП", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var outFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CommercialOffers");
                Directory.CreateDirectory(outFolder);

                var outPath = Path.Combine(outFolder, $"КП_{DateTime.Now:yyyyMMdd_HHmmss}.docx");

                var result = Cp.CpOneDocGenerator.GenerateOneDoc(
                    list,
                    templatePath,
                    outPath,
                    clientName: txtFirmName.Text,
                    docDate: DateTime.Today,
                    directorName: "Агамов Владислав Александрович",
                    contactName: SecurityManager.LoggedUser.FullName,
                    contactEmail: SecurityManager.LoggedUser.Email,
                    contactPhone: SecurityManager.LoggedUser.Phone
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