using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;
using System.Xml.XPath;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Passport.Classes;
using FogSoft.WinForm.Properties;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace FogSoft.WinForm.Passport.Forms
{
	public partial class PassportForm : TabbedForm
	{
		private bool applyClicked = false;
		private bool formIsValid = false;
		private bool isNewObject;
		private readonly List<PassportPage> pagesToCreate = new List<PassportPage>();
		private readonly List<PassportPage> pagesToEdit = new List<PassportPage>();

		public PassportForm()
		{
			InitializeComponent();
		}

		public PassportForm(PresentationObject presentationObject, DataSet ds)
			: base(presentationObject.Entity.XmlPassport)
		{
            InitializeComponent();
            isNewObject = presentationObject.IsNew;
			pageContext = new PageContext(ds, presentationObject);
			SetFormCaption();
		}

		protected PassportForm(string xmlPassport)
			: base(xmlPassport) {}

		public bool IsApplyClicked
		{
			get { return applyClicked; }
		}

		public bool FormIsValid
		{
			get { return formIsValid; }
		}

		protected override PassportPage CreatePage(XPathNavigator navigator)
		{
			PageControl.ShowStatus status = PageControl.ShowStatus.always;
			string strStatus = navigator.GetAttribute(PageControl.Attributes.Show, "");
			if (!string.IsNullOrEmpty(strStatus))
			{
				if (Enum.IsDefined(typeof(PageControl.ShowStatus), strStatus))
					status = (PageControl.ShowStatus)Enum.Parse(typeof(PageControl.ShowStatus), strStatus);
			}
			bool needCreate = true;
			if ((status == PageControl.ShowStatus.create && !isNewObject)
				|| (status == PageControl.ShowStatus.edit && isNewObject))
				needCreate = false;
			PassportPage page = new PassportPage(tabPassport, navigator, pageContext, pageContext.PresentationObject == null || pageContext.PresentationObject.IsNew 
					? Resources.New : Resources.Edit);
			if (isNewObject)
			{
				if (status == PageControl.ShowStatus.create)
					pagesToCreate.Add(page);
				if (status == PageControl.ShowStatus.edit)
					pagesToEdit.Add(page);
			}
			if (!needCreate)
				tabPassport.TabPages.Remove(page.Page);
			return needCreate ?  page : null;
		}

		private void SetFormCaption()
		{
			Text = pageContext.PresentationObject.IsNew ? string.Format("Новый: {0}", pageContext.PresentationObject.Entity.Name) 
				: string.Format("Cвойства: {0}", pageContext.PresentationObject.Name);
		}

		protected override bool IsReadonlyPassport(XPathNavigator navigator)
		{
			string attribute = navigator.SelectSingleNode("/passport").GetAttribute("readonly", "");
			if(attribute == string.Empty)
				return false;
			return ParseHelper.ParseToBoolean(attribute);
		}

		protected override void ApplyChanges(Button clickedButton)
		{
			int pageIndex = 0;
			try
			{
				foreach(PassportPage page in pages)
				{
					page.ApplyChanges();
					pageIndex++;
				}

				UpdateObjectParameters();

				if (!pageContext.PresentationObject.Update())
				{
					DialogResult = DialogResult.None;
					return;
				}

				if(clickedButton == AcceptButton)
				{
					DialogResult = DialogResult.OK;
				}
				else
				{
					pageContext.Parameters = pageContext.PresentationObject.Parameters;
					applyClicked = true;
					btnApply.Enabled = false;
				}

				if(clickedButton == btnOk)
					DialogResult = DialogResult.OK;
				formIsValid = true;

				if (isNewObject)
				{
					foreach (PassportPage page in pagesToCreate)
					{
						tabPassport.TabPages.Remove(page.Page);
						pages.Remove(page);
					}
					foreach (PassportPage page in pagesToEdit)
					{
						tabPassport.TabPages.Add(page.Page);
						pages.Add(page);
					}
					pagesToCreate.Clear();
					pagesToEdit.Clear();
					isNewObject = false;
				}
			}
			catch(PassportException ex)
			{
				DialogResult = DialogResult.None;
				tabPassport.SelectedIndex = pageIndex;
				ex.NativeControl.Select();
				formIsValid = false;
				MessageBox.ShowExclamation(ex.Message);
			}
		}

		private void UpdateObjectParameters()
		{
			bool isNewObject = pageContext.PresentationObject.IsNew;
			pageContext.PresentationObject.Parameters = pageContext.Parameters;
			pageContext.PresentationObject.IsNew = isNewObject;
		}
	}
}