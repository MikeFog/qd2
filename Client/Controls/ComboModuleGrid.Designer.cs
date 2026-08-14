using FogSoft.WinForm;

namespace Merlin.Controls
{
	partial class ComboModuleGrid
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

		#region Component Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
			System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
			this.grid = new System.Windows.Forms.DataGridView();
			this.Caption = new Merlin.Controls.NavigationCaption();
			((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
			this.SuspendLayout();
			//
			// grid
			//
			this.grid.AllowUserToAddRows = false;
			this.grid.AllowUserToDeleteRows = false;
			this.grid.AllowUserToResizeRows = false;
			this.grid.BackgroundColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
			dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Control;
			dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.WindowText;
			dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
			this.grid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
			this.grid.ColumnHeadersHeight = 29;
			dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
			dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
			dataGridViewCellStyle2.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.SelectionBackColor = System.Drawing.Color.Silver;
			dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.ControlText;
			dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
			this.grid.DefaultCellStyle = dataGridViewCellStyle2;
			this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
			this.grid.Location = new System.Drawing.Point(0, 23);
			this.grid.MultiSelect = false;
			this.grid.Name = "grid";
			this.grid.ReadOnly = true;
			this.grid.RowHeadersVisible = false;
			this.grid.ShowEditingIcon = false;
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
			// ComboModuleGrid
			//
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(this.grid);
			this.Controls.Add(this.Caption);
			this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.Name = "ComboModuleGrid";
			this.Size = new System.Drawing.Size(390, 339);
			((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
			this.ResumeLayout(false);
		}

		#endregion

		protected NavigationCaption Caption;

		/// <summary>
		/// Обычный DataGridView, а не GridWithColumnsAutoResizing: тот растягивает
		/// последнюю колонку на всю свободную ширину, а здесь все дни должны быть
		/// одинаковыми.
		/// </summary>
		protected System.Windows.Forms.DataGridView grid;
	}
}
