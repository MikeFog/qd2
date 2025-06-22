using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldBoolean : PageControl
	{
		public PageFieldBoolean(XPathNavigator navigator)
			: base(new CheckBox(), navigator)
		{
			control.Text = navigator.GetAttribute(Attributes.Caption, "");
			((CheckBox) control).CheckedChanged += PageFieldBoolean_CheckedChanged;
			SetControlLockedFlag(navigator);
		}

		private void PageFieldBoolean_CheckedChanged(object sender, EventArgs e)
		{
			FireValueChanged();
		}

		public override void Add2Page(Control parent, int left, int top, PageDimensions dimensions)
		{
			control.Width = dimensions.MaximumControlWidth;
			control.Enabled = !isLocked;
			base.Add2Page(parent, left, top, dimensions);
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			if(parameters.ContainsKey(control.Name))
				((CheckBox) control).Checked = ParseHelper.ParseToBoolean(parameters[control.Name].ToString());
		}

		public override void ApplyChanges(Dictionary<string, object> parameters)
		{
			parameters[control.Name] = ((CheckBox) control).Checked;
		}

		internal override void Focus()
		{
			control.Focus();
		}
	}
}