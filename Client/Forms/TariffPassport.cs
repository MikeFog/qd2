using System;
using System.Data;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;

namespace Merlin.Forms
{
	public partial class TariffPassport : PassportForm
	{
		public bool isInit = false;

		public TariffPassport(PresentationObject tariff, DataSet ds)
			: base(tariff, ds)
		{
			InitializeComponent();
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			if (!isInit)
			{
				CheckBox chkUnionEnable = (CheckBox) FindControl("isUnionEnable");
				chkUnionEnable.CheckedChanged += new EventHandler(chkUnionEnable_CheckedChanged);
				SetUnionsFieldsVisibility(chkUnionEnable.Checked);
			}
			isInit = true;
		}

		private void chkUnionEnable_CheckedChanged(object sender, EventArgs e)
		{
			CheckBox chkUnionEnable = (CheckBox)sender;
			SetUnionsFieldsVisibility(chkUnionEnable.Checked);
		}

		private void SetUnionsFieldsVisibility(bool visibly)
		{
			FindControl("tariffUnionID").Enabled = visibly;
		}
	}
}