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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AgencyPassportForm));
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).BeginInit();
            this.SuspendLayout();
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(450, 686);
            this.btnApply.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(320, 686);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            // 
            // pbFake
            // 
            this.pbFake.Image = ((System.Drawing.Image)(resources.GetObject("pbFake.Image")));
            this.pbFake.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            // 
            // tabPassport
            // 
            this.tabPassport.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            this.tabPassport.Size = new System.Drawing.Size(585, 672);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(189, 686);
            this.btnOk.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            // 
            // AgencyPassportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.ClientSize = new System.Drawing.Size(585, 729);
            this.Margin = new System.Windows.Forms.Padding(9, 9, 9, 9);
            this.Name = "AgencyPassportForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion
	}
}
