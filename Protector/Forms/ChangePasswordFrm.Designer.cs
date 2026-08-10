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
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
			this.tableLayoutPanel1.SuspendLayout();
			this.flowLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// buttonOk
			// 
			this.buttonOk.DialogResult = System.Windows.Forms.DialogResult.OK;
			this.buttonOk.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.buttonOk.Location = new System.Drawing.Point(153, 3);
			this.buttonOk.Name = "buttonOk";
			this.buttonOk.Size = new System.Drawing.Size(100, 33);
			this.buttonOk.TabIndex = 0;
			this.buttonOk.Text = "Сменить";
			this.buttonOk.UseVisualStyleBackColor = true;
			this.buttonOk.Click += new System.EventHandler(this.buttonOk_Click);
			// 
			// buttonCancel
			// 
			this.buttonCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonCancel.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.buttonCancel.Location = new System.Drawing.Point(259, 3);
			this.buttonCancel.Name = "buttonCancel";
			this.buttonCancel.Size = new System.Drawing.Size(100, 33);
			this.buttonCancel.TabIndex = 1;
			this.buttonCancel.Text = "Отмена";
			this.buttonCancel.UseVisualStyleBackColor = true;
			// 
			// textBoxPassword
			// 
			this.textBoxPassword.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxPassword.Font = new System.Drawing.Font("Verdana", 8.25F);
			this.textBoxPassword.Location = new System.Drawing.Point(117, 15);
			this.textBoxPassword.MaxLength = 16;
			this.textBoxPassword.Name = "textBoxPassword";
			this.textBoxPassword.PasswordChar = '*';
			this.textBoxPassword.Size = new System.Drawing.Size(254, 21);
			this.textBoxPassword.TabIndex = 2;
			// 
			// textBoxConfirm
			// 
			this.textBoxConfirm.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
			this.textBoxConfirm.Font = new System.Drawing.Font("Verdana", 8.25F);
			this.textBoxConfirm.Location = new System.Drawing.Point(117, 42);
			this.textBoxConfirm.MaxLength = 16;
			this.textBoxConfirm.Name = "textBoxConfirm";
			this.textBoxConfirm.PasswordChar = '*';
			this.textBoxConfirm.Size = new System.Drawing.Size(254, 21);
			this.textBoxConfirm.TabIndex = 3;
			// 
			// labelNewPassword
			// 
			this.labelNewPassword.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.labelNewPassword.AutoSize = true;
			this.labelNewPassword.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.labelNewPassword.Location = new System.Drawing.Point(15, 19);
			this.labelNewPassword.Name = "labelNewPassword";
			this.labelNewPassword.Size = new System.Drawing.Size(83, 13);
			this.labelNewPassword.TabIndex = 4;
			this.labelNewPassword.Text = "Новый пароль:";
			// 
			// lblConfirmNew
			// 
			this.lblConfirmNew.Anchor = System.Windows.Forms.AnchorStyles.Left;
			this.lblConfirmNew.AutoSize = true;
			this.lblConfirmNew.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.lblConfirmNew.Location = new System.Drawing.Point(15, 46);
			this.lblConfirmNew.Name = "lblConfirmNew";
			this.lblConfirmNew.Size = new System.Drawing.Size(94, 13);
			this.lblConfirmNew.TabIndex = 6;
			this.lblConfirmNew.Text = "Подтверждение:";
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 2;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Controls.Add(this.labelNewPassword, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.textBoxPassword, 1, 0);
			this.tableLayoutPanel1.Controls.Add(this.lblConfirmNew, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.textBoxConfirm, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.flowLayoutPanel1, 0, 2);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(12);
			this.tableLayoutPanel1.RowCount = 3;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.Size = new System.Drawing.Size(389, 123);
			this.tableLayoutPanel1.TabIndex = 7;
			// 
			// flowLayoutPanel1
			// 
			this.tableLayoutPanel1.SetColumnSpan(this.flowLayoutPanel1, 2);
			this.flowLayoutPanel1.Controls.Add(this.buttonCancel);
			this.flowLayoutPanel1.Controls.Add(this.buttonOk);
			this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
			this.flowLayoutPanel1.Location = new System.Drawing.Point(15, 72);
			this.flowLayoutPanel1.Name = "flowLayoutPanel1";
			this.flowLayoutPanel1.Size = new System.Drawing.Size(359, 39);
			this.flowLayoutPanel1.TabIndex = 7;
			// 
			// ChangePasswordFrm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AcceptButton = this.buttonOk;
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSize = true;
			this.CancelButton = this.buttonCancel;
			this.ClientSize = new System.Drawing.Size(389, 123);
			this.Controls.Add(this.tableLayoutPanel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ChangePasswordFrm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Сменить пароль пользователя \'admin\'";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.flowLayoutPanel1.ResumeLayout(false);
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
		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
	}
}