using FogSoft.WinForm.Classes;
using System;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Controls;
using System.Data.SqlClient;
using Merlin.Classes;
using System.Drawing;

namespace Merlin.Controls
{
    public partial class GenericGridView : DataGridView
    {
        private Entity _entity;
        private SelectionMode _selectionMode;
        private struct ColumnIndex
        {
            public const int CheckBox = 0;
            public const int ComboBox = 6;
            public const int Calendar = 7;
            public const int Calendar_Agency = 2;
            public const int Calendar_Clone = 6;
        }

        public GenericGridView()
        {
            
            InitializeComponent();
        }

        public GenericGridView(IContainer container)
        {
            container.Add(this);
            BackgroundColor = System.Drawing.SystemColors.Window;
            RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders;
            InitializeComponent();
            this.CurrentCellDirtyStateChanged += GenericGridView_CurrentCellDirtyStateChanged;
        }


        private void GenericGridView_CurrentCellDirtyStateChanged(object sender, EventArgs e)
        {
            if (!IsCurrentCellDirty || CurrentCell == null)
                return;

            // Коммитим изменения именно для combo/checkbox
            if (CurrentCell is DataGridViewComboBoxCell || CurrentCell is DataGridViewCheckBoxCell)
                CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        protected override void OnCellContentClick(DataGridViewCellEventArgs e)
        {
            base.OnCellContentClick(e);
            CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        protected override void OnCellValueChanged(DataGridViewCellEventArgs e)
        {
            base.OnCellValueChanged(e);

            if (e.RowIndex < 0 || e.ColumnIndex != ColumnIndex.CheckBox)
                return;

            var row = Rows[e.RowIndex];
            var checkCell = row?.Cells[e.ColumnIndex];
            bool isChecked = Convert.ToBoolean(checkCell?.Value ?? false);

            if (_selectionMode == Merlin.SelectionMode.Agency)
            {
                var calendarCell = row.Cells[ColumnIndex.Calendar_Agency];
                enableCell(calendarCell, isChecked);
                if (isChecked)
                    StartCalendarEdit(calendarCell);
            }
            else if (_selectionMode == Merlin.SelectionMode.Split)
            {
                // В режиме Split просто управляем доступностью календаря
                // Не переключаем фокус автоматически - пользователь сам кликнет когда нужно
                var calendarCell = row.Cells[ColumnIndex.Calendar];
                
                if (!isChecked)
                {
                    // Снимаем чекбокс - блокируем календарь
                    enableCell(calendarCell, false);
                }
                else
                {
                    // Включаем чекбокс - проверяем выбор в ComboBox
                    var val = DBData.Rows[e.RowIndex]["splitType"];
                    if (val != DBNull.Value)
                    {
                        // Если в ComboBox выбрано "По периоду" - включаем календарь
                        bool calendarEnabled = (int)val == (int)ActionOnMassmedia.SplitRule.SplitType.ByPeriod;
                        enableCell(calendarCell, calendarEnabled);
                        // Убрали StartCalendarEdit - не переключаемся автоматически
                    }
                    else
                    {
                        // Значение в ComboBox не выбрано - календарь заблокирован
                        enableCell(calendarCell, false);
                    }
                }
            }
            else
            {
                var calendarCell = row.Cells[ColumnIndex.Calendar_Clone];
                enableCell(calendarCell, isChecked);
                if (isChecked)
                    StartCalendarEdit(calendarCell);
            }
        }

        protected override void OnEditingControlShowing(DataGridViewEditingControlShowingEventArgs e)
        {
            base.OnEditingControlShowing(e);

            if (CurrentCell?.ColumnIndex != ColumnIndex.ComboBox)
                return;

            if (e.Control is ComboBox combo)
            {
                combo.SelectionChangeCommitted -= ComboBox_SelectionChangeCommitted;
                combo.SelectionChangeCommitted += ComboBox_SelectionChangeCommitted;
            }
        }

        private void ComboBox_SelectionChangeCommitted(object sender, EventArgs e)
        {
            // Скажем гриду "ячейка изменилась"
            NotifyCurrentCellDirty(true);

            // И принудительно зафиксируем (можно и без этого, если есть CurrentCellDirtyStateChanged,
            // но так вообще железобетон)
            CommitEdit(DataGridViewDataErrorContexts.Commit);
            EndEdit();

            // Дальше твоя логика включения/выключения календаря
            if (sender is ComboBox cb && cb.SelectedItem is DataRowView v && CurrentCell != null)
            {
                var row = Rows[CurrentCell.RowIndex];
                bool calendarEnabled = Convert.ToInt32(v.Row["Id"]) == (int)ActionOnMassmedia.SplitRule.SplitType.ByPeriod;

                var calendarCell = row.Cells[ColumnIndex.Calendar];
                enableCell(calendarCell, calendarEnabled);
            }
        }

        private void StartCalendarEdit(DataGridViewCell cell)
        {
            if (cell == null || cell.ReadOnly)
                return;

            if (cell.Value == null || cell.Value == DBNull.Value)
            {
                cell.Value = DateTime.Today;
            }

            if (CurrentCell != cell)
                CurrentCell = cell;

            if (!IsCurrentCellInEditMode)
                BeginEdit(true);
        }

        private void enableCell(DataGridViewCell dc, bool enabled)
        {
            //toggle read-only state
            dc.ReadOnly = !enabled;
            if (enabled)
            {
                //restore cell style to the default value
                dc.Style.BackColor = dc.OwningColumn.DefaultCellStyle.BackColor;
                dc.Style.ForeColor = dc.OwningColumn.DefaultCellStyle.ForeColor;
            }
            else
            {
                //gray out the cell
                dc.Style.BackColor = Color.LightGray;
                dc.Style.ForeColor = Color.DarkGray;
            }
        }

        internal void Fill(DataTable dtCampaigns, Entity entity, SelectionMode selectionMode)
        {
            AutoGenerateColumns = false;
            _entity = entity;
            _selectionMode = selectionMode;

            SetColumnHeaders(dtCampaigns.Columns);
            DataSource = dtCampaigns;
        }

        public DataTable DBData
        {
            get { return (DataTable)DataSource; }
        }

        private void SetColumnHeaders(DataColumnCollection columns)
        {
            foreach (Entity.Attribute entityAttribute in _entity.SortedAttributes)
            {
                if (columns.Contains(entityAttribute.Name))
                    AddColumn(entityAttribute);
            }
        }

        private void AddColumn(Entity.Attribute entityAttribute)
        {
            DataGridViewColumn column = CreateDataGridColumn(entityAttribute);
            column.DataPropertyName = entityAttribute.Name;
            column.HeaderText = entityAttribute.Alias;
            Columns.Add(column);
        }

        private DataGridViewColumn CreateDataGridColumn(Entity.Attribute attribute)
        {
            ColumnInfo columnInfo;
            _entity.ColumnsInfo.TryGetValue(attribute.Name, out columnInfo);

            if(attribute.DataType == "checkBox")
            {
                DataGridViewCheckBoxColumn column = new DataGridViewCheckBoxColumn
                {
                    ReadOnly = false,
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
                };
                return column;
            }
           
            if (attribute.DataType == "list")
            {
                DataGridViewComboBoxColumn column = new DataGridViewComboBoxColumn
                {
                    DataSource = CreateSplitTypeTable(),
                    DisplayMember = "Name",
                    ValueMember = "Id",
                    ReadOnly = false,
                    DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton
                };
                return column;
            }

            if (attribute.DataType == "datetime")
            {
                CalendarColumn column = new CalendarColumn
                {
                    ReadOnly = true
                };
                return column;
            }

            if (ColumnInfo.IsBooleanData(columnInfo, attribute))
            {
                DataGridViewImageColumn column = new DataGridViewImageColumn
                {
                    CellTemplate = new DataGridViewExBooleanCell(),
                    SortMode = DataGridViewColumnSortMode.Automatic,
                    Resizable = DataGridViewTriState.False,
                    ReadOnly = true
            };
                return column;
            }

            if (ColumnInfo.IsMoneyData(columnInfo, attribute))
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.CellTemplate.Style.Format = "c";
                column.CellTemplate.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.ReadOnly = true;
                return column;
            }

            if (ColumnInfo.IsFloatData(columnInfo, attribute))
            {
                DataGridViewTextBoxColumn column = new DataGridViewTextBoxColumn();
                column.CellTemplate.Style.Format = "f";
                column.CellTemplate.Style.Alignment = DataGridViewContentAlignment.MiddleRight;
                column.ReadOnly = true;
                return column;
            }

            DataGridViewTextBoxColumn column2 = new DataGridViewTextBoxColumn
            {
                Name = attribute.Name,
                ReadOnly = true
            };

            return column2;
        }

        private DataTable CreateSplitTypeTable()
        {
            DataTable table = new DataTable();
            table.Columns.Add("Id", typeof(int));
            table.Columns.Add("Name", typeof(string));
            DataRow row = table.NewRow();
            row["Id"] = (int)ActionOnMassmedia.SplitRule.SplitType.ByPeriod;
            row["Name"] = "По периоду";
            table.Rows.Add(row);
            row = table.NewRow();
            row["Id"] = (int)ActionOnMassmedia.SplitRule.SplitType.ByRollers;
            row["Name"] = "По роликам";
            table.Rows.Add(row);

            return table;
        }

        #region CalendarColumn --------------------------------------------------------------------

        public class CalendarColumn : DataGridViewColumn
        {
            public CalendarColumn() : base(new CalendarCell())
            {
            }

            public override DataGridViewCell CellTemplate
            {
                get
                {
                    return base.CellTemplate;
                }
                set
                {
                    if (value != null &&
                        !value.GetType().IsAssignableFrom(typeof(CalendarCell)))
                    {
                        throw new InvalidCastException("Must be a CalendarCell");
                    }
                    base.CellTemplate = value;
                }
            }
        }

        public class CalendarCell : DataGridViewTextBoxCell
        {

            public CalendarCell() : base()
            {
                // Use the short date format.
                this.Style.Format = "d";
            }

            public override void InitializeEditingControl(int rowIndex, object  initialFormattedValue, DataGridViewCellStyle dataGridViewCellStyle)
            {
                base.InitializeEditingControl(rowIndex, initialFormattedValue,
                    dataGridViewCellStyle);

                CalendarEditingControl ctl =
                    DataGridView.EditingControl as CalendarEditingControl;

                if (this.Value != System.DBNull.Value)
                {
                    ctl.Value = (DateTime)this.Value;
                }
                else
                {
                    ctl.Value = DateTime.Today;
                }

                // Removed writing back to DataGridView.CurrentCell here: it triggered
                // OnCellValueChanged → StartCalendarEdit → BeginEdit → recursion.
                // OnValueChanged already pushes the selected date into the cell.
            }

            public override Type EditType
            {
                get
                {
                    // Return the type of the editing contol that CalendarCell uses.
                    return typeof(CalendarEditingControl);
                }
            }

            public override Type ValueType
            {
                get
                {
                    // Return the type of the value that CalendarCell contains.
                    return typeof(DateTime);
                }
            }

            public override object DefaultNewRowValue
            {
                get
                {
                    // Use the current date and time as the default value.
                    return DateTime.Now;
                }
            }
        }

        class CalendarEditingControl : DateTimePicker, IDataGridViewEditingControl
        {
            DataGridView dataGridView;
            private bool valueChanged = false;
            int rowIndex;

            public CalendarEditingControl()
            {
                this.Format = DateTimePickerFormat.Short;
            }

            // Implements the IDataGridViewEditingControl.EditingControlFormattedValue 
            // property.
            public object EditingControlFormattedValue
            {
                get
                {
                    return this.Value.ToShortDateString();
                }
                set
                {
                    if (value is String)
                    {
                        this.Value = DateTime.Parse((String)value);
                    }
                }
            }

            // Implements the 
            // IDataGridViewEditingControl.GetEditingControlFormattedValue method.
            public object GetEditingControlFormattedValue(
                DataGridViewDataErrorContexts context)
            {
                return EditingControlFormattedValue;
            }

            // Implements the 
            // IDataGridViewEditingControl.ApplyCellStyleToEditingControl method.
            public void ApplyCellStyleToEditingControl(
                DataGridViewCellStyle dataGridViewCellStyle)
            {
                this.Font = dataGridViewCellStyle.Font;
                this.CalendarForeColor = dataGridViewCellStyle.ForeColor;
                this.CalendarMonthBackground = dataGridViewCellStyle.BackColor;
            }

            // Implements the IDataGridViewEditingControl.EditingControlRowIndex 
            // property.
            public int EditingControlRowIndex
            {
                get
                {
                    return rowIndex;
                }
                set
                {
                    rowIndex = value;
                }
            }

            // Implements the IDataGridViewEditingControl.EditingControlWantsInputKey 
            // method.
            public bool EditingControlWantsInputKey(Keys key, bool dataGridViewWantsInputKey)
            {
                // Let the DateTimePicker handle the keys listed.
                switch (key & Keys.KeyCode)
                {
                    case Keys.Left:
                    case Keys.Up:
                    case Keys.Down:
                    case Keys.Right:
                    case Keys.Home:
                    case Keys.End:
                    case Keys.PageDown:
                    case Keys.PageUp:
                        return true;
                    default:
                        return false;
                }
            }

            // Implements the IDataGridViewEditingControl.PrepareEditingControlForEdit 
            // method.
            public void PrepareEditingControlForEdit(bool selectAll)
            {
                // No preparation needs to be done.
            }

            // Implements the IDataGridViewEditingControl
            // .RepositionEditingControlOnValueChange property.
            public bool RepositionEditingControlOnValueChange
            {
                get
                {
                    return false;
                }
            }

            // Implements the IDataGridViewEditingControl
            // .EditingControlDataGridView property.
            public DataGridView EditingControlDataGridView
            {
                get
                {
                    return dataGridView;
                }
                set
                {
                    dataGridView = value;
                }
            }

            // Implements the IDataGridViewEditingControl
            // .EditingControlValueChanged property.
            public bool EditingControlValueChanged
            {
                get
                {
                    return valueChanged;
                }
                set
                {
                    valueChanged = value;
                }
            }

            // Implements the IDataGridViewEditingControl
            // .EditingPanelCursor property.
            public Cursor EditingPanelCursor
            {
                get
                {
                    return base.Cursor;
                }
            }

            protected override void OnValueChanged(EventArgs eventargs)
            {
                if (Visible) 
                {
                    valueChanged = true;
                    dataGridView.CurrentCell.Value = Value;
                    dataGridView.NotifyCurrentCellDirty(true);
                    dataGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
                }
            

                // Notify the DataGridView that the contents of the cell
                // have changed.
                //valueChanged = true;
                //this.EditingControlDataGridView.NotifyCurrentCellDirty(true);
                base.OnValueChanged(eventargs);
            }
        }

        #endregion
    }
}
