using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using FogSoft.WinForm.Classes;
using Merlin.Properties;
using Merlin.Classes;
using Merlin.Classes.GridExport;
using Merlin.Reports;
using System.Data;

namespace Merlin.Forms.GridReport
{
	public partial class ExportGridForm : Form
	{
        public ExportGridForm()
		{
			InitializeComponent();
			grdRadiostations.Entity = EntityManager.GetEntity((int)Entities.MassMedia);
			InitMassmediaGroups();
			ButtonExportEnabled();
		}

        private void InitMassmediaGroups()
        {
            cmbRadioStationGroup.ColumnWithID = "massmediaGroupID";
            cmbRadioStationGroup.DataSource = Massmedia.LoadGroupsWithShowAllOption();
        }

        private void btnFolderPath_Click(object sender, EventArgs e)
		{
			if (!string.IsNullOrEmpty(textBoxSelectedPath.Text))
				folderBrowserDialog.SelectedPath = textBoxSelectedPath.Text;
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				textBoxSelectedPath.Text = folderBrowserDialog.SelectedPath;
			}
		}

		private void btnExport_Click(object sender, EventArgs e)
		{
			if (Directory.Exists(textBoxSelectedPath.Text))
			{
				progressBar.Maximum = grdRadiostations.Added2Checked.Count * 2;
				progressBar.Value = 0;
				progressBar.Visible = true;
				SetControlStatus(false);
				backgroundWorker.RunWorkerAsync(grdRadiostations.Added2Checked);
			}
			else
				FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Resources.ExportProblemTitle, Resources.ExportProblemDirectoryDosntExist);
		}

		private void SetControlStatus(bool enabled)
		{
			btnExport.Enabled = enabled;
			grdRadiostations.Enabled = enabled;
			textBoxSelectedPath.Enabled = enabled;
			btnFolderPath.Enabled = enabled;
			dateTimePicker.Enabled = enabled;
		}

		private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<PresentationObject> list = e.Argument as List<PresentationObject>;
			BackgroundWorker worker = sender as BackgroundWorker;
			DateTime dateTime = dateTimePicker.Value;
			string selectedPath = textBoxSelectedPath.Text;
			bool containsErrors = false;
			if (list != null && worker != null)
			{
				int index = 0;
				foreach (Massmedia massmedia in list)
				{
					try
					{
						string fileName = string.Format("{0}{1}{2}", selectedPath, Path.DirectorySeparatorChar,
						                                ExportHelper.RemoveInvalidFileNameChars(massmedia.Name));
						GridReportCreator creater = new GridReportCreator(massmedia, dateTime, null);
						
						using (Grid report = creater.GetReport())
						{
							if (report != null)
								report.ExportToDisk(ExportHelper.CrystalExportFormatType, fileName + ExportHelper.CrystalExportFormatTypeExtension);
						}
						
						index++;
						worker.ReportProgress(index/(list.Count*2), index);

						creater.ExportDocument(fileName);

						index++;
						worker.ReportProgress(index/(list.Count*2), index);
					}
					catch(Exception exp)
					{
						ErrorManager.LogError("Cannot Export", exp);
						containsErrors = true;
					}
				}
			}
			e.Result = containsErrors;
		}

		private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			progressBar.Value = (int) e.UserState;
		}

		private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			bool result = (bool) e.Result;
			progressBar.Visible = false;
			SetControlStatus(true);
			ButtonExportEnabled();
			if (!result && ExportHelper.OpenFolderOnFinish)
				Process.Start(textBoxSelectedPath.Text);
			if (result)
				FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Resources.ExportProblemTitle, Resources.ExportProblem);
		}

		private void textBoxSelectedPath_TextChanged(object sender, EventArgs e)
		{
			ButtonExportEnabled();
		}

		private void smartGridMassmedia_ObjectChecked(PresentationObject presentationObject, bool state)
		{
			ButtonExportEnabled();
		}

		public void ButtonExportEnabled()
		{
			btnExport.Enabled = !string.IsNullOrEmpty(textBoxSelectedPath.Text) 
			                    && grdRadiostations.Added2Checked.Count > 0;
		}

        private void cmbRadioStationGroup_SelectedItemChanged(object sender, EventArgs e)
        {
			Massmedia.LoadRadiostationsByGroup(cmbRadioStationGroup, grdRadiostations);
        }
    }
}