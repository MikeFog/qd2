using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;
using System;
using System.Collections.Generic;
using System.Data;
using System.Windows.Forms;

namespace Merlin.Forms
{
    public partial class PriceCalculatorForm : Form
    {
        public PriceCalculatorForm()
        {
            InitializeComponent();
            templateEditor.CalculateButton.Click += CalculateButton_Click;
            templateEditor.ManagerDiscountNum.ValueChanged += (s, e) => grdPriceCalculator.SetManagerDiscount(templateEditor.ManagerDiscount);

           templateEditor.PositionChanged += (s, e) =>
                grdPriceCalculator.SetDefaultPosition(templateEditor.SelectedPosition);
        }

        private void CalculateButton_Click(object sender, EventArgs e)
        {
            if (!templateEditor.IsDaysOfWeekValid())
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation("Выберите хотя бы один день недели");
                return;
            }

            if (templateEditor.DateFrom > templateEditor.DateTo)
            {
                FogSoft.WinForm.Forms.MessageBox.ShowExclamation("Дата начала периода акции не может быть больше даты окончания");
                return;
            }

            try
            {
                UseWaitCursor = true;
                Cursor.Current = Cursors.WaitCursor;
                Application.DoEvents();

                grdPriceCalculator.LoadData(templateEditor.MassmediaGroupId, templateEditor.DateFrom, templateEditor.DateTo);

                var selectedDates = BuildSelectedDates(
                    templateEditor.DateFrom, templateEditor.DateTo,
                    templateEditor.UseDaysOfWeek,
                    templateEditor.DaysOfWeekChecked,
                    templateEditor.EvenDaysSelected
                );

                grdPriceCalculator.ApplyCalculation(
                    selectedDates,
                    templateEditor.DurationSec,
                    templateEditor.PrimePerDayWeekday,
                    templateEditor.NonPrimePerDayWeekday,
                    templateEditor.PrimePerDayWeekend,
                    templateEditor.NonPrimePerDayWeekend,
                    templateEditor.ManagerDiscount
                );

                // ✅ протянуть позицию из шаблона в таблицу грида после пересоздания данных
                grdPriceCalculator.SetDefaultPosition(templateEditor.SelectedPosition);

                grdPriceCalculator.SummaryUpdater = UpdateSummary;
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
            finally
            {
                UseWaitCursor = false;
                Cursor.Current = Cursors.Default;
            }
        }

        private static List<DateTime> BuildSelectedDates(
            DateTime dateFrom,
            DateTime dateTo,
            bool useDaysOfWeek,
            bool[] daysOfWeekChecked, // 7 элементов: Mon..Sun
            bool evenDays // если useDaysOfWeek=false: true=чётные, false=нечётные
        )
        {
            var res = new List<DateTime>();
            for (var d = dateFrom.Date; d <= dateTo.Date; d = d.AddDays(1))
            {
                bool ok;
                if (useDaysOfWeek)
                {
                    // C# DayOfWeek: Sunday=0 ... Saturday=6
                    int idx = ((int)d.DayOfWeek + 6) % 7; // Mon=0..Sun=6
                    ok = daysOfWeekChecked[idx];
                }
                else
                {
                    ok = evenDays ? (d.Day % 2 == 0) : (d.Day % 2 == 1);
                }

                if (ok) res.Add(d);
            }
            return res;
        }

        private void UpdateSummary()
        {
            try
            {
                var selectedRows = grdPriceCalculator.GetSelectedRadiostations();
                decimal totalBeforePackageDiscount = grdPriceCalculator.GetSelectedTotalWithManagerDiscount();

                decimal packageDiscount = GetPackageDiscount(
                    startDate: templateEditor.DateFrom,
                    priceTotal: totalBeforePackageDiscount,
                    selectedRows: selectedRows
                );

                decimal totalAfterPackage = totalBeforePackageDiscount * packageDiscount;

                // проставить значения в колонках грида (только для отмеченных строк)
                grdPriceCalculator.ApplyPackageTotals(packageDiscount);

                templateEditor.DisplayTotal(totalBeforePackageDiscount, packageDiscount, totalAfterPackage);
            }
            catch (Exception ex)
            {
                ErrorManager.PublishError(ex);
            }
        }

        private decimal GetPackageDiscount(DateTime startDate, decimal priceTotal, List<DataRowView> selectedRows)
        {
            if (selectedRows.Count == 0) return 1m;

            var tvpTable = BuildSelectedMassmediaTvp(selectedRows);

            var p = DataAccessor.CreateParametersDictionary();
            p["startDate"] = startDate;
            p["campaignTypeID"] = (byte)1;
            p["priceTotal"] = priceTotal;
            p["sel"] = new FogSoft.WinForm.DataAccess.TvpValue(tvpTable, "dbo.pc_SelectedMassmedia");

            p["discountValue"] = null;               // OUT
            p["packageDiscountPriceListID"] = null;  // OUT (если нужно показать какой пакет)

            DataAccessor.ExecuteNonQuery("pc_PackageDiscountCalculateModel", p, 30, false);

            var dv = p["discountValue"];
            return (dv == null || dv == DBNull.Value) ? 1m : Convert.ToDecimal(dv);
        }

        private DataTable BuildSelectedMassmediaTvp(IEnumerable<DataRowView> selectedRows)
        {
            var dt = new DataTable();
            dt.Columns.Add("massmediaID", typeof(short));
            dt.Columns.Add("durationSec", typeof(int)); // issuesDuration (секунды)

            foreach (var drv in selectedRows)
            {
                int massmediaId = Convert.ToInt32(drv["massmediaID"]);

                int primeWd = Convert.ToInt32(drv["PrimeTotalSpotsWeekday"]);
                int nonPrimeWd = Convert.ToInt32(drv["NonPrimeTotalSpotsWeekday"]);

                int primeWe = Convert.ToInt32(drv["PrimeTotalSpotsWeekend"]);
                int nonPrimeWe = Convert.ToInt32(drv["NonPrimeTotalSpotsWeekend"]);

                int rollDuration = Convert.ToInt32(drv["RollerDuration"]);

                int totalSpots =
                    primeWd + nonPrimeWd +
                    primeWe + nonPrimeWe;

                int issuesDurationSec = rollDuration * totalSpots;

                dt.Rows.Add((short)massmediaId, issuesDurationSec);
            }

            return dt;
        }
    }
}
