using System;
using System.Windows.Forms;

namespace Merlin.Controls
{
    public partial class TemplateEditorControl : UserControl
    {
        public TemplateEditorControl()
        {
            InitializeComponent();
        }

        private void TemplateEditorControl_Load(object sender, EventArgs e)
        {
            // Set initial state - Days of Week mode is checked by default
            rbDaysOfWeek.Checked = true;
            
            // Apply the correct enabled state based on the selection
            UpdateControlsState();
        }

        private void rbDaysOfWeek_CheckedChanged(object sender, EventArgs e)
        {
            // When Days of Week is selected, update the enabled state
            UpdateControlsState();
        }

        private void rbEvenOdd_CheckedChanged(object sender, EventArgs e)
        {
            // When Even/Odd is selected, update the enabled state
            UpdateControlsState();
        }

        private void UpdateControlsState()
        {
            if (rbDaysOfWeek.Checked)
            {
                // Days of Week mode: enable flpDays, disable groupBox1
                flpDays.Enabled = true;
                groupBox1.Enabled = false;
            }
            else if (rbEvenOdd.Checked)
            {
                // Even/Odd mode: enable groupBox1, disable flpDays
                flpDays.Enabled = false;
                groupBox1.Enabled = true;
            }
        }

        /// <summary>
        /// Gets whether the Days of Week mode is selected
        /// </summary>
        public bool IsDaysOfWeekMode
        {
            get { return rbDaysOfWeek.Checked; }
        }

        /// <summary>
        /// Gets whether the Even/Odd mode is selected
        /// </summary>
        public bool IsEvenOddMode
        {
            get { return rbEvenOdd.Checked; }
        }

        /// <summary>
        /// Gets the selected days of the week (Monday=0, Sunday=6)
        /// </summary>
        public bool[] GetSelectedDays()
        {
            return new bool[]
            {
                chkMon.Checked,
                chkTue.Checked,
                chkWed.Checked,
                chkThu.Checked,
                chkFri.Checked,
                chkSat.Checked,
                chkSun.Checked
            };
        }

        /// <summary>
        /// Sets the selected days of the week (Monday=0, Sunday=6)
        /// </summary>
        public void SetSelectedDays(bool[] days)
        {
            if (days != null && days.Length >= 7)
            {
                chkMon.Checked = days[0];
                chkTue.Checked = days[1];
                chkWed.Checked = days[2];
                chkThu.Checked = days[3];
                chkFri.Checked = days[4];
                chkSat.Checked = days[5];
                chkSun.Checked = days[6];
            }
        }

        /// <summary>
        /// Gets whether Even Days is selected (only valid in Even/Odd mode)
        /// </summary>
        public bool IsEvenDays
        {
            get { return rbEvenDays.Checked; }
            set { rbEvenDays.Checked = value; }
        }

        /// <summary>
        /// Gets whether Odd Days is selected (only valid in Even/Odd mode)
        /// </summary>
        public bool IsOddDays
        {
            get { return rbOddDays.Checked; }
            set { rbOddDays.Checked = value; }
        }
    }
}
