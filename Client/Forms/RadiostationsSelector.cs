using FogSoft.WinForm.Classes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class RadiostationsSelector : Form
    {
        public RadiostationsSelector()
        {
            InitializeComponent();
        }

        public List<PresentationObject> SelectedItems
        {
            get
            {
                return radiostationList.SelectedItems;
            }
        }

        public DateTime DeadLine
        {
            get
            {
                return dtDeadLine.Value.Date;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            radiostationList.LoadData();
            base.OnLoad(e);
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (SelectedItems.Count == 0)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.NoRadiostationSelected);
                this.DialogResult = DialogResult.None;
                return;
            }
           
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
