using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;
using Merlin.Properties;

namespace Merlin.Forms {
  partial class MdiForm {
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
		Settings.Default.Save();
		ExportManager.OnAppQuit();
		TempFileWorker.Clear();
		if (announcementTimer != null)
		{
			announcementTimer.Stop();
			announcementTimer.Dispose();
		}

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
		System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MdiForm));
		this.MDIMenu = new System.Windows.Forms.MenuStrip();
		this.SuspendLayout();
		// 
		// MDIMenu
		// 
		this.MDIMenu.LayoutStyle = System.Windows.Forms.ToolStripLayoutStyle.HorizontalStackWithOverflow;
		this.MDIMenu.Location = new System.Drawing.Point(0, 0);
		this.MDIMenu.Name = "MDIMenu";
		this.MDIMenu.Size = new System.Drawing.Size(792, 24);
		this.MDIMenu.TabIndex = 5;
		this.MDIMenu.Text = "MDIMenu";
		// 
		// MdiForm
		// 
		this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
		this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		this.ClientSize = new System.Drawing.Size(792, 553);
		this.Controls.Add(this.MDIMenu);
		this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
		this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
		this.IsMdiContainer = true;
		this.KeyPreview = true;
		this.MainMenuStrip = this.MDIMenu;
		this.MinimumSize = new System.Drawing.Size(620, 460);
		this.Name = "MdiForm";
		this.Text = "MdiForm";
		this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
		this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MDIForm_KeyDown);
		this.ResumeLayout(false);
		this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.MenuStrip MDIMenu;
  }
}