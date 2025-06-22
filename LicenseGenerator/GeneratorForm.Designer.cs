namespace FogSoft.LicenseGenerator
{
	partial class GeneratorForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(GeneratorForm));
			this.openTemplateDialog = new System.Windows.Forms.OpenFileDialog();
			this.cancelButton = new System.Windows.Forms.Button();
			this.filesGroupBox = new System.Windows.Forms.GroupBox();
			this.restoreTemplate = new System.Windows.Forms.Button();
			this.generateLicense = new System.Windows.Forms.Button();
			this.browseLicense = new System.Windows.Forms.Button();
			this.browseTemplate = new System.Windows.Forms.Button();
			this.licensePath = new System.Windows.Forms.TextBox();
			this.licenseLabel = new System.Windows.Forms.Label();
			this.templatePath = new System.Windows.Forms.TextBox();
			this.templateLabel = new System.Windows.Forms.Label();
			this.progressBar = new System.Windows.Forms.ProgressBar();
			this.openLicenseDialog = new System.Windows.Forms.OpenFileDialog();
			this.helpGroupBox = new System.Windows.Forms.GroupBox();
			this.helpLabel = new System.Windows.Forms.Label();
			this.filesGroupBox.SuspendLayout();
			this.helpGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// openTemplateDialog
			// 
			this.openTemplateDialog.CheckFileExists = false;
			this.openTemplateDialog.DefaultExt = "ltmpl";
			this.openTemplateDialog.Filter = "Template files|*.ltmpl";
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(408, 262);
			this.cancelButton.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(56, 23);
			this.cancelButton.TabIndex = 7;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// filesGroupBox
			// 
			this.filesGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.filesGroupBox.Controls.Add(this.restoreTemplate);
			this.filesGroupBox.Controls.Add(this.generateLicense);
			this.filesGroupBox.Controls.Add(this.browseLicense);
			this.filesGroupBox.Controls.Add(this.browseTemplate);
			this.filesGroupBox.Controls.Add(this.licensePath);
			this.filesGroupBox.Controls.Add(this.licenseLabel);
			this.filesGroupBox.Controls.Add(this.templatePath);
			this.filesGroupBox.Controls.Add(this.templateLabel);
			this.filesGroupBox.Location = new System.Drawing.Point(8, 10);
			this.filesGroupBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.filesGroupBox.Name = "filesGroupBox";
			this.filesGroupBox.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.filesGroupBox.Size = new System.Drawing.Size(456, 125);
			this.filesGroupBox.TabIndex = 7;
			this.filesGroupBox.TabStop = false;
			this.filesGroupBox.Text = "Files";
			// 
			// restoreTemplate
			// 
			this.restoreTemplate.Image = ((System.Drawing.Image)(resources.GetObject("restoreTemplate.Image")));
			this.restoreTemplate.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.restoreTemplate.Location = new System.Drawing.Point(181, 54);
			this.restoreTemplate.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.restoreTemplate.Name = "restoreTemplate";
			this.restoreTemplate.Size = new System.Drawing.Size(140, 26);
			this.restoreTemplate.TabIndex = 4;
			this.restoreTemplate.Text = "Restore Template";
			this.restoreTemplate.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.restoreTemplate.UseVisualStyleBackColor = true;
			this.restoreTemplate.Click += new System.EventHandler(this.restoreTemplate_Click);
			// 
			// generateLicense
			// 
			this.generateLicense.Image = ((System.Drawing.Image)(resources.GetObject("generateLicense.Image")));
			this.generateLicense.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
			this.generateLicense.Location = new System.Drawing.Point(7, 54);
			this.generateLicense.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.generateLicense.Name = "generateLicense";
			this.generateLicense.Size = new System.Drawing.Size(140, 26);
			this.generateLicense.TabIndex = 3;
			this.generateLicense.Text = "Generate License";
			this.generateLicense.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
			this.generateLicense.UseVisualStyleBackColor = true;
			this.generateLicense.Click += new System.EventHandler(this.generateLicense_Click);
			// 
			// browseLicense
			// 
			this.browseLicense.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseLicense.Location = new System.Drawing.Point(430, 99);
			this.browseLicense.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.browseLicense.Name = "browseLicense";
			this.browseLicense.Size = new System.Drawing.Size(21, 19);
			this.browseLicense.TabIndex = 6;
			this.browseLicense.Text = "...";
			this.browseLicense.UseVisualStyleBackColor = true;
			this.browseLicense.Click += new System.EventHandler(this.browseLicense_Click);
			// 
			// browseTemplate
			// 
			this.browseTemplate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseTemplate.Location = new System.Drawing.Point(430, 30);
			this.browseTemplate.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.browseTemplate.Name = "browseTemplate";
			this.browseTemplate.Size = new System.Drawing.Size(21, 19);
			this.browseTemplate.TabIndex = 2;
			this.browseTemplate.Text = "...";
			this.browseTemplate.UseVisualStyleBackColor = true;
			this.browseTemplate.Click += new System.EventHandler(this.browseTemplate_Click);
			// 
			// licensePath
			// 
			this.licensePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.licensePath.Location = new System.Drawing.Point(8, 100);
			this.licensePath.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.licensePath.Name = "licensePath";
			this.licensePath.Size = new System.Drawing.Size(425, 20);
			this.licensePath.TabIndex = 5;
			// 
			// licenseLabel
			// 
			this.licenseLabel.AutoSize = true;
			this.licenseLabel.Location = new System.Drawing.Point(4, 83);
			this.licenseLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.licenseLabel.Name = "licenseLabel";
			this.licenseLabel.Size = new System.Drawing.Size(63, 13);
			this.licenseLabel.TabIndex = 5;
			this.licenseLabel.Text = "License file:";
			// 
			// templatePath
			// 
			this.templatePath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.templatePath.Location = new System.Drawing.Point(7, 31);
			this.templatePath.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.templatePath.Name = "templatePath";
			this.templatePath.Size = new System.Drawing.Size(425, 20);
			this.templatePath.TabIndex = 1;
			// 
			// templateLabel
			// 
			this.templateLabel.AutoSize = true;
			this.templateLabel.Location = new System.Drawing.Point(4, 15);
			this.templateLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.templateLabel.Name = "templateLabel";
			this.templateLabel.Size = new System.Drawing.Size(70, 13);
			this.templateLabel.TabIndex = 6;
			this.templateLabel.Text = "Template file:";
			// 
			// progressBar
			// 
			this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.progressBar.Location = new System.Drawing.Point(9, 263);
			this.progressBar.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.progressBar.Name = "progressBar";
			this.progressBar.Size = new System.Drawing.Size(394, 19);
			this.progressBar.Step = 1;
			this.progressBar.TabIndex = 9;
			// 
			// openLicenseDialog
			// 
			this.openLicenseDialog.CheckFileExists = false;
			this.openLicenseDialog.DefaultExt = "lic";
			this.openLicenseDialog.Filter = "License Files|*.lic";
			// 
			// helpGroupBox
			// 
			this.helpGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.helpGroupBox.Controls.Add(this.helpLabel);
			this.helpGroupBox.Location = new System.Drawing.Point(9, 140);
			this.helpGroupBox.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.helpGroupBox.Name = "helpGroupBox";
			this.helpGroupBox.Padding = new System.Windows.Forms.Padding(2, 2, 2, 2);
			this.helpGroupBox.Size = new System.Drawing.Size(455, 119);
			this.helpGroupBox.TabIndex = 10;
			this.helpGroupBox.TabStop = false;
			this.helpGroupBox.Text = "Help";
			// 
			// helpLabel
			// 
			this.helpLabel.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.helpLabel.Location = new System.Drawing.Point(5, 18);
			this.helpLabel.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
			this.helpLabel.Name = "helpLabel";
			this.helpLabel.Size = new System.Drawing.Size(446, 98);
			this.helpLabel.TabIndex = 0;
			this.helpLabel.Text = resources.GetString("helpLabel.Text");
			// 
			// GeneratorForm
			// 
			this.AcceptButton = this.generateLicense;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(474, 294);
			this.Controls.Add(this.helpGroupBox);
			this.Controls.Add(this.progressBar);
			this.Controls.Add(this.filesGroupBox);
			this.Controls.Add(this.cancelButton);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(452, 307);
			this.Name = "GeneratorForm";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "License Generator";
			this.filesGroupBox.ResumeLayout(false);
			this.filesGroupBox.PerformLayout();
			this.helpGroupBox.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.OpenFileDialog openTemplateDialog;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.GroupBox filesGroupBox;
		private System.Windows.Forms.Button browseLicense;
		private System.Windows.Forms.Button browseTemplate;
		private System.Windows.Forms.TextBox licensePath;
		private System.Windows.Forms.Label licenseLabel;
		private System.Windows.Forms.TextBox templatePath;
		private System.Windows.Forms.Label templateLabel;
		private System.Windows.Forms.ProgressBar progressBar;
		private System.Windows.Forms.OpenFileDialog openLicenseDialog;
		private System.Windows.Forms.Button generateLicense;
		private System.Windows.Forms.Button restoreTemplate;
		private System.Windows.Forms.GroupBox helpGroupBox;
		private System.Windows.Forms.Label helpLabel;

	}
}

