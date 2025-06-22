using System;
using System.Data;
using System.IO;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;

namespace Merlin.Forms
{
	public partial class RollersCopyFrm : Form
	{
		private readonly string path;

		public RollersCopyFrm()
		{
			InitializeComponent();
			tbbExcel.Image = Globals.GetImage(Constants.ActionsImages.ExportExcel);
		}

		public RollersCopyFrm(DataView data, string path)
			:this()
		{
			grid.RawDataGridView.Columns.Add("filePath", "Файл");
			grid.RawDataGridView.Columns.Add("status", "Состояние");
			grid.RawDataGridView.Columns[0].DataPropertyName = "filePath";
			grid.RawDataGridView.Columns[1].DataPropertyName = "status";
			foreach (DataRowView row in data)
			{
				if (!string.IsNullOrEmpty(row["path"].ToString()))
					grid.RawDataGridView.Rows.Add(row["path"].ToString(), "...");
			}
			grid.SetColumnsWidth();
			tsProgressBar.Minimum = 0;
			tsProgressBar.Maximum = grid.RawDataGridView.Rows.Count;
			tsProgressBar.Value = 0;
			this.path = path;
		}

		private void tbbExcel_Click(object sender, EventArgs e)
		{
			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				if (grid.RawDataGridView.RowCount > 0)
				{
					DataGridView dg = grid.RawDataGridView;
					DataTable dt = new DataTable();
					dt.Columns.Add("filePath", typeof (string));
					dt.Columns.Add("status", typeof (string));
					foreach (DataGridViewRow row in dg.Rows)
					{
						dt.Rows.Add(row.Cells[0].Value, row.Cells[1].Value);
					}
					dg.DataSource = dt;
					ExportManager.ExportExcel(dg, null);
				}
			}
			catch (Exception ex)
			{
				ErrorManager.PublishError(ex);
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			try
			{
				Cursor.Current = Cursors.WaitCursor;
				Generate();
			}
			finally
			{
				Cursor.Current = Cursors.Default;
			}
		}

		private void Generate()
		{
			Application.DoEvents();
			grid.SetColumnsWidth();
			foreach (DataGridViewRow row in grid.RawDataGridView.Rows)
			{
				row.Cells["status"].Value = "Копируется";
				string status = "Скопирован";
				try
				{
					File.Copy(row.Cells["filePath"].Value.ToString(),
					          path + Path.DirectorySeparatorChar + Path.GetFileName(row.Cells["filePath"].Value.ToString()));
				}
				catch
				{
					status = "Не удалось";
				}
				finally
				{
					tsProgressBar.Value += 1;
				}
				row.Cells["status"].Value = status;
				Application.DoEvents();
				Update();
			}
		}
	}
}