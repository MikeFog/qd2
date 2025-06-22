using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FogSoft.WinForm.Forms
{
	public partial class About : Form
	{
		public About()
		{
			InitializeComponent();
		}

		public string ImgNameBackground { get; set; }
		public string ImgNameBackgroundMain { get; set; }

		protected override void OnLoad(EventArgs e)
		{
			alphaFormTransformer.BackgroundImage = Globals.GetImage(ImgNameBackground);
			panelMain.BackgroundImage = Globals.GetImage(ImgNameBackgroundMain);
			panelMain.Size = panelMain.BackgroundImage.Size;
			panelMain.Location = new Point(Math.Max((alphaFormTransformer.BackgroundImage.Width - panelMain.BackgroundImage.Width) / 2 - 1, 0)
					, Math.Max((alphaFormTransformer.BackgroundImage.Height - panelMain.BackgroundImage.Height) / 2 - 1, 0));
			alphaFormTransformer.TransformForm();
			lblDbVersionValue.Text = Globals.DBVersion.ToString();
			lblVersionValue.Text = Assembly.GetEntryAssembly().GetName().Version.ToString();
			base.OnLoad(e);
		}

		private void alphaFormTransformer_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void lblUrl_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			System.Diagnostics.Process.Start(lblUrl.Text);
		}

		private void Form_KeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			if (e.KeyCode == Keys.Escape)
			{
				Close();
			}
		}

	}
}
