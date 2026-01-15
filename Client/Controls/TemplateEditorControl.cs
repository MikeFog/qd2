using FogSoft.WinForm.Classes;
using Merlin.Classes;
using Merlin; // для RollerPositions
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;

namespace Merlin.Controls
{
    public partial class TemplateEditorControl : UserControl
    {
        public event EventHandler PositionChanged;

        public Button CalculateButton => btnCalculate;
        public NumericUpDown ManagerDiscountNum => nmManagerDiscount;

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

            rbDaysOfWeek.CheckedChanged += ScheduleMode_CheckedChanged;
            rbEvenOdd.CheckedChanged += ScheduleMode_CheckedChanged;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (DesignMode) return; // чтобы дизайнер не лез в БД

            LoadCities();
            SetManagerDiscount();

            InitPositions();

            rbDaysOfWeek.Checked = true;
            UpdateSchedulePatternEnabledState();

            dtEnd.Value = dtStart.Value.AddMonths(1);
        }

        private void InitPositions()
        {
            var items = new List<KeyValuePair<int, string>>
            {
                new KeyValuePair<int, string>((int)RollerPositions.Undefined, "без позиции"),
                new KeyValuePair<int, string>((int)RollerPositions.First, "первый"),
                new KeyValuePair<int, string>((int)RollerPositions.Second, "второй"),
                new KeyValuePair<int, string>((int)RollerPositions.Last, "последний"),
            };

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

        private void ScheduleMode_CheckedChanged(object sender, EventArgs e)
        {
            UpdateSchedulePatternEnabledState();
        }

        private void UpdateSchedulePatternEnabledState()
        {
            bool useDaysOfWeek = rbDaysOfWeek.Checked;

            flpDays.Enabled = useDaysOfWeek;
            groupBox1.Enabled = !useDaysOfWeek;
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

        public void DisplayTotal(decimal totalBeforePackageDiscount, decimal packageDiscount, decimal totalAfterPackage)
        {
            lblTotalBeforePackageDiscount.Text = "По радиостанциям: " + totalBeforePackageDiscount.ToString("c");
            lblPackageDiscount.Text = "Пакетная скидка: " + packageDiscount.ToString("N2");
            lblTotalAfterPackageDiscount.Text = "Итог: " + totalAfterPackage.ToString("c");
        }
    }
}
