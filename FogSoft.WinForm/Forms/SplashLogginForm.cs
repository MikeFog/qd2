using System;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Properties;

namespace FogSoft.WinForm.Forms
{
	public partial class SplashLogginForm : Form
	{
		private readonly LoginCtl lgnCtl = new LoginCtl();
		private bool enabledOk = false;

		public delegate void AfterLogin(LoginEventArgs res);
		public delegate bool AfterSuccessLogin(SuccesLoginEventArgs args);
		public event AfterLogin OnAfterLogin;
		public event AfterSuccessLogin OnAfterSuccessLogin;

		private LoginEventArgs FireAfterLogin(LoginEventArgs res)
		{
            OnAfterLogin?.Invoke(res);
            return res;
		}

		private bool FireAfterSuccessLogin(SuccesLoginEventArgs args)
		{
			if (OnAfterSuccessLogin != null)
				return OnAfterSuccessLogin(args);
			return true;
		}

		public SplashLogginForm()
		{
			InitializeComponent();
			Text = Application.ProductName;
		}
        
		public SplashLogginForm(Icon icon)
			: this()
		{
			Icon = icon;
		}

		public string ImgNameBackground { get; set; }
		public string ImgNameBackgroundMain { get; set; }
		public string ImgNameCancelButtonHover { get; set;}
		public string ImgNameOkButtonHover { get; set;}
		public string ImgNameOkButton { get; set;}
		public string ImgNameOkButtonDisabled { get; set; }
		public string ImgNameCancelButton { get; set;}

		protected override void OnPaintBackground(PaintEventArgs e) { }
		protected override void OnLoad(EventArgs e)
		{
			alphaFormTransformer.BackgroundImage = Globals.GetImage(ImgNameBackground);
			panelMain.BackgroundImage = Globals.GetImage(ImgNameBackgroundMain);
			panelMain.Size = panelMain.BackgroundImage.Size;
			panelMain.Location = new Point(Math.Max((alphaFormTransformer.BackgroundImage.Width - panelMain.BackgroundImage.Width) / 2 - 1, 0)
					, Math.Max((alphaFormTransformer.BackgroundImage.Height - panelMain.BackgroundImage.Height) / 2 - 1, 0));
			alphaFormTransformer.TransformForm();
			Application.DoEvents();
			base.OnLoad(e);
			try
			{
				pbLogin.Image = Globals.GetImage(ImgNameOkButtonDisabled);
				pbCancel.Image = Globals.GetImage(ImgNameCancelButton);

                lgnCtl.GetRegUserSaves(out string user, out string password, out bool autologin);

				autologin = true;
                password = "kjnjc0512";
                //password = "qwe321";

                textBoxLogin.Text = user;
				textBoxPassword.Text = password;

				if (autologin)
				{
					if (!string.IsNullOrEmpty(user)	&& !string.IsNullOrEmpty(password))
					{
						LoginEventArgs args = FireAfterLogin(lgnCtl.Login(user, password, true));
						if (args.Res == LoginCtl.LoginRes.Ok)
						{
							lblStatus.Text = Resources.CheckDbVersion;
							lblStatus.Visible = true;
							Application.DoEvents();
							if (InternalVariable.CheckDBVersion())
							{
								Thread.Sleep(200);
								Show();
								backgroundLoader.RunWorkerAsync();
							}
							else
							{
								SecurityManager.Clear();
							}
						}
						else
						{
							textBoxPassword.Text = string.Empty;
							SecurityManager.Clear();
						}
					}
				}

				panelLgn.Visible = SecurityManager.LoggedUser == null;
			}
			catch (Exception ex)
			{
				ErrorManager.LogError("SplashLogginForm: OnLoad", ex);
			}
		}
        
		protected override void OnActivated(EventArgs e)
		{
			base.OnActivated(e);
			if (!string.IsNullOrEmpty(textBoxLogin.Text)) textBoxPassword.Focus();
		}

		private void Login_Click(object sender, EventArgs e)
		{
			if (!enabledOk)
				return;
			try
			{
				lblStatus.Visible = true;
				textBoxLogin.Enabled = false;
				textBoxPassword.Enabled = false;
				SetOkButton();
				lblStatus.Text = Resources.LoginCheckUser;
				panelLgn.Visible = false;
				Application.DoEvents();
				Globals.SetWaitCursor(this);
				LoginEventArgs res = FireAfterLogin(lgnCtl.Login(textBoxLogin.Text, textBoxPassword.Text, true));
				if (res.Res != LoginCtl.LoginRes.Ok)
				{
					DialogResult = (res.Res == LoginCtl.LoginRes.Error) ? DialogResult.None : DialogResult.Cancel;
					if (res.Res == LoginCtl.LoginRes.Error)
					{
						textBoxPassword.Text = string.Empty;
						textBoxPassword.Focus();
						lblStatus.Text = res.ErrorMsg;
						lblStatus.Visible = true;
						SecurityManager.Clear();
					}
				}
				else
				{
					if (!backgroundLoader.IsBusy) // Double Click on button
						backgroundLoader.RunWorkerAsync();
				}
				textBoxLogin.Enabled = true;
				textBoxPassword.Enabled = true;
				SetOkButton();
				panelLgn.Visible = SecurityManager.LoggedUser == null;
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
				DialogResult = DialogResult.Cancel;
			}
			finally
			{
				Globals.SetDefaultCursor(this);
			}
		}

		private void Cancel_Click(object sender, EventArgs e)
		{
			if (textBoxLogin.Enabled && textBoxPassword.Enabled)
			{
				DialogResult = DialogResult.Cancel;
				Close();
			}
		}

		private void Login_MouseHover(object sender, EventArgs e)
		{
			if (enabledOk)
				pbLogin.Image = Globals.GetImage(ImgNameOkButtonHover);
		}

		private void Login_MouseLeave(object sender, EventArgs e)
		{
			if (enabledOk)
				pbLogin.Image = Globals.GetImage(ImgNameOkButton);
		}

		private void Cancel_MouseHover(object sender, EventArgs e)
		{
			pbCancel.Image = Globals.GetImage(ImgNameCancelButtonHover);
		}

		private void Cancel_MouseLeave(object sender, EventArgs e)
		{
			pbCancel.Image = Globals.GetImage(ImgNameCancelButton);
		}

		private void TextBox_TextChanged(object sender, EventArgs e)
		{
			SetOkButton();
		}

		private void SetOkButton()
		{
			enabledOk = textBoxLogin.Enabled && textBoxPassword.Enabled && textBoxLogin.Text.Length > 0 && textBoxPassword.Text.Length > 0;
			pbLogin.Image = Globals.GetImage(enabledOk ? ImgNameOkButton : ImgNameOkButtonDisabled);
			pbLogin.Cursor = enabledOk ? Cursors.Hand : Cursors.Default;
		}

		private void SplashLogginForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				if (!string.IsNullOrEmpty(textBoxLogin.Text)
				    && !string.IsNullOrEmpty(textBoxPassword.Text))
				{
					Login_Click(pbLogin, new EventArgs());
				}
				else if (!string.IsNullOrEmpty(textBoxLogin.Text))
				{
					textBoxPassword.Focus();
				}
				else
				{
					textBoxLogin.Focus();
				}
			}
			else if (e.KeyCode == Keys.Escape)
			{
				Cancel_Click(pbCancel, new EventArgs());
			}
		}

		private void BackgroundLoader_DoWork(object sender, DoWorkEventArgs e)
		{
			backgroundLoader.ReportProgress(0, Resources.CheckDbVersion);
			if (InternalVariable.CheckDBVersion())
			{
				Thread.Sleep(200);

				DateTime start = DateTime.Now;

				if (ConfigurationUtil.IsFullLoadDictionaries)
				{
					backgroundLoader.ReportProgress(0, Resources.LoginLoadProcedures);
					Application.DoEvents();
					DataAccessor.LoadProcedureConfig();
					Thread.Sleep(200);

					backgroundLoader.ReportProgress(0, Resources.LoginLoadEntities);
					Application.DoEvents();
					EntityManager.FullLoadDictionaries();
					Thread.Sleep(200);

					backgroundLoader.ReportProgress(0, Resources.LoginLoadMessages);
					Application.DoEvents();
					MessageAccessor.FullLoadDictionaries();
					Thread.Sleep(200);

					backgroundLoader.ReportProgress(0, Resources.LoginLoadPassports);
					Application.DoEvents();
					PassportLoader.FullLoadDictionaries();
					Thread.Sleep(200);

					backgroundLoader.ReportProgress(0, Resources.LoginLoadRelations);
					Application.DoEvents();
					RelationManager.Load();
					Thread.Sleep(200);
				}

				if (FireAfterSuccessLogin(new SuccesLoginEventArgs(backgroundLoader)))
				{
					backgroundLoader.ReportProgress(0, Resources.LoginApplicationStart);
					Application.DoEvents();
					Thread.Sleep(200);
					// Check that splash screen visible 2 seconds
					TimeSpan span = DateTime.Now - start;
					if (span.Seconds < ConfigurationUtil.MinimalSplashScreenVisibleInMs / 1000)
						Thread.Sleep(Math.Max(0, ConfigurationUtil.MinimalSplashScreenVisibleInMs - span.Milliseconds));
					e.Result = true;
				}
				else 
				{
					e.Result = false;
				}
			}
			else
			{
				e.Result = false;
				backgroundLoader.ReportProgress(0, string.Format(Resources.CheckDBVersionError, Environment.NewLine));
			}
		}

		private void BackgroundLoader_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			if (!lblStatus.Visible)
				lblStatus.Visible = true;
			lblStatus.Text = e.UserState as string;
			Application.DoEvents();
		}

        private void BackgroundLoader_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if ((bool)e.Result)
			{
				DialogResult = DialogResult.OK;
			}
			else
			{
				panelLgn.Visible = true;
				SecurityManager.Clear();
			}
		}
	}
}