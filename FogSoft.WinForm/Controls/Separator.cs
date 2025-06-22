using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace FogSoft.WinForm.Controls
{
	public class Separator : UserControl
	{
		private Label label1;
		private Container components = null;

		public Separator()
		{
			InitializeComponent();
		}

		protected override void Dispose(bool disposing)
		{
			if(disposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
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
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
			this.label1.Dock = System.Windows.Forms.DockStyle.Top;
			this.label1.Location = new System.Drawing.Point(0, 0);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(150, 2);
			this.label1.TabIndex = 0;
			this.label1.Text = "hhhhh";
			// 
			// Separator
			// 
			this.Controls.Add(this.label1);
			this.Name = "Separator";
			this.Resize += new System.EventHandler(this.Separator_Resize);
			this.ResumeLayout(false);
		}

		#endregion

		private void Separator_Resize(object sender, EventArgs e)
		{
			Height = 2;
		}
	}
}