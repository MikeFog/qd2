using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace Merlin.Classes
{
	/// <summary>
	/// Порог «Максимальный процент прайма» пакетной скидки в расчётах до размещения — калькулятор цены (qd2 и веб) и оценка
	/// Шаблона №3 (docs/tasks/package-discount-prime-limit.md). Общий файл qd2 и веб-ядра (FogSoft.Core подключает его ссылкой).
	/// У размещённых акций долю считает база (hlp_ActionCampaignPrimePercent).
	/// </summary>
	internal static class PackageDiscountPrime
	{
		/// <summary>Сегмент прайс-листа станции: есть ли в нём прайм по будням и по выходным (цена прайма ≠ офф-прайма).</summary>
		internal struct Segment
		{
			public Segment(DateTime start, DateTime end, decimal primeWeekday, decimal nonPrimeWeekday, decimal primeWeekend, decimal nonPrimeWeekend)
			{
				Start = start.Date;
				End = end.Date;
				HasPrimeWeekday = primeWeekday != nonPrimeWeekday;
				HasPrimeWeekend = primeWeekend != nonPrimeWeekend;
			}

			public DateTime Start { get; }
			public DateTime End { get; }
			public bool HasPrimeWeekday { get; }
			public bool HasPrimeWeekend { get; }
		}

		/// <summary>
		/// Выходы прайма строки: как и цена, раскладываются по сегментам пропорционально выбранным будням и выходным; праймом
		/// считаются только в сегментах, где цена прайма отличается от офф-прайма, — у однотарифной станции прайма нет, как в
		/// базе (день с одной ценой обычных окон — без прайма; уточнение заказчика 19.11.2026). Дни без прайс-листа — не прайм.
		/// </summary>
		public static decimal PrimeSpots(IEnumerable<Segment> segments, IEnumerable<DateTime> selectedDates, int primeWeekday, int primeWeekend)
		{
			if (segments == null || selectedDates == null)
				return 0m;

			var dates = selectedDates.Select(d => d.Date).ToList();
			var weekdays = dates.Where(d => !IsWeekend(d)).ToList();
			var weekends = dates.Where(IsWeekend).ToList();

			decimal spots = 0m;
			foreach (var s in segments)
			{
				if (weekdays.Count > 0 && s.HasPrimeWeekday && primeWeekday > 0)
					spots += (decimal)primeWeekday * weekdays.Count(d => s.Start <= d && d <= s.End) / weekdays.Count;
				if (weekends.Count > 0 && s.HasPrimeWeekend && primeWeekend > 0)
					spots += (decimal)primeWeekend * weekends.Count(d => s.Start <= d && d <= s.End) / weekends.Count;
			}
			return spots;
		}

		/// <summary>Секунды прайма: длительность ролика × выходы прайма, до целых секунд.</summary>
		public static int PrimeSeconds(decimal duration, decimal primeSpots)
		{
			return (int)Math.Round(duration * primeSpots, MidpointRounding.AwayFromZero);
		}

		/// <summary>
		/// Секунды прайма по станциям для процедур (тип <c>pc_MassmediaPrime</c>, параметр <c>@primes</c> у
		/// <c>pc_PackageDiscountCalculateModel</c> и <c>pc_CalcDiscountsModel</c>, Deploy/97). Станции без прайма не передаются — у них 0.
		/// </summary>
		public static DataTable PrimeTable(IEnumerable<KeyValuePair<int, int>> primeDurationByMassmedia)
		{
			var table = new DataTable();
			table.Columns.Add("massmediaID", typeof(short));
			table.Columns.Add("primeDurationSec", typeof(int));
			foreach (var g in primeDurationByMassmedia.Where(x => x.Value > 0).GroupBy(x => x.Key))
				table.Rows.Add((short)g.Key, g.Sum(x => x.Value));
			return table;
		}

		private static bool IsWeekend(DateTime d)
		{
			return d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
		}
	}
}
