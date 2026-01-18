using FogSoft.WinForm.Classes;
using Merlin.Classes;
using System;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Controls
{
    public partial class TemplateEditorControl : UserControl
    {
        public event EventHandler PositionChanged;
        public event EventHandler SpotsSettingsChanged;
        public Button CalculateButton => btnCalculate; 
        public Button ExcelButton => btnExcel;
        public NumericUpDown ManagerDiscountNum => nmManagerDiscount;
        public Label TotalBeforePackageDiscount => lblTotalBeforePackageDiscount;
        public Label PackageDiscount => lblPackageDiscount;
        public Label TotalAfterPackageDiscount => lblTotalAfterPackageDiscount; 


        public RollerPositions SelectedPosition
        {
            get
            {
                if (cbPosition.SelectedValue == null) return RollerPositions.Undefined;
                return (RollerPositions)Convert.ToInt32(cbPosition.SelectedValue);
            }
        }

        public string SelectedPositionName
        {
            get
            {
                var text = cbPosition.Text;
                return string.IsNullOrWhiteSpace(text) ? SelectedPosition.ToString() : text;
            }
        }

        public TemplateEditorControl()
        {
            InitializeComponent();

            rbDaysOfWeek.CheckedChanged += ScheduleMode_CheckedChanged;
            rbEvenOdd.CheckedChanged += ScheduleMode_CheckedChanged;
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

        private void OnSpotsSettingsChanged(object sender, EventArgs e)
        {
            SpotsSettingsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ScheduleMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateSchedulePatternEnabledState();
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
                nmManagerDiscount.Minimum = nmManagerDiscount.Value = SecurityManager.LoggedUser.GetDiscaunt(DateTime.Today);
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

            rbDaysOfWeek.Checked = true;
            UpdateSchedulePatternEnabledState();

            dtEnd.Value = dtStart.Value.AddMonths(1);
        }
    }
}
