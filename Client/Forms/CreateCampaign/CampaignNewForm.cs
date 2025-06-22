using System;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using Merlin.Classes;

namespace Merlin.Forms.CreateCampaign
{
	internal partial class CampaignNewForm : Form
	{
		private Campaign campaign;
		private readonly CampaignPassportFormBaseController controller;

		public CampaignNewForm()
		{
			InitializeComponent();
			controller = new CampaignPassportFormBaseController(this);
		}

		public Campaign Campaign
		{
			get { return campaign; }
		}

		private void CampaignNewForm_Load(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				controller.Init(cmbCampaignType, cmbPaymentType, lookUpRolType, grdAgency, grdMassmedia);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
			controller.OnCheckOkButton += CheckOkButton;
			controller.CheckOkButton();
		}
        
		private void CheckOkButton()
		{
			btnOk.Enabled =
				cmbPaymentType.SelectedValue != null &&
				cmbCampaignType.SelectedValue != null &&
				grdAgency.SelectedObject !=null &&
				(grdMassmedia.SelectedObject != null || !grdMassmedia.Enabled);
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				campaign = Campaign.CreateInstance(
					controller.CampaignTypeID,
					controller.PaymentTypeID,
					controller.Massmedia == null ? null : (int?)(int.Parse(controller.Massmedia.IDs[0].ToString())),
					controller.Agency.AgencyId);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}
    }
}