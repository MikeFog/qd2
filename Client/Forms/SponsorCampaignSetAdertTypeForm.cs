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
using FogSoft.WinForm.Properties;
using Merlin.Classes;

namespace Merlin.Forms
{
	internal partial class SponsorCampaignSetAdertTypeForm : PassportForm
	{
		private readonly Campaign _campaign;
		private ObjectPicker2 _opAdvertType;
        private int _advertTypeId;
        public readonly IList<int> SelectedIDs = new List<int>();

        public SponsorCampaignSetAdertTypeForm()
		{
			InitializeComponent();
		}

		public SponsorCampaignSetAdertTypeForm(Campaign campaign)
			: base(PassportLoader.Load("ChangeAdvertTypeForSponsorIssues"))
		{
            Text = "Предметы рекламы";
            _campaign = campaign;
			btnApply.Visible = false;
			DataSet ds = LoadData();
			pageContext = new PageContext(ds, CreateParameters());
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				Cursor.Current = Cursors.WaitCursor;
				base.OnLoad(e);
                _opAdvertType = FindControl("advertTypeID") as ObjectPicker2;
            }
			finally { Cursor.Current = Cursors.Default; }
		}

        public int AdvertTypeId
        {
            get { return _advertTypeId; }
        }


        private Dictionary<string, object> CreateParameters()
		{
			Dictionary<string, object> parameters = DataAccessor.CreateParametersDictionary();
			parameters["nameWithGroup"] = ((CampaignOnSingleMassmedia)_campaign).MassmediaNameWithGroup;
			return parameters;
		}

		private DataSet LoadData()
		{
			Dictionary<string, object> procParameters = DataAccessor.CreateParametersDictionary();

			procParameters[Campaign.ParamNames.CampaignId] = _campaign.CampaignId;
			procParameters[Campaign.ParamNames.CampaignTypeId] = 100;// (int)_campaign.CampaignType;
			return DataAccessor.LoadDataSet("CampaignDaysTreePassport", procParameters);
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
                if (_opAdvertType.SelectedObject == null)
                {
                    DialogResult = DialogResult.None;
                    FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.AdvertTypeNotSelected);
                    return;
                }

                _advertTypeId = int.Parse(_opAdvertType.SelectedObject.IDs[0].ToString());

                Application.DoEvents();
                Cursor = Cursors.WaitCursor;

                TreeView2 tvSelector = FindControl("days") as TreeView2;
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