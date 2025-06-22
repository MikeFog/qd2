using System.Xml.XPath;

namespace FogSoft.WinForm.Passport.Classes
{
	internal class ParentLookupInfo
	{
		public readonly string Name;
		public readonly string Filter;

		public ParentLookupInfo(XPathNavigator navigator)
		{
			Name = navigator.GetAttribute(PageControl.Attributes.ParentLookupName, "");
			Filter = navigator.GetAttribute(PageControl.Attributes.Filter, "");
		}
	}
}