namespace Protector.Forms
{
	partial class UserPassport
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
            this.SuspendLayout();
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(669, 686);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(537, 686);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(405, 686);
            // 
            // tabPassport
            // 
            this.tabPassport.Location = new System.Drawing.Point(0, 0);
            this.tabPassport.Size = new System.Drawing.Size(796, 674);
            // 
            // UserPassport
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.ClientSize = new System.Drawing.Size(804, 734);
            this.Margin = new System.Windows.Forms.Padding(9);
            this.Name = "UserPassport";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion
	}
}