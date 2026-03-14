using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Forms.CreateCampaign
{
	internal partial class CampaignNewForm : Form
	{
		private readonly List<Campaign> _campaigns = new List<Campaign>();
		private readonly CampaignPassportFormBaseController _controller;

		public CampaignNewForm()
		{
			InitializeComponent();
			_controller = new CampaignPassportFormBaseController(this);
		}

		public List<Campaign> Campaigns
		{
			get { return _campaigns; }
		}

		private void CampaignNewForm_Load(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;
				_controller.Init(cmbCampaignType, cmbPaymentType, lookUpRolType, grdAgency, grdMassmedia);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
			_controller.OnCheckOkButton += CheckOkButton;
			_controller.CheckOkButton();
		}
        
		private void CheckOkButton()
		{
			btnOk.Enabled =
				cmbPaymentType.SelectedValue != null &&
				cmbCampaignType.SelectedValue != null &&
				(_controller.Massmedias != null && _controller.Massmedias.Any() || !grdMassmedia.Enabled);
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				// это пакетная копания, у нее нет massmedia
				if(!grdMassmedia.Enabled)
                    _campaigns.Add(Campaign.CreateInstance(
                        _controller.CampaignTypeID,
                        _controller.PaymentTypeID,
                        null,
                        _controller.Agency.AgencyId));
				else
					foreach (Massmedia m in grdMassmedia.Added2Checked.Cast<Massmedia>())
					{
						int? agencyId = GetAgencyId(m);
						if (agencyId == null) 
						{
							this.DialogResult = DialogResult.None;
							return;
						}

						Campaign campaign = Campaign.CreateInstance(
							_controller.CampaignTypeID,
							_controller.PaymentTypeID,
							m.MassmediaId,
							(int)agencyId);

						_campaigns.Add(campaign);
					}

			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}


        private int? GetAgencyId(Massmedia m)
        {
            DataTable agencies = m.Agencies;
			if (agencies.Rows.Count == 1)
				return (int)agencies.Rows[0][Agency.ParamNames.AgencyId];

            SelectionForm selector = new SelectionForm(m, "Выбор агентства для радиостанции " + m.Name, false);
            if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
                return ((MassmediaAgency)selector.SelectedObject).AgencyId;

			return null;
        }
    }
}