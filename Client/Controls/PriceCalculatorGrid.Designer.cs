namespace Merlin.Controls
{
    partial class PriceCalculatorGrid
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.DataGridView dgvStations;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.dgvStations = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.dgvStations)).BeginInit();
            this.SuspendLayout();
            // 
            // dgvStations
            // 
            this.dgvStations.AllowUserToResizeRows = false;
            this.dgvStations.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvStations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvStations.EditMode = System.Windows.Forms.DataGridViewEditMode.EditOnEnter;
            this.dgvStations.Location = new System.Drawing.Point(0, 0);
            this.dgvStations.Name = "dgvStations";
            this.dgvStations.RowHeadersWidth = 62;
            this.dgvStations.Size = new System.Drawing.Size(600, 300);
            this.dgvStations.TabIndex = 0;
            // 
            // PriceCalculatorGrid
            // 
            this.Controls.Add(this.dgvStations);
            this.Name = "PriceCalculatorGrid";
            this.Size = new System.Drawing.Size(600, 300);
            ((System.ComponentModel.ISupportInitialize)(this.dgvStations)).EndInit();
            this.ResumeLayout(false);

        }
    }
}
