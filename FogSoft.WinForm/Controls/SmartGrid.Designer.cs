namespace FogSoft.WinForm.Controls {
	partial class SmartGrid {
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
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
            this.lblCaption = new System.Windows.Forms.Label();
            this.panelSearch = new System.Windows.Forms.Panel();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.txQuickSearch = new System.Windows.Forms.TextBox();
            this.lbQuickSearch = new System.Windows.Forms.Label();
            this.dataGrid = new System.Windows.Forms.DataGridView();
            this.panelSearch.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).BeginInit();
            this.SuspendLayout();
            // 
            // lblCaption
            // 
            this.lblCaption.BackColor = System.Drawing.SystemColors.InactiveCaption;
            this.lblCaption.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblCaption.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.lblCaption.ForeColor = System.Drawing.SystemColors.ActiveCaptionText;
            this.lblCaption.Location = new System.Drawing.Point(0, 0);
            this.lblCaption.Margin = new System.Windows.Forms.Padding(18, 0, 18, 0);
            this.lblCaption.Name = "lblCaption";
            this.lblCaption.Size = new System.Drawing.Size(476, 28);
            this.lblCaption.TabIndex = 2;
            this.lblCaption.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblCaption.Visible = false;
            // 
            // panelSearch
            // 
            this.panelSearch.Controls.Add(this.pictureBox1);
            this.panelSearch.Controls.Add(this.txQuickSearch);
            this.panelSearch.Controls.Add(this.lbQuickSearch);
            this.panelSearch.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelSearch.Location = new System.Drawing.Point(0, 28);
            this.panelSearch.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelSearch.Name = "panelSearch";
            this.panelSearch.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.panelSearch.Size = new System.Drawing.Size(476, 31);
            this.panelSearch.TabIndex = 3;
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::FogSoft.WinForm.Properties.Resources.Search;
            this.pictureBox1.InitialImage = global::FogSoft.WinForm.Properties.Resources.Search;
            this.pictureBox1.Location = new System.Drawing.Point(4, 3);
            this.pictureBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(19, 18);
            this.pictureBox1.TabIndex = 4;
            this.pictureBox1.TabStop = false;
            // 
            // txQuickSearch
            // 
            this.txQuickSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txQuickSearch.Location = new System.Drawing.Point(166, 3);
            this.txQuickSearch.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.txQuickSearch.Name = "txQuickSearch";
            this.txQuickSearch.ReadOnly = true;
            this.txQuickSearch.Size = new System.Drawing.Size(306, 31);
            this.txQuickSearch.TabIndex = 3;
            this.txQuickSearch.TextChanged += new System.EventHandler(this.TxQuickSearch_TextChanged);
            // 
            // lbQuickSearch
            // 
            this.lbQuickSearch.AutoSize = true;
            this.lbQuickSearch.Dock = System.Windows.Forms.DockStyle.Left;
            this.lbQuickSearch.Location = new System.Drawing.Point(4, 3);
            this.lbQuickSearch.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbQuickSearch.Name = "lbQuickSearch";
            this.lbQuickSearch.Padding = new System.Windows.Forms.Padding(23, 0, 0, 0);
            this.lbQuickSearch.Size = new System.Drawing.Size(162, 25);
            this.lbQuickSearch.TabIndex = 2;
            this.lbQuickSearch.Text = "Поиск по полю";
            this.lbQuickSearch.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dataGrid
            // 
            this.dataGrid.AllowUserToAddRows = false;
            this.dataGrid.AllowUserToDeleteRows = false;
            this.dataGrid.AllowUserToResizeRows = false;
            this.dataGrid.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.DisplayedCells;
            this.dataGrid.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.DisplayedCells;
            this.dataGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dataGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGrid.Location = new System.Drawing.Point(0, 59);
            this.dataGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.dataGrid.MultiSelect = false;
            this.dataGrid.Name = "dataGrid";
            this.dataGrid.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            this.dataGrid.RowHeadersWidth = 25;
            this.dataGrid.RowTemplate.Height = 20;
            this.dataGrid.RowTemplate.Resizable = System.Windows.Forms.DataGridViewTriState.True;
            this.dataGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGrid.Size = new System.Drawing.Size(476, 353);
            this.dataGrid.TabIndex = 4;
            this.dataGrid.Text = "dataGridView1";
            this.dataGrid.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.DataGrid_CellValueChanged);
            this.dataGrid.ColumnHeaderMouseClick += new System.Windows.Forms.DataGridViewCellMouseEventHandler(this.DataGrid_ColumnHeaderMouseClick);
            this.dataGrid.CurrentCellDirtyStateChanged += new System.EventHandler(this.DataGrid_CurrentCellDirtyStateChanged);
            this.dataGrid.DoubleClick += new System.EventHandler(this.DataGrid_DoubleClick);
            this.dataGrid.MouseDown += new System.Windows.Forms.MouseEventHandler(this.DataGrid_MouseDown);
            // 
            // SmartGrid
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.dataGrid);
            this.Controls.Add(this.panelSearch);
            this.Controls.Add(this.lblCaption);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "SmartGrid";
            this.Size = new System.Drawing.Size(476, 412);
            this.panelSearch.ResumeLayout(false);
            this.panelSearch.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGrid)).EndInit();
            this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Label lblCaption;
		private System.Windows.Forms.Panel panelSearch;
		private System.Windows.Forms.TextBox txQuickSearch;
		private System.Windows.Forms.Label lbQuickSearch;
		private System.Windows.Forms.DataGridView dataGrid;
		private System.Windows.Forms.PictureBox pictureBox1;

	}
}
