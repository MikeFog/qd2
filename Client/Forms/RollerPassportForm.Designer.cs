namespace Merlin.Forms
{
  partial class RollerPassportForm
  {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
      if(disposing && (components != null))
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
            this.Name = "RollerPassportForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);

    }

    #endregion
  }
}
