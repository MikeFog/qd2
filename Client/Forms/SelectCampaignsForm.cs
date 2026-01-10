using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Windows.Forms;
using static FogSoft.WinForm.Constants;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

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
        private const string CLONEDATE_COLUMN_NAME = "cloneDate";
        private const string SPLITTYPE_COLUMN_NAME = "splitType";
        private const string SPLITDATE_COLUMN_NAME = "splitDate";
        private const string CONTRACTDATE_COLUMN_NAME = "contractDate";

        private readonly ActionOnMassmedia _action;
        private List<ObjectAndDate> _selectedItems;
        private List<ActionOnMassmedia.SplitRule> _splitRules;
        private readonly SelectionMode _selectionMode;
        private readonly Entity _entity;

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

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            DataTable dtData = null;

            if (_selectionMode == SelectionMode.Split)
            {
                dtData = _action.SetCampaignsFilterByType(Campaign.CampaignTypes.Simple);
                dtData.Columns.Add(SPLITTYPE_COLUMN_NAME, typeof(int));
                dtData.Columns.Add(SPLITDATE_COLUMN_NAME, typeof(DateTime));
                _entity.AttributeSelector = 2;
            }
            else if(_selectionMode == SelectionMode.Clone)
            {
                dtData = _action.Campaigns();
                _entity.AttributeSelector = 1;
                dtData.Columns.Add(CLONEDATE_COLUMN_NAME, typeof(DateTime));
            }
            else
            {
                dtData = Agency.LoadAgencies(true).Tables[TableNames.Data];
                dtData.Columns.Add(CONTRACTDATE_COLUMN_NAME, typeof(DateTime));
                _entity.AttributeSelector = 1;
            }
            dtData.Columns.Add(SELECTION_COLUMN_NAME, typeof(Boolean));
           
            grdObjects.Fill(dtData, _entity, _selectionMode);
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

        private void PrepareCloneData()
        {
            _selectedItems = new List<ObjectAndDate>();

            foreach (DataRow row in grdObjects.DBData.Rows)
            {
                if (row[SELECTION_COLUMN_NAME] != DBNull.Value && (bool)row[SELECTION_COLUMN_NAME])
                {
                    if (row[CLONEDATE_COLUMN_NAME] == DBNull.Value)
                    {
                        MessageBox.ShowExclamation(MessageAccessor.GetMessage("NoCloneDataSelected"));
                        DialogResult = DialogResult.None;
                        return;
                    }

                    DateTime newDate = (DateTime)row[CLONEDATE_COLUMN_NAME];
                    if (newDate < DateTime.Today)
                    {
                        MessageBox.ShowExclamation(MessageAccessor.GetMessage("NotAllowedDataInThePast"));
                        DialogResult = DialogResult.None;
                        return;
                    }

                    _selectedItems.Add(new ObjectAndDate { date = (DateTime)row[CLONEDATE_COLUMN_NAME], presentationObject = Campaign.GetCampaignById((int)row["campaignID"]) });
                }
            }

            if (_selectedItems.Count == 0)
            {
                MessageBox.ShowExclamation(MessageAccessor.GetMessage("NoCampaignSelected"));
                DialogResult = DialogResult.None;
            }
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
                        MessageBox.ShowExclamation(MessageAccessor.GetMessage("SplitTypeNotSelected"));
                        DialogResult = DialogResult.None;
                        return;
                    }

                    CampaignOnSingleMassmedia campaign = (CampaignOnSingleMassmedia)Campaign.GetCampaignById((int)row["campaignID"]);
                    ActionOnMassmedia.SplitRule rule = new ActionOnMassmedia.SplitRule(campaign);
                    
                    if ((int)row[SPLITTYPE_COLUMN_NAME] == (int)ActionOnMassmedia.SplitRule.SplitType.ByPeriod)
                    {
                        if(campaign.StartDate == campaign.FinishDate)
                        {
                            MessageBox.ShowExclamation(string.Format(MessageAccessor.GetMessage("ImpossibleSplitByPeriod"), campaign.MassmediaNameWithGroup));
                            DialogResult = DialogResult.None;
                            return;
                        }

                        if (row[SPLITDATE_COLUMN_NAME] == DBNull.Value)
                        {
                            MessageBox.ShowExclamation(MessageAccessor.GetMessage("SplitDateNotEntered"));
                            DialogResult = DialogResult.None;
                            return;
                        }
                        DateTime splitDate = (DateTime)row[SPLITDATE_COLUMN_NAME];
                        if(splitDate <= campaign.StartDate || splitDate > campaign.FinishDate)
                        {
                            MessageBox.ShowExclamation(string.Format(MessageAccessor.GetMessage("SplitDateIncorrect"), campaign.MassmediaNameWithGroup));
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
                            MessageBox.ShowExclamation(string.Format(MessageAccessor.GetMessage("ActionSplitByRollersImpossible"), campaign.MassmediaNameWithGroup));
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
                MessageBox.ShowExclamation(MessageAccessor.GetMessage("NoCampaignSelected"));
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
                        MessageBox.ShowExclamation(Properties.Resources.NoContractDateSet);
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
                MessageBox.ShowExclamation(Properties.Resources.NoAgencySelected);
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
                MessageBox.ShowExclamation(MessageAccessor.GetMessage("NoRollersChecked"));
                return false;
            }
            else if (selectionForm.AddedItems.Count == selectionForm.ItemsCount)
            {
                MessageBox.ShowExclamation(MessageAccessor.GetMessage("ActionSplitAllRollersSelected"));
                return false;
            }
            return true;

        }
    }
}
