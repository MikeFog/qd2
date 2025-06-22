using System;
using System.ComponentModel;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;

namespace Merlin.Forms
{
	public class FrmConfirmation : Form
	{
		private Label label1;
		private Label label2;
		private PictureBox pictureBox1;
		private Button btnCancel;
		private Button btnOk;
		private TextBox txtPassword;
		private TextBox txtLogin;
		private Label label3;
		private Label label4;

		private readonly Container components = null;
		private SecurityManager.User user;

		public FrmConfirmation()
		{
			InitializeComponent();
		}

		public FrmConfirmation(string title, string text)
		{
			InitializeComponent();
			Text = title;
			label1.Text = text;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
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
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof (FrmConfirmation));
			this.label1 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.pictureBox1 = new System.Windows.Forms.PictureBox();
			this.btnCancel = new System.Windows.Forms.Button();
			this.btnOk = new System.Windows.Forms.Button();
			this.txtPassword = new System.Windows.Forms.TextBox();
			this.txtLogin = new System.Windows.Forms.TextBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.Font =
				new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
				                        ((System.Byte) (204)));
			this.label1.Location = new System.Drawing.Point(8, 8);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(400, 48);
			this.label1.TabIndex = 0;
			this.label1.Text = "Вы не обладаете необходимыми привилегиями для выполнения данной операции.  Необхо" +
			                   "димо подтверждение от пользователя, с соответствующим уровнем привилегий.";
			// 
			// label2
			// 
			this.label2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label2.Location = new System.Drawing.Point(8, 56);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(400, 2);
			this.label2.TabIndex = 1;
			this.label2.Text = "label2";
			// 
			// pictureBox1
			// 
			this.pictureBox1.Image = ((System.Drawing.Image) (resources.GetObject("pictureBox1.Image")));
			this.pictureBox1.Location = new System.Drawing.Point(357, 80);
			this.pictureBox1.Name = "pictureBox1";
			this.pictureBox1.Size = new System.Drawing.Size(32, 32);
			this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
			this.pictureBox1.TabIndex = 14;
			this.pictureBox1.TabStop = false;
			// 
			// btnCancel
			// 
			this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnCancel.Font =
				new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
				                        ((System.Byte) (204)));
			this.btnCancel.Location = new System.Drawing.Point(212, 136);
			this.btnCancel.Name = "btnCancel";
			this.btnCancel.Size = new System.Drawing.Size(83, 22);
			this.btnCancel.TabIndex = 13;
			this.btnCancel.Text = "Отмена";
			// 
			// btnOk
			// 
			this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.btnOk.Enabled = false;
			this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.btnOk.Font =
				new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
				                        ((System.Byte) (204)));
			this.btnOk.Location = new System.Drawing.Point(124, 136);
			this.btnOk.Name = "btnOk";
			this.btnOk.Size = new System.Drawing.Size(84, 22);
			this.btnOk.TabIndex = 12;
			this.btnOk.Text = "Ок";
			this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
			// 
			// txtPassword
			// 
			this.txtPassword.Location = new System.Drawing.Point(128, 96);
			this.txtPassword.Name = "txtPassword";
			this.txtPassword.PasswordChar = '*';
			this.txtPassword.Size = new System.Drawing.Size(200, 21);
			this.txtPassword.TabIndex = 11;
			this.txtPassword.Text = "";
			this.txtPassword.TextChanged += new System.EventHandler(this.txtLoginOrPassword_TextChanged);
			// 
			// txtLogin
			// 
			this.txtLogin.Location = new System.Drawing.Point(128, 72);
			this.txtLogin.Name = "txtLogin";
			this.txtLogin.Size = new System.Drawing.Size(200, 21);
			this.txtLogin.TabIndex = 10;
			this.txtLogin.Text = "";
			this.txtLogin.TextChanged += new System.EventHandler(this.txtLoginOrPassword_TextChanged);
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(8, 98);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(46, 17);
			this.label3.TabIndex = 9;
			this.label3.Text = "Пароль:";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Font =
				new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
				                        ((System.Byte) (204)));
			this.label4.Location = new System.Drawing.Point(8, 74);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(80, 17);
			this.label4.TabIndex = 8;
			this.label4.Text = "Пользователь:";
			// 
			// FrmConfirmation
			// 
			this.AcceptButton = this.btnOk;
			this.AutoScaleDimensions = new System.Drawing.SizeF(5, 14);
			this.CancelButton = this.btnCancel;
			this.ClientSize = new System.Drawing.Size(418, 169);
			this.Controls.Add(this.pictureBox1);
			this.Controls.Add(this.btnCancel);
			this.Controls.Add(this.btnOk);
			this.Controls.Add(this.txtPassword);
			this.Controls.Add(this.txtLogin);
			this.Controls.Add(this.label3);
			this.Controls.Add(this.label4);
			this.Controls.Add(this.label2);
			this.Controls.Add(this.label1);
			this.Font =
				new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point,
				                        ((System.Byte) (204)));
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "FrmConfirmation";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Подтверждение";
			this.ResumeLayout(false);
		}

		#endregion

		private void btnOk_Click(object sender, EventArgs e)
		{
			user = SecurityManager.GetUser(txtLogin.Text, txtPassword.Text);
		}

		private void txtLoginOrPassword_TextChanged(object sender, EventArgs e)
		{
			btnOk.Enabled = IsOkButtonEnabled;
		}

		private bool IsOkButtonEnabled
		{
			get { return txtLogin.Text.Length > 0 && txtPassword.Text.Length > 0; }
		}

		public SecurityManager.User User
		{
			get { return user; }
		}
	}
}