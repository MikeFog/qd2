using System;
using System.Data;
using System.Diagnostics;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Forms;

namespace Merlin.Forms
{
	public partial class SponsorTariffPassport : PassportForm
	{
		private CheckBox chkIsAlive;
		private const int controlsNum = 5;
		private readonly Control[] dependentsControls = new Control[controlsNum];
		private readonly string[] strControlsIDs = new string[controlsNum] { "path", "suffix", "needInJingle", "needOutJingle", "needExt" };

		public SponsorTariffPassport()
		{
			InitializeComponent();
		}

		public SponsorTariffPassport(PresentationObject presentationObject, DataSet ds)
			: base(presentationObject, ds)
		{
			InitializeComponent();
		}

		protected override void FormBuildCompleted()
		{
			base.FormBuildCompleted();

			chkIsAlive = FindControl("isAlive") as CheckBox;
			chkIsAlive.CheckedChanged += chkIsAlive_CheckedChanged;

			for (int i = 0; i < controlsNum; i++)
				dependentsControls[i] = FindControl(strControlsIDs[i]);

			updateDependentsFields();
		}

		private void chkIsAlive_CheckedChanged(object sender, EventArgs e)
		{
			updateDependentsFields();
		}

		private void updateDependentsFields()
		{
			for (int i = 0; i < controlsNum; i++)
				dependentsControls[i].Enabled = !chkIsAlive.Checked;
		}
	}
}