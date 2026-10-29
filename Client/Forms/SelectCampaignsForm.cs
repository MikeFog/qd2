using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using static FogSoft.WinForm.Constants;

namespace Merlin.Forms
{
    internal struct ObjectAndDate
    {
        public PresentationObject presentationObject;
        public DateTime date; 
    }

    internal partial class SelectCampaignsForm : Form
    {
        private const string SELECTION_COLUMN_NAME = "isSelected";
        private const string SPLITTYPE_COLUMN_NAME = "splitType";
        private const string SPLITDATE_COLUMN_NAME = "splitDate";
        private const string CONTRACTDATE_COLUMN_NAME = "contractDate";
        private const string NEWAGENCY_COLUMN_NAME = "newAgencyID";

        private readonly ActionOnMassmedia _action;
        private List<ObjectAndDate> _selectedItems;
        private List<ActionOnMassmedia.SplitRule> _splitRules;
        private readonly SelectionMode _selectionMode;
        private readonly Entity _entity;
        // Режим Clone: что будет с агентством каждой кампании (campaignID → проверка), см. ActionOnMassmedia.PlanCloneAgencies.
        private Dictionary<int, ActionOnMassmedia.CloneAgencyPlan> _agencyPlans;
        private Dictionary<int, int> _chosenAgencies;
        private int _newAgencyColumnIndex = -1;
        // Режим Clone: одна дата — начало новой акции (решение заказчика 29.10.2026; было — своя дата у каждой кампании).
        private DateTimePicker _dtpCloneStart;
        private DateTime _cloneStartDate;

        public SelectCampaignsForm()
        {
            InitializeComponent();
        }

        public SelectCampaignsForm(ActionOnMassmedia action, SelectionMode selectionMode) : this()
        {
            _action = action;
            _selectionMode = selectionMode;
            _entity = EntityManager.GetEntity((int)Entities.GeneralCampaign);
        }

        public SelectCampaignsForm(Entity entity) : this()
        {            
            _selectionMode = SelectionMode.Agency;
            _entity = entity;
            this.Text = "Выбор агентств";
        }

        [Browsable(false)]
        public List<ObjectAndDate> SelectedItems
        {
            get => _selectedItems; 
        }

        public List<ActionOnMassmedia.SplitRule> SplitRules
        {
            get => _splitRules; 
        }

        /// <summary>Режим Clone: начало новой акции.</summary>
        public DateTime CloneStartDate
        {
            get => _cloneStartDate;
        }

        /// <summary>Режим Clone: campaignID → выбранное пользователем агентство (где действующих у станции несколько).</summary>
        public Dictionary<int, int> ChosenAgencies
        {
            get => _chosenAgencies;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            DataTable dtData = null;

            if (_selectionMode == SelectionMode.Split)
            {
                dtData = _action.SetCampaignsFilterByType(Campaign.CampaignTypes.Simple);
                
                var splitTypeColumn = dtData.Columns.Add(SPLITTYPE_COLUMN_NAME, typeof(int));
                splitTypeColumn.DefaultValue = (int)ActionOnMassmedia.SplitRule.SplitType.ByPeriod;
                
                var splitDateColumn = dtData.Columns.Add(SPLITDATE_COLUMN_NAME, typeof(DateTime));
                
                foreach (DataRow row in dtData.Rows)
                {
                    row[SPLITTYPE_COLUMN_NAME] = (int)ActionOnMassmedia.SplitRule.SplitType.ByPeriod;
                    row[SPLITDATE_COLUMN_NAME] = DateTime.Today;
                }
                
                _entity.AttributeSelector = 2;
            }
            else if(_selectionMode == SelectionMode.Clone)
            {
                dtData = _action.Campaigns();
                _entity.AttributeSelector = 1;
                AddCloneStartDate();
                PrepareCloneAgencies(dtData);
                EnlargeForCloneAgencies();
            }
            else
            {
                dtData = Agency.LoadAgencies(true).Tables[TableNames.Data];
                dtData.Columns.Add(CONTRACTDATE_COLUMN_NAME, typeof(DateTime));
                _entity.AttributeSelector = 1;
            }
            dtData.Columns.Add(SELECTION_COLUMN_NAME, typeof(Boolean));
   
            grdObjects.Fill(dtData, _entity, _selectionMode);

            if (_selectionMode == SelectionMode.Clone)
                AddCloneAgencyColumns();
    
            // ✅ Синхронизируем состояние календарей после Fill
            if (_selectionMode == SelectionMode.Split)
            {
                foreach (DataGridViewRow row in grdObjects.Rows)
                {
                    var calendarCell = row.Cells[7]; // ColumnIndex.Calendar
                    // Т.к. по умолчанию везде ByPeriod - включаем календарь
                    calendarCell.ReadOnly = false;
                    calendarCell.Style.BackColor = calendarCell.OwningColumn.DefaultCellStyle.BackColor;
                    calendarCell.Style.ForeColor = calendarCell.OwningColumn.DefaultCellStyle.ForeColor;
                }
            }
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            if (_selectionMode == SelectionMode.Clone)
                PrepareCloneData();
            else if (_selectionMode == SelectionMode.Split)
                PrepareSplitData();
            else if (_selectionMode == SelectionMode.Agency)
                PrepareAgencyData();
        }

        // Строка над таблицей: «Начало новой акции» — одна дата на все кампании.
        private void AddCloneStartDate()
        {
            _dtpCloneStart = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Today,
                Width = 140,
                Anchor = AnchorStyles.Left
            };
            var panel = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 8), WrapContents = false };
            panel.Controls.Add(new Label { Text = "Начало новой акции:", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(0, 6, 6, 0) });
            panel.Controls.Add(_dtpCloneStart);
            panel.Controls.Add(new Label
            {
                Text = "Все кампании сдвигаются на одно число дней: самая ранняя из отмеченных начнётся в этот день.",
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(12, 6, 0, 0)
            });

            tableLayoutPanel1.SuspendLayout();
            tableLayoutPanel1.RowCount = 3;
            tableLayoutPanel1.RowStyles.Insert(0, new RowStyle(SizeType.AutoSize));
            tableLayoutPanel1.SetRow(flowLayoutPanel1, 2);
            tableLayoutPanel1.SetRow(grdObjects, 1);
            tableLayoutPanel1.Controls.Add(panel, 0, 0);
            tableLayoutPanel1.ResumeLayout(true);
        }

        private void PrepareCloneData()
        {
            _selectedItems = new List<ObjectAndDate>();
            _chosenAgencies = new Dictionary<int, int>();
            _cloneStartDate = _dtpCloneStart.Value.Date;
            if (_cloneStartDate < DateTime.Today)
            {
                UserMessage.ShowExclamation(MessageAccessor.GetMessage("NotAllowedDataInThePast"));
                DialogResult = DialogResult.None;
                return;
            }
            var selectedPlans = new List<ActionOnMassmedia.CloneAgencyPlan>();
            var selectedNames = new Dictionary<int, string>();

            foreach (DataRow row in grdObjects.DBData.Rows)
            {
                if (row[SELECTION_COLUMN_NAME] != DBNull.Value && (bool)row[SELECTION_COLUMN_NAME])
                {
                    ActionOnMassmedia.CloneAgencyPlan agencyPlan = _agencyPlans[Convert.ToInt32(row["campaignID"])];
                    if (agencyPlan.NewAgencyId == null)
                    {
                        UserMessage.ShowExclamation(string.Format(Properties.Resources.CloneAgencyNotChosen, row["name"]));
                        DialogResult = DialogResult.None;
                        return;
                    }
                    selectedPlans.Add(agencyPlan);
                    selectedNames[agencyPlan.CampaignId] = row["name"].ToString();
                    if (agencyPlan.Candidates.Count > 1)
                        _chosenAgencies[agencyPlan.CampaignId] = agencyPlan.NewAgencyId.Value;

                    _selectedItems.Add(new ObjectAndDate { date = _cloneStartDate, presentationObject = Campaign.GetCampaignById((int)row["campaignID"]) });
                }
            }

            if (_selectedItems.Count == 0)
            {
                UserMessage.ShowExclamation(MessageAccessor.GetMessage("NoCampaignSelected"));
                DialogResult = DialogResult.None;
                return;
            }

            var collision = ActionOnMassmedia.FindCloneAgencyCollision(selectedPlans);
            if (collision != null)
            {
                UserMessage.ShowExclamation(string.Format(Properties.Resources.CloneAgencyCollision,
                    selectedNames[collision.Value.first.CampaignId], selectedNames[collision.Value.second.CampaignId]));
                DialogResult = DialogResult.None;
            }
        }

        /// <summary>
        /// Сверяет агентство каждой кампании с действующими агентствами её станции (до показа таблицы),
        /// чтобы клон не отправил счета агентству, которое со станцией уже не связано.
        /// </summary>
        private void PrepareCloneAgencies(DataTable dtData)
        {
            Cursor.Current = Cursors.WaitCursor;
            try
            {
                _agencyPlans = new Dictionary<int, ActionOnMassmedia.CloneAgencyPlan>();
                foreach (DataRow row in dtData.Rows)
                {
                    DataRow source = row;
                    var plan = new ActionOnMassmedia.CloneAgencyPlan(name => source[name]);
                    _agencyPlans[plan.CampaignId] = plan;
                }
                ActionOnMassmedia.PlanCloneAgencies(_agencyPlans.Values);

                dtData.Columns.Add(NEWAGENCY_COLUMN_NAME, typeof(int));
                foreach (DataRow row in dtData.Rows)
                {
                    ActionOnMassmedia.CloneAgencyPlan plan = _agencyPlans[Convert.ToInt32(row["campaignID"])];
                    row[NEWAGENCY_COLUMN_NAME] = plan.NewAgencyId.HasValue ? (object)plan.NewAgencyId.Value : DBNull.Value;
                }
                dtData.AcceptChanges();
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        // Две колонки агентств не помещаются в окно обычного размера: сетка прокручивалась вбок и прятала галочки.
        private void EnlargeForCloneAgencies()
        {
            Rectangle area = Screen.FromControl(Globals.MdiParent ?? (Control)this).WorkingArea;
            ClientSize = new Size(Math.Min(1250, area.Width - 100), Math.Min(560, area.Height - 100));
            CenterToParent();
        }

        // Подсказка к ячейкам агентства: почему строка выделена.
        private static string AgencyNote(ActionOnMassmedia.CloneAgencyPlan plan)
        {
            switch (plan.State)
            {
                case ActionOnMassmedia.CloneAgencyState.Replace:
                    return "Заменено: прежнего агентства у станции уже нет";
                case ActionOnMassmedia.CloneAgencyState.Choose:
                    return "Выберите агентство: прежнего у станции уже нет";
                case ActionOnMassmedia.CloneAgencyState.Missing:
                    return "У станции нет действующего агентства: клонировать нельзя";
                default:
                    return plan.Candidates.Count > 1 ? "У станции несколько агентств: можно выбрать другое" : string.Empty;
            }
        }

        // Агентство новой акции выбирается, если у станции их несколько (прежнее может и подходить — тогда оно стоит по умолчанию).
        private static bool IsAgencyEditable(ActionOnMassmedia.CloneAgencyPlan plan)
        {
            return plan.Candidates.Count > 1
                && (plan.State == ActionOnMassmedia.CloneAgencyState.Keep || plan.State == ActionOnMassmedia.CloneAgencyState.Choose);
        }

        // Выбора ещё нет — оранжевый, агентство отличается от исходного — жёлтый, прежнее — без цвета.
        private static Color AgencyBackColor(ActionOnMassmedia.CloneAgencyPlan plan)
        {
            if (plan.State == ActionOnMassmedia.CloneAgencyState.Missing)
                return Color.Empty;
            if (plan.NewAgencyId == null)
                return Color.LightSalmon;
            return plan.NewAgencyId != plan.OldAgencyId ? Color.LemonChiffon : Color.Empty;
        }

        private void AddCloneAgencyColumns()
        {
            grdObjects.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "agencyName",
                HeaderText = "Агентство в исходной акции",
                ReadOnly = true,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            });
            var newAgency = new DataGridViewComboBoxColumn
            {
                DataPropertyName = NEWAGENCY_COLUMN_NAME,
                HeaderText = "Агентство в новой акции",
                DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
                // Flat — иначе ячейка не показывает цвет фона
                FlatStyle = FlatStyle.Flat,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells
            };
            _newAgencyColumnIndex = grdObjects.Columns.Add(newAgency);

            // Список агентств у каждой строки свой (станция своя), поэтому источник задаётся ячейкам;
            // после сортировки и перепривязки строки создаются заново — настраиваем их снова.
            grdObjects.DataError += (s, e) => e.ThrowException = false;
            grdObjects.DataBindingComplete += (s, e) => ApplyCloneAgencyRows();
            grdObjects.CellValueChanged += GrdObjects_AgencyCellValueChanged;
            grdObjects.CellClick += GrdObjects_AgencyCellClick;
            ApplyCloneAgencyRows();
        }

        private void ApplyCloneAgencyRows()
        {
            foreach (DataGridViewRow row in grdObjects.Rows)
            {
                if (!(row.DataBoundItem is DataRowView view))
                    continue;
                ActionOnMassmedia.CloneAgencyPlan plan = _agencyPlans[Convert.ToInt32(view["campaignID"])];

                var combo = (DataGridViewComboBoxCell)row.Cells[_newAgencyColumnIndex];
                combo.DataSource = plan.Candidates;
                combo.DisplayMember = "Value";
                combo.ValueMember = "Key";
                bool editable = IsAgencyEditable(plan);
                combo.ReadOnly = !editable;
                // выбирать не из чего — стрелку не показываем, чтобы список не казался сломанным
                combo.DisplayStyle = editable ? DataGridViewComboBoxDisplayStyle.DropDownButton : DataGridViewComboBoxDisplayStyle.Nothing;
                combo.FlatStyle = FlatStyle.Flat;

                if (plan.State == ActionOnMassmedia.CloneAgencyState.Missing)
                {
                    // кампанию нельзя клонировать: строка красная, галочка недоступна
                    row.DefaultCellStyle.BackColor = Color.MistyRose;
                    row.DefaultCellStyle.ForeColor = Color.DarkRed;
                    row.Cells[0].ReadOnly = true;
                }
                ColorAgencyCells(row, plan);
                for (int i = _newAgencyColumnIndex - 1; i <= _newAgencyColumnIndex; i++)
                    row.Cells[i].ToolTipText = AgencyNote(plan);
            }
        }

        private void ColorAgencyCells(DataGridViewRow row, ActionOnMassmedia.CloneAgencyPlan plan)
        {
            Color back = AgencyBackColor(plan);
            for (int i = _newAgencyColumnIndex - 1; i <= _newAgencyColumnIndex; i++)
                row.Cells[i].Style.BackColor = back;
        }

        // Список агентств открывается одним щелчком: по умолчанию первый щелчок только выделяет ячейку.
        private void GrdObjects_AgencyCellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != _newAgencyColumnIndex)
                return;
            DataGridViewCell cell = grdObjects[e.ColumnIndex, e.RowIndex];
            if (cell.ReadOnly)
                return;
            grdObjects.CurrentCell = cell;
            if (grdObjects.BeginEdit(true) && grdObjects.EditingControl is ComboBox combo)
                combo.DroppedDown = true;
        }

        private void GrdObjects_AgencyCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex != _newAgencyColumnIndex || !(grdObjects.Rows[e.RowIndex].DataBoundItem is DataRowView view))
                return;
            ActionOnMassmedia.CloneAgencyPlan plan = _agencyPlans[Convert.ToInt32(view["campaignID"])];
            if (view[NEWAGENCY_COLUMN_NAME] != DBNull.Value)
                plan.TryChoose(Convert.ToInt32(view[NEWAGENCY_COLUMN_NAME]));
            ColorAgencyCells(grdObjects.Rows[e.RowIndex], plan);
        }

        private void PrepareSplitData()
        {
            _splitRules = new List<ActionOnMassmedia.SplitRule>();
            foreach (DataRow row in grdObjects.DBData.Rows)
            {
                if (row[SELECTION_COLUMN_NAME] != DBNull.Value && (bool)row[SELECTION_COLUMN_NAME])
                {
                    if (row[SPLITTYPE_COLUMN_NAME] == DBNull.Value)
                    {
                        UserMessage.ShowExclamation(MessageAccessor.GetMessage("SplitTypeNotSelected"));
                        DialogResult = DialogResult.None;
                        return;
                    }

                    CampaignOnSingleMassmedia campaign = (CampaignOnSingleMassmedia)Campaign.GetCampaignById((int)row["campaignID"]);
                    ActionOnMassmedia.SplitRule rule = new ActionOnMassmedia.SplitRule(campaign);
                    
                    if ((int)row[SPLITTYPE_COLUMN_NAME] == (int)ActionOnMassmedia.SplitRule.SplitType.ByPeriod)
                    {
                        if(campaign.StartDate == campaign.FinishDate)
                        {
                            UserMessage.ShowExclamation(string.Format(MessageAccessor.GetMessage("ImpossibleSplitByPeriod"), campaign.MassmediaNameWithGroup));
                            DialogResult = DialogResult.None;
                            return;
                        }

                        if (row[SPLITDATE_COLUMN_NAME] == DBNull.Value)
                        {
                            UserMessage.ShowExclamation(MessageAccessor.GetMessage("SplitDateNotEntered"));
                            DialogResult = DialogResult.None;
                            return;
                        }
                        DateTime splitDate = (DateTime)row[SPLITDATE_COLUMN_NAME];
                        if(splitDate <= campaign.StartDate || splitDate > campaign.FinishDate)
                        {
                            UserMessage.ShowExclamation(string.Format(MessageAccessor.GetMessage("SplitDateIncorrect"), campaign.MassmediaNameWithGroup));
                            DialogResult = DialogResult.None;
                            return;
                        }
                        rule.splitType = ActionOnMassmedia.SplitRule.SplitType.ByPeriod;
                        rule.date = splitDate;
                    }
                    else
                    {
                        campaign.ChildEntity = EntityManager.GetEntity((int)Entities.CampaignRoller);
                        if (campaign.GetContent().Rows.Count < 2)
                        {
                            UserMessage.ShowExclamation(string.Format(MessageAccessor.GetMessage("ActionSplitByRollersImpossible"), campaign.MassmediaNameWithGroup));
                            DialogResult = DialogResult.None;
                            return;
                        }
                        rule.splitType = ActionOnMassmedia.SplitRule.SplitType.ByRollers;
                        
                    }
                    _splitRules.Add(rule);
                }
            }
            if (_splitRules.Count == 0)
            {
                UserMessage.ShowExclamation(MessageAccessor.GetMessage("NoCampaignSelected"));
                DialogResult = DialogResult.None;
            }
            // все проверки пройдены, теперь надо выбрать ролики для каждой кампании
            foreach (var rule in _splitRules)
            {
                if (rule.splitType == ActionOnMassmedia.SplitRule.SplitType.ByRollers)
                {
                    rule.rollers = SelectRollers(rule.campaign);
                    if (rule.rollers.Count == 0)
                        DialogResult = DialogResult.None;
                }
            }
        }

        private void PrepareAgencyData()
        {
            _selectedItems = new List<ObjectAndDate>();

            foreach (DataRow row in grdObjects.DBData.Rows)
            {
                if (row[SELECTION_COLUMN_NAME] != DBNull.Value && (bool)row[SELECTION_COLUMN_NAME])
                {
                    if (row[CONTRACTDATE_COLUMN_NAME] == DBNull.Value)
                    {
                        UserMessage.ShowExclamation(Properties.Resources.NoContractDateSet);
                        DialogResult = DialogResult.None;
                        return;
                    }

                    _selectedItems.Add(new ObjectAndDate
                    {
                        date = (DateTime)row[CONTRACTDATE_COLUMN_NAME],
                        presentationObject = new Agency(row)
                    });
                }
            }

            if (_selectedItems.Count == 0)
            {
                UserMessage.ShowExclamation(Properties.Resources.NoAgencySelected);
                DialogResult = DialogResult.None;
            }
        }

        private List<PresentationObject> SelectRollers(CampaignOnSingleMassmedia campaign)
        {
            try
            {
                SelectionForm fSelectRollers = new SelectionForm(campaign.ChildEntity, campaign.GetContent().DefaultView,
                    string.Format("{0} - выберите рекламные ролики для переноса", campaign.MassmediaNameWithGroup),
                    true, CheckRollersSelectionResult);

                if (fSelectRollers.ShowDialog(Globals.MdiParent) == DialogResult.OK)
                {
                    return fSelectRollers.AddedItems;
                }
                return null;
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private bool CheckRollersSelectionResult(SelectionForm selectionForm)
        {
            if (selectionForm.AddedItems.Count == 0)
            {
                UserMessage.ShowExclamation(MessageAccessor.GetMessage("NoRollersChecked"));
                return false;
            }
            else if (selectionForm.AddedItems.Count == selectionForm.ItemsCount)
            {
                UserMessage.ShowExclamation(MessageAccessor.GetMessage("ActionSplitAllRollersSelected"));
                return false;
            }
            return true;

        }
    }
}
