using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Classes;
using unoidl.com.sun.star.sheet;

namespace Merlin.Forms
{
	internal partial class CampaignDaysForm : PassportForm
	{
        private const string DAYS_TREE_TABLE_NAME = "days";
        private readonly bool isSponsorProgramm;
        public readonly IList<int> SelectedIDs = new List<int>();
		private readonly Campaign campaign;
		private readonly Dictionary<string, object> extraParameters;
        private LookUp cmbCurrentPosition;
        private TreeView2 tvSelector;

        public CampaignDaysForm()
		{
			InitializeComponent();
		}

		public CampaignDaysForm(Campaign campaign, bool isSponsorProgramm, Dictionary<string, object> extrsParameters = null)
			: base(PassportLoader.Load("CampaignDaysDelete"))
		{
			this.campaign = campaign;
			this.isSponsorProgramm = isSponsorProgramm;
			this.extraParameters = extrsParameters;
			btnApply.Visible = false;
			DataSet ds = LoadData();
			pageContext = new PageContext(ds, CreateParameters());
			Text = "”далить выбранные выпуски рекламной кампании";
		}

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            tvSelector = FindControl("days") as TreeView2;
            cmbCurrentPosition = FindControl("lookupCurrentPosition") as LookUp;
			cmbCurrentPosition.SelectedItemChanged += CmbCurrentPosition_SelectedItemChanged;
        }

        private Dictionary<string, object> CreateParameters()
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
            parameters["radiostation"] = campaign.Name;
            parameters["campaignType"] = campaign.CampaignTypeName;
            return parameters;
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

        private DataSet LoadData()
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();
			procParameters[Campaign.ParamNames.CampaignId] = campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = isSponsorProgramm ? 100 : (int)campaign.CampaignType;
			if(extraParameters != null)
				foreach(string paramName in extraParameters.Keys)
					procParameters[paramName] = extraParameters[paramName];
			if (cmbCurrentPosition != null && cmbCurrentPosition.SelectedValue != null)
				procParameters[Issue.ParamNames.Position] = cmbCurrentPosition.SelectedValue;

            DataSet ds = DataAccessor.LoadDataSet("CampaignDaysTreePassport", procParameters);
            return ds;
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

                TreeView2 tvSelector = FindControl("days") as TreeView2;
                //  ¬ AddedIDs будут IssueID - они целочисленные, и идентификаторы дней, так как у дней в дереве тоже можно
				// галочку поставить. ¬ качестве ID дн€ используетс€ дата. Ќам нужны только рекламные выпуски
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