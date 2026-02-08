using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Controls
{
    public partial class TemplateEditorControl : UserControl
    {
        public event EventHandler PositionChanged;
        public event EventHandler SpotsSettingsChanged;
        public event EventHandler DurationChanged;
        public event EventHandler ScheduleChanged;
        public event EventHandler ManagerDiscountModeChanged;

        private bool _loaded;

        public Button CalculateButton => btnCalculate;
        public Button ExcelButton => btnExcel;
        public Button SaveButton => btnSave;
        public NumericUpDown ManagerDiscountNum => nmManagerDiscount;
        public Label TotalAfterPackageDiscount => lblTotalAfterPackageDiscount;

        public RollerPositions SelectedPosition
        {
            get
            {
                if (cbPosition.SelectedValue == null) return RollerPositions.Undefined;
                return (RollerPositions)Convert.ToInt32(cbPosition.SelectedValue);
            }
        }

        public TemplateEditorControl()
        {
            InitializeComponent();

            rbManagerDiscountPeriod.CheckedChanged += (s, e) =>
            {
                ManagerDiscountModeChanged?.Invoke(this, EventArgs.Empty);
                nmManagerDiscount.Enabled = rbManagerDiscountSingle.Checked;
            };
            //rbManagerDiscountSingle.CheckedChanged += (s, e) => ManagerDiscountModeChanged?.Invoke(this, EventArgs.Empty);

            rbDaysOfWeek.CheckedChanged += ScheduleMode_CheckedChanged;
            rbEvenOdd.CheckedChanged += ScheduleMode_CheckedChanged;
            rbEvenDays.CheckedChanged += OnScheduleChanged;
            rbOddDays.CheckedChanged += OnScheduleChanged;

            dtStart.ValueChanged += (s, e) => { SetManagerDiscount(); };
            dtEnd.ValueChanged += (s, e) => { SetManagerDiscount(); };

            // Дни недели
            chkMon.CheckedChanged += OnScheduleChanged;
            chkTue.CheckedChanged += OnScheduleChanged;
            chkWed.CheckedChanged += OnScheduleChanged;
            chkThu.CheckedChanged += OnScheduleChanged;
            chkFri.CheckedChanged += OnScheduleChanged;
            chkSat.CheckedChanged += OnScheduleChanged;
            chkSun.CheckedChanged += OnScheduleChanged;
        }

        private void InitPositions()
        {
            var items = Issue.GetRollerPositionItems();

            cbPosition.BeginUpdate();
            try
            {
                cbPosition.DisplayMember = "Value";
                cbPosition.ValueMember = "Key";
                cbPosition.DataSource = items;
                cbPosition.SelectedValue = (int)RollerPositions.Undefined;
            }
            finally
            {
                cbPosition.EndUpdate();
            }

            cbPosition.SelectedValueChanged -= CbPosition_SelectedValueChanged;
            cbPosition.SelectedValueChanged += CbPosition_SelectedValueChanged;
        }

        private void CbPosition_SelectedValueChanged(object sender, EventArgs e)
        {
            PositionChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnSpotsSettingsChanged(object sender, EventArgs e) =>
            SpotsSettingsChanged?.Invoke(this, EventArgs.Empty);

        private void OnDurationChanged(object sender, EventArgs e) =>
            DurationChanged?.Invoke(this, EventArgs.Empty);

        private void ScheduleMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateSchedulePatternEnabledState();
            OnScheduleChanged(sender, e);
        }

        private void UpdateSchedulePatternEnabledState()
        {
            bool useDaysOfWeek = rbDaysOfWeek.Checked;

            flpDays.Enabled = useDaysOfWeek;
            flpOdds.Enabled = !useDaysOfWeek;
        }

        private void SetManagerDiscount()
        {
            if (SecurityManager.LoggedUser.IsAdmin)
            {
                nmManagerDiscount.Minimum = 0;
                nmManagerDiscount.Value = 1;
            }
            else
            {
                nmManagerDiscount.Value = nmManagerDiscount.Minimum =  SecurityManager.LoggedUser.GetDiscount(dtStart.Value, dtEnd.Value);
            }
        }

        private void LoadCities()
        {
            // твой метод, который возвращает DataView
            DataView dv = Massmedia.LoadGroupsWithShowAllOption();

            cbCity.BeginUpdate();
            try
            {
                cbCity.DataSource = dv;

                // ВАЖНО: поставь реальные имена колонок.
                // Если колонки без имён — см. блок ниже (как узнать имена).
                cbCity.ValueMember = dv.Table.Columns[0].ColumnName;
                cbCity.DisplayMember = dv.Table.Columns[1].ColumnName;

                cbCity.SelectedValue = 1;
            }
            finally
            {
                cbCity.EndUpdate();
            }
        }

        public int MassmediaGroupId
        {
            get
            {
                if (cbCity.SelectedValue == null) return 0;
                return Convert.ToInt32(cbCity.SelectedValue);
            }
        }

        public string MassmediaGroupName => cbCity.Text;
        public DateTime DateFrom => dtStart.Value.Date;
        public DateTime DateTo => dtEnd.Value.Date;
        public int DurationSec => Convert.ToInt32(nudDuration.Value);
        public int PrimePerDayWeekday => Convert.ToInt32(nudPrimeWeekday.Value);
        public int NonPrimePerDayWeekday => Convert.ToInt32(nudNonPrimeWeekday.Value);
        public int PrimePerDayWeekend => Convert.ToInt32(numPrimeWeekend.Value);
        public int NonPrimePerDayWeekend => Convert.ToInt32(numNonPrimeWeekend.Value);

        public decimal ManagerDiscount
        {
            get => nmManagerDiscount.Value;
        }

        public bool ManagerDiscountModeSingle
        {
            get => rbManagerDiscountSingle.Checked;
        }

        public bool[] DaysOfWeekChecked
        {
            get
            {
                // Порядок ВАЖЕН: Mon..Sun (0..6)
                return new[]
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
        }

        public bool UseDaysOfWeek
        {
            get { return rbDaysOfWeek.Checked; }
        }

        public bool EvenDaysSelected
        {
            get { return rbEvenDays.Checked; }
        }

        public bool IsDaysOfWeekValid()
        {
            if (!UseDaysOfWeek) return true;
            return DaysOfWeekChecked.Any(x => x);
        }

        // Setters for loading saved variants
        public void SetMassmediaGroupId(int groupId)
        {
            if (cbCity.DataSource == null) return;
            cbCity.SelectedValue = groupId;
        }

        public void SetDateRange(DateTime dateFrom, DateTime dateTo)
        {
            _loaded = false;
            dtStart.Value = dateFrom;
            dtEnd.Value = dateTo;
            _loaded = true;
        }

        public void SetDurationSec(int durationSec)
        {
            nudDuration.Value = Math.Max(nudDuration.Minimum, Math.Min(nudDuration.Maximum, durationSec));
        }

        public void SetSpotsSettings(int primeWeekday, int nonPrimeWeekday, int primeWeekend, int nonPrimeWeekend)
        {
            nudPrimeWeekday.Value = Math.Max(nudPrimeWeekday.Minimum, Math.Min(nudPrimeWeekday.Maximum, primeWeekday));
            nudNonPrimeWeekday.Value = Math.Max(nudNonPrimeWeekday.Minimum, Math.Min(nudNonPrimeWeekday.Maximum, nonPrimeWeekday));
            numPrimeWeekend.Value = Math.Max(numPrimeWeekend.Minimum, Math.Min(numPrimeWeekend.Maximum, primeWeekend));
            numNonPrimeWeekend.Value = Math.Max(numNonPrimeWeekend.Minimum, Math.Min(numNonPrimeWeekend.Maximum, nonPrimeWeekend));
        }

        public void SetManagerDiscountSettings(decimal discount, bool isSingleMode)
        {
            rbManagerDiscountSingle.Checked = isSingleMode;
            rbManagerDiscountPeriod.Checked = !isSingleMode;
            if (isSingleMode)
            {
                nmManagerDiscount.Value = Math.Max(nmManagerDiscount.Minimum, Math.Min(nmManagerDiscount.Maximum, discount));
            }
        }

        public void SetPosition(int positionValue)
        {
            if (cbPosition.DataSource == null) return;
            cbPosition.SelectedValue = positionValue;
        }

        public void SetSchedulePattern(bool useDaysOfWeek, bool evenDaysSelected, bool[] daysOfWeekChecked)
        {
            _loaded = false;
            
            if (useDaysOfWeek)
            {
                rbDaysOfWeek.Checked = true;
                if (daysOfWeekChecked != null && daysOfWeekChecked.Length >= 7)
                {
                    chkMon.Checked = daysOfWeekChecked[0];
                    chkTue.Checked = daysOfWeekChecked[1];
                    chkWed.Checked = daysOfWeekChecked[2];
                    chkThu.Checked = daysOfWeekChecked[3];
                    chkFri.Checked = daysOfWeekChecked[4];
                    chkSat.Checked = daysOfWeekChecked[5];
                    chkSun.Checked = daysOfWeekChecked[6];
                }
            }
            else
            {
                rbEvenOdd.Checked = true;
                rbEvenDays.Checked = evenDaysSelected;
                rbOddDays.Checked = !evenDaysSelected;
            }

            UpdateSchedulePatternEnabledState();
            _loaded = true;
        }

        private void OnScheduleChanged(object sender, EventArgs e)
        {
            if (!_loaded) return;
            ScheduleChanged?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (DesignMode) return;
            LoadCities();
            SetManagerDiscount();
            InitPositions();

            // подписки на изменения количеств выходов
            nudPrimeWeekday.ValueChanged += OnSpotsSettingsChanged;
            nudNonPrimeWeekday.ValueChanged += OnSpotsSettingsChanged;
            numPrimeWeekend.ValueChanged += OnSpotsSettingsChanged;
            numNonPrimeWeekend.ValueChanged += OnSpotsSettingsChanged;

            nudDuration.ValueChanged += OnDurationChanged;

            AttachEmptyToZeroGuard(nudPrimeWeekday, nudNonPrimeWeekday, numPrimeWeekend, numNonPrimeWeekend, nudDuration);

            rbDaysOfWeek.Checked = true;
            UpdateSchedulePatternEnabledState();

            dtEnd.Value = dtStart.Value.AddMonths(1);

            _loaded = true;
        }

        private void AttachEmptyToZeroGuard(params NumericUpDown[] controls)
        {
            foreach (var nud in controls)
            {
                if (nud == null) continue;
                nud.Validating -= NumericOnValidating;
                nud.Validating += NumericOnValidating;
            }
        }

        private void NumericOnValidating(object sender, CancelEventArgs e)
        {
            var nud = sender as NumericUpDown;
            if (nud == null)
                return;

            if (!string.IsNullOrWhiteSpace(nud.Text))
                return;

            decimal fallback = 0m;
            if (fallback < nud.Minimum || fallback > nud.Maximum)
                fallback = nud.Minimum;

            nud.Value = fallback;
        }
    }
}