namespace Merlin.Forms
{
	partial class AgencyPassportForm
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
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).BeginInit();
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(300, 457);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(213, 457);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(126, 457);
            // 
            // tabPassport
            // 
            this.tabPassport.Size = new System.Drawing.Size(382, 448);
            // 
            // pbFake
            // 
            this.pbFake.Image = global::FogSoft.WinForm.Properties.Resources.New;
            this.pbFake.Size = new System.Drawing.Size(64, 64);
            // 
            // PassportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.ClientSize = new System.Drawing.Size(390, 486);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "AgencyPassportForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion
	}
}
