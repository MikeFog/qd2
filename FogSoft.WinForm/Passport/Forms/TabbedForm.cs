using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Classes;

namespace FogSoft.WinForm.Passport.Forms
{
	public partial class TabbedForm : Form
	{
		protected List<PassportPage> pages = new List<PassportPage>();
		protected XPathNavigator xmlPassport;
		protected PageContext pageContext;
		private bool isPassportCreated;

		public TabbedForm()
		{
			InitializeComponent();
		}

		protected TabbedForm(string xml)
			: this()
		{
			CreateXmlPassport(xml);
		}

		protected TabbedForm(XPathNavigator xml)
			: this()
		{
			xmlPassport = xml;
		}

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			if (isPassportCreated) return;

			isPassportCreated = true;
			try
			{
				Application.DoEvents();
				Cursor.Current = Cursors.WaitCursor;

				tabPassport.Visible = false;
				if (xmlPassport != null) BuildPassport();

				PlaceButtons();

				tabPassport.TabPages.RemoveAt(0);
				tabPassport.Visible = true;
				FormBuildCompleted();

				foreach (PassportPage page in pages)
					foreach (PageControl control in page.PageControls)
						control.OnAfterCreate();
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void PlaceButtons()
		{
			if (!btnApply.Visible)
			{
				btnOk.Location = new Point(btnCancel.Location.X, btnOk.Location.Y); 
				btnCancel.Location = new Point(btnApply.Location.X, btnCancel.Location.Y);
			}

			if (!btnCancel.Visible)
			{
				btnOk.Location = new Point(btnCancel.Location.X, btnOk.Location.Y); 
			}
		}

		protected virtual PassportPage CreatePage(XPathNavigator navigator)
		{
			return null;
		}

		protected Control FindControl(string name)
		{
			Control[] find = tabPassport.Controls.Find(name, true);
			return find.Length > 0 ? find[0] : null;
		}

		protected virtual void FormBuildCompleted() {}

		protected virtual void ApplyChanges(Button clickedButton) {}

		private void CreateXmlPassport(string xml)
		{
			Stream stream = new MemoryStream(
				Encoding.UTF8.GetBytes(xml)
				);

			xmlPassport = new XPathDocument(stream).CreateNavigator();
		}

		private void BuildPassport()
		{
			XPathNodeIterator pageNodes = xmlPassport.Select("/*/page");
			while(pageNodes.MoveNext())
			{
				PassportPage page = CreatePage(pageNodes.Current);
				if (page != null)
				{
					page.ValueChanged += OnPageValueChanged;
					pages.Add(page);
				}
			}

			if(pages.Count > 0)
				pages[0].FirstControl.Focus();

			if (IsReadonlyPassport(xmlPassport))
				btnOk.Visible = btnApply.Visible = false;
		}

		protected virtual bool IsReadonlyPassport(XPathNavigator navigator)
		{
			return false;
		}

		private void OnPageValueChanged()
		{
			if (!AlwaysAffectDisabled)
				btnApply.Enabled = true;
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				ApplyChanges(sender as Button);
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
				DialogResult = DialogResult.None;
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		public bool AlwaysAffectDisabled { get; set; }
	}
}