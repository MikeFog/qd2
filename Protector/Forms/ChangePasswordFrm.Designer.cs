namespace Protector.Forms
{
	partial class ChangePasswordFrm
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
			this.buttonOk = new System.Windows.Forms.Button();
			this.buttonCancel = new System.Windows.Forms.Button();
			this.textBoxPassword = new System.Windows.Forms.TextBox();
			this.textBoxConfirm = new System.Windows.Forms.TextBox();
			this.labelNewPassword = new System.Windows.Forms.Label();
			this.lblConfirmNew = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// buttonOk
			// 
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.buttonOk.Location = new System.Drawing.Point(180, 112);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(75, 23);
			this.buttonOk.TabIndex = 0;
			this.buttonOk.Text = "Сменить";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.buttonCancel.Location = new System.Drawing.Point(280, 112);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(75, 23);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Отмена";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// textBoxPassword
			// 
			this.textBoxPassword.Font = new System.Drawing.Font("Verdana", 8.25F);
			this.textBoxPassword.Location = new System.Drawing.Point(141, 25);
			this.textBoxPassword.MaxLength = 16;
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.PasswordChar = '*';
			this.textBoxPassword.Size = new System.Drawing.Size(214, 21);
			this.textBoxPassword.TabIndex = 2;
			// 
			// textBoxConfirm
			// 
			this.textBoxConfirm.Font = new System.Drawing.Font("Verdana", 8.25F);
			this.textBoxConfirm.Location = new System.Drawing.Point(141, 64);
			this.textBoxConfirm.MaxLength = 16;
			this.textBoxConfirm.Name = "textBoxConfirm";
			this.textBoxConfirm.PasswordChar = '*';
			this.textBoxConfirm.Size = new System.Drawing.Size(214, 21);
			this.textBoxConfirm.TabIndex = 3;
			// 
			// labelNewPassword
			// 
			this.labelNewPassword.AutoSize = true;
			this.labelNewPassword.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.labelNewPassword.Location = new System.Drawing.Point(29, 28);
			this.labelNewPassword.Name = "labelNewPassword";
			this.labelNewPassword.Size = new System.Drawing.Size(83, 13);
			this.labelNewPassword.TabIndex = 4;
			this.labelNewPassword.Text = "Новый пароль:";
			// 
			// lblConfirmNew
			// 
			this.lblConfirmNew.AutoSize = true;
			this.lblConfirmNew.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.lblConfirmNew.Location = new System.Drawing.Point(29, 67);
			this.lblConfirmNew.Name = "lblConfirmNew";
			this.lblConfirmNew.Size = new System.Drawing.Size(94, 13);
			this.lblConfirmNew.TabIndex = 6;
			this.lblConfirmNew.Text = "Подтверждение:";
			// 
			// ChangePasswordFrm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(389, 160);
			this.Controls.Add(this.lblConfirmNew);
			this.Controls.Add(this.labelNewPassword);
			this.Controls.Add(this.textBoxConfirm);
			this.Controls.Add(this.textBoxPassword);
			this.Controls.Add(this.buttonCancel);
			this.Controls.Add(this.buttonOk);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ChangePasswordFrm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Сменить пароль пользователя \'admin\'";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Button buttonOk;
		private System.Windows.Forms.Button buttonCancel;
		private System.Windows.Forms.TextBox textBoxPassword;
		private System.Windows.Forms.TextBox textBoxConfirm;
		private System.Windows.Forms.Label labelNewPassword;
		private System.Windows.Forms.Label lblConfirmNew;
	}
}