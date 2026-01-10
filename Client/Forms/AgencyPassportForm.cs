using System;
using System.Data;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Passport.Forms;
using Merlin.Classes;

namespace Merlin.Forms
{
	public partial class AgencyPassportForm : PassportForm
	{
		private bool _isEventsCreated = false;
		private readonly Agency _agency;

		public AgencyPassportForm(Agency ag, DataSet ds) : base(ag, ds)
		{
			InitializeComponent();
			_agency = ag;
		}
		
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);

			if (_isEventsCreated) return;

			foreach (PageControl ctl in pages[pages.Count-1].PageControls)
			{
                if (ctl is PageFieldImage ctlImage)
                {
                    ctlImage.SetImage(_agency.Signature);
                }
            }

			_isEventsCreated = true;
		}
	}
}