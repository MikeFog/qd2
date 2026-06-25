using System;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class ActionActivateSettingsForm : Form
    {
        public bool TryTransferFailedIssues { get; private set; }
        public bool AllowDifferentWindowPrice { get; private set; }
        public int TransferAttemptCount { get; private set; }

        public ActionActivateSettingsForm()
        {
            InitializeComponent();
            SetTransferSettingsEnabled();
        }

        private void ChkTryTransferFailedIssues_CheckedChanged(object sender, EventArgs e)
        {
            SetTransferSettingsEnabled();
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            TryTransferFailedIssues = chkTryTransferFailedIssues.Checked;
            AllowDifferentWindowPrice = TryTransferFailedIssues && chkAllowDifferentWindowPrice.Checked;
            TransferAttemptCount = TryTransferFailedIssues ? (int)numTransferAttemptCount.Value : 0;
        }

        private void SetTransferSettingsEnabled()
        {
            grpTransferSettings.Enabled = chkTryTransferFailedIssues.Checked;
        }
    }
}
