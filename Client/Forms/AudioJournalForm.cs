using System;
using System.Drawing;
using System.Windows.Forms;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Forms;
using Merlin.Classes;
using Merlin.Controls;
using MessageBox = FogSoft.WinForm.Forms.MessageBox;

namespace Merlin.Forms
{
    public class AudioJournalForm : JournalForm, IMediaControlContainer
    {
        private readonly MediaControl mediaControl;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton tsbListen;
        private ToolStripButton tsbStop;
		private ToolStripSeparator toolStripSeparator2;
		private ToolStripButton tsbExport;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton tsbDeleteAll;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton tsbImport;

        public AudioJournalForm(Entity entity, string caption) : base(entity, caption)
        {
            mediaControl = new MediaControl(this);

            this.ClientSize = new System.Drawing.Size(800, 377);
        }

        protected override void Dispose(bool disposing)
        {
            if (mediaControl != null)
                mediaControl.Stop();
            base.Dispose(disposing);
        }

        #region IMediaControlContainer Members

        public bool IsPlaying
        {
			set { tsbStop.Enabled = value; }
        }

        #endregion

        protected override void SubInitializeComponent()
        {
            base.SubInitializeComponent();

            tsbListen = new ToolStripButton();
            tsbStop = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
			toolStripSeparator2 = new ToolStripSeparator();
        	tsbExport = new ToolStripButton();

            TsJournal.Items.Insert(3, toolStripSeparator1);
            TsJournal.Items.Insert(4, tsbListen);
            TsJournal.Items.Insert(5, tsbStop);
			TsJournal.Items.Insert(6, toolStripSeparator2);
			TsJournal.Items.Insert(7, tsbExport);

            tsbListen.DisplayStyle = ToolStripItemDisplayStyle.Image;
			tsbListen.Image = Globals.GetImage(Constants.ActionsImages.Play);
            tsbListen.ImageTransparentColor = Color.Magenta;
            tsbListen.Name = "tsbListen";
            tsbListen.Size = new Size(41, 22);
            tsbListen.Text = "Прослушать ролик";
            tsbListen.ToolTipText = "Прослушать ролик";
            tsbListen.Click += new EventHandler(tsbListen_Click);

            tsbStop.DisplayStyle = ToolStripItemDisplayStyle.Image;
            tsbStop.Enabled = false;
			tsbStop.Image = Globals.GetImage(Constants.ActionsImages.Stop);
            tsbStop.ImageTransparentColor = Color.Magenta;
            tsbStop.Name = "tsbStop";
            tsbStop.Size = new Size(23, 22);
            tsbStop.Text = "Остановить прослушивание";
            tsbStop.ToolTipText = "Остановить прослушивание";
            tsbStop.Click += new EventHandler(tsbStop_Click);

			tsbExport.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
			tsbExport.Enabled = true;
			tsbExport.Image = Globals.GetImage(Constants.ActionsImages.Save);
			tsbExport.ImageTransparentColor = Color.Magenta;
			tsbExport.Name = "tsbExport";
			tsbExport.Size = new Size(41, 22);
			tsbExport.Text = "Сохранить";
			tsbExport.ToolTipText = "Экспортировать ролики";
			tsbExport.Click += new EventHandler(tsbExport_Click);

            if (SecurityManager.LoggedUser.IsAdmin)
            {
                toolStripSeparator3 = new ToolStripSeparator();
                tsbDeleteAll = new ToolStripButton();

                TsJournal.Items.Insert(8, toolStripSeparator3);
                TsJournal.Items.Insert(9, tsbDeleteAll);

                tsbDeleteAll.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                tsbDeleteAll.Enabled = true;
                tsbDeleteAll.Image = Globals.GetImage(Constants.ActionsImages.Delete);
                tsbDeleteAll.ImageTransparentColor = Color.Magenta;
                tsbDeleteAll.Name = "tsbDeleteAll";
                tsbDeleteAll.Size = new Size(41, 22);
                tsbDeleteAll.Text = "Удалить все";
                tsbDeleteAll.ToolTipText = "Удалить все не используемые ролики";
                tsbDeleteAll.Click += new EventHandler(tsbDeleteAll_Click);
            }

            if (ConfigurationUtil.HasLegacyDBConnectionString)
            {
                toolStripSeparator4 = new ToolStripSeparator();
                tsbImport = new ToolStripButton();

                TsJournal.Items.Insert(10, toolStripSeparator4);
                TsJournal.Items.Insert(11, tsbImport);

                tsbImport.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                tsbImport.Image = Globals.GetImage("Icons.Import.png");
                tsbImport.ImageTransparentColor = Color.Magenta;
                tsbImport.Name = "tsbImport";
                tsbImport.Size = new Size(41, 22);
                tsbImport.Text = "Импортировать";
                tsbImport.ToolTipText = "Импортировать ролики...";
                tsbImport.Click += new EventHandler(tsbImport_Click);
            }
        }

        private void tsbListen_Click(object sender, EventArgs e)
        {
            if (Grid != null && Grid.SelectedObject != null)
            {
                mediaControl.Play((Roller) Grid.SelectedObject);
            }
        }

        private void tsbStop_Click(object sender, EventArgs e)
        {
            mediaControl.Stop();
        }

		private void tsbExport_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.Description = "Выбирете каталог для сохранения роликов";
			if (dlg.ShowDialog(this) == DialogResult.OK)
			{
				RollersCopyFrm frm = new RollersCopyFrm(Grid.DataSource, dlg.SelectedPath);
				frm.ShowDialog(this);
			}
		}

        private void tsbDeleteAll_Click(object sender, EventArgs e)
        {
            if (this.Grid.InternalGrid != null && this.Grid.InternalGrid.Rows != null && this.Grid.InternalGrid.Rows.Count > 0)
            {
                if (MessageBox.ShowQuestion("Удалить все отображаемые ролики вместе с физическими файлами?") == DialogResult.Yes)
                {
                    RollersDeleteFrm frm = new RollersDeleteFrm(Grid.DataSource);
                    frm.ShowDialog(this);
                }
            }
        }

        private void tsbImport_Click(object sender, EventArgs eventArgs)
        {
            AudioJournalImportForm form = new AudioJournalImportForm();
            form.ShowDialog(this);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // AudioJournalForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.ClientSize = new System.Drawing.Size(756, 377);
            this.Name = "AudioJournalForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}