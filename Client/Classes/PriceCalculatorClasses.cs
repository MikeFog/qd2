using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Merlin.Classes
{

    [Serializable]
    public class CampaignCalcSnapshot : IEquatable<CampaignCalcSnapshot>
    {
        // Параметры (верх формы)
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }

        public int TotalDays { get; set; }

        public int TotalDuration { get; set; }

        // Итог по всему расчёту
        public decimal GrandTotal { get; set; }

        // Template editor settings for restoration
        public int MassmediaGroupId { get; set; }
        public int DurationSec { get; set; }
        public int PrimePerDayWeekday { get; set; }
        public int NonPrimePerDayWeekday { get; set; }
        public int PrimePerDayWeekend { get; set; }
        public int NonPrimePerDayWeekend { get; set; }
        public decimal ManagerDiscountValue { get; set; }
        public bool ManagerDiscountModeSingle { get; set; }
        public int PositionValue { get; set; }

        // Schedule settings
        public bool UseDaysOfWeek { get; set; }
        public bool EvenDaysSelected { get; set; }
        public bool[] DaysOfWeekChecked { get; set; }
        public string GetRadiostationsList()
        {
            return Rows == null
                ? string.Empty
                : string.Join(", ", Rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.StationName))
                    .Select(r => r.StationName));
        }

        // Выбранные строки
        public List<CampaignCalcRow> Rows { get; set; } = new List<CampaignCalcRow>();

        /// <summary>
        /// Compares snapshots by date range and row contents.
        /// </summary>
        public bool Equals(CampaignCalcSnapshot other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            if (DateFrom.Date != other.DateFrom.Date || DateTo.Date != other.DateTo.Date)
                return false;

            var leftRows = Rows ?? new List<CampaignCalcRow>();
            var rightRows = other.Rows ?? new List<CampaignCalcRow>();

            if (leftRows.Count != rightRows.Count)
                return false;

            return leftRows.SequenceEqual(rightRows);
        }

        /// <summary>
        /// Compares snapshots by date range and row contents.
        /// </summary>
        public override bool Equals(object obj) => Equals(obj as CampaignCalcSnapshot);

        /// <summary>
        /// Gets a hash code based on date range and row contents.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + DateFrom.Date.GetHashCode();
                hash = (hash * 23) + DateTo.Date.GetHashCode();

                if (Rows != null)
                {
                    foreach (var row in Rows)
                    {
                        hash = (hash * 23) + (row != null ? row.GetHashCode() : 0);
                    }
                }

                return hash;
            }
        }
    }

    [Serializable]
    public class CampaignCalcRow : IEquatable<CampaignCalcRow>
    {
        // Если есть ключи/имя — заполним, если колонок нет — будут null/0
        public int MassmediaId { get; set; }
        public string StationName { get; set; }
        public string GroupName { get; set; }
        public string ShortName { get; set; }

        // Calc columns
        public int PrimeTotalSpotsWeekday { get; set; }
        public int NonPrimeTotalSpotsWeekday { get; set; }
        public int PrimeTotalSpotsWeekend { get; set; }
        public int NonPrimeTotalSpotsWeekend { get; set; }

        public int RollerDuration { get; set; }
        public int Position { get; set; }

        public decimal TotalBeforePackage { get; set; }
        public decimal PackageDiscount { get; set; }
        public decimal TotalAfterPackage { get; set; }

        public decimal CompanyDiscount { get; set; }
        public decimal TotalWithDiscount { get; set; }

        public decimal ManagerDiscount { get; set; }
        public decimal TotalAmount { get; set; }

        /// <summary>
        /// Compares rows by all persisted column values.
        /// </summary>
        public bool Equals(CampaignCalcRow other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            return MassmediaId == other.MassmediaId
                   && string.Equals(StationName, other.StationName, StringComparison.Ordinal)
                   && string.Equals(GroupName, other.GroupName, StringComparison.Ordinal)
                   && string.Equals(ShortName, other.ShortName, StringComparison.Ordinal)
                   && PrimeTotalSpotsWeekday == other.PrimeTotalSpotsWeekday
                   && NonPrimeTotalSpotsWeekday == other.NonPrimeTotalSpotsWeekday
                   && PrimeTotalSpotsWeekend == other.PrimeTotalSpotsWeekend
                   && NonPrimeTotalSpotsWeekend == other.NonPrimeTotalSpotsWeekend
                   && RollerDuration == other.RollerDuration
                   && Position == other.Position
                   && TotalBeforePackage == other.TotalBeforePackage
                   && PackageDiscount == other.PackageDiscount
                   && TotalAfterPackage == other.TotalAfterPackage
                   && CompanyDiscount == other.CompanyDiscount
                   && TotalWithDiscount == other.TotalWithDiscount
                   && ManagerDiscount == other.ManagerDiscount
                   && TotalAmount == other.TotalAmount;
        }

        /// <summary>
        /// Compares rows by all persisted column values.
        /// </summary>
        public override bool Equals(object obj) => Equals(obj as CampaignCalcRow);

        /// <summary>
        /// Gets a hash code based on all persisted column values.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = (hash * 23) + MassmediaId.GetHashCode();
                hash = (hash * 23) + (StationName != null ? StringComparer.Ordinal.GetHashCode(StationName) : 0);
                hash = (hash * 23) + (GroupName != null ? StringComparer.Ordinal.GetHashCode(GroupName) : 0);
                hash = (hash * 23) + (ShortName != null ? StringComparer.Ordinal.GetHashCode(ShortName) : 0);
                hash = (hash * 23) + PrimeTotalSpotsWeekday.GetHashCode();
                hash = (hash * 23) + NonPrimeTotalSpotsWeekday.GetHashCode();
                hash = (hash * 23) + PrimeTotalSpotsWeekend.GetHashCode();
                hash = (hash * 23) + NonPrimeTotalSpotsWeekend.GetHashCode();
                hash = (hash * 23) + RollerDuration.GetHashCode();
                hash = (hash * 23) + Position.GetHashCode();
                hash = (hash * 23) + TotalBeforePackage.GetHashCode();
                hash = (hash * 23) + PackageDiscount.GetHashCode();
                hash = (hash * 23) + TotalAfterPackage.GetHashCode();
                hash = (hash * 23) + CompanyDiscount.GetHashCode();
                hash = (hash * 23) + TotalWithDiscount.GetHashCode();
                hash = (hash * 23) + ManagerDiscount.GetHashCode();
                hash = (hash * 23) + TotalAmount.GetHashCode();
                return hash;
            }
        }
    }
}
