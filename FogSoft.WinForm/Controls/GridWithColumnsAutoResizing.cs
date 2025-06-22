using System;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;

namespace FogSoft.WinForm.Controls
{
	public partial class GridWithColumnsAutoResizing : UserControl
	{
		public GridWithColumnsAutoResizing()
		{
			InitializeComponent();
		}

		private void grid_Resize(object sender, EventArgs e)
		{
			try
			{
				SetColumnsWidth();
			}
			catch(Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
		}

		public void SetColumnsWidth()
		{
			if(rawDataGridView.AutoSizeColumnsMode == DataGridViewAutoSizeColumnsMode.Fill)
				return;
			if(rawDataGridView.ColumnCount == 0) return;
			for(int i = 0; i < rawDataGridView.ColumnCount - 1; i++)
				rawDataGridView.AutoResizeColumn(i);

			ProcessLastColumn();
		}

		private void ProcessLastColumn()
		{
			DataGridViewColumn lastColumn =
				rawDataGridView.Columns[rawDataGridView.ColumnCount - 1];
			lastColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
			int columnWidth = lastColumn.Width;
			lastColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
			if(lastColumn.Width < columnWidth)
				lastColumn.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
		}

		public DataGridView RawDataGridView
		{
			get { return rawDataGridView; }
		}

		public override Cursor Cursor
		{
			get { return base.Cursor; }
			set
			{
				base.Cursor = value;
				rawDataGridView.Cursor = value;
			}
		}
	}
}