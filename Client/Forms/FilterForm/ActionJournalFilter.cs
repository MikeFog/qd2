using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using Merlin.Classes;

namespace Merlin.Forms.FilterForm
{
	internal partial class ActionJournalFilter : FogSoft.WinForm.Passport.Forms.FilterForm
	{
		public ActionJournalFilter(Entity entity, DataSet ds, Dictionary<string, object> filter) 
			: base(entity, ds, filter)
		{
			InitializeComponent();
		}

		public ActionJournalFilter(Entity entity, XPathNavigator navigator, DataSet ds, Dictionary<string, object> filter) 
			: base(entity, navigator, ds, filter)
		{
			InitializeComponent();
		}

		private ObjectPicker2 opManager;

		protected override void OnLoad(System.EventArgs e)
		{
			base.OnLoad(e);

			opManager = FindControl("userID") as ObjectPicker2;
			CheckBox chbManager = FindControl("userID_checkbox") as CheckBox;
			opManager.Enabled = chbManager.Enabled = SecurityManager.LoggedUser.IsRightToViewForeignActions() || SecurityManager.LoggedUser.IsRightToViewGroupActions();
		}
	}
}