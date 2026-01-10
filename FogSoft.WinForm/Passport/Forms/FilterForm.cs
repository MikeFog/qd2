using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Classes;

namespace FogSoft.WinForm.Passport.Forms
{
	public partial class FilterForm : TabbedForm
	{
		public FilterForm()
		{
			InitializeComponent();
		}

		public FilterForm(Entity entity, DataSet ds, Dictionary<string, object> filter)
			: base(entity.XmlFilter)
		{
			pageContext = new PageContext(ds, filter, entity);
            btnApply.Visible = false;
        }

		public FilterForm(Entity entity, XPathNavigator navigator, DataSet ds, Dictionary<string, object> filter)
			: base(navigator)
		{
			pageContext = new PageContext(ds, filter, entity);
			btnApply.Visible = false;
		}

		protected override PassportPage CreatePage(XPathNavigator navigator)
		{
			return new PassportPage(tabPassport, navigator, pageContext);
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			foreach(PassportPage page in pages)
				page.ApplyChanges();
		}

		protected override void OnLoad(System.EventArgs e)
		{
			base.OnLoad(e);
			Text = "Установить фильтр";
        }
	}
}