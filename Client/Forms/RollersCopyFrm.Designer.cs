namespace Merlin.Forms
{
	partial class RollersCopyFrm
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
			this.toolStrip1 = new System.Windows.Forms.ToolStrip();
			this.tbbExcel = new System.Windows.Forms.ToolStripButton();
			this.tsProgressBar = new System.Windows.Forms.ToolStripProgressBar();
			this.grid = new FogSoft.WinForm.Controls.GridWithColumnsAutoResizing();
			this.toolStrip1.SuspendLayout();
			this.SuspendLayout();
			// 
			// toolStrip1
			// 
			this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tbbExcel,
            this.tsProgressBar});
			this.toolStrip1.Location = new System.Drawing.Point(0, 0);
			this.toolStrip1.Name = "toolStrip1";
			this.toolStrip1.Size = new System.Drawing.Size(439, 25);
			this.toolStrip1.TabIndex = 0;
			this.toolStrip1.Text = "toolStrip1";
			// 
			// tbbExcel
			// 
			this.tbbExcel.ImageTransparentColor = System.Drawing.Color.Magenta;
			this.tbbExcel.Name = "tbbExcel";
			this.tbbExcel.Size = new System.Drawing.Size(52, 22);
			this.tbbExcel.Text = "Экспорт";
			this.tbbExcel.Click += new System.EventHandler(this.tbbExcel_Click);
			// 
			// tsProgressBar
			// 
			this.tsProgressBar.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
			this.tsProgressBar.AutoSize = false;
			this.tsProgressBar.Margin = new System.Windows.Forms.Padding(1, 2, 5, 1);
			this.tsProgressBar.Name = "tsProgressBar";
			this.tsProgressBar.Size = new System.Drawing.Size(300, 11);
			// 
			// grid
			// 
			this.grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.grid.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.grid.Location = new System.Drawing.Point(0, 28);
			this.grid.Name = "grid";
			this.grid.Size = new System.Drawing.Size(439, 351);
			this.grid.TabIndex = 1;
			// 
			// RollersCopyFrm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
			this.ClientSize = new System.Drawing.Size(439, 380);
			this.Controls.Add(this.grid);
			this.Controls.Add(this.toolStrip1);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "RollersCopyFrm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Копирование роликов";
			this.toolStrip1.ResumeLayout(false);
			this.toolStrip1.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.ToolStrip toolStrip1;
		private System.Windows.Forms.ToolStripButton tbbExcel;
		private System.Windows.Forms.ToolStripProgressBar tsProgressBar;
		private FogSoft.WinForm.Controls.GridWithColumnsAutoResizing grid;
	}
}