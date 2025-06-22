namespace FogSoft.WinForm.Passport.Forms
{
  partial class FilterForm
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
      if (disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
		((System.ComponentModel.ISupportInitialize)(this.pbFake)).BeginInit();
		this.SuspendLayout();
		// 
		// pbFake
		// 
		this.pbFake.Image = global::FogSoft.WinForm.Properties.Resources.New;
		// 
		// FilterForm
		// 
		this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
		this.ClientSize = new System.Drawing.Size(373, 447);
		this.Name = "FilterForm";
		((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
		this.ResumeLayout(false);

	}
  }
}