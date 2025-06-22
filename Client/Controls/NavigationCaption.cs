using System;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;

namespace Merlin.Controls
{
	public partial class NavigationCaption : UserControl
	{
		public event EmptyDelegate GoPrevious;
		public event EmptyDelegate GoNext;

		public NavigationCaption()
		{
			InitializeComponent();
		}

		public string Caption
		{
			set { lblCaption.Text = value; }
		}

		public Color CaptionBackColor
		{
			set { lblCaption.BackColor = value; }
		}

		private void btnGoLeft_Click(object sender, EventArgs e)
		{
			if (GoPrevious != null)
				GoPrevious();
		}

		private void btnGoRight_Click(object sender, EventArgs e)
		{
			if (GoNext != null)
				GoNext();
		}
	}
}