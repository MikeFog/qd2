namespace FogSoft.WinForm.Passport.Forms
{
  public partial class PassportForm
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

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).BeginInit();
            this.SuspendLayout();
            // pbFake
            // 
            this.pbFake.Image = global::FogSoft.WinForm.Properties.Resources.New;
            this.pbFake.Margin = new System.Windows.Forms.Padding(6);
            this.pbFake.Size = new System.Drawing.Size(96, 96);
            // 
            // PassportForm
            // 

            this.Name = "PassportForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);
    }

    #endregion
  }
}