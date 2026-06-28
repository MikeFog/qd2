using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using Merlin.Classes;
using System;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class FrmTemplate2 : Form
    {
        private readonly IssueTemplate _template;

        public FrmTemplate2()
        {
            InitializeComponent();
        }

        public FrmTemplate2(string rollerName, IssueTemplate template) : this()
        {
            if (template != null && template.StartTime != DateTime.MinValue && template.FinishTime != DateTime.MinValue)
                _template = template;
            else
            {
                var (startDate, finishDate) = GetDefaultPeriod();
                _template = new IssueTemplate(startDate, finishDate,
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 12, 0, 0),
                    new DateTime(DateTime.Today.Year, DateTime.Today.Month, DateTime.Today.Day, 15, 0, 0),
                    2)
                {
                    IsModeAdd = true,
                    IgnoreWindowsWithTheSameFirmIssue = true
                };
                for (int i = 0; i < _template.WeekDays.Length; i++)
                    _template.WeekDays[i] = true;
            }
            lblRollerName.Text = rollerName;
        }

        private static (DateTime start, DateTime end) GetDefaultPeriod()
        {
            DateTime tomorrow = DateTime.Today.AddDays(1);
            int daysToSunday = (7 - (int)tomorrow.DayOfWeek) % 7;
            DateTime endOfWeek = tomorrow.AddDays(daysToSunday);
            return (tomorrow, endOfWeek);
        }

        private static void SetDateTimeValue(DateTimePicker control, DateTime date)
        {
            if (control.MinDate < date && control.MaxDate > date)
                control.Value = date;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            SetDateTimeValue(dtStartDate, _template.StartDate);
            SetDateTimeValue(dtFinishDate, _template.FinishDate);
            SetDateTimeValue(dtStartTime, _template.StartTime);
            SetDateTimeValue(dtFinishTime, _template.FinishTime);
            if (_template.Quantity > 0)
                txtQuantity.Value = _template.Quantity;

            clbWeekDays.Items.Clear();
            for (int i = 0; i < DateTimeUtils.WeekDayNames.Length; i++)
            {
                clbWeekDays.Items.Add(DateTimeUtils.WeekDayNames[i], _template.WeekDays[i]);
            }

            cbSplitPrime.Checked = _template.Quantity == 0;
            numQuantityPrime.Value = _template.QuantityPrime;
            numQuantityNonPrime.Value = _template.QuantityNonPrime;
            cbIgnoreWindows.Checked = _template.IgnoreWindowsWithTheSameFirmIssue;

            rbDays.Checked = _template.Day2AddMode == Day2AddMode.WeekDays;
            rbNumber.Checked = _template.Day2AddMode == Day2AddMode.OddEvenDays;
            rbOdd.Checked = _template.IsOdd;
            rbEven.Checked = !_template.IsOdd;
            SetEnabled();

            this.rbDays.Click += new System.EventHandler(this.groupButton_CheckChanged);
            this.rbNumber.Click += new System.EventHandler(this.groupButton_CheckChanged);
        }

        private void groupButton_CheckChanged(object sender, EventArgs e)
        {
            RadioButton senderRB = (RadioButton)sender;
            senderRB.Checked = !senderRB.Checked;
            if (sender == rbNumber)
                rbDays.Checked = !senderRB.Checked;
            else if (sender == rbDays)
                rbNumber.Checked = !senderRB.Checked;
            _template.IsOdd = rbOdd.Checked;
            _template.Day2AddMode = (rbNumber.Checked) ? Day2AddMode.OddEvenDays : Day2AddMode.WeekDays;
            SetEnabled();
        }

        public IssueTemplate Template
        {
            get => _template;
        }

        private void SaveData2Template()
        {
            _template.StartDate = dtStartDate.Value.Date;
            _template.FinishDate = dtFinishDate.Value.Date;
            _template.StartTime = dtStartTime.Value;
            _template.FinishTime = dtFinishTime.Value;
            _template.Quantity = cbSplitPrime.Checked ? 0 : ((int)txtQuantity.Value);
            _template.QuantityPrime = (int)numQuantityPrime.Value;
            _template.QuantityNonPrime = (int)numQuantityNonPrime.Value;

            // FIX: сохраняем выбор чёт/нечёт и режим шаблона при OK
            _template.IsOdd = rbOdd.Checked;
            _template.Day2AddMode = rbNumber.Checked ? Day2AddMode.OddEvenDays : Day2AddMode.WeekDays;

            _template.TemplateType = IssueTemplateType.TimePeriod;
            _template.IgnoreWindowsWithTheSameFirmIssue = cbIgnoreWindows.Checked;
        }

        private void clbWeekDays_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            _template.WeekDays[e.Index] = e.NewValue == CheckState.Checked;
        }

        private void btnOk_Click(object sender, EventArgs e)
        {
            SaveData2Template();
            bool errorFlag = false;

            if(cbSplitPrime.Checked && _template.QuantityNonPrime + _template.QuantityPrime == 0)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(Properties.Resources.PrimeAndNonPrimeQuantityMissing);
                errorFlag = true;
            }

            
            if(_template.StartDate > _template.FinishDate)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("TemplateStartFinishDateError"));
                errorFlag=true;
            }
            if (_template.StartTime >= _template.FinishTime)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation(MessageAccessor.GetMessage("TemplateStartFinishTimeError"));
                errorFlag = true;
            }
            if (errorFlag)
                DialogResult = DialogResult.None;
        }

        private void cbSplitPrime_CheckedChanged(object sender, EventArgs e)
        {
            numQuantityPrime.Enabled = numQuantityNonPrime.Enabled = cbSplitPrime.Checked;
            txtQuantity.Enabled = !cbSplitPrime.Checked;
        }

        private void SetEnabled()
        {
            rbEven.Enabled = rbNumber.Checked;
            rbOdd.Enabled = rbNumber.Checked;
            clbWeekDays.Enabled = rbDays.Checked;
        }
    }
}
