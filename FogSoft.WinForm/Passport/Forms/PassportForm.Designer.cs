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
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(669, 687);
            this.btnApply.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(539, 687);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(408, 687);
            this.btnOk.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            // 
            // tabPassport
            // 
            this.tabPassport.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.tabPassport.Size = new System.Drawing.Size(794, 669);
            // 
            // pbFake
            // 
            this.pbFake.Image = global::FogSoft.WinForm.Properties.Resources.New;
            this.pbFake.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.pbFake.Size = new System.Drawing.Size(96, 96);
            // 
            // PassportForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(144F, 144F);
            this.ClientSize = new System.Drawing.Size(804, 734);
            this.Margin = new System.Windows.Forms.Padding(6, 6, 6, 6);
            this.MinimumSize = new System.Drawing.Size(826, 692);
            this.Name = "PassportForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);

    }

    #endregion
  }
}