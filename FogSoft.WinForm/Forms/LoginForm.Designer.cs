namespace FogSoft.WinForm.Forms
{
  partial class LoginForm
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
      if(disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
		this.pictureBoxMain = new System.Windows.Forms.PictureBox();
		this.btnCancel = new System.Windows.Forms.Button();
		this.btnOk = new System.Windows.Forms.Button();
		this.txtPassword = new System.Windows.Forms.TextBox();
		this.txtLogin = new System.Windows.Forms.TextBox();
		this.label2 = new System.Windows.Forms.Label();
		this.label1 = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)(this.pictureBoxMain)).BeginInit();
		this.SuspendLayout();
		// 
		// pictureBoxMain
		// 
		this.pictureBoxMain.Image = ((System.Drawing.Image)(resources.GetObject("pictureBoxMain.Image")));
		this.pictureBoxMain.Location = new System.Drawing.Point(328, 12);
		this.pictureBoxMain.Name = "pictureBoxMain";
		this.pictureBoxMain.Size = new System.Drawing.Size(32, 32);
		this.pictureBoxMain.SizeMode = System.Windows.Forms.PictureBoxSizeMode.AutoSize;
		this.pictureBoxMain.TabIndex = 14;
		this.pictureBoxMain.TabStop = false;
		// 
		// btnCancel
		// 
		this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
		this.btnCancel.FlatStyle = System.Windows.Forms.FlatStyle.System;
		this.btnCancel.Location = new System.Drawing.Point(186, 69);
		this.btnCancel.Name = "btnCancel";
		this.btnCancel.Size = new System.Drawing.Size(109, 24);
		this.btnCancel.TabIndex = 13;
		this.btnCancel.Text = "Отмена";
		// 
		// btnOk
		// 
		this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
		this.btnOk.Enabled = false;
		this.btnOk.FlatStyle = System.Windows.Forms.FlatStyle.System;
		this.btnOk.Location = new System.Drawing.Point(72, 69);
		this.btnOk.Name = "btnOk";
		this.btnOk.Size = new System.Drawing.Size(109, 24);
		this.btnOk.TabIndex = 12;
		this.btnOk.Text = "Ок";
		this.btnOk.Click += new System.EventHandler(this.btnOk_Click);
		// 
		// txtPassword
		// 
		this.txtPassword.Location = new System.Drawing.Point(101, 37);
		this.txtPassword.Name = "txtPassword";
		this.txtPassword.PasswordChar = '*';
		this.txtPassword.Size = new System.Drawing.Size(200, 21);
		this.txtPassword.TabIndex = 11;
		this.txtPassword.TextChanged += new System.EventHandler(this.TextBoxes_TextChanged);
		// 
		// txtLogin
		// 
		this.txtLogin.Location = new System.Drawing.Point(101, 12);
		this.txtLogin.Name = "txtLogin";
		this.txtLogin.Size = new System.Drawing.Size(200, 21);
		this.txtLogin.TabIndex = 10;
		this.txtLogin.TextChanged += new System.EventHandler(this.TextBoxes_TextChanged);
		// 
		// label2
		// 
		this.label2.AutoSize = true;
		this.label2.Location = new System.Drawing.Point(12, 40);
		this.label2.Name = "label2";
		this.label2.Size = new System.Drawing.Size(48, 13);
		this.label2.TabIndex = 9;
		this.label2.Text = "Пароль:";
		// 
		// label1
		// 
		this.label1.AutoSize = true;
		this.label1.Location = new System.Drawing.Point(12, 14);
		this.label1.Name = "label1";
		this.label1.Size = new System.Drawing.Size(83, 13);
		this.label1.TabIndex = 8;
		this.label1.Text = "Пользователь:";
		// 
		// LoginForm
		// 
		this.AcceptButton = this.btnOk;
		this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
		this.CancelButton = this.btnCancel;
		this.ClientSize = new System.Drawing.Size(367, 103);
		this.Controls.Add(this.pictureBoxMain);
		this.Controls.Add(this.btnCancel);
		this.Controls.Add(this.btnOk);
		this.Controls.Add(this.txtPassword);
		this.Controls.Add(this.txtLogin);
		this.Controls.Add(this.label2);
		this.Controls.Add(this.label1);
		this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
		this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
		this.MaximizeBox = false;
		this.MinimizeBox = false;
		this.Name = "LoginForm";
		this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
		this.Text = "Quenn Diamond II";
		((System.ComponentModel.ISupportInitialize)(this.pictureBoxMain)).EndInit();
		this.ResumeLayout(false);
		this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.PictureBox pictureBoxMain;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.TextBox txtPassword;
    private System.Windows.Forms.TextBox txtLogin;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label1;
  }
}