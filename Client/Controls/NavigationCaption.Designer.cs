namespace Merlin.Controls {
	partial class NavigationCaption {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
      this.btnGoRight = new System.Windows.Forms.Button();
      this.btnGoLeft = new System.Windows.Forms.Button();
      this.lblCaption = new System.Windows.Forms.Label();
      this.SuspendLayout();
      // 
      // btnGoRight
      // 
      this.btnGoRight.Dock = System.Windows.Forms.DockStyle.Right;
      this.btnGoRight.Image = Merlin.Properties.Resources.GoRight;
      this.btnGoRight.Location = new System.Drawing.Point(600, 0);
      this.btnGoRight.Name = "btnGoRight";
      this.btnGoRight.Size = new System.Drawing.Size(40, 23);
      this.btnGoRight.TabIndex = 1;
      this.btnGoRight.Click += new System.EventHandler(this.btnGoRight_Click);
      // 
      // btnGoLeft
      // 
      this.btnGoLeft.Dock = System.Windows.Forms.DockStyle.Left;
      this.btnGoLeft.Image = Merlin.Properties.Resources.GoLeft;
      this.btnGoLeft.Location = new System.Drawing.Point(0, 0);
      this.btnGoLeft.Name = "btnGoLeft";
      this.btnGoLeft.Size = new System.Drawing.Size(40, 23);
      this.btnGoLeft.TabIndex = 0;
      this.btnGoLeft.Click += new System.EventHandler(this.btnGoLeft_Click);
      // 
      // lblCaption
      // 
      this.lblCaption.BackColor = System.Drawing.SystemColors.GrayText;
      this.lblCaption.Dock = System.Windows.Forms.DockStyle.Fill;
      this.lblCaption.Font = new System.Drawing.Font("Verdana", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.lblCaption.ForeColor = System.Drawing.SystemColors.Window;
      this.lblCaption.Location = new System.Drawing.Point(40, 0);
      this.lblCaption.Name = "lblCaption";
      this.lblCaption.Size = new System.Drawing.Size(560, 23);
      this.lblCaption.TabIndex = 2;
      this.lblCaption.Text = "Прайс-лист от:";
      this.lblCaption.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
      // 
      // NavigationCaption2
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.lblCaption);
      this.Controls.Add(this.btnGoRight);
      this.Controls.Add(this.btnGoLeft);
      this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.Name = "NavigationCaption2";
      this.Size = new System.Drawing.Size(640, 23);
      this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button btnGoRight;
		private System.Windows.Forms.Button btnGoLeft;
		private System.Windows.Forms.Label lblCaption;

	}
}
