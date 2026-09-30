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
using FogSoft.WinForm.Forms;

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
			: base(PassportLoader.Load(ProgramPartOfSponsorCampaign.AdvertTypePassport))
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
                _opAdvertType = FindControl(ProgramPartOfSponsorCampaign.AdvertTypeParams.AdvertTypeId) as ObjectPicker2;
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
			parameters[ProgramPartOfSponsorCampaign.AdvertTypeParams.NameWithGroup] = ProgramPartOfSponsorCampaign.AdvertTypePassportCaption(_campaign);
			return parameters;
		}

		// Данные, подпись и проверка — в ядре (ProgramPartOfSponsorCampaign), их зовёт и веб.
		private DataSet LoadData()
		{
			return ProgramPartOfSponsorCampaign.LoadAdvertTypePassportData(_campaign);
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			try
			{
                object advertTypeId = _opAdvertType.SelectedObject?.IDs[0];
                TreeView2 tvSelector = FindControl(ProgramPartOfSponsorCampaign.AdvertTypeParams.Days) as TreeView2;
                string error = ProgramPartOfSponsorCampaign.ValidateAdvertTypeAssignment(advertTypeId, tvSelector.AddedIDs, out List<int> issueIds);
                if (error != null)
                {
                    DialogResult = DialogResult.None;
                    UserMessage.ShowExclamation(error);
                    return;
                }

                _advertTypeId = int.Parse(advertTypeId.ToString());
                foreach (int issueId in issueIds)
                    SelectedIDs.Add(issueId);
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