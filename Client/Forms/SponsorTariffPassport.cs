using System;
using System.Collections.Generic;
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
		private readonly IList<Control> _dependentsControls = new List<Control>();
		private readonly string[] _strControlsIDs = new string[] { "path", "suffix", "needInJingle", "needOutJingle" };

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

			for (int i = 0; i < _strControlsIDs.Length; i++)
				_dependentsControls.Add(FindControl(_strControlsIDs[i]));

			updateDependentsFields();
		}

		private void chkIsAlive_CheckedChanged(object sender, EventArgs e)
		{
			updateDependentsFields();
		}

		private void updateDependentsFields()
		{
			for (int i = 0; i < _dependentsControls.Count; i++)
				_dependentsControls[i].Enabled = !chkIsAlive.Checked;
		}
	}
}