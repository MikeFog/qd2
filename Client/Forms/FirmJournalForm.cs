using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FogSoft.WinForm.Forms;
using FogSoft.WinForm.Classes;
using System.Windows.Forms;
using FogSoft.WinForm;
using System.Drawing;

namespace Merlin.Forms
{
    public class FirmJournalForm : MasterDetailForm
    {
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton tsbImport;

        public FirmJournalForm(string caption)
            : base(EntityManager.GetEntity((int)Entities.Firm),
                   EntityManager.GetEntity((int)Entities.FirmBrand), caption)
        {
            if (ConfigurationUtil.HasLegacyDBConnectionString)
            {
                toolStripSeparator1 = new ToolStripSeparator();
                tsbImport = new ToolStripButton();

                TsJournal.Items.Insert(3, toolStripSeparator1);
                TsJournal.Items.Insert(4, tsbImport);

                tsbImport.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
                tsbImport.Image = Globals.GetImage("Icons.Import.png");
                tsbImport.ImageTransparentColor = Color.Magenta;
                tsbImport.Name = "tsbImport";
                tsbImport.Size = new Size(41, 22);
                tsbImport.Text = "Импортировать";
                tsbImport.ToolTipText = "Импортировать фирмы...";
                tsbImport.Click += new EventHandler(tsbImport_Click);
            }
        }

        private void tsbImport_Click(object sender, EventArgs eventArgs)
        {
            FirmImportJournalForm form = new FirmImportJournalForm();
            form.ShowDialog(this);
        }
    }
}
