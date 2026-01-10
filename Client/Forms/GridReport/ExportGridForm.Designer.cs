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
            this.txtPath2SaveTxt = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            this.grdRadiostations = new FogSoft.WinForm.Controls.SmartGrid();
            this.btnExport = new System.Windows.Forms.Button();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.backgroundWorker = new System.ComponentModel.BackgroundWorker();
            this.cmbRadioStationGroup = new FogSoft.WinForm.LookUp();
            this.label1 = new System.Windows.Forms.Label();
            this.btnFolderPath2 = new System.Windows.Forms.Button();
            this.txtPath2SaveWord = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.btnFolderPath = new System.Windows.Forms.Button();
            this.dateTimePicker = new System.Windows.Forms.DateTimePicker();
            this.lblDate = new System.Windows.Forms.Label();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtPath2SaveTxt
            // 
            this.txtPath2SaveTxt.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtPath2SaveTxt.Location = new System.Drawing.Point(8, 108);
            this.txtPath2SaveTxt.Name = "txtPath2SaveTxt";
            this.txtPath2SaveTxt.Size = new System.Drawing.Size(487, 31);
            this.txtPath2SaveTxt.TabIndex = 2;
            this.txtPath2SaveTxt.TextChanged += new System.EventHandler(this.textBoxSelectedPath_TextChanged);
            // 
            // folderBrowserDialog
            // 
            this.folderBrowserDialog.Description = "Выберите папку для экспорта";
            // 
            // grdRadiostations
            // 
            this.grdRadiostations.Caption = "Радиостанции";
            this.grdRadiostations.CaptionVisible = true;
            this.grdRadiostations.CheckBoxes = true;
            this.grdRadiostations.ColumnNameHighlight = null;
            this.grdRadiostations.DataSource = null;
            this.grdRadiostations.DependantGrid = null;
            this.grdRadiostations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grdRadiostations.Entity = null;
            this.grdRadiostations.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.grdRadiostations.IsHighlightInvertColor = false;
            this.grdRadiostations.IsNeedHighlight = false;
            this.grdRadiostations.Location = new System.Drawing.Point(9, 286);
            this.grdRadiostations.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.grdRadiostations.MenuEnabled = false;
            this.grdRadiostations.Name = "grdRadiostations";
            this.grdRadiostations.QuickSearchVisible = false;
            this.grdRadiostations.SelectedObject = null;
            this.grdRadiostations.ShowMultiselectColumn = true;
            this.grdRadiostations.Size = new System.Drawing.Size(485, 310);
            this.grdRadiostations.TabIndex = 7;
            this.grdRadiostations.ObjectChecked += new FogSoft.WinForm.ObjectCheckedDelegate(this.smartGridMassmedia_ObjectChecked);
            // 
            // btnExport
            // 
            this.btnExport.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnExport.Location = new System.Drawing.Point(178, 652);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(146, 44);
            this.btnExport.TabIndex = 8;
            this.btnExport.Text = "Экспорт";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // progressBar
            // 
            this.progressBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.progressBar.ForeColor = System.Drawing.Color.LimeGreen;
            this.progressBar.Location = new System.Drawing.Point(8, 602);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(487, 44);
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
            this.cmbRadioStationGroup.Dock = System.Windows.Forms.DockStyle.Top;
            this.cmbRadioStationGroup.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.cmbRadioStationGroup.IsNullable = false;
            this.cmbRadioStationGroup.Location = new System.Drawing.Point(8, 247);
            this.cmbRadioStationGroup.Name = "cmbRadioStationGroup";
            this.cmbRadioStationGroup.SelectedIndex = -1;
            this.cmbRadioStationGroup.SelectedValue = null;
            this.cmbRadioStationGroup.Size = new System.Drawing.Size(487, 33);
            this.cmbRadioStationGroup.TabIndex = 55;
            this.cmbRadioStationGroup.SelectedItemChanged += new System.EventHandler(this.cmbRadioStationGroup_SelectedItemChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(8, 220);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(71, 24);
            this.label1.TabIndex = 56;
            this.label1.Text = "Группа:";
            // 
            // btnFolderPath2
            // 
            this.btnFolderPath2.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnFolderPath2.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnFolderPath2.Image = global::Merlin.Properties.Resources.open3;
            this.btnFolderPath2.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnFolderPath2.Location = new System.Drawing.Point(5, 142);
            this.btnFolderPath2.Margin = new System.Windows.Forms.Padding(0);
            this.btnFolderPath2.Name = "btnFolderPath2";
            this.btnFolderPath2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnFolderPath2.Size = new System.Drawing.Size(493, 41);
            this.btnFolderPath2.TabIndex = 59;
            this.btnFolderPath2.Text = "Выбор папки для файлов Word";
            this.btnFolderPath2.UseVisualStyleBackColor = false;
            this.btnFolderPath2.Click += new System.EventHandler(this.btnFolderPath2_Click);
            // 
            // txtPath2SaveWord
            // 
            this.txtPath2SaveWord.Dock = System.Windows.Forms.DockStyle.Top;
            this.txtPath2SaveWord.Location = new System.Drawing.Point(8, 186);
            this.txtPath2SaveWord.Name = "txtPath2SaveWord";
            this.txtPath2SaveWord.Size = new System.Drawing.Size(487, 31);
            this.txtPath2SaveWord.TabIndex = 58;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.btnFolderPath, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.btnExport, 0, 10);
            this.tableLayoutPanel1.Controls.Add(this.progressBar, 0, 9);
            this.tableLayoutPanel1.Controls.Add(this.grdRadiostations, 0, 8);
            this.tableLayoutPanel1.Controls.Add(this.dateTimePicker, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.lblDate, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.txtPath2SaveWord, 0, 5);
            this.tableLayoutPanel1.Controls.Add(this.txtPath2SaveTxt, 0, 3);
            this.tableLayoutPanel1.Controls.Add(this.btnFolderPath2, 0, 4);
            this.tableLayoutPanel1.Controls.Add(this.label1, 0, 6);
            this.tableLayoutPanel1.Controls.Add(this.cmbRadioStationGroup, 0, 7);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(5);
            this.tableLayoutPanel1.RowCount = 11;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(503, 704);
            this.tableLayoutPanel1.TabIndex = 60;
            // 
            // btnFolderPath
            // 
            this.btnFolderPath.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnFolderPath.Dock = System.Windows.Forms.DockStyle.Top;
            this.btnFolderPath.Image = global::Merlin.Properties.Resources.open3;
            this.btnFolderPath.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnFolderPath.Location = new System.Drawing.Point(5, 66);
            this.btnFolderPath.Margin = new System.Windows.Forms.Padding(0);
            this.btnFolderPath.Name = "btnFolderPath";
            this.btnFolderPath.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnFolderPath.Size = new System.Drawing.Size(493, 39);
            this.btnFolderPath.TabIndex = 6;
            this.btnFolderPath.Text = "Выбор папки для текстовых файлов";
            this.btnFolderPath.UseVisualStyleBackColor = false;
            this.btnFolderPath.Click += new System.EventHandler(this.btnFolderPath_Click);
            // 
            // dateTimePicker
            // 
            this.dateTimePicker.CalendarFont = new System.Drawing.Font("Segoe UI Variable Display", 9F);
            this.dateTimePicker.Dock = System.Windows.Forms.DockStyle.Top;
            this.dateTimePicker.Location = new System.Drawing.Point(8, 32);
            this.dateTimePicker.Name = "dateTimePicker";
            this.dateTimePicker.Size = new System.Drawing.Size(487, 31);
            this.dateTimePicker.TabIndex = 2;
            // 
            // lblDate
            // 
            this.lblDate.AutoSize = true;
            this.lblDate.Dock = System.Windows.Forms.DockStyle.Top;
            this.lblDate.Location = new System.Drawing.Point(8, 5);
            this.lblDate.Name = "lblDate";
            this.lblDate.Size = new System.Drawing.Size(487, 24);
            this.lblDate.TabIndex = 3;
            this.lblDate.Text = "Дата экспорта:";
            // 
            // ExportGridForm
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(503, 704);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Font = new System.Drawing.Font("Segoe UI Variable Display", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(334, 487);
            this.Name = "ExportGridForm";
            this.ShowInTaskbar = false;
            this.Text = "Экспорт сеток вещания";
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

		}

		#endregion
		private System.Windows.Forms.TextBox txtPath2SaveTxt;
		private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog;
		private FogSoft.WinForm.Controls.SmartGrid grdRadiostations;
		private System.Windows.Forms.Button btnExport;
		private System.Windows.Forms.ProgressBar progressBar;
		private System.ComponentModel.BackgroundWorker backgroundWorker;
        private FogSoft.WinForm.LookUp cmbRadioStationGroup;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnFolderPath2;
        private System.Windows.Forms.TextBox txtPath2SaveWord;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.DateTimePicker dateTimePicker;
        private System.Windows.Forms.Label lblDate;
        private System.Windows.Forms.Button btnFolderPath;
    }
}