
namespace FogSoft.WinForm.Forms
{
	partial class SplashLogginForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.panelLgn = new System.Windows.Forms.Panel();
            this.pbLogin = new System.Windows.Forms.PictureBox();
            this.pbCancel = new System.Windows.Forms.PictureBox();
            this.textBoxPassword = new System.Windows.Forms.TextBox();
            this.textBoxLogin = new System.Windows.Forms.TextBox();
            this.labelPassword = new System.Windows.Forms.Label();
            this.labelUser = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.alphaFormTransformer = new FogSoft.WinForm.Win32.AlphaFormTransformer.AlphaFormTransformer();
            this.panelMain = new System.Windows.Forms.Panel();
            this.alphaFormMarker1 = new FogSoft.WinForm.Win32.AlphaFormTransformer.AlphaFormMarker();
            this.backgroundLoader = new System.ComponentModel.BackgroundWorker();
            this.panelLgn.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLogin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCancel)).BeginInit();
            this.alphaFormTransformer.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelLgn
            // 
            this.panelLgn.BackColor = System.Drawing.Color.Transparent;
            this.panelLgn.Controls.Add(this.pbLogin);
            this.panelLgn.Controls.Add(this.pbCancel);
            this.panelLgn.Controls.Add(this.textBoxPassword);
            this.panelLgn.Controls.Add(this.textBoxLogin);
            this.panelLgn.Controls.Add(this.labelPassword);
            this.panelLgn.Controls.Add(this.labelUser);
            this.panelLgn.Location = new System.Drawing.Point(220, 82);
            this.panelLgn.Name = "panelLgn";
            this.panelLgn.Size = new System.Drawing.Size(334, 129);
            this.panelLgn.TabIndex = 0;
            this.panelLgn.Visible = false;
            // 
            // pbLogin
            // 
            this.pbLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbLogin.Location = new System.Drawing.Point(145, 88);
            this.pbLogin.Name = "pbLogin";
            this.pbLogin.Size = new System.Drawing.Size(82, 22);
            this.pbLogin.TabIndex = 5;
            this.pbLogin.TabStop = false;
            this.pbLogin.Click += new System.EventHandler(this.Login_Click);
            this.pbLogin.MouseLeave += new System.EventHandler(this.Login_MouseLeave);
            this.pbLogin.MouseHover += new System.EventHandler(this.Login_MouseHover);
            // 
            // pbCancel
            // 
            this.pbCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.pbCancel.Location = new System.Drawing.Point(233, 88);
            this.pbCancel.Name = "pbCancel";
            this.pbCancel.Size = new System.Drawing.Size(82, 22);
            this.pbCancel.TabIndex = 4;
            this.pbCancel.TabStop = false;
            this.pbCancel.Click += new System.EventHandler(this.Cancel_Click);
            this.pbCancel.MouseLeave += new System.EventHandler(this.Cancel_MouseLeave);
            this.pbCancel.MouseHover += new System.EventHandler(this.Cancel_MouseHover);
            // 
            // textBoxPassword
            // 
            this.textBoxPassword.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxPassword.Location = new System.Drawing.Point(145, 62);
            this.textBoxPassword.MaxLength = 16;
            this.textBoxPassword.Name = "textBoxPassword";
            this.textBoxPassword.PasswordChar = '*';
            this.textBoxPassword.Size = new System.Drawing.Size(170, 27);
            this.textBoxPassword.TabIndex = 3;
            this.textBoxPassword.Text = "kjnjc0512";
            this.textBoxPassword.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            // 
            // textBoxLogin
            // 
            this.textBoxLogin.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.textBoxLogin.Location = new System.Drawing.Point(145, 33);
            this.textBoxLogin.MaxLength = 32;
            this.textBoxLogin.Name = "textBoxLogin";
            this.textBoxLogin.Size = new System.Drawing.Size(170, 27);
            this.textBoxLogin.TabIndex = 2;
            this.textBoxLogin.TextChanged += new System.EventHandler(this.TextBox_TextChanged);
            // 
            // labelPassword
            // 
            this.labelPassword.AutoSize = true;
            this.labelPassword.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelPassword.Location = new System.Drawing.Point(84, 63);
            this.labelPassword.Name = "labelPassword";
            this.labelPassword.Size = new System.Drawing.Size(78, 24);
            this.labelPassword.TabIndex = 1;
            this.labelPassword.Text = "Пароль";
            // 
            // labelUser
            // 
            this.labelUser.AutoSize = true;
            this.labelUser.Font = new System.Drawing.Font("Tahoma", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelUser.Location = new System.Drawing.Point(41, 34);
            this.labelUser.Name = "labelUser";
            this.labelUser.Size = new System.Drawing.Size(138, 24);
            this.labelUser.TabIndex = 0;
            this.labelUser.Text = "Пользователь";
            // 
            // lblStatus
            // 
            this.lblStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblStatus.Font = new System.Drawing.Font("Verdana", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblStatus.ForeColor = System.Drawing.Color.White;
            this.lblStatus.Location = new System.Drawing.Point(119, 214);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(433, 40);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "lblStatus";
            this.lblStatus.TextAlign = System.Drawing.ContentAlignment.BottomRight;
            this.lblStatus.Visible = false;
            // 
            // alphaFormTransformer
            // 
            this.alphaFormTransformer.Controls.Add(this.panelMain);
            this.alphaFormTransformer.Controls.Add(this.alphaFormMarker1);
            this.alphaFormTransformer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.alphaFormTransformer.DragSleep = ((uint)(30u));
            this.alphaFormTransformer.Location = new System.Drawing.Point(0, 0);
            this.alphaFormTransformer.Name = "alphaFormTransformer";
            this.alphaFormTransformer.Size = new System.Drawing.Size(578, 278);
            this.alphaFormTransformer.TabIndex = 0;
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.panelLgn);
            this.panelMain.Controls.Add(this.lblStatus);
            this.panelMain.Location = new System.Drawing.Point(12, 12);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(554, 254);
            this.panelMain.TabIndex = 2;
            // 
            // alphaFormMarker1
            // 
            this.alphaFormMarker1.FillBorder = ((uint)(4u));
            this.alphaFormMarker1.Location = new System.Drawing.Point(410, 140);
            this.alphaFormMarker1.Name = "alphaFormMarker1";
            this.alphaFormMarker1.Size = new System.Drawing.Size(26, 23);
            this.alphaFormMarker1.TabIndex = 0;
            // 
            // backgroundLoader
            // 
            this.backgroundLoader.WorkerReportsProgress = true;
            this.backgroundLoader.DoWork += new System.ComponentModel.DoWorkEventHandler(this.BackgroundLoader_DoWork);
            this.backgroundLoader.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.BackgroundLoader_ProgressChanged);
            this.backgroundLoader.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.BackgroundLoader_RunWorkerCompleted);
            // 
            // SplashLogginForm
            // 
            this.ClientSize = new System.Drawing.Size(578, 278);
            this.Controls.Add(this.alphaFormTransformer);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.KeyPreview = true;
            this.Name = "SplashLogginForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.SplashLogginForm_KeyDown);
            this.panelLgn.ResumeLayout(false);
            this.panelLgn.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLogin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbCancel)).EndInit();
            this.alphaFormTransformer.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panelLgn;
		private System.Windows.Forms.TextBox textBoxPassword;
		private System.Windows.Forms.TextBox textBoxLogin;
		private System.Windows.Forms.Label labelPassword;
		private System.Windows.Forms.Label labelUser;
		private System.Windows.Forms.PictureBox pbLogin;
		private System.Windows.Forms.PictureBox pbCancel;
		private System.Windows.Forms.Label lblStatus;
		private FogSoft.WinForm.Win32.AlphaFormTransformer.AlphaFormTransformer alphaFormTransformer;
		private FogSoft.WinForm.Win32.AlphaFormTransformer.AlphaFormMarker alphaFormMarker1;
		private System.Windows.Forms.Panel panelMain;
		private System.ComponentModel.BackgroundWorker backgroundLoader;
	}
}