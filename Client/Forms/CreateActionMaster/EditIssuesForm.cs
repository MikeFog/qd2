using System;
using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin.Classes.Domain;
using Merlin.Controls;

namespace Merlin.Forms.CreateActionMaster
{
	internal partial class EditIssuesForm : CampaignForm
	{
        private readonly ActionOnMassmedia _action;

        private EditIssuesForm()
		{
			InitializeComponent();
		}

		public EditIssuesForm(Firm firm, ActionOnMassmedia action, int massmediasCount)
			: this()
		{
			_firm = firm;
            _action = action;
            _tariffGrid = new TariffWithRangeGrid(action, massmediasCount);
            //SetTariffGrid(new TariffWithRangeGrid(action, massmediasCount));
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
				

                tbbTemplate.Visible = true;
                grdCurrentCampaignIssues.Caption = "Добавленные выпуски";

                RefreshGrid();
				_tariffGrid.GridRefreshed += TariffGridRefreshed;
				_action.DisplayData(lstStat);
				grdCurrentCampaignIssues.Entity = EntityManager.GetEntity((int)Entities.MasterIssues);
				ShowCurrentIssues(_tariffGrid as TariffWithRangeGrid);
				grdCurrentCampaignIssues.ObjectDeleted += GrdCurrentCampaignIssues_ObjectDeleted;

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
			tbMarkPrimeWindows.Visible = true;
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
            ShowCurrentIssues(((TariffWithRangeGrid)_tariffGrid));
            _action.DisplayData(lstStat);
        }

		private void GrdCurrentCampaignIssues_ObjectDeleted(FogSoft.WinForm.Classes.PresentationObject presentationObject)
		{
			TariffWithRangeGrid rangeGrid = (TariffWithRangeGrid)_tariffGrid;
			MasterIssue issue = (MasterIssue)presentationObject;
			foreach (System.Data.DataRow row in rangeGrid.AddedIssues.Select(
				string.Format("RowNum = '{0}'", issue["RowNum"])))
				rangeGrid.AddedIssues.Rows.Remove(row);
			_action.Recalculate();
			rangeGrid.RefreshGrid();
		}
	}
}
