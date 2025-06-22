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
	public partial class SOActionJournalFilter : FogSoft.WinForm.Passport.Forms.FilterForm
	{
		public SOActionJournalFilter(Entity entity, DataSet ds, Dictionary<string, object> filter) 
			: base(entity, ds, filter)
		{
			InitializeComponent();
		}

		public SOActionJournalFilter(Entity entity, XPathNavigator navigator, DataSet ds, Dictionary<string, object> filter) 
			: base(entity, navigator, ds, filter)
		{
			InitializeComponent();
		}

		private ObjectPicker2 opManager;
		private LookUp luGroup;

		protected override void OnLoad(System.EventArgs e)
		{
			base.OnLoad(e);

			opManager = FindControl("userID") as ObjectPicker2;
			luGroup = FindControl("groupID") as LookUp;

			CheckBox chbManager = FindControl("userID_checkbox") as CheckBox;
			CheckBox chbGroup = FindControl("groupID_checkbox") as CheckBox;

			opManager.Enabled 
				= luGroup.Enabled
				  = chbManager.Enabled
				    = chbGroup.Enabled 
				      = SecurityManager.LoggedUser.IsRightToViewForeignSOActions()
				        || SecurityManager.LoggedUser.IsRightToViewGroupSOActions();
		}
	}
}