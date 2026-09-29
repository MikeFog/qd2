using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Classes;

namespace Merlin.Forms
{
	// Массовая смена типа оплаты для акции: выбираем целевой тип оплаты и
	// отмечаем галочками кампании акции. Применение - в
	// ActionOnMassmedia.ApplyPaymentTypeChangeMass (по одной кампании через
	// существующий CampaignIUD, best-effort + журнал ошибок).
	internal partial class ChangePaymentTypeMassForm : Form
	{
		private readonly ActionOnMassmedia _action;

		public ChangePaymentTypeMassForm()
		{
			InitializeComponent();
		}

		public ChangePaymentTypeMassForm(ActionOnMassmedia action) : this()
		{
			_action = action;
		}

		public int SelectedPaymentTypeId { get; private set; }

		public IList<PresentationObject> SelectedCampaigns { get; private set; }

		protected override void OnLoad(EventArgs e)
		{
			try
			{
				base.OnLoad(e);

				lookUpPaymentType.ColumnWithID = Campaign.ParamNames.PaymentTypeID;
				lookUpPaymentType.DataSource = ActionOnMassmedia.LoadActivePaymentTypes().DefaultView;

				grdCampaigns.Entity = ActionOnMassmedia.CampaignListEntity();
				grdCampaigns.DataSource = _action.Campaigns().DefaultView;

				UpdateOkEnabled();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void lookUpPaymentType_SelectedItemChanged(object sender, EventArgs e)
		{
			UpdateOkEnabled();
		}

		private void grdCampaigns_ObjectChecked(PresentationObject presentationObject, bool state)
		{
			UpdateOkEnabled();
		}

		private void UpdateOkEnabled()
		{
			btnOk.Enabled = lookUpPaymentType.SelectedValue != null && grdCampaigns.Added2Checked.Count > 0;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				SelectedPaymentTypeId = int.Parse(lookUpPaymentType.SelectedValue.ToString());
				SelectedCampaigns = new List<PresentationObject>(grdCampaigns.Added2Checked);
			}
			catch (Exception ex)
			{
				DialogResult = DialogResult.None;
				ErrorManager.PublishError(ex);
			}
		}
	}
}
