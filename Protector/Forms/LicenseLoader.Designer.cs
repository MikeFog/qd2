namespace Protector.Forms
{
	partial class LicenseLoader
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
			this.groupBoxLicenseLoader = new System.Windows.Forms.GroupBox();
			this.btnLoadLicense = new System.Windows.Forms.Button();
			this.btnPathLicense = new System.Windows.Forms.Button();
			this.textBoxLicensePath = new System.Windows.Forms.TextBox();
			this.labelLicencePath = new System.Windows.Forms.Label();
			this.groupBoxLicense = new System.Windows.Forms.GroupBox();
			this.lblLicence = new System.Windows.Forms.Label();
			this.frmBrowseLicence = new System.Windows.Forms.OpenFileDialog();
			this.buttonClose = new System.Windows.Forms.Button();
			this.groupBoxLicenseLoader.SuspendLayout();
			this.groupBoxLicense.SuspendLayout();
			this.SuspendLayout();
			// 
			// groupBoxLicenseLoader
			// 
			this.groupBoxLicenseLoader.Controls.Add(this.btnLoadLicense);
			this.groupBoxLicenseLoader.Controls.Add(this.btnPathLicense);
			this.groupBoxLicenseLoader.Controls.Add(this.textBoxLicensePath);
			this.groupBoxLicenseLoader.Controls.Add(this.labelLicencePath);
			this.groupBoxLicenseLoader.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.groupBoxLicenseLoader.Location = new System.Drawing.Point(12, 12);
			this.groupBoxLicenseLoader.Name = "groupBoxLicenseLoader";
			this.groupBoxLicenseLoader.Size = new System.Drawing.Size(448, 111);
			this.groupBoxLicenseLoader.TabIndex = 0;
			this.groupBoxLicenseLoader.TabStop = false;
			this.groupBoxLicenseLoader.Text = "Загрузка лицензии";
			// 
			// btnLoadLicense
			// 
			this.btnLoadLicense.Enabled = false;
			this.btnLoadLicense.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.btnLoadLicense.Image = global::Protector.Properties.Resources.ArrowDown;
			this.btnLoadLicense.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.btnLoadLicense.Location = new System.Drawing.Point(277, 71);
			this.btnLoadLicense.Name = "btnLoadLicense";
			this.btnLoadLicense.Size = new System.Drawing.Size(159, 29);
			this.btnLoadLicense.TabIndex = 3;
			this.btnLoadLicense.Text = "Загрузить лицензию";
			this.btnLoadLicense.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			this.btnLoadLicense.UseVisualStyleBackColor = true;
			this.btnLoadLicense.Click += new System.EventHandler(this.btnLoadLicense_Click);
			// 
			// btnPathLicense
			// 
			this.btnPathLicense.Image = global::Protector.Properties.Resources.open;
			this.btnPathLicense.Location = new System.Drawing.Point(411, 37);
			this.btnPathLicense.Margin = new System.Windows.Forms.Padding(0);
			this.btnPathLicense.Name = "btnPathLicense";
			this.btnPathLicense.Size = new System.Drawing.Size(25, 20);
			this.btnPathLicense.TabIndex = 2;
			this.btnPathLicense.UseVisualStyleBackColor = true;
			this.btnPathLicense.Click += new System.EventHandler(this.btnPathLicense_Click);
			// 
			// textBoxLicensePath
			// 
			this.textBoxLicensePath.Location = new System.Drawing.Point(6, 37);
			this.textBoxLicensePath.Name = "textBoxLicensePath";
			this.textBoxLicensePath.Size = new System.Drawing.Size(402, 21);
			this.textBoxLicensePath.TabIndex = 1;
			this.textBoxLicensePath.TextChanged += new System.EventHandler(this.textBoxLicensePath_TextChanged);
			// 
			// labelLicencePath
			// 
			this.labelLicencePath.AutoSize = true;
			this.labelLicencePath.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.labelLicencePath.Location = new System.Drawing.Point(7, 20);
			this.labelLicencePath.Name = "labelLicencePath";
			this.labelLicencePath.Size = new System.Drawing.Size(137, 13);
			this.labelLicencePath.TabIndex = 0;
			this.labelLicencePath.Text = "Путь до файла лицензии:";
			// 
			// groupBoxLicense
			// 
			this.groupBoxLicense.Controls.Add(this.lblLicence);
			this.groupBoxLicense.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.groupBoxLicense.Location = new System.Drawing.Point(12, 129);
			this.groupBoxLicense.Name = "groupBoxLicense";
			this.groupBoxLicense.Size = new System.Drawing.Size(448, 115);
			this.groupBoxLicense.TabIndex = 1;
			this.groupBoxLicense.TabStop = false;
			this.groupBoxLicense.Text = "Текущая лицензия";
			// 
			// lblLicence
			// 
			this.lblLicence.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.lblLicence.Location = new System.Drawing.Point(7, 20);
			this.lblLicence.Name = "lblLicence";
			this.lblLicence.Size = new System.Drawing.Size(435, 82);
			this.lblLicence.TabIndex = 0;
			this.lblLicence.Text = "Лицензия не предоставлена";
			this.lblLicence.TextAlign = System.Drawing.ContentAlignment.BottomLeft;
			// 
			// frmBrowseLicence
			// 
			this.frmBrowseLicence.DefaultExt = "lic";
			this.frmBrowseLicence.FileName = "frmBrowseLicence";
			this.frmBrowseLicence.Filter = "Файл лицензии|*.lic";
			this.frmBrowseLicence.Title = "Файл лицензии";
			// 
			// buttonClose
			// 
			this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.buttonClose.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.buttonClose.Location = new System.Drawing.Point(387, 250);
			this.buttonClose.Name = "buttonClose";
			this.buttonClose.Size = new System.Drawing.Size(75, 23);
			this.buttonClose.TabIndex = 2;
			this.buttonClose.Text = "Закрыть";
			this.buttonClose.UseVisualStyleBackColor = true;
			// 
			// LicenseLoader
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(474, 285);
			this.Controls.Add(this.buttonClose);
			this.Controls.Add(this.groupBoxLicense);
			this.Controls.Add(this.groupBoxLicenseLoader);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MaximumSize = new System.Drawing.Size(480, 310);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(480, 310);
			this.Name = "LicenseLoader";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Лицензия";
			this.groupBoxLicenseLoader.ResumeLayout(false);
			this.groupBoxLicenseLoader.PerformLayout();
			this.groupBoxLicense.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox groupBoxLicenseLoader;
		private System.Windows.Forms.Button btnPathLicense;
		private System.Windows.Forms.TextBox textBoxLicensePath;
		private System.Windows.Forms.Label labelLicencePath;
		private System.Windows.Forms.GroupBox groupBoxLicense;
		private System.Windows.Forms.Button btnLoadLicense;
		private System.Windows.Forms.OpenFileDialog frmBrowseLicence;
		private System.Windows.Forms.Button buttonClose;
		private System.Windows.Forms.Label lblLicence;
	}
}