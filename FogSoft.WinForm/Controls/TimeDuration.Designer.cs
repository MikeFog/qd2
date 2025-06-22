namespace FogSoft.WinForm.Controls
{
  partial class TimeDuration
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
      if(disposing && (components != null))
      {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
            this.txtMin = new System.Windows.Forms.NumericUpDown();
            this.lblMin = new System.Windows.Forms.Label();
            this.txtSec = new System.Windows.Forms.NumericUpDown();
            this.lblSec = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.txtMin)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSec)).BeginInit();
            this.SuspendLayout();
            // 
            // txtMin
            // 
            this.txtMin.Dock = System.Windows.Forms.DockStyle.Left;
            this.txtMin.Location = new System.Drawing.Point(0, 0);
            this.txtMin.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtMin.Maximum = new decimal(new int[] {
            10000,
            0,
            0,
            0});
            this.txtMin.Name = "txtMin";
            this.txtMin.Size = new System.Drawing.Size(64, 23);
            this.txtMin.TabIndex = 0;
            this.txtMin.ValueChanged += new System.EventHandler(this.OnValueChanged);
            this.txtMin.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtMin_KeyUp);
            // 
            // lblMin
            // 
            this.lblMin.AutoSize = true;
            this.lblMin.Location = new System.Drawing.Point(71, 2);
            this.lblMin.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblMin.Name = "lblMin";
            this.lblMin.Size = new System.Drawing.Size(33, 15);
            this.lblMin.TabIndex = 1;
            this.lblMin.Text = "мин.";
            // 
            // txtSec
            // 
            this.txtSec.Location = new System.Drawing.Point(138, 0);
            this.txtSec.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txtSec.Maximum = new decimal(new int[] {
            59,
            0,
            0,
            0});
            this.txtSec.Name = "txtSec";
            this.txtSec.Size = new System.Drawing.Size(64, 23);
            this.txtSec.TabIndex = 2;
            this.txtSec.ValueChanged += new System.EventHandler(this.OnValueChanged);
            this.txtSec.KeyUp += new System.Windows.Forms.KeyEventHandler(this.txtSec_KeyUp);
            // 
            // lblSec
            // 
            this.lblSec.AutoSize = true;
            this.lblSec.Location = new System.Drawing.Point(209, 2);
            this.lblSec.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblSec.Name = "lblSec";
            this.lblSec.Size = new System.Drawing.Size(28, 15);
            this.lblSec.TabIndex = 3;
            this.lblSec.Text = "сек.";
            // 
            // TimeDuration
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.lblSec);
            this.Controls.Add(this.txtSec);
            this.Controls.Add(this.lblMin);
            this.Controls.Add(this.txtMin);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "TimeDuration";
            this.Size = new System.Drawing.Size(488, 53);
            ((System.ComponentModel.ISupportInitialize)(this.txtMin)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.txtSec)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.NumericUpDown txtMin;
    private System.Windows.Forms.Label lblMin;
    private System.Windows.Forms.NumericUpDown txtSec;
    private System.Windows.Forms.Label lblSec;
  }
}
