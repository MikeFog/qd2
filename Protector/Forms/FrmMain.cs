using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Controls;
using FogSoft.WinForm.DataAccess;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Win32;
using Protector.Domain;
using Protector.Properties;
using System.Text;

namespace Protector.Forms
{
	public class FrmMain : Form
	{
        private const int WORKINGDB_VERSION = 33;

		public struct RelationScenarios
		{
			public const string Group = "Group";
			public const string MenuItem = "Пункты меню";
		}

		private const string RefreshAlias = "Обновить";
		private readonly bool exitFlag;
		private MenuStrip MDIMenu;
		private ToolStripMenuItem miTop;
		private ToolStripMenuItem miUsers;
		private ToolStripMenuItem miGroups;
		private ToolStripSeparator toolStripMenuItem1;
		private ToolStripMenuItem miExit;
		private ToolStripMenuItem tlsmAdministration;
		private ToolStripMenuItem miAbout;
		private ToolStripMenuItem tsmiLicense;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripMenuItem miChangePassAdmin;
		private ToolStripMenuItem miSysParams;
		private ToolStripMenuItem tsmiRecalculateTariffWindows;

		private readonly MdiClientController mdiClientController = new MdiClientController();

		public FrmMain()
		{
			InitializeComponent();
			mdiClientController.ParentForm = this;
			mdiClientController.BackColor = Color.FromArgb(204, 204, 204);
			mdiClientController.Paint += new PaintEventHandler(mdiClientController_Paint);
			Globals.DBVersion = WORKINGDB_VERSION;
			SplashLogginForm fLogin = new SplashLogginForm (Icon)
			                          	{
			                          		ImgNameBackground = "Resources.Splash.png",
			                          		ImgNameBackgroundMain = "Resources.SplashMain.png", 
			                          		ImgNameCancelButtonHover = "Resources.cancelhover.gif",
			                          		ImgNameOkButtonHover = "Resources.okhover.gif",
			                          		ImgNameOkButtonDisabled = "Resources.okdisabled.gif",
			                          		ImgNameOkButton = "Resources.ok.gif",
			                          		ImgNameCancelButton = "Resources.cancel.gif"
			                          	};
			Globals.MdiParent = fLogin;
			fLogin.OnAfterLogin += CheckUser;
			if (fLogin.ShowDialog() != DialogResult.OK
			    || !SecurityManager.LoggedUser.IsAdmin)
			{
				Globals.MdiParent = this;
				exitFlag = true;
				return;
			}
			Globals.MdiParent = this;
			SetFormCaption();
			Globals.IconLoader = Globals.GetIcon;
			miUsers.Image = Globals.GetImage("Icons.User.png");
			miExit.Image = Globals.GetImage("Icons.Exit.png");
			miTop.Image = Globals.GetImage("Icons.User.png");
			tlsmAdministration.Image = Globals.GetImage("Icons.Administration.png");
		}

		public void CheckUser(LoginEventArgs args)
		{
			if (args.Res == LoginCtl.LoginRes.Ok)
			{
				if (SecurityManager.LoggedUser != null && !SecurityManager.LoggedUser.IsAdmin)
				{
					args.Res = LoginCtl.LoginRes.Error;
					SecurityManager.Clear();
					args.ErrorMsg = string.Format(Resources.NotAdminLogin, Environment.NewLine);
				}
			}
		}

		private void SetFormCaption()
		{
            StringBuilder titleBuilder = new StringBuilder();
            titleBuilder.AppendFormat("{0} ", Application.ProductName);
            if (!string.IsNullOrEmpty(ConfigurationUtil.Title))
            {
                titleBuilder.AppendFormat("- {0} - ", ConfigurationUtil.Title);
            }
            titleBuilder.AppendFormat("[{0} {1}]", SecurityManager.LoggedUser.LastName, SecurityManager.LoggedUser.FirstName);
            Text = titleBuilder.ToString();
		}


		protected override void OnLoad(EventArgs e)
		{
			try
			{
				if (exitFlag)
				{
					ApplicationExit();
					return;
				}
				base.OnLoad(e);
				MenuManager.CreateMDIListMenu(MDIMenu);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private static void ApplicationExit()
		{
			Application.Exit();
		}

		protected override void Dispose(bool disposing)
		{
			ExportManager.OnAppQuit();
			TempFileWorker.Clear();
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmMain));
            this.MDIMenu = new System.Windows.Forms.MenuStrip();
            this.miTop = new System.Windows.Forms.ToolStripMenuItem();
            this.miUsers = new System.Windows.Forms.ToolStripMenuItem();
            this.miGroups = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.miChangePassAdmin = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.miExit = new System.Windows.Forms.ToolStripMenuItem();
            this.tlsmAdministration = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiRecalculateTariffWindows = new System.Windows.Forms.ToolStripMenuItem();
            this.miSysParams = new System.Windows.Forms.ToolStripMenuItem();
            this.tsmiLicense = new System.Windows.Forms.ToolStripMenuItem();
            this.miAbout = new System.Windows.Forms.ToolStripMenuItem();
            this.MDIMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // MDIMenu
            // 
            this.MDIMenu.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.MDIMenu.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.MDIMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miTop,
            this.tlsmAdministration,
            this.miAbout});
            this.MDIMenu.Location = new System.Drawing.Point(0, 0);
            this.MDIMenu.Name = "MDIMenu";
            this.MDIMenu.Size = new System.Drawing.Size(792, 33);
            this.MDIMenu.TabIndex = 1;
            this.MDIMenu.Text = "menuStrip1";
            // 
            // miTop
            // 
            this.miTop.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miUsers,
            this.miGroups,
            this.toolStripSeparator2,
            this.miChangePassAdmin,
            this.toolStripMenuItem1,
            this.miExit});
            this.miTop.Name = "miTop";
            this.miTop.Size = new System.Drawing.Size(143, 29);
            this.miTop.Text = "Пользователи";
            // 
            // miUsers
            // 
            this.miUsers.Name = "miUsers";
            this.miUsers.Size = new System.Drawing.Size(426, 34);
            this.miUsers.Text = "Журнал пользователей";
            this.miUsers.Click += new System.EventHandler(this.miUsers_Click);
            // 
            // miGroups
            // 
            this.miGroups.Name = "miGroups";
            this.miGroups.Size = new System.Drawing.Size(426, 34);
            this.miGroups.Text = "Группы и пользователи";
            this.miGroups.Click += new System.EventHandler(this.miGroups_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(423, 6);
            // 
            // miChangePassAdmin
            // 
            this.miChangePassAdmin.Name = "miChangePassAdmin";
            this.miChangePassAdmin.Size = new System.Drawing.Size(426, 34);
            this.miChangePassAdmin.Text = "Сменить пароль пользователя \'admin\'";
            this.miChangePassAdmin.Click += new System.EventHandler(this.miChangePassAdmin_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(423, 6);
            // 
            // miExit
            // 
            this.miExit.Name = "miExit";
            this.miExit.Size = new System.Drawing.Size(426, 34);
            this.miExit.Text = "Выход";
            this.miExit.Click += new System.EventHandler(this.miExit_Click);
            // 
            // tlsmAdministration
            // 
            this.tlsmAdministration.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tsmiRecalculateTariffWindows,
            this.miSysParams,
            this.tsmiLicense});
            this.tlsmAdministration.Name = "tlsmAdministration";
            this.tlsmAdministration.Size = new System.Drawing.Size(199, 29);
            this.tlsmAdministration.Text = "Администрирование";
            // 
            // tsmiRecalculateTariffWindows
            // 
            this.tsmiRecalculateTariffWindows.Name = "tsmiRecalculateTariffWindows";
            this.tsmiRecalculateTariffWindows.Size = new System.Drawing.Size(349, 34);
            this.tsmiRecalculateTariffWindows.Text = "Пересчитать остатки в окнах";
            this.tsmiRecalculateTariffWindows.Click += new System.EventHandler(this.tsmiRecalculateTariffWindow_Click);
            // 
            // miSysParams
            // 
            this.miSysParams.Name = "miSysParams";
            this.miSysParams.Size = new System.Drawing.Size(349, 34);
            this.miSysParams.Text = "Настройки...";
            this.miSysParams.Click += new System.EventHandler(this.miSysParams_Click);
            // 
            // tsmiLicense
            // 
            this.tsmiLicense.Name = "tsmiLicense";
            this.tsmiLicense.Size = new System.Drawing.Size(349, 34);
            this.tsmiLicense.Text = "Лицензия...";
            this.tsmiLicense.Visible = false;
            this.tsmiLicense.Click += new System.EventHandler(this.tsmiLicense_Click);
            // 
            // miAbout
            // 
            this.miAbout.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
            this.miAbout.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.miAbout.Image = global::Protector.Properties.Resources.about;
            this.miAbout.Name = "miAbout";
            this.miAbout.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.miAbout.Size = new System.Drawing.Size(40, 29);
            this.miAbout.Click += new System.EventHandler(this.miAbout_Click);
            // 
            // FrmMain
            // 
            this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(204)))), ((int)(((byte)(204)))));
            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.ClientSize = new System.Drawing.Size(792, 553);
            this.Controls.Add(this.MDIMenu);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.IsMdiContainer = true;
            this.MainMenuStrip = this.MDIMenu;
            this.MinimumSize = new System.Drawing.Size(620, 460);
            this.Name = "FrmMain";
            this.Text = "Lord Protector";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.MDIMenu.ResumeLayout(false);
            this.MDIMenu.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private void miUsers_Click(object sender, EventArgs e)
		{
			try
			{
				Globals.ShowSimpleJournal(
					EntityManager.GetEntity((int) Entities.User), ((ToolStripMenuItem) sender).Text);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		private void miGroups_Click(object sender, EventArgs e)
		{
			try
			{
				Entity.Action[] menu = new[]
				                       	{
				                       		new Entity.Action(Constants.EntityActions.Refresh, RefreshAlias, Constants.ActionsImages.Refresh),
				                       		new Entity.Action(Constants.EntityActions.AddNew, "Создать новую группу")
				                       	};

				FakeContainer container =
					new FakeContainer("Группы пользователей", menu, RelationManager.GetScenario(RelationScenarios.Group));
				Globals.ShowBrowser(container, ((ToolStripMenuItem) sender).Text, this);
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void miExit_Click(object sender, EventArgs e)
		{
			Application.Exit();
		}

		private void tsmiRecalculateTariffWindow_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Globals.SetWaitCursor(this);
				ProgressForm.Show(this, RecalculateWindows, "Пересчитываются остатки...", null);
				Globals.ShowInfo("TariffWindowsRecalculates");
			}
			catch (Exception exp)
			{
				ErrorManager.PublishError(exp);
			}
			finally
			{
				Globals.SetDefaultCursor(this);
			}
		}

		private static void RecalculateWindows(object sender, System.ComponentModel.DoWorkEventArgs e)
		{
			DataAccessor.ExecuteNonQuery("job_DeleteEmptyActions", new Dictionary<string, object>());
		}

		#region Paint MDI Client Area

		private static Image RightImage
		{
			get { return Globals.GetImage("Resources.rightImage.jpg"); }
		}

		private static Image LeftImage
		{
			get { return Globals.GetImage("Resources.leftImageAdmin.png"); }
		}

		private static void mdiClientController_Paint(object sender, PaintEventArgs e)
		{
			Image imgRight = RightImage;
			if (imgRight != null)
				e.Graphics.DrawImage(imgRight, e.ClipRectangle.Right - imgRight.Size.Width, e.ClipRectangle.Bottom - imgRight.Size.Height, imgRight.Size.Width,
				                     imgRight.Size.Height);
			Image imgLeft = LeftImage;
			if (imgLeft != null)
				e.Graphics.DrawImage(imgLeft, 30, e.ClipRectangle.Bottom - imgLeft.Size.Height - 30, imgLeft.Size.Width, imgLeft.Size.Height);
		}

		#endregion

		private void miAbout_Click(object sender, EventArgs e)
		{
			About frm = new About { ImgNameBackground = "Resources.Splash.png", ImgNameBackgroundMain = "Resources.SplashMain.png" };
			frm.ShowDialog(this);
		}

		private void tsmiLicense_Click(object sender, EventArgs e)
		{
			LicenseLoader loader = new LicenseLoader();
			loader.ShowDialog(this);
		}

		private void miChangePassAdmin_Click(object sender, EventArgs e)
		{
			ChangePasswordFrm frm = new ChangePasswordFrm();
			frm.ShowDialog(this);
		}

		private void miSysParams_Click(object sender, EventArgs e)
		{
			PresentationObject po = new PresentationObject(EntityManager.GetEntity((int)Entities.SysParam)) {IsNew = false};
			po.Refresh();
			po.ShowPassport(this);
		}
	}
}