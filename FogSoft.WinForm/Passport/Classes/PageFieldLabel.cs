using System.Collections.Generic;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldLabel : PageField
	{
		private readonly FieldTypeResolver typeResolver;

		public PageFieldLabel(XPathNavigator navigator, PageContext context)
			: base(navigator, new Label(), context.PageType)
		{
			typeResolver = GetTypeResolver(navigator, GetColumnInfo(navigator, context));
		}

		public override void SetValue(Dictionary<string, object> parameters)
		{
			if (parameters.ContainsKey(Name))
				controlInRightColumn.Text = typeResolver.IsMoney ? decimal.Parse(parameters[Name].ToString()).ToString("c") : parameters[Name].ToString();
		}

		public override void ApplyChanges(Dictionary<string, object> parameters) {}
	}
}