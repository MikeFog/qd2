namespace Merlin.Forms.GridReport
{
	partial class ExportGridForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.lblDate = new System.Windows.Forms.Label();
            this.textBoxSelectedPath = new System.Windows.Forms.TextBox();
            this.lblExportfolder = new System.Windows.Forms.Label();
            this.btnFolderPath = new System.Windows.Forms.Button();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.grdRadiostations = new FogSoft.WinForm.Controls.SmartGrid();
            this.btnExport = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.cmbRadioStationGroup = new FogSoft.WinForm.LookUp();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // dateTimePicker
            // 
            this.dateTimePicker.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dateTimePicker.CalendarFont = new System.Drawing.Font("Tahoma", 8.25F);
            this.dateTimePicker.Location = new System.Drawing.Point(12, 19);
            this.dateTimePicker.Name = "dateTimePicker";
            this.dateTimePicker.Size = new System.Drawing.Size(388, 21);
            this.dateTimePicker.TabIndex = 0;
            // 
            // lblDate
            // 
            this.lblDate.AutoSize = true;
            this.lblDate.Location = new System.Drawing.Point(12, 3);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(86, 13);
            this.lblDate.TabIndex = 1;
            this.lblDate.Text = "Дата экспорта:";
            // 
            // textBoxSelectedPath
            // 
            this.textBoxSelectedPath.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxSelectedPath.Location = new System.Drawing.Point(12, 58);
            this.textBoxSelectedPath.Name = "textBoxSelectedPath";
            this.textBoxSelectedPath.Size = new System.Drawing.Size(358, 21);
            this.textBoxSelectedPath.TabIndex = 2;
            this.textBoxSelectedPath.TextChanged += new System.EventHandler(this.textBoxSelectedPath_TextChanged);
            // 
            // lblExportfolder
            // 
            this.lblExportfolder.AutoSize = true;
            this.lblExportfolder.Location = new System.Drawing.Point(12, 44);
            this.lblExportfolder.Name = "lblExportfolder";
            this.lblExportfolder.Size = new System.Drawing.Size(113, 13);
            this.lblExportfolder.TabIndex = 3;
            this.lblExportfolder.Text = "Папка для экспорта:";
            // 
            // btnFolderPath
            // 
            this.btnFolderPath.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnFolderPath.Image = global::Merlin.Properties.Resources.open3;
            this.btnFolderPath.Location = new System.Drawing.Point(373, 58);
            this.btnFolderPath.Margin = new System.Windows.Forms.Padding(0);
            this.btnFolderPath.Name = "btnFolderPath";
            this.btnFolderPath.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnFolderPath.Size = new System.Drawing.Size(27, 21);
            this.btnFolderPath.TabIndex = 5;
            this.btnFolderPath.UseVisualStyleBackColor = true;
            this.btnFolderPath.Click += new System.EventHandler(this.btnFolderPath_Click);
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.Description = "Выберите папку для экспорта";
            // 
            // grdRadiostations
            // 
            this.grdRadiostations.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdRadiostations.Caption = "Радиостанции";
            this.grdRadiostations.CaptionVisible = true;
            this.grdRadiostations.CheckBoxes = true;
            this.grdRadiostations.ColumnNameHighlight = null;
            this.grdRadiostations.DataSource = null;
            this.grdRadiostations.DependantGrid = null;
            this.grdRadiostations.Entity = null;
            this.grdRadiostations.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdRadiostations.IsHighlightInvertColor = false;
            this.grdRadiostations.IsNeedHighlight = false;
            this.grdRadiostations.Location = new System.Drawing.Point(12, 124);
            this.grdRadiostations.MenuEnabled = false;
            this.grdRadiostations.Name = "grdRadiostations";
            this.grdRadiostations.QuickSearchVisible = false;
            this.grdRadiostations.SelectedObject = null;
            this.grdRadiostations.Size = new System.Drawing.Size(385, 295);
            this.grdRadiostations.TabIndex = 7;
            this.grdRadiostations.ObjectChecked += new FogSoft.WinForm.ObjectCheckedDelegate(this.smartGridMassmedia_ObjectChecked);
            // 
            // btnExport
            // 
            this.btnExport.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnExport.Location = new System.Drawing.Point(322, 425);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 23);
            this.btnExport.TabIndex = 8;
            this.btnExport.Text = "Экспорт";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // progressBar
            // 
            this.progressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.progressBar.ForeColor = System.Drawing.Color.LimeGreen;
            this.progressBar.Location = new System.Drawing.Point(13, 425);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(303, 23);
            this.progressBar.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            this.progressBar.TabIndex = 9;
            this.progressBar.Visible = false;
            // 
            // backgroundWorker
            // 
            this.backgroundWorker.WorkerReportsProgress = true;
            this.backgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.backgroundWorker_DoWork);
            this.backgroundWorker.ProgressChanged += new System.ComponentModel.ProgressChangedEventHandler(this.backgroundWorker_ProgressChanged);
            this.backgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.backgroundWorker_RunWorkerCompleted);
            // 
            // cmbRadioStationGroup
            // 
            this.cmbRadioStationGroup.AccessibleRole = System.Windows.Forms.AccessibleRole.MenuBar;
            this.cmbRadioStationGroup.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.cmbRadioStationGroup.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbRadioStationGroup.IsNullable = false;
            this.cmbRadioStationGroup.Location = new System.Drawing.Point(12, 97);
            this.cmbRadioStationGroup.Name = "cmbRadioStationGroup";
            this.cmbRadioStationGroup.SelectedIndex = -1;
            this.cmbRadioStationGroup.SelectedValue = null;
            this.cmbRadioStationGroup.Size = new System.Drawing.Size(385, 21);
            this.cmbRadioStationGroup.TabIndex = 55;
            this.cmbRadioStationGroup.SelectedItemChanged += new System.EventHandler(this.cmbRadioStationGroup_SelectedItemChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 81);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(47, 13);
            this.label1.TabIndex = 56;
            this.label1.Text = "Группа:";
            // 
            // ExportGridForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(409, 460);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cmbRadioStationGroup);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.grdRadiostations);
            this.Controls.Add(this.btnFolderPath);
            this.Controls.Add(this.lblExportfolder);
            this.Controls.Add(this.textBoxSelectedPath);
            this.Controls.Add(this.lblDate);
            this.Controls.Add(this.dateTimePicker);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(334, 487);
            this.Name = "ExportGridForm";
            this.ShowInTaskbar = false;
            this.Text = "Экспорт сеток вещания";
            this.ResumeLayout(false);
            this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.DateTimePicker dateTimePicker;
		private System.Windows.Forms.Label lblDate;
		private System.Windows.Forms.TextBox textBoxSelectedPath;
		private System.Windows.Forms.Label lblExportfolder;
		private System.Windows.Forms.Button btnFolderPath;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private FogSoft.WinForm.Controls.SmartGrid grdRadiostations;
		private System.Windows.Forms.Button btnExport;
		private System.Windows.Forms.ProgressBar progressBar;
		private System.ComponentModel.BackgroundWorker backgroundWorker;
        private FogSoft.WinForm.LookUp cmbRadioStationGroup;
        private System.Windows.Forms.Label label1;
    }
}