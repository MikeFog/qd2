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
using System.Linq;

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
			if (!string.IsNullOrEmpty(txtPath2SaveTxt.Text))
				folderBrowserDialog.SelectedPath = txtPath2SaveTxt.Text;
			if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
			{
				txtPath2SaveTxt.Text = folderBrowserDialog.SelectedPath;
			}
		}

        private void btnFolderPath2_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPath2SaveWord.Text))
                folderBrowserDialog.SelectedPath = txtPath2SaveWord.Text;
            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                txtPath2SaveWord.Text = folderBrowserDialog.SelectedPath;
            }
        }

        private void btnExport_Click(object sender, EventArgs e)
		{
			if (Directory.Exists(txtPath2SaveTxt.Text))
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
			txtPath2SaveTxt.Enabled = enabled;
			btnFolderPath.Enabled = enabled;
			dateTimePicker.Enabled = enabled;
		}

		private void backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<PresentationObject> list = e.Argument as List<PresentationObject>;
			BackgroundWorker worker = sender as BackgroundWorker;
			DateTime dateTime = dateTimePicker.Value;
            string fileName = string.Empty;

            bool containsErrors = false;
			if (list != null && worker != null)
			{
				this.UseWaitCursor = true;
                int index = 0;
				foreach (Massmedia massmedia in list.Cast<Massmedia>())
				{
					try
					{
						GridReportCreator creater = new GridReportCreator(massmedia, dateTime, null);
						
						if(!string.IsNullOrEmpty(txtPath2SaveWord.Text))
						using (Grid report = creater.GetReport())
							{
								fileName = string.Format("{0}{1}{2}", txtPath2SaveWord.Text, Path.DirectorySeparatorChar, 
									ExportHelper.RemoveInvalidFileNameChars(massmedia.Name));
								report?.ExportToDisk(ExportHelper.CrystalExportFormatType, fileName + ExportHelper.CrystalExportFormatTypeExtension);
							}
						
						index++;
						worker.ReportProgress(index/(list.Count*2), index);
                        fileName = string.Format("{0}{1}{2}", txtPath2SaveTxt.Text, Path.DirectorySeparatorChar,
                                                        ExportHelper.RemoveInvalidFileNameChars(massmedia.Name));
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
				this.UseWaitCursor = false;
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
				Process.Start(txtPath2SaveTxt.Text);
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
			btnExport.Enabled = !string.IsNullOrEmpty(txtPath2SaveTxt.Text) 
			                    && grdRadiostations.Added2Checked.Count > 0;
		}

        private void cmbRadioStationGroup_SelectedItemChanged(object sender, EventArgs e)
        {
			Massmedia.LoadRadiostationsByGroup(cmbRadioStationGroup, grdRadiostations);
        }
    }
}