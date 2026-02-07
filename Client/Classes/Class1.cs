using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Merlin.Classes
{
    internal class Class1
    {
        public static List<CampaignCalcSnapshot> BuildTestSnapshotsForCp()
        {
            var list = new List<CampaignCalcSnapshot>();

            // ---------- ГРУППА 1 ----------
            list.Add(BuildSnapshot(
                dateFrom: new DateTime(2026, 2, 1),
                dateTo: new DateTime(2026, 3, 1),
                totalDays: 29,
                rows: new[]
                {
            BuildRow(1, "Европа Плюс", 20, RollerPositions.First, 63, 63, 24, 24, 30000),
            BuildRow(2, "Маруся ФМ",   20, RollerPositions.First, 63, 63, 24, 24, 28867),
                },
                grandTotal: 58867
            ));

            list.Add(BuildSnapshot(
    dateFrom: new DateTime(2026, 2, 1),
    dateTo: new DateTime(2026, 3, 11),
    totalDays: 29,
    rows: new[]
    {
            BuildRow(1, "Европа Плюс", 10, RollerPositions.First, 63, 63, 24, 24, 310000),
            BuildRow(2, "Маруся ФМ",   20, RollerPositions.First, 63, 63, 24, 24, 2867),
    },
    grandTotal: 9999
));

            list.Add(BuildSnapshot(
                dateFrom: new DateTime(2026, 2, 1),
                dateTo: new DateTime(2026, 3, 1),
                totalDays: 29,
                rows: new[]
                {
            BuildRow(1, "Европа Плюс", 20, RollerPositions.First, 63, 63, 24, 24, 31000),
            BuildRow(3, "Радио Дача",  20, RollerPositions.First, 63, 63, 24, 24, 27856),
                },
                grandTotal: 58856
            ));

            // ---------- ГРУППА 2 ----------
            list.Add(BuildSnapshot(
                dateFrom: new DateTime(2026, 4, 1),
                dateTo: new DateTime(2026, 4, 30),
                totalDays: 30,
                rows: new[]
                {
            BuildRow(4, "NRG",       15, RollerPositions.Last, 40, 40, 10, 10, 42000),
            BuildRow(5, "Хит FM",    15, RollerPositions.Last, 40, 40, 10, 10, 38000),
                },
                grandTotal: 80000
            ));

            list.Add(BuildSnapshot(
                dateFrom: new DateTime(2026, 4, 1),
                dateTo: new DateTime(2026, 4, 30),
                totalDays: 30,
                rows: new[]
                {
            BuildRow(4, "NRG",       15, RollerPositions.Last, 40, 40, 10, 10, 45000),
            BuildRow(6, "Юмор FM",   15, RollerPositions.Last, 40, 40, 10, 10, 37000),
                },
                grandTotal: 82000
            ));

            return list;
        }

        private static CampaignCalcSnapshot BuildSnapshot(
    DateTime dateFrom,
    DateTime dateTo,
    int totalDays,
    IEnumerable<CampaignCalcRow> rows,
    decimal grandTotal,
    int totalDuration = 100)
        {
            return new CampaignCalcSnapshot
            {
                DateFrom = dateFrom,
                DateTo = dateTo,
                TotalDays = totalDays,
                Rows = rows.ToList(),
                GrandTotal = grandTotal,
                TotalDuration = totalDuration
            };
        }

        private static CampaignCalcRow BuildRow(
            int massmediaId,
            string stationName,
            int rollerSec,
            RollerPositions position,
            int pw,
            int npw,
            int pwe,
            int npwe,
            decimal totalAmount)
        {
            return new CampaignCalcRow
            {
                MassmediaId = massmediaId,
                StationName = stationName,

                RollerDuration = rollerSec,
                Position = (int)position,

                PrimeTotalSpotsWeekday = pw,
                NonPrimeTotalSpotsWeekday = npw,
                PrimeTotalSpotsWeekend = pwe,
                NonPrimeTotalSpotsWeekend = npwe,

                TotalAmount = totalAmount
            };
        }
    }
}
