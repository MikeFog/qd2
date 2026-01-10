using Merlin.Classes;
using System;
using System.Windows.Forms;

namespace Merlin.Forms
{

    public partial class PrintMediaPlanSettings : Form
    {
        public readonly PrintSettings Settings = new PrintSettings();

        public PrintMediaPlanSettings()
        {
            InitializeComponent();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            // Сохранение значений чекбоксов
            Settings.PrintWithSignatures = chkPrintWithSignatures.Checked;
            Settings.ShowAdvertisingInfo = chkShowAdvertisingInfo.Checked;
            Settings.HideTariffPrice = chkShowOnlyFinalCost.Checked;
        }
    }
}
