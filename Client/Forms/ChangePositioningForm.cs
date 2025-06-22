using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Merlin.Forms
{
    internal partial class ChangePositioningForm : PassportForm
    {
        private const string DAYS_TREE_TABLE_NAME = "days";
        private readonly CampaignPart campaignPart;
        private LookUp cmbCurrentPosition;
        private TreeView2 tvSelector;
        private RollerPositions newPosition;

        public readonly IList<int> SelectedIDs = new List<int>();

        public ChangePositioningForm()
        {
            InitializeComponent();
        }

        public ChangePositioningForm(CampaignPart campaignPart) : base(PassportLoader.Load("IssueChangePositioning"))
        {
            InitializeComponent();
            this.campaignPart = campaignPart;

            btnApply.Visible = false;
            DataSet ds = LoadData();
            pageContext = new PageContext(ds, CreateParameters());
        }

        public RollerPositions NewPosition
        {
            get { return newPosition; }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            tvSelector = FindControl("days") as TreeView2;
            cmbCurrentPosition = FindControl("lookupCurrentPosition") as LookUp;
            cmbCurrentPosition.SelectedItemChanged += CmbCurrentPosition_SelectedItemChanged;
            cmbCurrentPosition.SelectedValue = (int)RollerPositions.Undefined;
        }

        private void CmbCurrentPosition_SelectedItemChanged(object sender, EventArgs e)
        {
            try
            {
                Cursor.Current = Cursors.WaitCursor;
                tvSelector.ShowData(LoadData().Tables[DAYS_TREE_TABLE_NAME]);
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }

        private Dictionary<string, object> CreateParameters()
        {
            Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters["radiostation"] = campaignPart.Campaign.Name;
            parameters["campaignType"] = campaignPart.Campaign.CampaignTypeName;
            parameters["rollerName"] = campaignPart.Name;
            //parameters["rollerDuration"] = campaignRoller.Roller.DurationString;
            return parameters;
        }

        private DataSet LoadData()
        {
            Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
            procParameters[Campaign.ParamNames.CampaignId] = campaignPart.Campaign.CampaignId;
            procParameters[Campaign.ParamNames.CampaignTypeId] = (int)campaignPart.Campaign.CampaignType;
            if(campaignPart is CampaignPartPackModule)
                procParameters[CampaignPart.OBJECT_ID] = campaignPart.IDs[1]; // packModuleID находится в IDs[1]
            else if (!(campaignPart is CampaignPackModule) && !(campaignPart is CampaignOnSingleMassmedia) && !(campaignPart is CampaignModule) && !(campaignPart is RollerPartOfSponsorCampaign))
                procParameters[CampaignPart.OBJECT_ID] = campaignPart.IDs[0];
            procParameters[Issue.ParamNames.Position] = cmbCurrentPosition != null ? cmbCurrentPosition.SelectedValue : 123; // несуществующий ID позиции, чтоб вернулась пустая таблица

            return DataAccessor.LoadDataSet("IssueChangePositioningPassport", procParameters);
        }

        protected override void ApplyChanges(Button clickedButton)
        {
            try
            {
                Application.DoEvents();
                Cursor = Cursors.WaitCursor;
                LookUp cmbFuturePosition = FindControl("lookupFuturePosition") as LookUp;

                if(cmbCurrentPosition.SelectedIndex == cmbFuturePosition.SelectedIndex)
                {
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("CurrentAndFuturePositionsSholdBeDifferent"));
                    DialogResult = DialogResult.None;
                    return;
                }

                //  В AddedIDs будут IssueID - они целочисленные, и идентификаторы дней, так как у дней в дереве тоже можно
                // галочку поставить. В качестве ID дня используется дата. Нам нужны только рекламные выпуски
                foreach (object id in tvSelector.AddedIDs)
                {
                    if (id != null && int.TryParse(id.ToString(), out int issueId))
                        SelectedIDs.Add(issueId);
                }

                if (SelectedIDs.Count == 0)
                {
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("NoIssuesSelected"));
                    DialogResult = DialogResult.None;
                    return;
                }

                newPosition = (RollerPositions)Enum.Parse(typeof(RollerPositions), cmbFuturePosition.SelectedValue.ToString());
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }
    }
}
