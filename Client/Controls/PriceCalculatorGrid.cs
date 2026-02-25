using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Controls
{
    public partial class PriceCalculatorGrid : UserControl
    {
        public enum PriceCalculatorColumn
        {
            PrimeTotalSpotsWeekday,
            NonPrimeTotalSpotsWeekday,
            PrimeTotalSpotsWeekend,
            NonPrimeTotalSpotsWeekend,
            TotalAmount,
            CompanyDiscount,
            TotalWithDiscount,
            RollerDuration,
            ManagerDiscount,
            IsSelected,
            PackageDiscount,
            TotalAfterPackage,
            Position,
            TotalBeforePackage,
        }

        private static string ColumnName(PriceCalculatorColumn column) => column.ToString();
        private const string ExtraChargeFirstRollerColumn = "extraChargeFirstRoller";
        private const string ExtraChargeSecondRollerColumn = "extraChargeSecondRoller";
        private const string ExtraChargeLastRollerColumn = "extraChargeLastRoller";

        private List<DateTime> _selectedDates;
        private DateTime _startDate;
        private decimal _managerDiscount = 1m;
        private bool _suppressRecalc;
        private CheckBox _headerCheckBox;
        private bool _bulkUpdating;
        private bool _sortFlag;
        private readonly BindingSource _bindingSource = new BindingSource();
        private object _lastSelectedItem = null;
        private string _lastCurrentColumnName;
        private DataGridViewCell _pendingTargetCell; // <--- Здесь
        private int _lastDataColumnIndex = -1; // колонка для удержания фокуса при стрелках
        private bool _skipCellEndEdit;
        private readonly Dictionary<string, string> _defaultColumnHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public bool UseManagerDiscountPeriods { get; set; }  // переключатель метода расчёта
        public System.Action SummaryUpdater { get; set; }
        public DataTable SegmentsTable { get; private set; }   // второй рекордсет (сегменты)
        public DataTable SummaryTable { get; private set; }   // первый рекордсет (для грида)
        public DataTable ManagerDiscountTable { get; private set; }   // третий рекордсет (периоды maxRatio)

        public PriceCalculatorGrid()
        {
            InitializeComponent();
            UseManagerDiscountPeriods = true;
            ConfigureGrid();
        }

        /// <summary>
        /// 
        /// </summary>
        private void ConfigureGrid()
        {
            dgvStations.AutoGenerateColumns = false;
            dgvStations.AllowUserToAddRows = false;
            dgvStations.AllowUserToDeleteRows = false;
            dgvStations.MultiSelect = false;

            dgvStations.RowHeadersVisible = false;

            // было:
            // dgvStations.SelectionMode = DataGridViewSelectionMode.RowHeaderSelect;

            // ✅ стало: нормальное поведение фокуса при редактировании
            dgvStations.SelectionMode = DataGridViewSelectionMode.CellSelect;

            dgvStations.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgvStations.EditingControlShowing += DgvStations_EditingControlShowing;
            dgvStations.DataError += (s, e) => { e.ThrowException = false; };

            dgvStations.CellValidating += DgvStations_CellValidating;

            // Коммитим сразу только checkbox/combobox, текстовые не трогаем
            dgvStations.CurrentCellDirtyStateChanged += (s, e) =>
            {
                if (!dgvStations.IsCurrentCellDirty) return;
                var cell = dgvStations.CurrentCell;
                if (cell is DataGridViewCheckBoxCell || cell is DataGridViewComboBoxCell)
                    dgvStations.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            // ✅ ЕДИНАЯ точка пересчета для всех редактируемых колонок
            dgvStations.CellEndEdit += (s, e) =>
            {
                if (_suppressRecalc) return;
                if (_skipCellEndEdit) return;

                BeginInvoke((System.Action)(() =>
                {
                    if (_suppressRecalc) return;
                    if (_skipCellEndEdit) return;
                    HandleEditableCellChanged(e.RowIndex, e.ColumnIndex);
                    //RestorePendingTargetCell();
                }));
            };

            dgvStations.CellValueChanged += (s, e) =>
            {
                if (_suppressRecalc) return;
                if (e.RowIndex < 0) return;

                string colName = dgvStations.Columns[e.ColumnIndex].Name;

                // чекбокс — сразу обновляем summary
                if (colName == "colSelected")
                {
                    RunWithWaitCursor(() =>
                    {
                        SummaryUpdater?.Invoke();
                    });
                }
            };

            dgvStations.MouseDown += (s, e) =>
            {
                var hitInfo = dgvStations.HitTest(e.X, e.Y);

                if (hitInfo.Type == DataGridViewHitTestType.Cell &&
                    hitInfo.RowIndex >= 0 && hitInfo.ColumnIndex >= 0)
                {
                    _pendingTargetCell = dgvStations
                        .Rows[hitInfo.RowIndex]
                        .Cells[hitInfo.ColumnIndex];
                }
                else if (hitInfo.Type == DataGridViewHitTestType.ColumnHeader)
                {
                    if (dgvStations.CurrentRow != null)
                    {
                        _lastSelectedItem = dgvStations.CurrentRow.DataBoundItem;
                        _sortFlag = true;
                    }
                }
            };

            // ✅ запоминаем целевую ячейку и по MouseDown, и по MouseUp
            dgvStations.CellMouseDown += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    _pendingTargetCell = dgvStations.Rows[e.RowIndex].Cells[e.ColumnIndex];
            };
            dgvStations.CellMouseUp += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
                    _pendingTargetCell = dgvStations.Rows[e.RowIndex].Cells[e.ColumnIndex];
            };

            dgvStations.SelectionChanged += (s, e) =>
            {
                if (_sortFlag) return;

                var cell = dgvStations.CurrentCell;
                var colName = cell?.OwningColumn?.Name;
                if (!string.IsNullOrEmpty(colName) && colName != "colSelected")
                {
                    _lastCurrentColumnName = colName;
                    _lastDataColumnIndex = cell.ColumnIndex;
                }

                if (dgvStations.CurrentRow != null)
                    _lastSelectedItem = dgvStations.CurrentRow.DataBoundItem;
            };

            // ✅ восстановленный обработчик сортировки
            dgvStations.Sorted += (s, e) =>
            {
                if (!_sortFlag) return;              // только если сами кликнули по заголовку
                if (_lastSelectedItem == null) { _sortFlag = false; return; }

                int newIndex = _bindingSource.IndexOf(_lastSelectedItem);

                try
                {
                    if (newIndex >= 0)
                    {
                        dgvStations.ClearSelection();
                        dgvStations.Rows[newIndex].Selected = true;

                        int colIndex = 0;
                        if (_pendingTargetCell != null && _pendingTargetCell.RowIndex == newIndex)
                            colIndex = _pendingTargetCell.ColumnIndex;
                        else if (!string.IsNullOrEmpty(_lastCurrentColumnName) &&
                                 dgvStations.Columns.Contains(_lastCurrentColumnName))
                            colIndex = dgvStations.Columns[_lastCurrentColumnName].Index;

                        dgvStations.CurrentCell = dgvStations.Rows[newIndex].Cells[colIndex];
                        dgvStations.FirstDisplayedScrollingRowIndex = newIndex;
                    }
                }
                finally
                {
                    _pendingTargetCell = null;
                    _sortFlag = false;
                }
            };

            dgvStations.Columns.Clear();

            dgvStations.Columns.Add(new DataGridViewCheckBoxColumn
            {
                Name = "colSelected",
                HeaderText = "",
                Width = 24,
                DataPropertyName = ColumnName(PriceCalculatorColumn.IsSelected),
                TrueValue = true,
                FalseValue = false,
                SortMode = DataGridViewColumnSortMode.Automatic
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colName",
                HeaderText = "Радиостанция",
                DataPropertyName = "name",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                //MinimumWidth = 160,
                ReadOnly = true,
            });

            const int NumericColWidth = 80;

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colRollerDuration",
                HeaderText = "Пр-ть ролика",
                DataPropertyName = ColumnName(PriceCalculatorColumn.RollerDuration),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight }
            });

            dgvStations.Columns.Add(new DataGridViewComboBoxColumn
            {
                Name = "colPosition",
                HeaderText = "Позиция",
                DataPropertyName = ColumnName(PriceCalculatorColumn.Position),
                Width = NumericColWidth,
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                FlatStyle = FlatStyle.Flat,
                ValueType = typeof(int),
                ValueMember = "Key",
                DisplayMember = "Value",
                DataSource = Issue.GetRollerPositionItems()
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrimeWeekday",
                HeaderText = "Цена будни прайм",
                DataPropertyName = "PrimePricePerSecWeekday",
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNonPrimeWeekday",
                HeaderText = "Цена будни не прайм",
                DataPropertyName = "NonPrimePricePerSecWeekday",
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrimeWeekend",
                HeaderText = "Цена выходные прайм",
                DataPropertyName = "PrimePricePerSecWeekend",
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNonPrimeWeekend",
                HeaderText = "Цена выходные не прайм",
                DataPropertyName = "NonPrimePricePerSecWeekend",
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrimeTotalSpotsWeekday",
                HeaderText = "Кол-во выходов будни прайм",
                DataPropertyName = ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekday),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, NullValue = "0" }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNonPrimeTotalSpotsWeekday",
                HeaderText = "Кол-во выходов будни не прайм",
                DataPropertyName = ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekday),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, NullValue = "0" }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPrimeTotalSpotsWeekend",
                HeaderText = "Кол-во выходов выходные прайм",
                DataPropertyName = ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekend),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, NullValue = "0" }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colNonPrimeTotalSpotsWeekend",
                HeaderText = "Кол-во выходов выходные не прайм",
                DataPropertyName = ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekend),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleRight, NullValue = "0" }
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTotalAmount",
                HeaderText = "Цена кампании",
                DataPropertyName = ColumnName(PriceCalculatorColumn.TotalAmount),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colCompanyDiscount",
                HeaderText = "Объёмная скидка",
                DataPropertyName = ColumnName(PriceCalculatorColumn.CompanyDiscount),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colSeasonCoeff",
                HeaderText = "Сезонный коэфф.",
                DataPropertyName = ColumnName(PriceCalculatorColumn.ManagerDiscount),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTotalBeforePackage",
                HeaderText = "Цена до пакетной скидки",
                DataPropertyName = ColumnName(PriceCalculatorColumn.TotalBeforePackage),
                Width = NumericColWidth + 30,
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colPackageDiscount",
                HeaderText = "Пакетная скидка",
                DataPropertyName = ColumnName(PriceCalculatorColumn.PackageDiscount),
                Width = NumericColWidth,
                DefaultCellStyle = { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });

            dgvStations.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "colTotalAfterPackage",
                HeaderText = "Итог",
                DataPropertyName = ColumnName(PriceCalculatorColumn.TotalAfterPackage),
                Width = NumericColWidth + 50,
                DefaultCellStyle = { Format = "c", Alignment = DataGridViewContentAlignment.MiddleRight },
                ReadOnly = true
            });
            ApplyEditableColumnsBaseStyle();
            CaptureDefaultColumnHeaders();
            InitHeaderCheckbox();

            dgvStations.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Up && e.KeyCode != Keys.Down) return;
                if (dgvStations.IsCurrentCellInEditMode) return; // в режиме редактирования обрабатывает EditingControl
                e.Handled = true;
                e.SuppressKeyPress = true;
                MoveVertical(e.KeyCode == Keys.Down ? 1 : -1);
            };

            dgvStations.CellPainting += DgvStations_CellPainting;

            // подсветим редактируемые колонки сразу
            foreach (DataGridViewColumn c in dgvStations.Columns)
                if (EditableColumns.Contains(c.Name))
                    c.DefaultCellStyle.BackColor = Color.LightYellow;
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

                // cerrar edición current
                SuppressRecalc(() =>
                {
                    dgvStations.EndEdit();
                    dgvStations.CommitEdit(DataGridViewDataErrorContexts.Commit);
                });
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

                RunWithWaitCursor(() => SummaryUpdater?.Invoke());
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

        private void ApplyEditableColumnsBaseStyle()
        {
            foreach (DataGridViewColumn c in dgvStations.Columns)
            {
                if (EditableColumns.Contains(c.Name))
                    c.DefaultCellStyle.BackColor = Color.LightYellow;
            }
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
            string colName = dgvStations.CurrentCell?.OwningColumn?.Name;

            // общий хендлер стрелок в режиме редактирования
            e.Control.KeyDown -= EditingControl_KeyDown;
            e.Control.KeyDown += EditingControl_KeyDown;

            // 1) числовые колонки
            if (colName == "colRollerDuration" ||
                colName == "colPrimeTotalSpotsWeekday" ||
                colName == "colNonPrimeTotalSpotsWeekday" ||
                colName == "colPrimeTotalSpotsWeekend" ||
                colName == "colNonPrimeTotalSpotsWeekend")
            {
                if (e.Control is TextBox tb)
                {
                    tb.KeyPress -= NumericTextBox_KeyPress;
                    tb.KeyPress += NumericTextBox_KeyPress;
                }
                return;
            }

            // 2) позиция
            if (colName == "colPosition" && e.Control is ComboBox cb)
            {
                cb.SelectionChangeCommitted -= PositionCombo_SelectionChangeCommitted;
                cb.SelectionChangeCommitted += PositionCombo_SelectionChangeCommitted;
                return;
            }
        }

        private void EditingControl_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Up && e.KeyCode != Keys.Down) return;
            e.Handled = true;
            e.SuppressKeyPress = true;
            MoveVertical(e.KeyCode == Keys.Down ? 1 : -1);
        }

        private void MoveVertical(int delta)
        {
            var cell = dgvStations.CurrentCell;
            if (cell == null) return;

            int targetRow = cell.RowIndex + delta;
            if (targetRow < 0 || targetRow >= dgvStations.Rows.Count) return;

            int col = GetPreferredDataColumnIndex(cell);
            if (col < 0) return;

            // зафиксировать введённое значение перед переходом
            SuppressRecalc(() =>
            {
                dgvStations.EndEdit();
                _bindingSource.EndEdit();
            });

            try
            {
                dgvStations.CurrentCell = dgvStations.Rows[targetRow].Cells[col];
            }
            catch { }

            var curName = dgvStations.CurrentCell?.OwningColumn?.Name;
            if (!string.IsNullOrEmpty(curName) && curName != "colSelected")
            {
                _lastCurrentColumnName = curName;
                _lastDataColumnIndex = dgvStations.CurrentCell.ColumnIndex;
            }
        }

        private int GetPreferredDataColumnIndex(DataGridViewCell currentCell)
        {
            // если не чекбокс — остаёмся в той же колонке
            if (currentCell?.OwningColumn?.Name != "colSelected")
                return currentCell.ColumnIndex;

            // если чекбокс — берём последнюю нормальную колонку
            if (_lastDataColumnIndex >= 0 &&
                _lastDataColumnIndex < dgvStations.Columns.Count &&
                dgvStations.Columns[_lastDataColumnIndex].Name != "colSelected")
                return _lastDataColumnIndex;

            // fallback: первая не-чекбоксная колонка
            for (int i = 0; i < dgvStations.Columns.Count; i++)
                if (dgvStations.Columns[i].Name != "colSelected")
                    return i;

            return -1;
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

        private static readonly HashSet<string> EditableColumns = new HashSet<string>
        {
            "colRollerDuration",
            "colPrimeTotalSpotsWeekday",
            "colNonPrimeTotalSpotsWeekday",
            "colPrimeTotalSpotsWeekend",
            "colNonPrimeTotalSpotsWeekend",
            "colPosition"
        };

        private void HandleEditableCellChanged(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0) return;

            var desiredCell = dgvStations.Rows[rowIndex].Cells[columnIndex];
            string colName = dgvStations.Columns[columnIndex].Name;

            // При слитых колонках больше не дублируем значение в выходные — храним только введённое

            if (EditableColumns.Contains(colName))
            {
                RecalcRow(rowIndex);
                if (RowIsSelected(rowIndex))
                    //RequestSummaryUpdate();
                    SummaryUpdater?.Invoke();

                // processed; drop it so it won't affect next edit
                _pendingTargetCell = null;
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
            int roller = SafeInt(drv[ColumnName(PriceCalculatorColumn.RollerDuration)], 0, 3600);

            int primeWdTotal = SafeInt(drv[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekday)], 0, 1_000_000);
            int nonPrimeWdTotal = SafeInt(drv[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekday)], 0, 1_000_000);

            int primeWeTotal = SafeInt(drv[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekend)], 0, 1_000_000);
            int nonPrimeWeTotal = SafeInt(drv[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekend)], 0, 1_000_000);

            int position = NormalizePosition(drv[ColumnName(PriceCalculatorColumn.Position)]);

            // обратно нормализованные значения (если пользователь ввёл мусор)
            _suppressRecalc = true;
            try
            {
                drv[ColumnName(PriceCalculatorColumn.RollerDuration)] = roller;

                drv[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekday)] = primeWdTotal;
                drv[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekday)] = nonPrimeWdTotal;

                drv[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekend)] = primeWeTotal;
                drv[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekend)] = nonPrimeWeTotal;

                drv[ColumnName(PriceCalculatorColumn.Position)] = position;
            }
            finally
            {
                _suppressRecalc = false;
            }

            decimal baseAmount;
            decimal amountWithManager;
            decimal managerRatio;

            if (UseManagerDiscountPeriods)
            {
                var managerPeriods = GetManagerDiscountPeriods();
                CalculateCampaignTotalsWithManagerDiscount(
                    stationId: massmediaId,
                    segmentsTable: SegmentsTable,
                    managerPeriods: managerPeriods,
                    durationSec: roller,
                    primeWdTotal: primeWdTotal,
                    nonPrimeWdTotal: nonPrimeWdTotal,
                    primeWeTotal: primeWeTotal,
                    nonPrimeWeTotal: nonPrimeWeTotal,
                    position: position,
                    baseTotal: out baseAmount,
                    totalWithManager: out amountWithManager,
                    managerAvgRatio: out managerRatio);
            }
            else
            {
                baseAmount = CalculateCampaignTariffPrice(
                    stationId: massmediaId,
                    segmentsTable: SegmentsTable,
                    durationSec: roller,
                    primeWdTotal: primeWdTotal,
                    nonPrimeWdTotal: nonPrimeWdTotal,
                    primeWeTotal: primeWeTotal,
                    nonPrimeWeTotal: nonPrimeWeTotal,
                    position: position);

                managerRatio = _managerDiscount;
                amountWithManager = baseAmount * managerRatio;
            }

            drv[ColumnName(PriceCalculatorColumn.TotalAmount)] = baseAmount;

            // строчная скидка (объёмная)
            decimal companyDiscount = GetCompanyDiscount(massmediaId, _startDate, baseAmount);
            drv[ColumnName(PriceCalculatorColumn.CompanyDiscount)] = companyDiscount;

            // итоги
            decimal totalWithDiscount = baseAmount * companyDiscount;
            drv[ColumnName(PriceCalculatorColumn.TotalWithDiscount)] = totalWithDiscount;

            decimal totalBeforePackage = amountWithManager * companyDiscount;
            drv[ColumnName(PriceCalculatorColumn.ManagerDiscount)] = managerRatio;
            drv[ColumnName(PriceCalculatorColumn.TotalBeforePackage)] = totalBeforePackage;

            UpdatePackageTotal(drv.Row);
        }

        private decimal CalculateCampaignTariffPrice(
            int stationId,
            DataTable segmentsTable,
            int durationSec,
            int primeWdTotal,
            int nonPrimeWdTotal,
            int primeWeTotal,
            int nonPrimeWeTotal,
            int position)
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
                decimal nonPrimeWD = Convert.ToDecimal(r["NonPrimePricePerSecWeekend"]);
                decimal primeWE = Convert.ToDecimal(r["PrimePricePerSecWeekend"]);
                decimal nonPrimeWE = Convert.ToDecimal(r["NonPrimePricePerSecWeekend"]);

                decimal segAmount =
                    (primeWD * primeWdInSeg +
                     nonPrimeWD * nonPrimeWdInSeg +
                     primeWE * primeWeInSeg +
                     nonPrimeWE * nonPrimeWeInSeg)
                    * durationSec;

                // extraCharge: проценты наценки (увеличиваем цену)
                decimal extraPercent = GetExtraChargePercent(r, position);
                if (extraPercent != 0m)
                    segAmount *= (1m + extraPercent / 100m);

                total += segAmount;
            }

            return total;
        }

        private static int NormalizePosition(object value)
        {
            if (value == null || value == DBNull.Value)
                return (int)Merlin.RollerPositions.Undefined;

            try
            {
                return Convert.ToInt32(value);
            }
            catch
            {
                return (int)Merlin.RollerPositions.Undefined;
            }
        }

        private static decimal GetExtraChargePercent(DataRow segmentRow, int position)
        {
            if (segmentRow == null) return 0m;

            if (position == (int)Merlin.RollerPositions.First)
                return SafeDecimal(segmentRow, ExtraChargeFirstRollerColumn);
            if (position == (int)Merlin.RollerPositions.Second)
                return SafeDecimal(segmentRow, ExtraChargeSecondRollerColumn);
            if (position == (int)Merlin.RollerPositions.Last)
                return SafeDecimal(segmentRow, ExtraChargeLastRollerColumn);

            return 0m; // Undefined
        }

        private static decimal SafeDecimal(DataRow row, string columnName)
        {
            if (row?.Table == null || !row.Table.Columns.Contains(columnName)) return 0m;

            object v = row[columnName];
            if (v == null || v == DBNull.Value) return 0m;

            return Convert.ToDecimal(v);
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

        public void LoadData(int massmediaGroupId, DateTime dateFrom, DateTime dateTo)
        {
            // 0) Закоммитить возможное редактирование (особенно чекбокс)
            dgvStations?.EndEdit();
            _bindingSource?.EndEdit();

            // 1) Сохраняем выбранные MassmediaID из текущей SummaryTable
            var selectedIds = CaptureSelectedIds(SummaryTable);

            var p = DataAccessor.CreateParametersDictionary();
            if (massmediaGroupId > 0)
                p["MassmediaGroupID"] = massmediaGroupId;
            p["DateFrom"] = dateFrom.Date;
            p["DateTo"] = dateTo.Date;

            DataSet ds = DataAccessor.LoadDataSet("pc_GetStationsPriceModel", p);

            SummaryTable = ds.Tables.Count > 0 ? ds.Tables[0] : null;
            SegmentsTable = ds.Tables.Count > 1 ? ds.Tables[1] : null;
            ManagerDiscountTable = ds.Tables.Count > 2 ? ds.Tables[2] : null;

            EnsureCalcColumns(SummaryTable);

            // 2) Восстанавливаем чекбоксы в новой SummaryTable
            RestoreSelectedIds(SummaryTable, selectedIds);

            _bindingSource.DataSource = SummaryTable;
            dgvStations.DataSource = _bindingSource;
            ApplyPriceColumnLayout();

            // 3) Хедер-чекбокс: теперь можно ставить не всегда false, а по состоянию таблицы
            if (_headerCheckBox != null)
            {
                _bulkUpdating = true;
                try
                {
                    string isSelectedColumn = ColumnName(PriceCalculatorColumn.IsSelected);
                    _headerCheckBox.Checked = SummaryTable != null
                        && SummaryTable.Rows.Count > 0
                        && SummaryTable.AsEnumerable().All(r => r.Field<bool?>(isSelectedColumn) == true);
                }
                finally { _bulkUpdating = false; }
            }
        }

        private static void EnsureCalcColumns(DataTable dt)
        {
            if (dt == null) return;

            void EnsureColumn<T>(PriceCalculatorColumn column)
            {
                string name = ColumnName(column);
                if (!dt.Columns.Contains(name))
                    dt.Columns.Add(name, typeof(T));
            }

            EnsureColumn<int>(PriceCalculatorColumn.PrimeTotalSpotsWeekday);
            EnsureColumn<int>(PriceCalculatorColumn.NonPrimeTotalSpotsWeekday);
            EnsureColumn<int>(PriceCalculatorColumn.PrimeTotalSpotsWeekend);
            EnsureColumn<int>(PriceCalculatorColumn.NonPrimeTotalSpotsWeekend);
            EnsureColumn<decimal>(PriceCalculatorColumn.TotalAmount);
            EnsureColumn<decimal>(PriceCalculatorColumn.CompanyDiscount);
            EnsureColumn<decimal>(PriceCalculatorColumn.TotalWithDiscount);
            EnsureColumn<int>(PriceCalculatorColumn.RollerDuration);
            EnsureColumn<decimal>(PriceCalculatorColumn.ManagerDiscount);
            EnsureColumn<bool>(PriceCalculatorColumn.IsSelected);
            EnsureColumn<decimal>(PriceCalculatorColumn.PackageDiscount);
            EnsureColumn<decimal>(PriceCalculatorColumn.TotalAfterPackage);
            EnsureColumn<int>(PriceCalculatorColumn.Position);
            EnsureColumn<decimal>(PriceCalculatorColumn.TotalBeforePackage);
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

        public List<DateTime> GetSelectedDates()
        {
            return _selectedDates != null
                ? new List<DateTime>(_selectedDates)
                : new List<DateTime>();
        }

        public void ApplyCalculation(
            List<DateTime> selectedDates,
            int durationSec,
            int primePerDayWeekday,
            int nonPrimePerDayWeekday,
            int primePerDayWeekend,
            int nonPrimePerDayWeekend,
            decimal managerDiscount,
            bool managerDiscountModeSingle)
        {
            UseManagerDiscountPeriods = !managerDiscountModeSingle;
            if (UseManagerDiscountPeriods)
            {
                ApplyCalculationWithManagerDiscountPeriods(selectedDates, durationSec,
                    primePerDayWeekday, nonPrimePerDayWeekday,
                    primePerDayWeekend, nonPrimePerDayWeekend);
                return;
            }

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
                row[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekday)] = primeTotalWd;
                row[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekday)] = nonPrimeTotalWd;
                row[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekend)] = primeTotalWe;
                row[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekend)] = nonPrimeTotalWe; // <-- было nonPrimeTotalWd

                row[ColumnName(PriceCalculatorColumn.RollerDuration)] = durationSec;

                // важно: DataGridViewComboBoxColumn не любит DBNull, нормализуем
                if (row[ColumnName(PriceCalculatorColumn.Position)] == DBNull.Value)
                    row[ColumnName(PriceCalculatorColumn.Position)] = (int)Merlin.RollerPositions.Undefined;

                int position = NormalizePosition(row[ColumnName(PriceCalculatorColumn.Position)]);
                int massmediaId = Convert.ToInt32(row["massmediaID"]);

                decimal amount = CalculateCampaignTariffPrice(
                    stationId: massmediaId,
                    segmentsTable: SegmentsTable,
                    durationSec: durationSec,
                    primeWdTotal: primeTotalWd,
                    nonPrimeWdTotal: nonPrimeTotalWd,
                    primeWeTotal: primeTotalWe,
                    nonPrimeWeTotal: nonPrimeTotalWe,
                    position: position
                );

                row[ColumnName(PriceCalculatorColumn.TotalAmount)] = amount;

                decimal discountValue = GetCompanyDiscount(
                    massmediaId: massmediaId,
                    startDate: _startDate,
                    tariffPrice: amount
                );

                row[ColumnName(PriceCalculatorColumn.CompanyDiscount)] = discountValue;
                row[ColumnName(PriceCalculatorColumn.TotalWithDiscount)] = amount * discountValue;
                row[ColumnName(PriceCalculatorColumn.ManagerDiscount)] = _managerDiscount;
                decimal totalBeforePackage = amount * discountValue * _managerDiscount;
                row[ColumnName(PriceCalculatorColumn.TotalBeforePackage)] = totalBeforePackage;

                UpdatePackageTotal(row);
            }

            _suppressRecalc = false;

            ApplyPriceColumnLayout(); // повторно применяем лэйаут/слияния после пересчёта
            _bindingSource.ResetBindings(false);
            dgvStations.Refresh();
        }

        public void ApplyCalculationWithManagerDiscountPeriods(
            List<DateTime> selectedDates,
            int durationSec,
            int primePerDayWeekday,
            int nonPrimePerDayWeekday,
            int primePerDayWeekend,
            int nonPrimePerDayWeekend)
        {
            if (SummaryTable == null) return;

            _suppressRecalc = true;

            _selectedDates = selectedDates;
            UpdateWeekdayWeekendColumnsEnabled();

            _startDate = _selectedDates != null && _selectedDates.Count > 0
                ? _selectedDates[0].Date
                : DateTime.Today;

            var managerPeriods = GetManagerDiscountPeriods();

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
                row[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekday)] = primeTotalWd;
                row[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekday)] = nonPrimeTotalWd;
                row[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekend)] = primeTotalWe;
                row[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekend)] = nonPrimeTotalWe;
                row[ColumnName(PriceCalculatorColumn.RollerDuration)] = durationSec;

                if (row[ColumnName(PriceCalculatorColumn.Position)] == DBNull.Value)
                    row[ColumnName(PriceCalculatorColumn.Position)] = (int)Merlin.RollerPositions.Undefined;

                int position = NormalizePosition(row[ColumnName(PriceCalculatorColumn.Position)]);
                int massmediaId = Convert.ToInt32(row["massmediaID"]);

                CalculateCampaignTotalsWithManagerDiscount(
                    massmediaId,
                    SegmentsTable,
                    managerPeriods,
                    durationSec,
                    primeTotalWd,
                    nonPrimeTotalWd,
                    primeTotalWe,
                    nonPrimeTotalWe,
                    position,
                    out decimal baseAmount,
                    out decimal amountWithManager,
                    out decimal managerAvgRatio);

                row[ColumnName(PriceCalculatorColumn.TotalAmount)] = baseAmount;

                decimal discountValue = GetCompanyDiscount(
                    massmediaId,
                    _startDate,
                    baseAmount);

                row[ColumnName(PriceCalculatorColumn.CompanyDiscount)] = discountValue;
                row[ColumnName(PriceCalculatorColumn.TotalWithDiscount)] = baseAmount * discountValue;

                row[ColumnName(PriceCalculatorColumn.ManagerDiscount)] = managerAvgRatio; // фактически применённый средневзвешенный коэфф.
                decimal totalBeforePackage = amountWithManager * discountValue;
                row[ColumnName(PriceCalculatorColumn.TotalBeforePackage)] = totalBeforePackage;

                UpdatePackageTotal(row);
            }

            _suppressRecalc = false;

            ApplyPriceColumnLayout();
            _bindingSource.ResetBindings(false);
            dgvStations.Refresh();
        }

        public decimal ApplyPackageTotals(decimal packageDiscount)
        {
            if (SummaryTable == null) return 0;

            // зафиксировать редактирование перед массовым обновлением
            DoWithSkipCellEndEdit(() =>
            {
                SuppressRecalc(() =>
                {
                    dgvStations.EndEdit();
                    _bindingSource.EndEdit();
                });
            });

            _suppressRecalc = true;
            try
            {
                decimal totalAfterPackage = 0m;

                foreach (DataRow row in SummaryTable.Rows)
                {
                    var isSelectedValue = row[ColumnName(PriceCalculatorColumn.IsSelected)];
                    bool isSelected = isSelectedValue != DBNull.Value && Convert.ToBoolean(isSelectedValue);
                    if (isSelected)
                    {
                        row[ColumnName(PriceCalculatorColumn.PackageDiscount)] = packageDiscount;

                        decimal totalBeforePackage = 0m;
                        object v = row[ColumnName(PriceCalculatorColumn.TotalBeforePackage)];
                        if (v != null && v != DBNull.Value)
                            totalBeforePackage = Convert.ToDecimal(v);

                        totalAfterPackage += totalBeforePackage * packageDiscount;
                        row[ColumnName(PriceCalculatorColumn.TotalAfterPackage)] = totalBeforePackage * packageDiscount;
                    }
                    else
                    {
                        row[ColumnName(PriceCalculatorColumn.PackageDiscount)] = DBNull.Value;
                        row[ColumnName(PriceCalculatorColumn.TotalAfterPackage)] = DBNull.Value;
                    }
                }
                return totalAfterPackage;
            }
            finally
            {
                _suppressRecalc = false;
            }
        }

        private List<ManagerDiscountPeriod> GetManagerDiscountPeriods()
        {
            var result = new List<ManagerDiscountPeriod>();

            if (ManagerDiscountTable == null || ManagerDiscountTable.Rows.Count == 0)
            {
                result.Add(new ManagerDiscountPeriod(DateTime.MinValue, DateTime.MaxValue, 1m));
                return result;
            }

            foreach (DataRow row in ManagerDiscountTable.Rows)
            {
                DateTime start = Convert.ToDateTime(row["startDate"]).Date;
                DateTime finish = Convert.ToDateTime(row["finishDate"]).Date;
                decimal ratio = 1m;
                object v = row["maxRatio"];
                if (v != null && v != DBNull.Value)
                    ratio = Convert.ToDecimal(v);
                if (ratio <= 0m) ratio = 1m;

                result.Add(new ManagerDiscountPeriod(start, finish, ratio));
            }

            result.Sort((a, b) => a.Start.CompareTo(b.Start));
            return result;
        }

        private void CalculateCampaignTotalsWithManagerDiscount(
            int stationId,
            DataTable segmentsTable,
            List<ManagerDiscountPeriod> managerPeriods,
            int durationSec,
            int primeWdTotal,
            int nonPrimeWdTotal,
            int primeWeTotal,
            int nonPrimeWeTotal,
            int position,
            out decimal baseTotal,
            out decimal totalWithManager,
            out decimal managerAvgRatio)
        {
            baseTotal = 0m;
            totalWithManager = 0m;
            managerAvgRatio = 1m;

            if (segmentsTable == null || _selectedDates == null || _selectedDates.Count == 0 || durationSec <= 0)
                return;

            var wdDates = new List<DateTime>(_selectedDates.Count);
            var weDates = new List<DateTime>(_selectedDates.Count);
            foreach (var d in _selectedDates)
            {
                var day = d.Date;
                var dow = day.DayOfWeek;
                if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday) weDates.Add(day);
                else wdDates.Add(day);
            }

            int wdTotalDays = wdDates.Count;
            int weTotalDays = weDates.Count;

            if (wdTotalDays == 0) { primeWdTotal = 0; nonPrimeWdTotal = 0; }
            if (weTotalDays == 0) { primeWeTotal = 0; nonPrimeWeTotal = 0; }

            var rows = segmentsTable.Select("massmediaID = " + stationId, "SegmentStart ASC");
            foreach (DataRow r in rows)
            {
                DateTime segStart = Convert.ToDateTime(r["SegmentStart"]).Date;
                DateTime segEnd = Convert.ToDateTime(r["SegmentEnd"]).Date;

                foreach (var p in managerPeriods)
                {
                    DateTime partStart = segStart > p.Start ? segStart : p.Start;
                    DateTime partEnd = segEnd < p.End ? segEnd : p.End;
                    if (partStart > partEnd) continue;

                    int wdInPart = CountDatesInRange(wdDates, partStart, partEnd);
                    int weInPart = CountDatesInRange(weDates, partStart, partEnd);
                    if (wdInPart == 0 && weInPart == 0) continue;

                    decimal wdShare = wdTotalDays > 0 ? (decimal)wdInPart / wdTotalDays : 0m;
                    decimal weShare = weTotalDays > 0 ? (decimal)weInPart / weTotalDays : 0m;

                    decimal primeWdInPart = primeWdTotal * wdShare;
                    decimal nonPrimeWdInPart = nonPrimeWdTotal * wdShare;
                    decimal primeWeInPart = primeWeTotal * weShare;
                    decimal nonPrimeWeInPart = nonPrimeWeTotal * weShare;

                    decimal primeWD = Convert.ToDecimal(r["PrimePricePerSecWeekday"]);
                    decimal nonPrimeWD = Convert.ToDecimal(r["NonPrimePricePerSecWeekend"]);
                    decimal primeWE = Convert.ToDecimal(r["PrimePricePerSecWeekend"]);
                    decimal nonPrimeWE = Convert.ToDecimal(r["NonPrimePricePerSecWeekend"]);

                    decimal partAmount =
                        (primeWD * primeWdInPart +
                         nonPrimeWD * nonPrimeWdInPart +
                         primeWE * primeWeInPart +
                         nonPrimeWE * nonPrimeWeInPart)
                         * durationSec;

                    decimal extraPercent = GetExtraChargePercent(r, position);
                    if (extraPercent != 0m)
                        partAmount *= (1m + extraPercent / 100m);

                    baseTotal += partAmount;
                    totalWithManager += partAmount * p.Ratio;
                }
            }

            if (baseTotal > 0m)
                managerAvgRatio = totalWithManager / baseTotal;
        }

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

        public decimal GetSelectedTotalWithCampaignDiscount()
        {
            decimal sum = 0m;

            foreach (DataGridViewRow row in dgvStations.Rows)
            {
                bool isSelected = Convert.ToBoolean(row.Cells["colSelected"].Value ?? false);
                if (!isSelected) continue;

                if (!(row.DataBoundItem is DataRowView drv)) continue;

                object v = drv[ColumnName(PriceCalculatorColumn.TotalWithDiscount)];
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
            // сохраняем жёлтую подсветку для редактируемых, иначе — приглушаем
            col.DefaultCellStyle.BackColor = enabled
                ? (EditableColumns.Contains(columnName) ? Color.LightYellow : System.Drawing.Color.White)
                : System.Drawing.SystemColors.Control;
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
                    // обновляем видимую колонку "Сезонный коэффициент"
                    row[ColumnName(PriceCalculatorColumn.ManagerDiscount)] = _managerDiscount;

                    // базовая сумма после "строчной" скидки уже посчитана
                    decimal totalWithDiscount = 0m;

                    object v = row[ColumnName(PriceCalculatorColumn.TotalWithDiscount)];
                    if (v != null && v != DBNull.Value)
                        totalWithDiscount = Convert.ToDecimal(v);

                    decimal totalBeforePackage = totalWithDiscount * _managerDiscount;
                    row[ColumnName(PriceCalculatorColumn.TotalBeforePackage)] = totalBeforePackage;

                    UpdatePackageTotal(row);
                }
            }
            finally
            {
                _suppressRecalc = false;
            }

            if (AnySelectedRows())
                SummaryUpdater?.Invoke();
        }

        public void SetDefaultPosition(Merlin.RollerPositions position)
        {
            if (SummaryTable == null) return;

            DoWithSkipCellEndEdit(() =>
            {
                dgvStations.EndEdit();
                _bindingSource.EndEdit();
            });

            _suppressRecalc = true;
            try
            {
                int pos = (int)position;

                foreach (DataRow row in SummaryTable.Rows)
                    row["Position"] = pos;

                // ✅ после массовой смены позиции пересчитываем цены по строкам
                ReCalcAllRows();
            }
            finally
            {
                _suppressRecalc = false;
            }

            _bindingSource.ResetBindings(false);
            dgvStations.Invalidate();
            if (AnySelectedRows())
                SummaryUpdater?.Invoke();
        }

        private void ReCalcAllRows()
        {
            if (dgvStations.Rows.Count == 0) return;

            // Важно: RecalcRow зависит от dgvStations.Rows[rowIndex].DataBoundItem
            for (int i = 0; i < dgvStations.Rows.Count; i++)
                RecalcRow(i);
        }

        private void UpdatePackageTotal(DataRow row)
        {
            if (row == null) return;

            object discountValue = row[ColumnName(PriceCalculatorColumn.PackageDiscount)];
            if (discountValue == null || discountValue == DBNull.Value)
            {
                row[ColumnName(PriceCalculatorColumn.TotalAfterPackage)] = DBNull.Value;
                return;
            }

            object totalWithDiscount = row[ColumnName(PriceCalculatorColumn.TotalWithDiscount)];
            if (totalWithDiscount == null || totalWithDiscount == DBNull.Value)
            {
                row[ColumnName(PriceCalculatorColumn.TotalAfterPackage)] = DBNull.Value;
                return;
            }

            decimal packageDiscount = Convert.ToDecimal(discountValue);
            decimal baseTotal = Convert.ToDecimal(totalWithDiscount);
            row[ColumnName(PriceCalculatorColumn.TotalAfterPackage)] = baseTotal * packageDiscount;
        }

        private void PositionCombo_SelectionChangeCommitted(object sender, EventArgs e)
        {
            if (_suppressRecalc) return;

            var cell = dgvStations.CurrentCell;
            if (cell == null) return;

            var cb = sender as ComboBox ?? dgvStations.EditingControl as ComboBox;
            var newVal = cb != null ? (cb.SelectedValue ?? cb.SelectedItem) : null;

            DoWithSkipCellEndEdit(() =>
            {
                // сразу прокидываем выбранное значение в ячейку/датароу
                if (newVal != null && cell is DataGridViewComboBoxCell comboCell)
                    comboCell.Value = newVal;

                dgvStations.CommitEdit(DataGridViewDataErrorContexts.Commit);
                dgvStations.EndEdit();
                _bindingSource.EndEdit();

                // пересчёт прямо сейчас, без ожидания потери фокуса
                HandleEditableCellChanged(cell.RowIndex, cell.ColumnIndex);
            });
        }

        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // разрешаем цифры, backspace, enter
            if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar))
                return;

            // запрещаем остальное
            e.Handled = true;
        }

        /// <summary>
        /// Таблица для экспорта: только отмеченные строки, видимые колонки (кроме чекбокса), значения как текст.
        /// ExtendedProperties["Headers"]: словарь ColumnName -> HeaderText.
        /// </summary>
        public DataTable BuildSelectedExportTable()
        {
            if (SummaryTable == null)
                return null;

            var selectedRows = new HashSet<DataRow>();

            foreach (DataGridViewRow gridRow in dgvStations.Rows)
            {
                bool isSelected = Convert.ToBoolean(gridRow.Cells["colSelected"].Value ?? false);
                if (!isSelected) continue;

                var drv = gridRow.DataBoundItem as DataRowView;
                if (drv != null)
                    selectedRows.Add(drv.Row);
            }

            if (selectedRows.Count == 0)
                return null;

            var exportCols = dgvStations.Columns.Cast<DataGridViewColumn>()
                .Where(c => c.Visible && c.Name != "colSelected")
                .OrderBy(c => c.DisplayIndex)
                .ToList();

            var dt = new DataTable("Export");
            var positionLookup = Issue.GetRollerPositionItems()
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

            string ResolvePositionText(object rawValue)
            {
                int key = NormalizePosition(rawValue);
                if (positionLookup.TryGetValue(key, out string text))
                    return text;

                if (rawValue == null || rawValue == DBNull.Value)
                    return null;

                return rawValue.ToString();
            }

            foreach (var col in exportCols)
            {
                Type columnType = typeof(string);

                if (col.Name != "colPosition" &&
                    !string.IsNullOrEmpty(col.DataPropertyName) &&
                    SummaryTable.Columns.Contains(col.DataPropertyName))
                {
                    columnType = SummaryTable.Columns[col.DataPropertyName].DataType;
                }

                dt.Columns.Add(col.Name, columnType);
            }

            foreach (DataRow srcRow in selectedRows)
            {
                var newRow = dt.NewRow();
                DataGridViewRow gridRow = null;

                foreach (var col in exportCols)
                {
                    object value = null;

                    if (!string.IsNullOrEmpty(col.DataPropertyName) &&
                        SummaryTable.Columns.Contains(col.DataPropertyName))
                    {
                        value = srcRow[col.DataPropertyName];
                    }
                    else
                    {
                        if (gridRow == null)
                        {
                            gridRow = dgvStations.Rows
                                .Cast<DataGridViewRow>()
                                .FirstOrDefault(r =>
                                {
                                    var drv = r.DataBoundItem as DataRowView;
                                    return drv != null && ReferenceEquals(drv.Row, srcRow);
                                });
                        }

                        if (gridRow != null)
                            value = gridRow.Cells[col.Name].Value;
                    }

                    if (col.Name == "colPosition")
                        value = ResolvePositionText(value);

                    newRow[col.Name] = value ?? (object)DBNull.Value;
                }

                dt.Rows.Add(newRow);
            }

            dt.ExtendedProperties["Headers"] =
                exportCols.ToDictionary(c => c.Name, c => c.HeaderText);

            return dt;
        }

        private void CaptureDefaultColumnHeaders()
        {
            _defaultColumnHeaders.Clear();
            foreach (DataGridViewColumn column in dgvStations.Columns)
                _defaultColumnHeaders[column.Name] = column.HeaderText;
        }

        private void ApplyPriceColumnLayout()
        {
            ResetPriceColumnLayout();
            if (SummaryTable == null || SummaryTable.Rows.Count == 0)
                return;

            // Все четыре одинаковые -> одна колонка
            bool allMerged =
                PriceColumnsEqual("PrimePricePerSecWeekday", "PrimePricePerSecWeekend") &&
                PriceColumnsEqual("PrimePricePerSecWeekday", "NonPrimePricePerSecWeekday") &&
                PriceColumnsEqual("PrimePricePerSecWeekday", "NonPrimePricePerSecWeekend");

            if (allMerged)
            {
                SetColumnLayout("colPrimeWeekday", true, "Цена");
                SetColumnLayout("colPrimeWeekend", false);
                SetColumnLayout("colNonPrimeWeekday", false);
                SetColumnLayout("colNonPrimeWeekend", false);
                return;
            }

            // 1) обычное слияние прайм и не прайм по будни/выходные
            bool primeMerged = PriceColumnsEqual("PrimePricePerSecWeekday", "PrimePricePerSecWeekend");
            bool nonPrimeMerged = PriceColumnsEqual("NonPrimePricePerSecWeekday", "NonPrimePricePerSecWeekend");

            if (primeMerged || nonPrimeMerged)
            {
                ApplyPrimeLayout(primeMerged);
                ApplyNonPrimeLayout(nonPrimeMerged);
                return;
            }

            // 2) альтернативное слияние: прайм с не-прайм по будним и по выходным
            bool crossWeekdayMerged = PriceColumnsEqual("PrimePricePerSecWeekday", "NonPrimePricePerSecWeekday");
            bool crossWeekendMerged = PriceColumnsEqual("PrimePricePerSecWeekend", "NonPrimePricePerSecWeekend");

            if (crossWeekdayMerged)
            {
                SetColumnLayout("colPrimeWeekday", true, "Цена будни");
                SetColumnLayout("colNonPrimeWeekday", false);
            }

            if (crossWeekendMerged)
            {
                SetColumnLayout("colPrimeWeekend", true, "Цена выходные");
                SetColumnLayout("colNonPrimeWeekend", false);
            }
        }

        private void ResetPriceColumnLayout()
        {
            SetColumnLayout("colPrimeWeekday", true);
            SetColumnLayout("colPrimeWeekend", true);
            SetColumnLayout("colNonPrimeWeekday", true);
            SetColumnLayout("colNonPrimeWeekend", true);

            SetColumnLayout("colPrimeTotalSpotsWeekday", true);
            SetColumnLayout("colPrimeTotalSpotsWeekend", true);
            SetColumnLayout("colNonPrimeTotalSpotsWeekday", true);
            SetColumnLayout("colNonPrimeTotalSpotsWeekend", true);
        }

        private void ApplyPrimeLayout(bool merged)
        {
            if (!merged) return;
            // сжимаем только цены
            SetColumnLayout("colPrimeWeekday", true, "Цена прайм");
            SetColumnLayout("colPrimeWeekend", false);
        }

        private void ApplyNonPrimeLayout(bool merged)
        {
            if (!merged) return;
            // сжимаем только цены
            SetColumnLayout("colNonPrimeWeekday", true, "Цена не прайм");
            SetColumnLayout("colNonPrimeWeekend", false);
        }

        private void SetColumnLayout(string columnName, bool visible, string customHeader = null)
        {
            if (!dgvStations.Columns.Contains(columnName))
                return;

            var column = dgvStations.Columns[columnName];
            column.Visible = visible;

            if (customHeader != null)
                column.HeaderText = customHeader;
            else if (_defaultColumnHeaders.TryGetValue(columnName, out var header))
                column.HeaderText = header;
        }

        private bool PriceColumnsEqual(string leftColumn, string rightColumn)
        {
            if (SummaryTable == null) return false;
            if (!SummaryTable.Columns.Contains(leftColumn) ||
                !SummaryTable.Columns.Contains(rightColumn))
                return false;

            foreach (DataRow row in SummaryTable.Rows)
            {
                decimal? left = ReadNullableDecimal(row[leftColumn]);
                decimal? right = ReadNullableDecimal(row[rightColumn]);
                if (left != right)
                    return false;
            }
            return true;
        }

        private static decimal? ReadNullableDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return null;
            return Convert.ToDecimal(value);
        }

        private void RunWithWaitCursor(System.Action action)
        {
            if (action == null) return;
            var old = Cursor.Current;
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                action();
            }
            finally
            {
                Cursor.Current = old;
            }
        }

        private void SuppressRecalc(System.Action action)
        {
            if (action == null) return;
            bool prev = _suppressRecalc;
            _suppressRecalc = true;
            try { action(); }
            finally { _suppressRecalc = prev; }
        }

        private void DoWithSkipCellEndEdit(System.Action action)
        {
            if (action == null) return;
            bool prev = _skipCellEndEdit;
            _skipCellEndEdit = true;
            try { action(); }
            finally { _skipCellEndEdit = prev; }
        }

        private bool RowIsSelected(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvStations.Rows.Count) return false;
            return Convert.ToBoolean(dgvStations.Rows[rowIndex].Cells["colSelected"].Value ?? false);
        }

        private bool AnySelectedRows()
        {
            foreach (DataGridViewRow row in dgvStations.Rows)
                if (Convert.ToBoolean(row.Cells["colSelected"].Value ?? false))
                    return true;
            return false;
        }

        // Полное количество выходов по строке (прайм/непрайм, будни/выходные)
        private static int GetRowTotalSpots(DataRowView drv)
        {
            if (drv == null) return 0;
            int primeWd = SafeInt(drv[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekday)], 0, int.MaxValue);
            int nonPrimeWd = SafeInt(drv[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekday)], 0, int.MaxValue);
            int primeWe = SafeInt(drv[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekend)], 0, int.MaxValue);
            int nonPrimeWe = SafeInt(drv[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekend)], 0, int.MaxValue);
            return primeWd + nonPrimeWd + primeWe + nonPrimeWe;
        }

        // Суммарное количество выходов по всем строкам (optional: только выбранные)
        public int GetTotalSpots(bool onlySelected = true)
        {
            int total = 0;
            foreach (DataGridViewRow row in dgvStations.Rows)
            {
                if (onlySelected && !Convert.ToBoolean(row.Cells["colSelected"].Value ?? false))
                    continue;
                var drv = row.DataBoundItem as DataRowView;
                total += GetRowTotalSpots(drv);
            }
            return total;
        }

        // Общее время (в секундах) = сумма (кол-во выходов по строке * длительность ролика), опционально только выбранные
        public int GetTotalSeconds(bool onlySelected = true)
        {
            int totalSeconds = 0;
            foreach (DataGridViewRow row in dgvStations.Rows)
            {
                if (onlySelected && !Convert.ToBoolean(row.Cells["colSelected"].Value ?? false))
                    continue;

                if (!(row.DataBoundItem is DataRowView drv)) continue;

                int spots = GetRowTotalSpots(drv);
                int duration = SafeInt(drv["RollerDuration"], 0, 3600);
                totalSeconds += spots * duration;
            }
            return totalSeconds;
        }

        private sealed class ManagerDiscountPeriod
        {
            public ManagerDiscountPeriod(DateTime start, DateTime end, decimal ratio)
            {
                Start = start;
                End = end;
                Ratio = ratio;
            }

            public DateTime Start { get; private set; }
            public DateTime End { get; private set; }
            public decimal Ratio { get; private set; }
        }

        private void DgvStations_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex != -1) return; // только заголовок
            var col = dgvStations.Columns[e.ColumnIndex];
            if (!EditableColumns.Contains(col.Name)) return;

            e.PaintBackground(e.ClipBounds, false);
            e.PaintContent(e.ClipBounds);

            // замените на свою иконку из ресурсов, например Properties.Resources.Edit16
            using (var iconBmp = FogSoft.WinForm.Properties.Resources.EditItem)
            {
                var imgSize = 12;
                var rect = new Rectangle(
                    e.CellBounds.Right - imgSize - 4,
                    e.CellBounds.Top + (e.CellBounds.Height - imgSize) / 2,
                    imgSize, imgSize);
                e.Graphics.DrawImage(iconBmp, rect);
            }

            e.Handled = true;
        }

        private static HashSet<int> CaptureSelectedIds(DataTable dt)
        {
            var set = new HashSet<int>();
            if (dt == null) return set;

            string isSelectedColumn = ColumnName(PriceCalculatorColumn.IsSelected);

            if (!dt.Columns.Contains(isSelectedColumn)) return set;
            if (!dt.Columns.Contains("MassmediaID"))
                throw new InvalidOperationException("SummaryTable не содержит колонку MassmediaID.");

            foreach (DataRow r in dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;

                bool selected = r[isSelectedColumn] != DBNull.Value && (bool)r[isSelectedColumn];
                if (!selected) continue;

                if (r["MassmediaID"] == DBNull.Value) continue;
                set.Add(Convert.ToInt32(r["MassmediaID"]));
            }

            return set;
        }

        private static void RestoreSelectedIds(DataTable dt, HashSet<int> selectedIds)
        {
            string isSelectedColumn = ColumnName(PriceCalculatorColumn.IsSelected);
            if (!dt.Columns.Contains(isSelectedColumn))
                dt.Columns.Add(isSelectedColumn, typeof(bool));

            if (!dt.Columns.Contains("MassmediaID"))
                throw new InvalidOperationException("SummaryTable не содержит колонку MassmediaID.");

            foreach (DataRow r in dt.Rows)
            {
                if (r.RowState == DataRowState.Deleted) continue;

                int id = (r["MassmediaID"] == DBNull.Value) ? 0 : Convert.ToInt32(r["MassmediaID"]);
                r[isSelectedColumn] = (id != 0 && selectedIds.Contains(id));
            }
        }

        /// <summary>
        /// Восстанавливает индивидуальные значения редактируемых полей из сохраненного snapshot
        /// </summary>
        public void RestoreRowDetails(List<CampaignCalcRow> savedRows)
        {
            if (SummaryTable == null || savedRows == null || savedRows.Count == 0)
                return;

            DoWithSkipCellEndEdit(() =>
            {
                dgvStations.EndEdit();
                _bindingSource.EndEdit();
            });

            dgvStations.SuspendLayout();
            _suppressRecalc = true;
            try
            {
                var savedRowsDict = savedRows.ToDictionary(r => r.MassmediaId);
                var validPositions = Issue.GetRollerPositionItems()
                    .Select(kvp => kvp.Key)
                    .ToHashSet();

                foreach (DataRow row in SummaryTable.Rows)
                {
                    if (row.RowState == DataRowState.Deleted) continue;

                    int massmediaId = Convert.ToInt32(row["massmediaID"]);

                    if (!savedRowsDict.TryGetValue(massmediaId, out CampaignCalcRow savedRow))
                        continue;

                    row[ColumnName(PriceCalculatorColumn.RollerDuration)] = savedRow.RollerDuration;

                    int positionValue = savedRow.Position;
                    if (!validPositions.Contains(positionValue))
                        positionValue = (int)Merlin.RollerPositions.Undefined;

                    row[ColumnName(PriceCalculatorColumn.Position)] = positionValue;

                    row[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekday)] = savedRow.PrimeTotalSpotsWeekday;
                    row[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekday)] = savedRow.NonPrimeTotalSpotsWeekday;
                    row[ColumnName(PriceCalculatorColumn.PrimeTotalSpotsWeekend)] = savedRow.PrimeTotalSpotsWeekend;
                    row[ColumnName(PriceCalculatorColumn.NonPrimeTotalSpotsWeekend)] = savedRow.NonPrimeTotalSpotsWeekend;
                }

                // ✅ Сначала уведомляем BindingSource об изменениях
                _bindingSource.ResetBindings(false);
            }
            finally
            {
                _suppressRecalc = false;
                dgvStations.ResumeLayout();
            }

            // ✅ Пересчитываем после обновления биндинга
            ReCalcAllRows();

            // ✅ Финальное обновление
            _bindingSource.ResetBindings(false);
            dgvStations.Invalidate();
            dgvStations.Refresh();
        }
    }
}