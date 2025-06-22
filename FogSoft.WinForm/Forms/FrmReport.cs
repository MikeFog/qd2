using System.ComponentModel;
using System.Windows.Forms;
using CrystalDecisions.Windows.Forms;

namespace FogSoft.WinForm.Forms
{
	public class FrmReport : Form
	{
		private CrystalReportViewer viewer;
		private Container components = null;

		public FrmReport()
		{
			InitializeComponent();
			if (Globals.MdiParent != null)
				Icon = Globals.MdiParent.Icon;
		}

		public FrmReport(object reportSource) : this()
		{
			viewer.ReportSource = reportSource;
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

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.viewer = new CrystalDecisions.Windows.Forms.CrystalReportViewer();
			this.SuspendLayout();
			// 
			// viewer
			// 
			this.viewer.ActiveViewIndex = -1;
			this.viewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.viewer.DisplayBackgroundEdge = false;
			this.viewer.Dock = System.Windows.Forms.DockStyle.Fill;
			this.viewer.Location = new System.Drawing.Point(0, 0);
			this.viewer.Name = "viewer";
			this.viewer.SelectionFormula = "";
			this.viewer.ShowCloseButton = false;
			this.viewer.ShowGotoPageButton = false;
			this.viewer.ShowGroupTreeButton = false;
			this.viewer.ShowRefreshButton = false;
			this.viewer.ShowTextSearchButton = false;
			this.viewer.Size = new System.Drawing.Size(616, 614);
			this.viewer.TabIndex = 0;
			this.viewer.ViewTimeSelectionFormula = "";
			// 
			// FrmReport
			// 
			this.ClientSize = new System.Drawing.Size(616, 614);
			this.Controls.Add(this.viewer);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Name = "FrmReport";
			this.Text = "Report";
			this.ResumeLayout(false);

		}

		#endregion
	}
}