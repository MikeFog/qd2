using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Merlin.Controls
{
    public partial class PriceCalculatorGrid : UserControl
    {
        private List<DateTime> _selectedDates;
        private DateTime _startDate;
        private decimal _managerDiscount = 1m;
        private bool _suppressRecalc;
        private CheckBox _headerCheckBox;
        private bool _bulkUpdating;
        private bool _sortFlag;
        private readonly BindingSource _bindingSource = new BindingSource();
        private object _lastSelectedItem = null;

        public Action SummaryUpdater { get; set; }

        public DataTable SegmentsTable { get; private set; }   // второй рекордсет (сегменты)
        public DataTable SummaryTable { get; private set; }   // первый рекордсет (для грида)

        public PriceCalculatorGrid()
        {
            InitializeComponent();
            ConfigureGrid();
        }

        private void ConfigureGrid()
        {
            dgvStations.AutoGenerateColumns = false;
            dgvStations.AllowUserToAddRows = false;
            dgvStations.AllowUserToDeleteRows = false;
            dgvStations.MultiSelect = false;
            dgvStations.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;

            dgvStations.EditingControlShowing += DgvStations_EditingControlShowing;
            dgvStations.DataError += (s, e) => { e.ThrowException = false; }; // чтобы не всплывали исключения

            dgvStations.CellValidating += DgvStations_CellValidating;


            dgvStations.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (dgvStations.IsCurrentCellDirty)
                    dgvStations.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            dgvStations.CellEndEdit += (s, e) =>
            {
                if (_suppressRecalc) return;
                HandleEditableCellChanged(e.RowIndex, e.ColumnIndex);
            };

            dgvStations.CellValueChanged += (s, e) =>
            {
                if (_suppressRecalc) return;
                if (e.RowIndex < 0) return;

                // чекбокс отрабатывает через CellValueChanged
                if (dgvStations.Columns[e.ColumnIndex].Name == "colSelected")
                    SummaryUpdater?.Invoke();
            };

            dgvStations.MouseDown += (s, e) =>
            {
                // Проверяем, что кликнули именно по ЗАГОЛОВКУ КОЛОНКИ
                var hitInfo = dgvStations.HitTest(e.X, e.Y);

                if (hitInfo.Type == DataGridViewHitTestType.ColumnHeader)
                {
                    // ФИКСИРУЕМ выделение ПЕРЕД тем как начнётся сортировка
                    if (dgvStations.CurrentRow != null)
                    {
                        _lastSelectedItem = dgvStations.CurrentRow.DataBoundItem;
                        _sortFlag = true; // Включаем блокировку СЕЙЧАС
                    }
                }
            };

            dgvStations.SelectionChanged += (s, e) => 
            {
                if (_sortFlag) return;

                if (dgvStations.CurrentRow != null)
                {
                    // Запоминаем объект (DataRowView), а не ID
                    _lastSelectedItem = dgvStations.CurrentRow.DataBoundItem;
                }
            };

            dgvStations.Sorted += (s, e) =>
            {
                if (_lastSelectedItem == null) return;

                int newIndex = _bindingSource.IndexOf(_lastSelectedItem);

                try
                {
                    if (newIndex >= 0)
                    {
                        dgvStations.ClearSelection();
                        dgvStations.Rows[newIndex].Selected = true;
                        dgvStations.CurrentCell = dgvStations.Rows[newIndex].Cells[0];
                        dgvStations.FirstDisplayedScrollingRowIndex = newIndex;
                    }
                }
                finally
                {
                    // Всегда снимаем флаг, даже если была ошибка
                    _sortFlag = false;
                }
            };

            dgvStations.Columns.Clear();

            dgvStations.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "colSelected",
                HeaderText = "",
                Width = 32,
                DataPropertyName = "IsSelected", // можно не биндить пока
                TrueValue = true,
                FalseValue = false,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "Радиостанция",
                DataPropertyName = "name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                MinimumWidth = 220,
                ReadOnly = true,
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colRollerDuration",
                HeaderText = "Пр-ть ролика",
                DataPropertyName = "RollerDuration",
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrimeWeekday",
                HeaderText = "Цена прайм будни",
                DataPropertyName = "PrimePricePerSecWeekday",
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNonPrimeWeekday",
                HeaderText = "Цена не-прайм будни",
                DataPropertyName = "NonPrimePricePerSecWeekday",
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrimeWeekend",
                HeaderText = "Цена прайм выходные",
                DataPropertyName = "PrimePricePerSecWeekend",
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNonPrimeWeekend",
                HeaderText = "Цена не-прайм выходные",
                DataPropertyName = "NonPrimePricePerSecWeekend",
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrimeTotalSpotsWeekday",
                HeaderText = "Кол-во выходов прайм будни",
                DataPropertyName = "PrimeTotalSpotsWeekday",
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNonPrimeTotalSpotsWeekday",
                HeaderText = "Кол-во выходов не прайм будни",
                DataPropertyName = "NonPrimeTotalSpotsWeekday",
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrimeTotalSpotsWeekend",
                HeaderText = "Кол-во выходов прайм выходные",
                DataPropertyName = "PrimeTotalSpotsWeekend",
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNonPrimeTotalSpotsWeekend",
                HeaderText = "Кол-во выходов не прайм выходные",
                DataPropertyName = "NonPrimeTotalSpotsWeekend",
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTotalAmount",
                HeaderText = "Цена компании",
                DataPropertyName = "TotalAmount",
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCompanyDiscount",
                HeaderText = "Скидка",
                DataPropertyName = "CompanyDiscount",
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTotalWithDiscount",
                HeaderText = "Цена компании со скидкой",
                DataPropertyName = "TotalWithDiscount",
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTotalTotalWithManagerDiscount",
                HeaderText = "Цена компании со скидкой менеджера",
                DataPropertyName = "TotalWithManagerDiscount",
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });



            dgvStations.Columns["colName"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            /*
            foreach (DataGridViewColumn col in dgvStations.Columns)
            {
                if (col.Name == "colName")
                    continue;

                col.AutoSizeMode = DataGridViewAutoSizeColumnMode.ColumnHeader;
            }
            */

            InitHeaderCheckbox();
        }

        private void InitHeaderCheckbox()
        {
            if (_headerCheckBox != null) return;

            _headerCheckBox = new CheckBox
            {
                Size = new System.Drawing.Size(15, 15),
                BackColor = System.Drawing.Color.Transparent
            };

            _headerCheckBox.CheckedChanged += (s, e) =>
            {
                if (_bulkUpdating) return;

                // cerrar edición actual
                dgvStations.EndEdit();
                dgvStations.CommitEdit(DataGridViewDataErrorContexts.Commit);
                dgvStations.CurrentCell = null;

                _bulkUpdating = true;
                try
                {
                    dgvStations.SuspendLayout();

                    bool check = _headerCheckBox.Checked;

                    foreach (DataGridViewRow row in dgvStations.Rows)
                    {
                        if (row.DataBoundItem is DataRowView drv)
                            drv["IsSelected"] = check;          // ✅ пишем в DataTable
                        else
                            row.Cells["colSelected"].Value = check; // fallback
                    }
                }
                finally
                {
                    dgvStations.ResumeLayout();
                    _bulkUpdating = false;
                }

                SummaryUpdater?.Invoke();
            };


            dgvStations.Controls.Add(_headerCheckBox);

            // пересчитывать позицию при изменениях layout/скролла
            dgvStations.ColumnWidthChanged += (s, e) => PositionHeaderCheckbox();
            dgvStations.Scroll += (s, e) => PositionHeaderCheckbox();
            dgvStations.SizeChanged += (s, e) => PositionHeaderCheckbox();

            PositionHeaderCheckbox();

            // синхронизация состояния header-чекбокса при изменении строк
            dgvStations.CellValueChanged += (s, e) =>
            {
                if (_bulkUpdating) return;
                if (e.RowIndex < 0) return;
                if (dgvStations.Columns[e.ColumnIndex].Name != "colSelected") return;

                SyncHeaderCheckboxState();
            };
        }

        private void SyncHeaderCheckboxState()
        {
            if (_headerCheckBox == null) return;
            if (dgvStations.Rows.Count == 0) return;

            bool all = true;

            foreach (DataGridViewRow row in dgvStations.Rows)
            {
                bool selected = Convert.ToBoolean(row.Cells["colSelected"].Value ?? false);
                if (!selected) { all = false; break; }
            }

            _bulkUpdating = true;
            try { _headerCheckBox.Checked = all; }
            finally { _bulkUpdating = false; }
        }

        private void PositionHeaderCheckbox()
        {
            if (_headerCheckBox == null) return;

            var col = dgvStations.Columns["colSelected"];
            var rect = dgvStations.GetCellDisplayRectangle(col.Index, -1, true);

            // центрируем в заголовке
            int x = rect.X + (rect.Width - _headerCheckBox.Width) / 2;
            int y = rect.Y + (rect.Height - _headerCheckBox.Height) / 2;

            _headerCheckBox.Location = new System.Drawing.Point(x, y);
        }

        private void DgvStations_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            // интересуют только 3 редактируемые числовые колонки
            string colName = dgvStations.CurrentCell?.OwningColumn?.Name;
            if (colName != "colRollerDuration" &&
                colName != "colPrimeTotalSpots" &&
                colName != "colNonPrimeTotalSpots")
                return;

            if (e.Control is TextBox tb)
            {
                tb.KeyPress -= NumericTextBox_KeyPress;
                tb.KeyPress += NumericTextBox_KeyPress;
            }
        }

        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // разрешаем цифры, backspace, enter
            if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar))
                return;

            // запрещаем всё остальное (буквы, пробелы, запятые, точки и т.п.)
            e.Handled = true;
        }
        private void DgvStations_CellValidating(object sender, DataGridViewCellValidatingEventArgs e)
        {
            if (e.RowIndex < 0) return;

            string colName = dgvStations.Columns[e.ColumnIndex].Name;
            if (colName != "colRollerDuration" &&
                colName != "colPrimeTotalSpotsWeekday" &&
                colName != "colNonPrimeTotalSpotsWeekday" &&
                colName != "colPrimeTotalSpotsWeekend" &&
                colName != "colNonPrimeTotalSpotsWeekend")
                return;

            string text = (e.FormattedValue ?? "").ToString().Trim();

            // пусто => считаем 0 (или можно запретить)
            if (text.Length == 0) return;

            if (!int.TryParse(text, out int val))
            {
                e.Cancel = true;
                dgvStations.Rows[e.RowIndex].ErrorText = "Введите целое число";
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            // диапазоны (под себя подстрой)
            if (colName == "colRollerDuration" && (val < 0 || val > 3600))
            {
                e.Cancel = true;
                dgvStations.Rows[e.RowIndex].ErrorText = "Длительность: 0..3600";
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            if ((colName == "colPrimeTotalSpots" || colName == "colNonPrimeTotalSpots") && (val < 0 || val > 1_000_000))
            {
                e.Cancel = true;
                dgvStations.Rows[e.RowIndex].ErrorText = "Количество: 0..1 000 000";
                System.Media.SystemSounds.Beep.Play();
                return;
            }

            dgvStations.Rows[e.RowIndex].ErrorText = "";
        }


        private void HandleEditableCellChanged(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0) return;

            string colName = dgvStations.Columns[columnIndex].Name;

            if (colName == "colRollerDuration" ||
                colName == "colPrimeTotalSpotsWeekday" ||
                colName == "colNonPrimeTotalSpotsWeekday" ||
                colName == "colPrimeTotalSpotsWeekend" ||
                colName == "colNonPrimeTotalSpotsWeekend")
            {
                RecalcRow(rowIndex);
                SummaryUpdater?.Invoke();
            }
        }

        private void RecalcRow(int rowIndex)
        {
            if (SummaryTable == null || SegmentsTable == null) return;
            if (_selectedDates == null || _selectedDates.Count == 0) return;
            if (rowIndex < 0 || rowIndex >= dgvStations.Rows.Count) return;

            var gridRow = dgvStations.Rows[rowIndex];
            var drv = gridRow.DataBoundItem as DataRowView;
            if (drv == null) return;

            int massmediaId = Convert.ToInt32(drv["massmediaID"]);

            // читаем редактируемые поля из строки
            int roller = SafeInt(drv["RollerDuration"], 0, 3600);

            int primeWdTotal = SafeInt(drv["PrimeTotalSpotsWeekday"], 0, 1_000_000);
            int nonPrimeWdTotal = SafeInt(drv["NonPrimeTotalSpotsWeekday"], 0, 1_000_000);

            int primeWeTotal = SafeInt(drv["PrimeTotalSpotsWeekend"], 0, 1_000_000);
            int nonPrimeWeTotal = SafeInt(drv["NonPrimeTotalSpotsWeekend"], 0, 1_000_000);

            // обратно нормализованные значения (если пользователь ввёл мусор)
            _suppressRecalc = true;
            try
            {
                drv["RollerDuration"] = roller;

                drv["PrimeTotalSpotsWeekday"] = primeWdTotal;
                drv["NonPrimeTotalSpotsWeekday"] = nonPrimeWdTotal;

                drv["PrimeTotalSpotsWeekend"] = primeWeTotal;
                drv["NonPrimeTotalSpotsWeekend"] = nonPrimeWeTotal;
            }
            finally
            {
                _suppressRecalc = false;
            }

            // считаем сумму по сегментам (учёт смены прайслистов внутри периода)
            decimal amount = CalculateCampaignTariffPrice(
                stationId: massmediaId,
                segmentsTable: SegmentsTable,
                durationSec: roller,
                primeWdTotal: primeWdTotal,
                nonPrimeWdTotal: nonPrimeWdTotal,
                primeWeTotal: primeWeTotal,
                nonPrimeWeTotal: nonPrimeWeTotal
            );

            drv["TotalAmount"] = amount;

            // строчная скидка (объёмная)
            decimal companyDiscount = GetCompanyDiscount(massmediaId, _startDate, amount);
            drv["CompanyDiscount"] = companyDiscount;

            // итоги
            drv["TotalWithDiscount"] = amount * companyDiscount;
            drv["TotalWithManagerDiscount"] = amount * companyDiscount * _managerDiscount;
        }

        private decimal CalculateCampaignTariffPrice(
    int stationId,
    DataTable segmentsTable,
    int durationSec,
    int primeWdTotal,
    int nonPrimeWdTotal,
    int primeWeTotal,
    int nonPrimeWeTotal)
        {
            if (segmentsTable == null || _selectedDates == null || _selectedDates.Count == 0 || durationSec <= 0)
                return 0m;

            // Разбиваем выбранные даты на будни/выходные
            var wdDates = new List<DateTime>(_selectedDates.Count);
            var weDates = new List<DateTime>(_selectedDates.Count);

            foreach (var d in _selectedDates)
            {
                var day = d.Date;
                var dow = day.DayOfWeek;
                if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) weDates.Add(day);
                else wdDates.Add(day);
            }

            // Если нет таких дней — их totals не участвуют в расчёте
            int wdTotalDays = wdDates.Count;
            int weTotalDays = weDates.Count;

            if (wdTotalDays == 0) { primeWdTotal = 0; nonPrimeWdTotal = 0; }
            if (weTotalDays == 0) { primeWeTotal = 0; nonPrimeWeTotal = 0; }

            decimal total = 0m;

            // Сегменты прайслистов по станции
            var rows = segmentsTable.Select($"massmediaID = {stationId}", "SegmentStart ASC");

            foreach (var r in rows)
            {
                var segStart = Convert.ToDateTime(r["SegmentStart"]).Date;
                var segEnd = Convert.ToDateTime(r["SegmentEnd"]).Date;

                int wdInSeg = CountDatesInRange(wdDates, segStart, segEnd);
                int weInSeg = CountDatesInRange(weDates, segStart, segEnd);
                if (wdInSeg == 0 && weInSeg == 0) continue;

                // Доля дней сегмента от всех выбранных дней (по будням/выходным отдельно)
                decimal wdShare = (wdTotalDays > 0) ? (decimal)wdInSeg / wdTotalDays : 0m;
                decimal weShare = (weTotalDays > 0) ? (decimal)weInSeg / weTotalDays : 0m;

                // Сколько выходов "попадает" в сегмент (может быть дробным)
                decimal primeWdInSeg = primeWdTotal * wdShare;
                decimal nonPrimeWdInSeg = nonPrimeWdTotal * wdShare;
                decimal primeWeInSeg = primeWeTotal * weShare;
                decimal nonPrimeWeInSeg = nonPrimeWeTotal * weShare;

                decimal primeWD = Convert.ToDecimal(r["PrimePricePerSecWeekday"]);
                decimal nonPrimeWD = Convert.ToDecimal(r["NonPrimePricePerSecWeekday"]);
                decimal primeWE = Convert.ToDecimal(r["PrimePricePerSecWeekend"]);
                decimal nonPrimeWE = Convert.ToDecimal(r["NonPrimePricePerSecWeekend"]);

                decimal segAmount =
                    (primeWD * primeWdInSeg +
                     nonPrimeWD * nonPrimeWdInSeg +
                     primeWE * primeWeInSeg +
                     nonPrimeWE * nonPrimeWeInSeg)
                    * durationSec;

                total += segAmount;
            }

            return total;
        }

        public void LoadData(int massmediaGroupId, DateTime dateFrom, DateTime dateTo)
        {
            var p = DataAccessor.CreateParametersDictionary();
            // Имена ключей ДОЛЖНЫ совпадать с параметрами процедуры (без @)
            if (massmediaGroupId > 0)
                p["MassmediaGroupID"] = massmediaGroupId;
            p["DateFrom"] = dateFrom.Date;
            p["DateTo"] = dateTo.Date;

            DataSet ds = DataAccessor.LoadDataSet("pc_GetStationsPriceModel", p);

            SummaryTable = ds.Tables.Count > 0 ? ds.Tables[0] : null;
            SegmentsTable = ds.Tables.Count > 1 ? ds.Tables[1] : null;

            EnsureCalcColumns(SummaryTable);
            //dgvStations.DataSource = SummaryTable;
            _bindingSource.DataSource = SummaryTable; // Ваш DataTable
            dgvStations.DataSource = _bindingSource;
        }

        private static void EnsureCalcColumns(DataTable dt)
        {
            if (dt == null) return;

            if (!dt.Columns.Contains("PrimeTotalSpotsWeekday"))
                dt.Columns.Add("PrimeTotalSpotsWeekday", typeof(int));

            if (!dt.Columns.Contains("NonPrimeTotalSpotsWeekday"))
                dt.Columns.Add("NonPrimeTotalSpotsWeekday", typeof(int));

            if (!dt.Columns.Contains("PrimeTotalSpotsWeekend"))
                dt.Columns.Add("PrimeTotalSpotsWeekend", typeof(int));

            if (!dt.Columns.Contains("NonPrimeTotalSpotsWeekend"))
                dt.Columns.Add("NonPrimeTotalSpotsWeekend", typeof(int));

            if (!dt.Columns.Contains("TotalAmount"))
                dt.Columns.Add("TotalAmount", typeof(decimal));

            if (!dt.Columns.Contains("CompanyDiscount"))
                dt.Columns.Add("CompanyDiscount", typeof(decimal));

            if (!dt.Columns.Contains("TotalWithDiscount"))
                dt.Columns.Add("TotalWithDiscount", typeof(decimal));

            if (!dt.Columns.Contains("TotalWithManagerDiscount"))
                dt.Columns.Add("TotalWithManagerDiscount", typeof(decimal));

            if (!dt.Columns.Contains("RollerDuration"))
                dt.Columns.Add("RollerDuration", typeof(int));

            if (!dt.Columns.Contains("IsSelected"))
            {
                dt.Columns.Add("IsSelected", typeof(bool));
                foreach (DataRow r in dt.Rows)
                    r["IsSelected"] = false; 
            }
        }

        public List<DataRowView> GetSelectedRadiostations()
        {
            var rows = new List<DataRowView>();
            foreach (DataGridViewRow row in dgvStations.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["colSelected"].Value ?? false);
                if (!isSelected) continue;

                var drv = row.DataBoundItem as DataRowView;

                rows.Add(drv);
            }
            return rows;
        }

        public void ApplyCalculation(
            List<DateTime> selectedDates,
            int durationSec,
            int primePerDayWeekday,
            int nonPrimePerDayWeekday,
            int primePerDayWeekend,
            int nonPrimePerDayWeekend,
            decimal managerDiscount)
        {
            if (SummaryTable == null) return;

            _suppressRecalc = true;

            _selectedDates = selectedDates;
            UpdateWeekdayWeekendColumnsEnabled();

            _startDate = _selectedDates != null && _selectedDates.Count > 0
                ? _selectedDates[0].Date
                : DateTime.Today;

            _managerDiscount = managerDiscount;

            int wdCount = 0, weCount = 0;
            if (_selectedDates != null)
            {
                foreach (var d in _selectedDates)
                {
                    var dow = d.DayOfWeek;
                    if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) weCount++;
                    else wdCount++;
                }
            }

            int primeTotalWd = wdCount * primePerDayWeekday;
            int nonPrimeTotalWd = wdCount * nonPrimePerDayWeekday;

            int primeTotalWe = weCount * primePerDayWeekend;
            int nonPrimeTotalWe = weCount * nonPrimePerDayWeekend;

            foreach (DataRow row in SummaryTable.Rows)
            {
                row["PrimeTotalSpotsWeekday"] = primeTotalWd;
                row["NonPrimeTotalSpotsWeekday"] = nonPrimeTotalWd;
                row["PrimeTotalSpotsWeekend"] = primeTotalWe;
                row["NonPrimeTotalSpotsWeekend"] = nonPrimeTotalWe;

                row["RollerDuration"] = durationSec;

                int massmediaId = Convert.ToInt32(row["massmediaID"]);

                decimal amount = CalculateCampaignTariffPrice(
                    stationId: massmediaId,
                    segmentsTable: SegmentsTable,
                    durationSec: durationSec,
                    primeWdTotal: primeTotalWd,
                    nonPrimeWdTotal: nonPrimeTotalWd,
                    primeWeTotal: primeTotalWe,
                    nonPrimeWeTotal: nonPrimeTotalWe
                );

                row["TotalAmount"] = amount;

                decimal discountValue = GetCompanyDiscount(
                    massmediaId: massmediaId,
                    startDate: _startDate,
                    tariffPrice: amount
                );

                row["CompanyDiscount"] = discountValue;
                row["TotalWithDiscount"] = amount * discountValue;
                row["TotalWithManagerDiscount"] = amount * discountValue * managerDiscount;
            }

            _suppressRecalc = false;

            foreach (DataGridViewRow row in dgvStations.Rows)
                row.Cells["colSelected"].Value = false;
        }

        private static decimal GetCompanyDiscount(int massmediaId, DateTime startDate, decimal tariffPrice)
        {
            var prms = DataAccessor.CreateParametersDictionary();

            prms["massMediaID"] = (short)massmediaId;
            prms["campaignTypeID"] = (byte)1;
            prms["startDate"] = startDate;
            prms["tariffPrice"] = tariffPrice;

            // OUT
            prms["discountValue"] = null;

            DataAccessor.ExecuteNonQuery("hlp_CompanyDiscountCalculate", prms, 30, false);

            object val = prms.ContainsKey("discountValue") ? prms["discountValue"] : null;
            if (val == null || val == DBNull.Value) return 1m;

            // discountValue в SQL float, прилетит как double чаще всего
            return Convert.ToDecimal(val);
        }

        // Эти 3 метода можно вынести в общий util, но для простоты держим тут
        private static int CountDatesInRange(List<DateTime> dates, DateTime start, DateTime end)
        {
            int lo = LowerBound(dates, start);
            int hi = UpperBound(dates, end);
            return Math.Max(0, hi - lo);
        }

        private static int LowerBound(List<DateTime> a, DateTime x)
        {
            int l = 0, r = a.Count;
            while (l < r)
            {
                int m = (l + r) / 2;
                if (a[m] < x) l = m + 1; else r = m;
            }
            return l;
        }

        private static int UpperBound(List<DateTime> a, DateTime x)
        {
            int l = 0, r = a.Count;
            while (l < r)
            {
                int m = (l + r) / 2;
                if (a[m] <= x) l = m + 1; else r = m;
            }
            return l;
        }

        public decimal GetSelectedTotalWithManagerDiscount()
        {
            decimal sum = 0m;

            foreach (DataGridViewRow row in dgvStations.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["colSelected"].Value ?? false);
                if (!isSelected) continue;

                var drv = row.DataBoundItem as DataRowView;
                if (drv == null) continue;

                object v = drv["TotalWithManagerDiscount"];
                if (v == DBNull.Value) continue;

                sum += Convert.ToDecimal(v);
            }

            return sum;
        }

        private static int SafeInt(object value, int min, int max)
        {
            if (value == null || value == DBNull.Value)
                return min;

            if (!int.TryParse(value.ToString(), out int result))
                return min;

            if (result < min) return min;
            if (result > max) return max;

            return result;
        }

        private void UpdateWeekdayWeekendColumnsEnabled()
        {
            bool hasWeekdays = false;
            bool hasWeekends = false;

            if (_selectedDates != null)
            {
                foreach (var d in _selectedDates)
                {
                    var dow = d.DayOfWeek;
                    if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday)
                        hasWeekends = true;
                    else
                        hasWeekdays = true;
                }
            }

            // Будни
            SetColumnEditable("colPrimeTotalSpotsWeekday", hasWeekdays);
            SetColumnEditable("colNonPrimeTotalSpotsWeekday", hasWeekdays);

            // Выходные
            SetColumnEditable("colPrimeTotalSpotsWeekend", hasWeekends);
            SetColumnEditable("colNonPrimeTotalSpotsWeekend", hasWeekends);
        }

        private void SetColumnEditable(string columnName, bool enabled)
        {
            var col = dgvStations.Columns[columnName];
            if (col == null) return;

            col.ReadOnly = !enabled;

            // визуально приглушаем
            col.DefaultCellStyle.BackColor =
                enabled ? System.Drawing.Color.White : System.Drawing.SystemColors.Control;
        }

        public void SetManagerDiscount(decimal managerDiscount)
        {
            if (SummaryTable == null) return;

            // нормализуем
            if (managerDiscount <= 0m) managerDiscount = 1m;

            _managerDiscount = managerDiscount;

            _suppressRecalc = true;
            try
            {
                foreach (DataRow row in SummaryTable.Rows)
                {
                    // базовая сумма после "строчной" скидки уже посчитана
                    decimal totalWithDiscount = 0m;

                    object v = row["TotalWithDiscount"];
                    if (v != null && v != DBNull.Value)
                        totalWithDiscount = Convert.ToDecimal(v);

                    row["TotalWithManagerDiscount"] = totalWithDiscount * _managerDiscount;
                }
            }
            finally
            {
                _suppressRecalc = false;
            }

            SummaryUpdater?.Invoke();
        }
    }
}