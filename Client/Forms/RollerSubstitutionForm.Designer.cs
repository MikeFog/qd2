namespace Merlin.Forms {
  partial class RollerSubstitutionForm {
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

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).BeginInit();
            this.SuspendLayout();
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(721, 935);
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(634, 935);
            // 
            // tabPassport
            // 
            this.tabPassport.Size = new System.Drawing.Size(804, 895);
            // 
            // btnOk
            // 
            this.btnOk.Location = new System.Drawing.Point(547, 935);
            // 
            // RollerSubstitutionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(804, 936);
            this.Cursor = System.Windows.Forms.Cursors.Default;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
            this.Name = "RollerSubstitutionForm";
            this.Text = "Замена ролика";
            ((System.ComponentModel.ISupportInitialize)(this.pbFake)).EndInit();
            this.ResumeLayout(false);

    }

    #endregion
  }
}