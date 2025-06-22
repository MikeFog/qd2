using System.Collections.Generic;
using System.Data;
using System.Xml.XPath;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldDependentLookup : PageFieldLookUp, IDependentControl
	{
		private ParentLookupInfo parentLookupInfo;
		private PageControl sourceControl;
		private object selectedValue;

		internal PageFieldDependentLookup(XPathNavigator navigator, PageContext context, PageTypes type)
			: base(navigator, context, type)
		{
			SaveDataOnParentLookup(navigator);
		}

		private void SaveDataOnParentLookup(XPathNavigator navigator)
		{
			parentLookupInfo = new ParentLookupInfo(navigator);
		}

		public void SetSourceControl(Dictionary<string, PageControl> controls)
		{
			if(parentLookupInfo != null)
			{
				sourceControl = controls[parentLookupInfo.Name];
				sourceControl.ValueChanged += new EmptyDelegate(sourceControl_ValueChanged);

				sourceControl_ValueChanged();
				Lookup.SelectedValue = selectedValue;
			}
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			if(parameters.ContainsKey(Name))
				selectedValue = parameters[Name];
		}

		private void sourceControl_ValueChanged()
		{
			if(((PageFieldLookUp) sourceControl).Lookup.SelectedValue == null
				 || string.IsNullOrEmpty(((PageFieldLookUp)sourceControl).Lookup.SelectedValue.ToString()))
			{
				Lookup.DataSource = null;
			}
			else
			{
				string sourceLookupSelectedValue =
					((PageFieldLookUp) sourceControl).Lookup.SelectedValue.ToString();

				string filter = string.Format(parentLookupInfo.Filter, sourceLookupSelectedValue);
				DataView dv = dtData.DefaultView;
				dv.RowFilter = filter;
				Lookup.DataSource = dv;
			}
		}
	}
}