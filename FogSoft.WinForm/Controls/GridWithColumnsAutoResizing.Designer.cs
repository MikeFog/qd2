namespace FogSoft.WinForm.Controls {
  partial class GridWithColumnsAutoResizing {
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rawDataGridView = new System.Windows.Forms.DataGridView();
            ((System.ComponentModel.ISupportInitialize)(this.rawDataGridView)).BeginInit();
            this.SuspendLayout();
            // 
            // rawDataGridView
            // 
            this.rawDataGridView.AllowUserToAddRows = false;
            this.rawDataGridView.AllowUserToDeleteRows = false;
            this.rawDataGridView.AllowUserToResizeRows = false;
            this.rawDataGridView.BackgroundColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.rawDataGridView.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.rawDataGridView.ColumnHeadersHeight = 29;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.Silver;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.rawDataGridView.DefaultCellStyle = dataGridViewCellStyle2;
            this.rawDataGridView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rawDataGridView.Location = new System.Drawing.Point(0, 0);
            this.rawDataGridView.MultiSelect = false;
            this.rawDataGridView.Name = "rawDataGridView";
            this.rawDataGridView.ReadOnly = true;
            this.rawDataGridView.RowHeadersVisible = false;
            this.rawDataGridView.RowHeadersWidth = 51;
            this.rawDataGridView.ShowEditingIcon = false;
            this.rawDataGridView.Size = new System.Drawing.Size(321, 346);
            this.rawDataGridView.TabIndex = 0;
            this.rawDataGridView.Text = "dataGridView1";
            this.rawDataGridView.Resize += new System.EventHandler(this.grid_Resize);
            // 
            // GridWithColumnsAutoResizing
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.rawDataGridView);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "GridWithColumnsAutoResizing";
            this.Size = new System.Drawing.Size(321, 346);
            ((System.ComponentModel.ISupportInitialize)(this.rawDataGridView)).EndInit();
            this.ResumeLayout(false);

    }

    #endregion

    protected System.Windows.Forms.DataGridView rawDataGridView;

  }
}
