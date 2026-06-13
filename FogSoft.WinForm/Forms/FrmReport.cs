using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using CrystalDecisions.Windows.Forms;
using FogSoft.WinForm.Classes;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace FogSoft.WinForm.Forms
{
    public class FrmReport : Form
    {
        private CrystalReportViewer _viewer;
        private ToolStrip toolStrip1;
        private ToolStripButton btnSave;
        private Container components = null;
        private ToolStripButton btnSaveDirectly;
        private readonly string _fileName2SaveDirectly = string.Empty;
        private const string Path2SaveReports = "Path2SaveReports";


        public FrmReport()
        {
            InitializeComponent();
            if (Globals.MdiParent != null)
                Icon = Globals.MdiParent.Icon;
        }

        public FrmReport(object reportSource, string fileName2SaveDirectly) : this()
        {
            _viewer.ReportSource = reportSource;
            _fileName2SaveDirectly = fileName2SaveDirectly;
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FrmReport));
            this._viewer = new CrystalDecisions.Windows.Forms.CrystalReportViewer();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.btnSave = new System.Windows.Forms.ToolStripButton();
            this.btnSaveDirectly = new System.Windows.Forms.ToolStripButton();
            this.toolStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // _viewer
            // 
            this._viewer.ActiveViewIndex = -1;
            this._viewer.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this._viewer.Cursor = System.Windows.Forms.Cursors.Default;
            this._viewer.DisplayBackgroundEdge = false;
            this._viewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this._viewer.Location = new System.Drawing.Point(0, 33);
            this._viewer.Name = "_viewer";
            this._viewer.SelectionFormula = "";
            this._viewer.ShowCloseButton = false;
            this._viewer.ShowGotoPageButton = false;
            this._viewer.ShowGroupTreeButton = false;
            this._viewer.ShowRefreshButton = false;
            this._viewer.ShowTextSearchButton = false;
            this._viewer.Size = new System.Drawing.Size(616, 581);
            this._viewer.TabIndex = 0;
            this._viewer.ViewTimeSelectionFormula = "";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnSave,
            this.btnSaveDirectly});
            this.toolStrip1.Location = new System.Drawing.Point(0, 0);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(616, 33);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // btnSave
            // 
            this.btnSave.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSave.Image = global::FogSoft.WinForm.Properties.Resources.folder_download;
            this.btnSave.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(34, 28);
            this.btnSave.Text = "Сохранить как ...";
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnSaveDirectly
            // 
            this.btnSaveDirectly.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.btnSaveDirectly.Image = ((System.Drawing.Image)(resources.GetObject("btnSaveDirectly.Image")));
            this.btnSaveDirectly.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnSaveDirectly.Name = "btnSaveDirectly";
            this.btnSaveDirectly.Size = new System.Drawing.Size(34, 28);
            this.btnSaveDirectly.Text = "toolStripButton1";
            this.btnSaveDirectly.ToolTipText = "Сохранить в папку по умолчанию";
            this.btnSaveDirectly.Click += new System.EventHandler(this.btnSaveDirectly_Click);
            // 
            // FrmReport
            // 
            this.ClientSize = new System.Drawing.Size(616, 614);
            this.Controls.Add(this._viewer);
            this.Controls.Add(this.toolStrip1);
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
            this.Name = "FrmReport";
            this.Text = "Report";
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private void btnSave_Click(object sender, System.EventArgs e)
        {
            try
            {
                var report = _viewer.ReportSource as ReportDocument;

                if (report == null)
                {
                    MessageBox.ShowExclamation("Отчёт не загружен.");
                    return;
                }

                string folder = UserSettings.Load(Path2SaveReports) ?? string.Empty;

                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                }

                using (var dialog = new SaveFileDialog())
                {
                    dialog.InitialDirectory = folder;
                    dialog.FileName = _fileName2SaveDirectly;
                    dialog.Filter =
                        "PDF files (*.pdf)|*.pdf|" +
                        "Word files (*.doc)|*.doc";

                    dialog.FilterIndex = 2; // 1 = PDF, 2 = Word
                    dialog.DefaultExt = "doc";
                    dialog.AddExtension = true;
                    dialog.OverwritePrompt = true;

                    if (dialog.ShowDialog(this) != DialogResult.OK)
                        return;

                    string fileName = dialog.FileName;

                    ExportFormatType exportFormat;

                    switch (dialog.FilterIndex)
                    {
                        case 1:
                            exportFormat = ExportFormatType.PortableDocFormat;
                            break;

                        case 2:
                            exportFormat = ExportFormatType.WordForWindows;
                            break;

                        default:
                            MessageBox.ShowExclamation("Неизвестный формат экспорта.");
                            return;
                    }

                    UserSettings.Save(Path2SaveReports, Path.GetDirectoryName(fileName));

                    report.ExportToDisk(exportFormat, fileName);
                }
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        private void btnSaveDirectly_Click(object sender, EventArgs e)
        {
            try
            {
                string folder = UserSettings.Load(Path2SaveReports) ?? string.Empty;
                if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
                {
                    btnSave_Click(sender, e);
                    return;
                }

                if (!(_viewer.ReportSource is ReportDocument report))
                {
                    MessageBox.ShowExclamation("Отчёт не загружен.");
                    return;
                }

                string fileName = Path.Combine(folder, _fileName2SaveDirectly);
                report.ExportToDisk(ExportFormatType.WordForWindows, fileName);
                MessageBox.ShowCompleted($"Файл успено сохранён здесь: {fileName}");
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }
    }
}
