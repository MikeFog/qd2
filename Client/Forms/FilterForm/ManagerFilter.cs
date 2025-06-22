using System;
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
	public partial class ManagerFilter : FogSoft.WinForm.Passport.Forms.FilterForm
	{
		private ManagerFilter(Entity entity, DataSet ds, Dictionary<string, object> filter)
			: base(entity, ds, filter)
		{
			InitializeComponent();
		}

		private ObjectPicker2 opManager;

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			opManager = (FindControl("managerID") ?? FindControl("userID")) as ObjectPicker2;

			CheckBox chbManager = (FindControl("managerID_checkbox") ?? FindControl("userID_checkbox")) as CheckBox;

			if (opManager != null && chbManager != null)
			{
				opManager.Enabled
					= chbManager.Enabled
					  = SecurityManager.LoggedUser.IsRightToViewForeignActions()
					    || SecurityManager.LoggedUser.IsRightToViewGroupActions();
			}
		}

		public static bool FilterClick(Form parent, Entity entity, XPathNavigator xmlFilter,
		                               Dictionary<string, object> filterValues)
		{
			if (xmlFilter != null)
				throw new NotImplementedException();

			ManagerFilter frm = new ManagerFilter(entity, Globals.PrepareForFilter(entity), filterValues);

			return (frm.ShowDialog(parent) == DialogResult.OK);
		}
	}
}