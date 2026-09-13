namespace Merlin.Forms.CreateActionMaster
{
	partial class VeerColorLegendForm
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
			this.rtbLegend = new System.Windows.Forms.RichTextBox();
			this.SuspendLayout();
			//
			// rtbLegend
			//
			this.rtbLegend.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.rtbLegend.Dock = System.Windows.Forms.DockStyle.Fill;
			this.rtbLegend.Font = new System.Drawing.Font("Segoe UI", 9.75F);
			this.rtbLegend.Location = new System.Drawing.Point(0, 0);
			this.rtbLegend.Name = "rtbLegend";
			this.rtbLegend.ReadOnly = true;
			this.rtbLegend.Size = new System.Drawing.Size(520, 480);
			this.rtbLegend.TabIndex = 0;
			this.rtbLegend.Text = "";
			//
			// VeerColorLegendForm
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(520, 480);
			this.Controls.Add(this.rtbLegend);
			this.MinimizeBox = false;
			this.MinimumSize = new System.Drawing.Size(360, 300);
			this.Name = "VeerColorLegendForm";
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
			this.Text = "Цвета в сетке веера";
			this.ResumeLayout(false);
		}

		private System.Windows.Forms.RichTextBox rtbLegend;
	}
}
