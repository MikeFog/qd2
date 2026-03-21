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
            this.pbFake.Margin = new System.Windows.Forms.Padding(6);
            this.pbFake.Size = new System.Drawing.Size(72, 72);
            // 
            // FilterForm
            // 
            this.ClientSize = new System.Drawing.Size(805, 767);
            this.Font = new System.Drawing.Font("Segoe UI Variable Text", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "FilterForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);

	}
  }
}