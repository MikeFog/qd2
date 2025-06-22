using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.XPath;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageButton : PageControl
	{
		public PageButton(XPathNavigator navigator) : base(new Button())
		{
			control.Text = navigator.GetAttribute(Attributes.Caption, "");
			control.Name = navigator.GetAttribute(Attributes.Name, "");
		}

		public override void Add2Page(Control parent, int left, int top, PageDimensions dimensions)
		{
			control.Size = new Size(dimensions.MaximumControlWidth, control.Height);
			base.Add2Page(parent, left, top, dimensions);
		}

		internal override void Focus() {}

		public override void SetValue(Dictionary<string, object> parameters) {}

		public override void ApplyChanges(Dictionary<string, object> parameters) {}
	}
}