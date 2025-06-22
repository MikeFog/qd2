using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class TrafficManagementForm : Form
	{
		public TrafficManagementForm()
		{
			InitializeComponent();
			grdTariffWindow.ShowTrafficWindows = true;
			Entity issueEntity = (Entity)EntityManager.GetEntity((int) Entities.Issue).Clone();
			issueEntity.AttributeSelector = (int) RollerIssue.AttributeSelectors.TrafficManager;
			grdSelectedCellIssues.Entity = issueEntity;
			grdSelectedCellIssues.CheckBoxes = true;
			Icon = Globals.MdiParent.Icon;
			tbbRefresh.Image = Globals.GetImage(Constants.ActionsImages.Refresh);

            Entity entityMassmedia = (Entity)Massmedia.GetEntity().Clone();
            entityMassmedia.AttributeSelector = (int)Massmedia.AttributeSelectors.NameAndGroupOnly;
            grdMassmedia.Entity = entityMassmedia;
        }

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);

			grdTariffWindow.CurrentDate = DateTime.Today;
			grdTariffWindow.BindControls(grdMassmedia, grdSelectedCellIssues, chkDate);

			InitRadiostationGroupsCombo();
			
			Application.DoEvents();
			grdTariffWindow.Refresh();
		}

        private void InitRadiostationGroupsCombo()
        {
            cmbRadioStationGroup.ColumnWithID = "massmediaGroupID";
            cmbRadioStationGroup.DataSource = Massmedia.LoadGroupsWithShowAllOption();
        }

		private void tbbRefresh_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				grdTariffWindow.RefreshGrid();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void tsbJump_Click(object sender, EventArgs e)
		{
			if (grdTariffWindow.Jump2Date())
				grdTariffWindow.RefreshGrid();
		}

        private void cmbRadioStationGroup_SelectedItemChanged(object sender, EventArgs e)
        {
            Massmedia.LoadRadiostationsByGroup(cmbRadioStationGroup, grdMassmedia);
        }
    }
}