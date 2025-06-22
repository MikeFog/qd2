namespace FogSoft.WinForm.Forms
{
	partial class About
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
            this.alphaFormTransformer = new FogSoft.WinForm.Win32.AlphaFormTransformer.AlphaFormTransformer();
            this.panelMain = new System.Windows.Forms.Panel();
            this.lblVersionValue = new System.Windows.Forms.Label();
            this.lblDbVersionValue = new System.Windows.Forms.Label();
            this.lblUrl = new System.Windows.Forms.LinkLabel();
            this.lblModified = new System.Windows.Forms.Label();
            this.lblFogSoftCop = new System.Windows.Forms.Label();
            this.lblVersion = new System.Windows.Forms.Label();
            this.labelDbVersionText = new System.Windows.Forms.Label();
            this.alphaFormMarker1 = new FogSoft.WinForm.Win32.AlphaFormTransformer.AlphaFormMarker();
            this.alphaFormTransformer.SuspendLayout();
            this.panelMain.SuspendLayout();
            this.SuspendLayout();
            // 
            // alphaFormTransformer
            // 
            this.alphaFormTransformer.Controls.Add(this.panelMain);
            this.alphaFormTransformer.Controls.Add(this.alphaFormMarker1);
            this.alphaFormTransformer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.alphaFormTransformer.DragSleep = ((uint)(30u));
            this.alphaFormTransformer.Location = new System.Drawing.Point(0, 0);
            this.alphaFormTransformer.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.alphaFormTransformer.Name = "alphaFormTransformer";
            this.alphaFormTransformer.Size = new System.Drawing.Size(963, 535);
            this.alphaFormTransformer.TabIndex = 0;
            this.alphaFormTransformer.Click += new System.EventHandler(this.alphaFormTransformer_Click);
            // 
            // panelMain
            // 
            this.panelMain.Controls.Add(this.lblVersionValue);
            this.panelMain.Controls.Add(this.lblDbVersionValue);
            this.panelMain.Controls.Add(this.lblUrl);
            this.panelMain.Controls.Add(this.lblModified);
            this.panelMain.Controls.Add(this.lblFogSoftCop);
            this.panelMain.Controls.Add(this.lblVersion);
            this.panelMain.Controls.Add(this.labelDbVersionText);
            this.panelMain.Location = new System.Drawing.Point(20, 22);
            this.panelMain.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.panelMain.Name = "panelMain";
            this.panelMain.Size = new System.Drawing.Size(923, 489);
            this.panelMain.TabIndex = 1;
            this.panelMain.Click += new System.EventHandler(this.alphaFormTransformer_Click);
            this.panelMain.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form_KeyDown);
            // 
            // lblVersionValue
            // 
            this.lblVersionValue.BackColor = System.Drawing.Color.Transparent;
            this.lblVersionValue.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblVersionValue.ForeColor = System.Drawing.Color.White;
            this.lblVersionValue.Location = new System.Drawing.Point(703, 336);
            this.lblVersionValue.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblVersionValue.Name = "lblVersionValue";
            this.lblVersionValue.Size = new System.Drawing.Size(222, 29);
            this.lblVersionValue.TabIndex = 6;
            this.lblVersionValue.Text = "0";
            this.lblVersionValue.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form_KeyDown);
            // 
            // lblDbVersionValue
            // 
            this.lblDbVersionValue.BackColor = System.Drawing.Color.Transparent;
            this.lblDbVersionValue.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.lblDbVersionValue.ForeColor = System.Drawing.Color.White;
            this.lblDbVersionValue.Location = new System.Drawing.Point(703, 308);
            this.lblDbVersionValue.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblDbVersionValue.Name = "lblDbVersionValue";
            this.lblDbVersionValue.Size = new System.Drawing.Size(222, 29);
            this.lblDbVersionValue.TabIndex = 5;
            this.lblDbVersionValue.Text = "0";
            this.lblDbVersionValue.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form_KeyDown);
            // 
            // lblUrl
            // 
            this.lblUrl.ActiveLinkColor = System.Drawing.Color.White;
            this.lblUrl.AutoSize = true;
            this.lblUrl.BackColor = System.Drawing.Color.Transparent;
            this.lblUrl.Cursor = System.Windows.Forms.Cursors.Hand;
            this.lblUrl.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblUrl.ForeColor = System.Drawing.Color.White;
            this.lblUrl.LinkBehavior = System.Windows.Forms.LinkBehavior.HoverUnderline;
            this.lblUrl.LinkColor = System.Drawing.Color.White;
            this.lblUrl.Location = new System.Drawing.Point(500, 394);
            this.lblUrl.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblUrl.Name = "lblUrl";
            this.lblUrl.Size = new System.Drawing.Size(192, 21);
            this.lblUrl.TabIndex = 4;
            this.lblUrl.TabStop = true;
            this.lblUrl.Text = "http://www.4g-soft.com";
            this.lblUrl.VisitedLinkColor = System.Drawing.Color.White;
            this.lblUrl.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.lblUrl_LinkClicked);
            this.lblUrl.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form_KeyDown);
            // 
            // lblModified
            // 
            this.lblModified.AutoSize = true;
            this.lblModified.BackColor = System.Drawing.Color.Transparent;
            this.lblModified.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblModified.ForeColor = System.Drawing.Color.White;
            this.lblModified.Location = new System.Drawing.Point(500, 422);
            this.lblModified.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblModified.Name = "lblModified";
            this.lblModified.Size = new System.Drawing.Size(259, 21);
            this.lblModified.TabIndex = 3;
            this.lblModified.Text = "Modified by Denis Gladkikh, 2014.";
            this.lblModified.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form_KeyDown);
            // 
            // lblFogSoftCop
            // 
            this.lblFogSoftCop.AutoSize = true;
            this.lblFogSoftCop.BackColor = System.Drawing.Color.Transparent;
            this.lblFogSoftCop.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblFogSoftCop.ForeColor = System.Drawing.Color.White;
            this.lblFogSoftCop.Location = new System.Drawing.Point(500, 365);
            this.lblFogSoftCop.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblFogSoftCop.Name = "lblFogSoftCop";
            this.lblFogSoftCop.Size = new System.Drawing.Size(237, 21);
            this.lblFogSoftCop.TabIndex = 2;
            this.lblFogSoftCop.Text = "ФогСофт © Ярославль. 2009.";
            this.lblFogSoftCop.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form_KeyDown);
            // 
            // lblVersion
            // 
            this.lblVersion.AutoSize = true;
            this.lblVersion.BackColor = System.Drawing.Color.Transparent;
            this.lblVersion.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblVersion.ForeColor = System.Drawing.Color.White;
            this.lblVersion.Location = new System.Drawing.Point(500, 336);
            this.lblVersion.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblVersion.Name = "lblVersion";
            this.lblVersion.Size = new System.Drawing.Size(173, 21);
            this.lblVersion.TabIndex = 1;
            this.lblVersion.Text = "Версия приложения:";
            // 
            // labelDbVersionText
            // 
            this.labelDbVersionText.AutoSize = true;
            this.labelDbVersionText.BackColor = System.Drawing.Color.Transparent;
            this.labelDbVersionText.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.labelDbVersionText.ForeColor = System.Drawing.Color.White;
            this.labelDbVersionText.Location = new System.Drawing.Point(500, 308);
            this.labelDbVersionText.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelDbVersionText.Name = "labelDbVersionText";
            this.labelDbVersionText.Size = new System.Drawing.Size(177, 21);
            this.labelDbVersionText.TabIndex = 0;
            this.labelDbVersionText.Text = "Версия базы данных:";
            this.labelDbVersionText.PreviewKeyDown += new System.Windows.Forms.PreviewKeyDownEventHandler(this.Form_KeyDown);
            // 
            // alphaFormMarker1
            // 
            this.alphaFormMarker1.FillBorder = ((uint)(4u));
            this.alphaFormMarker1.Location = new System.Drawing.Point(671, 350);
            this.alphaFormMarker1.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.alphaFormMarker1.Name = "alphaFormMarker1";
            this.alphaFormMarker1.Size = new System.Drawing.Size(29, 32);
            this.alphaFormMarker1.TabIndex = 0;
            this.alphaFormMarker1.Click += new System.EventHandler(this.alphaFormTransformer_Click);
            // 
            // About
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(963, 535);
            this.Controls.Add(this.alphaFormTransformer);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.Name = "About";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About";
            this.Click += new System.EventHandler(this.alphaFormTransformer_Click);
            this.alphaFormTransformer.ResumeLayout(false);
            this.panelMain.ResumeLayout(false);
            this.panelMain.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion

		private FogSoft.WinForm.Win32.AlphaFormTransformer.AlphaFormTransformer alphaFormTransformer;
		private System.Windows.Forms.Panel panelMain;
		private FogSoft.WinForm.Win32.AlphaFormTransformer.AlphaFormMarker alphaFormMarker1;
		private System.Windows.Forms.Label lblFogSoftCop;
		private System.Windows.Forms.Label lblVersion;
		private System.Windows.Forms.Label labelDbVersionText;
		private System.Windows.Forms.Label lblModified;
		private System.Windows.Forms.LinkLabel lblUrl;
		private System.Windows.Forms.Label lblDbVersionValue;
		private System.Windows.Forms.Label lblVersionValue;
	}
}