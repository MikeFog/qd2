using System;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin.Classes.Domain;
using Merlin.Controls;

namespace Merlin.Forms.CreateActionMaster
{
	internal partial class EditIssuesForm : CampaignForm
	{

		private EditIssuesForm()
		{
			InitializeComponent();
		}

		public EditIssuesForm(Firm firm, ActionOnMassmedia action, int massmediasCount)
			: this()
		{
			_firm = firm;
            SetTariffGrid(new TariffWithRangeGrid(action, massmediasCount));
		}

		protected override Firm Firm
		{
			get { return _firm; }
		}

		protected override void SetFormCaption()
		{
			//base.SetFormCaption();
		}

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				base.OnLoad(e);

				// Remove All Issues Grid
				splitContainer4.Panel1Collapsed = true;
                tbbTemplate.Visible = true;
                tbbTemplate2.Visible = false;
                grdCurrentCampaignIssues.Caption = "Добавленные выпуски";

                RefreshGrid();
				tariffGrid.GridRefreshed += TariffGridRefreshed;
				((TariffWithRangeGrid)tariffGrid).Action.DisplayData(lstStat);
				grdCurrentCampaignIssues.Entity = EntityManager.GetEntity((int)Entities.MasterIssues);
				ShowCurrentIssues(tariffGrid as TariffWithRangeGrid);

            }
			catch (Exception ex)
			{
                ErrorManager.PublishError(ex);
            }
		}

        protected override void ProcessToolbar()
        {
            base.ProcessToolbar();
			tsbMuteRoller.Enabled = true;
        }

        protected override void ShowWindowIssues(ITariffWindow tariffWindow)
        {
            //base.ShowWindowIssues(tariffWindow);
            TariffGridRefreshed();
        }

	    private void ShowCurrentIssues(TariffWithRangeGrid grid)
	    {
			grdCurrentCampaignIssues.DataSource = grid.AddedIssues.DefaultView;
	    }

	    private void TariffGridRefreshed()
        {
            ShowCurrentIssues(((TariffWithRangeGrid)tariffGrid));
            ((TariffWithRangeGrid)tariffGrid).Action.DisplayData(lstStat);
        }

		public void DeleteIssue(MasterIssue issue)
		{
			((TariffWithRangeGrid)tariffGrid).DeleteIssue(issue);
		}
	}
}
