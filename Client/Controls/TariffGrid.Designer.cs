using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Controls;

namespace Merlin.Controls {
	partial class TariffGrid {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing) {
			if(disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
      this.grid = new FogSoft.WinForm.Controls.GridWithColumnsAutoResizing();
      this.Caption = new Merlin.Controls.NavigationCaption();
      this.SuspendLayout();
      // 
      // grid
      // 
      this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
      this.grid.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.grid.Location = new System.Drawing.Point(0, 23);
      this.grid.Name = "grid";
      this.grid.Size = new System.Drawing.Size(390, 316);
      this.grid.TabIndex = 2;
      // 
      // Caption
      // 
      this.Caption.Dock = System.Windows.Forms.DockStyle.Top;
      this.Caption.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.Caption.Location = new System.Drawing.Point(0, 0);
      this.Caption.Name = "Caption";
      this.Caption.Size = new System.Drawing.Size(390, 23);
      this.Caption.TabIndex = 3;
      this.Caption.GoNext += new EmptyDelegate(this.Caption_GoNext);
      this.Caption.GoPrevious += new EmptyDelegate(this.Caption_GoPrevious);
      // 
      // TariffGrid
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.grid);
      this.Controls.Add(this.Caption);
      this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
      this.Name = "TariffGrid";
      this.Size = new System.Drawing.Size(390, 339);
      this.ResumeLayout(false);

		}

		#endregion

    protected NavigationCaption Caption;
    protected GridWithColumnsAutoResizing grid;

	  protected static bool IsCellChecked(DataGridViewCell cell) {
	    return !bool.Parse(cell.Value.ToString());
	  }
	}
}
