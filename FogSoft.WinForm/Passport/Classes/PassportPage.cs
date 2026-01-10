using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.XPath;

namespace FogSoft.WinForm.Passport.Classes
{
	public class PassportPage
	{
		private TabPage tabPage;
		private readonly PageContext context;
		private int currentY;
		private PageDimensions dimensions;
		private readonly List<PageControl> pageControls = new List<PageControl>();
		private readonly Dictionary<string, PageControl> sourceControls = new Dictionary<string, PageControl>();

		public event EmptyDelegate ValueChanged;

		private PassportPage(PageContext context)
		{
			this.context = context;
		}

		public PassportPage(
			TabControl tabControl, XPathNavigator navigator,
			PageContext context, Image image)
			: this(context)
		{
			CreateTabPage(tabControl, navigator.GetAttribute(PageControl.Attributes.Caption, ""));
			InitDimensions();

			AddIcon(image, 5, 5);
			if(this.context.PresentationObject != null)
				AddObjectDescription(5 + image.Size.Width + 5, 5);
			AddSeparator();
			AddPageBody(navigator);
			SetControlValue();
			SetSourceControls();
		}

		public PassportPage(TabControl tabControl, XPathNavigator navigator, PageContext context)
			: this(context)
		{
			CreateTabPage(tabControl, navigator.GetAttribute(PageControl.Attributes.Caption, ""));
			InitDimensions();

			AddPageBody(navigator);
			SetControlValue();
		}

		public TabPage Page
		{
			get { return tabPage; }
		}

		public bool Enabled
		{
			set { tabPage.Enabled = value; }
		}

		public void ResetValues()
		{
			foreach (PageControl pageControl in pageControls)
			{
				pageControl.Clear();
			}
		}

		private void InitDimensions()
		{
			dimensions = new PageDimensions(tabPage);
		}

		private void SetControlValue()
		{
			foreach(PageControl pageControl in pageControls)
			{
				pageControl.SetValue(context.Parameters);
			}
		}

		private void SetSourceControls()
		{
			foreach(PageControl control in pageControls)
			{
				IDependentControl dependentControl = control as IDependentControl;
				if(dependentControl != null)
					dependentControl.SetSourceControl(sourceControls);
			}
		}

		private void AddPageBody(XPathNavigator navigator)
		{
			XPathNodeIterator nodes = navigator.SelectChildren(XPathNodeType.Element);
			while(nodes.MoveNext())
			{
				PageControl pageControl = PageControl.CreateInstance(nodes.Current, context);

                //if(pageControl == null) continue;

                pageControl.Add2Page(tabPage, PageDimensions.Offsets.LeftMargin, currentY, dimensions);

				pageControls.Add(pageControl);
				if(pageControl is IObjectSelector)
					sourceControls.Add(pageControl.Name, pageControl);

				pageControl.ValueChanged += new EmptyDelegate(pageControl_ValueChanged);

				//if (!this.context.PresentationObject.IsNew)
				//pageControl.SetValue(this.context.Parameters);

				UpdateCurrentY(pageControl);
			}
		}

		private void pageControl_ValueChanged()
		{
			if(ValueChanged != null) ValueChanged();
		}

		private void CreateTabPage(TabControl tabControl, string text)
		{
			tabPage = new TabPage(text) {Visible = false};
			tabControl.TabPages.Add(tabPage);
		}

		private void AddIcon(Image image, int left, int top)
		{
			PictureBox picture = new PictureBox
			                     	{
			                     		Image = image,
			                     		SizeMode = PictureBoxSizeMode.AutoSize,
			                     		Location = new Point(left, top)
			                     	};
			tabPage.Controls.Add(picture);
			currentY = picture.Top + picture.Height + PageDimensions.Offsets.LineSpacing;
		}

		private void AddObjectDescription(int left, int top)
		{
			PageLabel labelType = new PageLabel(GetString(context.PresentationObject.Entity.Name));
			labelType.Add2Page(tabPage, left, top, dimensions);
			labelType.OnAfterCreate();
			currentY += PageDimensions.Offsets.LineSpacing;

			if (!context.PresentationObject.IsNew && !string.IsNullOrEmpty(context.PresentationObject.Name))
			{
				PageLabel label = new PageLabel(string.Format("'{0}'", GetString(context.PresentationObject.Name)));
				label.SetBold();
				label.Add2Page(tabPage, left, top + labelType.Height + 5, dimensions);
				label.OnAfterCreate();
				currentY += PageDimensions.Offsets.LineSpacing;
			}
		}

		private static string GetString(string title)
		{
			const int i = 40;
			return title.Length > i ? string.Format("{0}...", title.Substring(0, i)) : title;
		}

		private void AddSeparator()
		{
			Separator separator = new Separator();
			separator.Add2Page(tabPage, PageDimensions.Offsets.LeftMargin/2, currentY, dimensions);
			currentY += PageDimensions.Offsets.LineSpacing;
			separator.OnAfterCreate();
		}

		private void UpdateCurrentY(PageControl control)
		{
			currentY += control.Height + PageDimensions.Offsets.LineSpacing;
		}

		public void ApplyChanges()
		{
			foreach(PageControl control in pageControls)
				control.ValidateUserInput();

			foreach(PageControl control in pageControls)
				control.ApplyChanges(context.Parameters);
		}

		internal PageControl FirstControl
		{
			get
			{
				if(pageControls.Count == 0) return null;
				return pageControls[0];
			}
		}
		
		public List<PageControl> PageControls
		{
			get { return pageControls; }
		}
	}
}