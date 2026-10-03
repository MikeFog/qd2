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
				// список собирается заново на каждое «Ок»: если диалог остался открытым,
				// повторное «Ок» не должно добавить те же станции второй раз (UIX_Campaign)
				_campaigns.Clear();

				// СЌС‚Рѕ РїР°РєРµС‚РЅР°СЏ РєРѕРїР°РЅРёСЏ, Сѓ РЅРµРµ РЅРµС‚ massmedia
				if(!grdMassmedia.Enabled)
				{
					if (_controller.Agency == null)
					{
						UserMessage.ShowExclamation(Properties.Resources.AgencyIsRequied);
						DialogResult = DialogResult.None;
						return;
					}

                    _campaigns.Add(Campaign.CreateInstance(
                        _controller.CampaignTypeID,
                        _controller.PaymentTypeID,
                        null,
                        _controller.Agency.AgencyId));
				}
				else
				{
					// «Отмена» в выборе агентства пропускает только эту станцию, остальные добавляются
					List<string> skipped = new List<string>();
					foreach (Massmedia m in grdMassmedia.Added2Checked.Cast<Massmedia>())
					{
						int? agencyId = GetAgencyId(m);
						if (agencyId == null)
						{
							skipped.Add(m.Name);
							continue;
						}

						Campaign campaign = Campaign.CreateInstance(
							_controller.CampaignTypeID,
							_controller.PaymentTypeID,
							m.MassmediaId,
							(int)agencyId);
						// имя станции - для итогового сообщения карточки, если запись не пройдёт
						campaign[Campaign.ParamNames.MassmediaName] = m.Name;

						_campaigns.Add(campaign);
					}

					if (skipped.Count > 0)
						UserMessage.ShowExclamation("Не выбрано агентство, станции не добавлены: " + string.Join(", ", skipped) + ".");

					// пропущены все - не закрываться «Ок»: карточка новой акции записала бы пустую акцию
					if (_campaigns.Count == 0)
						DialogResult = DialogResult.None;
				}
			}
			catch (Exception ex)
			{
				// ошибка - не «Ок»: иначе карточка запишет акцию с недособранным списком
				_campaigns.Clear();
				DialogResult = DialogResult.None;
				ErrorManager.PublishError(ex);
			}
		}


        private int? GetAgencyId(Massmedia m)
        {
            DataTable agencies = m.Agencies;
			if (agencies.Rows.Count == 1)
				return Convert.ToInt32(agencies.Rows[0][Agency.ParamNames.AgencyId]);
			if (agencies.Rows.Count == 0)
				return null;   // выбирать не из чего - станция пропускается без пустого окна выбора

            SelectionForm selector = new SelectionForm(m, "Выбор агентства для радиостанции " + m.Name, false, CheckAgencySelection);
            if (selector.ShowDialog(Globals.MdiParent) == DialogResult.OK)
                return ((MassmediaAgency)selector.SelectedObject).AgencyId;

			return null;
        }

        private bool CheckAgencySelection(SelectionForm selectionForm)
        {
            if (selectionForm.SelectedObject == null)
            {
                UserMessage.ShowExclamation(Properties.Resources.AgencyIsRequied);
                return false;
            }

            return true;
        }
    }
}
