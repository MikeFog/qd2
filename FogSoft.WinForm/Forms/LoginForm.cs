using System;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;

namespace FogSoft.WinForm.Forms
{
	public partial class LoginForm : Form
	{
		private readonly LoginCtl lgnCtl = new LoginCtl();

		public LoginForm()
		{
			InitializeComponent();
			Text = Application.ProductName;
		}

		protected override void OnLoad(EventArgs e)
		{
			if (Globals.MdiParent != null)
				Icon = Globals.MdiParent.Icon;

			base.OnLoad(e);
			try
			{
				if (!ConfigurationUtil.IsRegUserSave)
					return;

				string user, password;
				bool autologin;
				lgnCtl.GetRegUserSaves(out user, out password, out autologin);

				txtLogin.Text = user;
				txtPassword.Text = password;

				if (!String.IsNullOrEmpty(txtLogin.Text)
				    && !String.IsNullOrEmpty(txtPassword.Text)
					&& autologin)
				{
					btnOk.PerformClick();
				}
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			if(txtLogin.Text != string.Empty) txtPassword.Focus();
		}

		private void btnOk_Click(object sender, EventArgs e)
		{
			try
			{
				Cursor.Current = Cursors.WaitCursor;
				LoginCtl.LoginRes res = lgnCtl.Login(txtLogin.Text, txtPassword.Text).Res;
				if (res != LoginCtl.LoginRes.Ok)
				{
					DialogResult = (res == LoginCtl.LoginRes.Error) ? DialogResult.None : DialogResult.Cancel;
					if (res == LoginCtl.LoginRes.Error)
					{
						txtPassword.Text = string.Empty;
						txtPassword.Focus();
					}
				}
			}
			catch(Exception ex)
			{
				DialogResult = DialogResult.None;
				ErrorManager.PublishError(ex);
			}
		}

		private void TextBoxes_TextChanged(object sender, EventArgs e)
		{
			// Enable Ok button only if password and login are not empty
			btnOk.Enabled = txtLogin.Text.Length > 0 && txtPassword.Text.Length > 0;
		}
	}
}