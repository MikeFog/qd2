using System.Collections.Generic;
using System.Xml.XPath;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class PageFieldDependent : PageFieldText, IDependentControl
	{
		private readonly string sourceControlName;
		private IObjectSelector selector;

		public PageFieldDependent(XPathNavigator navigator, PageTypes pageType)
			: base(navigator, pageType)
		{
			TextBox.ReadOnly = true;
			sourceControlName = GetSourceControlName(navigator);
		}

		private static string GetSourceControlName(XPathNavigator navigator)
		{
			return navigator.GetAttribute(Attributes.Source, "");
		}

		public override void SetValue(Dictionary<string, object> parameters) {}

		public override void ApplyChanges(Dictionary<string, object> parameters) {}

		public void SetSourceControl(Dictionary<string, PageControl> controls)
		{
			PageControl sourceControl = controls[sourceControlName];
			sourceControl.ValueChanged += new EmptyDelegate(SourceControl_ValueChanged);
			selector = sourceControl as IObjectSelector;
			SourceControl_ValueChanged();
		}

		private void SourceControl_ValueChanged()
		{
			TextBox.Text = selector.GetSelectedObjectValue(Name);
		}
	}
}