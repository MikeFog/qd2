using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes.Export;
using FogSoft.WinForm.Classes;
using System.IO;
using Merlin.Classes;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Forms
{
    public partial class RollersDeleteFrm : Form
    {
        private bool bWorking = false;
        private bool bCancel = false;

        public RollersDeleteFrm()
        {
            InitializeComponent();
            tbbExcel.Image = Globals.GetImage(Constants.ActionsImages.ExportExcel);
        }

        public RollersDeleteFrm(DataView data)
			:this()
		{
            grid.RawDataGridView.AutoGenerateColumns = false;
            grid.RawDataGridView.Columns.Add("name", "Имя");
			grid.RawDataGridView.Columns.Add("path", "Путь");
			grid.RawDataGridView.Columns.Add("dbStatus", "Ролик");
            grid.RawDataGridView.Columns.Add("fileStatus", "Файл");
            grid.RawDataGridView.Columns[0].DataPropertyName = "name";
            grid.RawDataGridView.Columns[1].DataPropertyName = "path";
            grid.RawDataGridView.Columns[2].DataPropertyName = "dbStatus";
            grid.RawDataGridView.Columns[3].DataPropertyName = "fileStatus";
			grid.RawDataGridView.DataSource = data;
			grid.SetColumnsWidth();
			tsProgressBar.Minimum = 0;
			tsProgressBar.Maximum = grid.RawDataGridView.Rows.Count;
			tsProgressBar.Value = 0;
		}

		private void tbbExcel_Click(object sender, EventArgs e)
		{
            if (bWorking)
            {
                return;
            }

			try
			{
				Application.DoEvents();
				Cursor = Cursors.WaitCursor;

				if (grid.RawDataGridView.RowCount > 0)
				{
                    DataGridView dg = new DataGridView();
                    dg.Columns.Add("name", "Имя");
                    dg.Columns.Add("path", "Путь");
                    dg.Columns.Add("dbStatus", "Ролик");
                    dg.Columns.Add("fileStatus", "Файл");
                    dg.Columns[0].DataPropertyName = "name";
                    dg.Columns[1].DataPropertyName = "path";
                    dg.Columns[2].DataPropertyName = "dbStatus";
                    dg.Columns[3].DataPropertyName = "fileStatus";
					DataTable dt = new DataTable();
                    dt.Columns.Add("name", typeof(string));
					dt.Columns.Add("path", typeof (string));
					dt.Columns.Add("dbStatus", typeof (string));
                    dt.Columns.Add("fileStatus", typeof(string));
                    foreach (DataGridViewRow row in grid.RawDataGridView.Rows)
					{
                        dt.Rows.Add(row.Cells["name"].Value, row.Cells["path"].Value, row.Cells["dbStatus"].Value, row.Cells["fileStatus"].Value);
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

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            if (bWorking)
            {
                if (MessageBox.ShowQuestion("Прервать процедуру?") == DialogResult.Yes)
                {
                    bCancel = true;
                }
                else
                {
                    e.Cancel = true;
                }
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
            bWorking = true;
            tbbExcel.Enabled = false;
			Application.DoEvents();
			grid.SetColumnsWidth();
			foreach (DataGridViewRow row in grid.RawDataGridView.Rows)
			{
                if (bCancel)
                {
                    break;
                }

				string fileStatus = string.Empty;
                string dbStatus = string.Empty;
                bool deleted = false;

                Roller r = new Roller((row.DataBoundItem as DataRowView).Row);
                try
                {
                    r.Delete(true);
                    deleted = true;
                    dbStatus = "Удален";
                }
                catch
                {
                    dbStatus = "Используется";
                }

                if (deleted) 
                {
				    try
				    {
                        string filePath = row.Cells["path"].Value.ToString();
                        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
                        {
                            File.Delete(filePath);
                            fileStatus = "Удален";
                        }
                        else
                        {
                            fileStatus = "Не существует";
                        }
				    }
				    catch
				    {
					    fileStatus = "Не удалось";
				    }
                }

                tsProgressBar.Value += 1;

                row.Cells["dbStatus"].Value = dbStatus;
				row.Cells["fileStatus"].Value = fileStatus;

				Application.DoEvents();
				Update();
			}
            bWorking = false;
            tbbExcel.Enabled = true;
            Application.DoEvents();
            Update();
            Application.DoEvents();
		}
    }
}
