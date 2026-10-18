using System;
using System.Collections.Generic;
using System.Linq;

namespace Merlin.Classes
{
	/// <summary>
	/// Модель цен калькулятора на период (docs/tasks/web-price-calculator.md, §2.1, этап К.1): станции с сегментами
	/// прайс-листов и периоды сезонного коэффициента выбранного менеджера. Читает <see cref="PriceCalculatorData.LoadModel"/>.
	/// </summary>
	public sealed class PcModel
	{
		public DateTime DateFrom { get; set; }
		public DateTime DateTo { get; set; }
		public int MassmediaGroupId { get; set; }
		public int UserId { get; set; }
		public List<PcStation> Stations { get; } = new List<PcStation>();
		/// <summary>Периоды UserDiscount менеджера как есть, без промежутков (их добавляет <see cref="PriceCalculatorEngine.FillRatioGaps"/>).</summary>
		public List<PcRatioPeriod> RatioPeriods { get; } = new List<PcRatioPeriod>();
	}

	public sealed class PcStation
	{
		public int MassmediaId { get; set; }
		/// <summary>Название с группой — «Радиостанция» таблицы.</summary>
		public string Name { get; set; }
		public string GroupName { get; set; }
		public string ShortName { get; set; }
		/// <summary>Сегменты (прайс-лист ∩ период) по возрастанию начала.</summary>
		public List<PcSegment> Segments { get; } = new List<PcSegment>();
	}

	/// <summary>Сегмент — часть периода внутри одного прайс-листа: цены за секунду и наценки за позицию.</summary>
	public sealed class PcSegment
	{
		public int PricelistId { get; set; }
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
		public decimal PrimeWeekday { get; set; }
		public decimal NonPrimeWeekday { get; set; }
		public decimal PrimeWeekend { get; set; }
		public decimal NonPrimeWeekend { get; set; }
		/// <summary>Наценки прайс-листа в процентах: «Первый», «Второй», «Последний».</summary>
		public decimal ExtraFirst { get; set; }
		public decimal ExtraSecond { get; set; }
		public decimal ExtraLast { get; set; }
	}

	public sealed class PcRatioPeriod
	{
		public PcRatioPeriod(DateTime start, DateTime end, decimal ratio)
		{
			Start = start;
			End = end;
			Ratio = ratio;
		}

		public DateTime Start { get; }
		public DateTime End { get; }
		public decimal Ratio { get; }
	}

	/// <summary>Строка таблицы: что можно править (количества выходов за период, длительность, позиция) и отметка.</summary>
	public sealed class PcRowInput
	{
		public int MassmediaId { get; set; }
		public bool Selected { get; set; }
		/// <summary>«Пр-ть ролика», секунды.</summary>
		public decimal Duration { get; set; }
		/// <summary>Позиция: значения <see cref="RollerPositions"/> (0 — «Не определен»).</summary>
		public int Position { get; set; }
		public int PrimeWeekday { get; set; }
		public int NonPrimeWeekday { get; set; }
		public int PrimeWeekend { get; set; }
		public int NonPrimeWeekend { get; set; }

		public int Spots => PrimeWeekday + NonPrimeWeekday + PrimeWeekend + NonPrimeWeekend;
	}

	/// <summary>Правило сезонного коэффициента (ВК-1 б): «Сезонный коэфф.:» одним числом или «Актуальный сезонный коэфф.» по периодам.</summary>
	public sealed class PcRatioRule
	{
		public bool Single { get; set; }
		/// <summary>При <see cref="Single"/> — значение поля; 0 и меньше считается как 1, как в десктопе.</summary>
		public decimal SingleRatio { get; set; } = 1m;
		/// <summary>Без <see cref="Single"/> — периоды менеджера; дни вне них — с коэффициентом 1 (К-1).</summary>
		public IList<PcRatioPeriod> Periods { get; set; }
	}

	/// <summary>Цена строки до скидок: «Цена кампании» T и она же с сезонным коэффициентом W.</summary>
	public sealed class PcRowPrice
	{
		/// <summary>«Цена кампании», округлена до копеек — так её получает объёмная скидка (К-29).</summary>
		public decimal TariffPrice { get; set; }
		/// <summary>T с сезонным коэффициентом, не округлена: одним числом T × m, по периодам Σ частей × коэффициент периода.</summary>
		public decimal WithManagerRatio { get; set; }
		/// <summary>«Сезонный коэфф.» строки: одним числом — m, по периодам — W / T (до округления T).</summary>
		public decimal ManagerRatio { get; set; }
		/// <summary>Выбранные дни, для которых у станции нет прайс-листа: выходы на них идут по 0 (К-3).</summary>
		public int UncoveredDays { get; set; }
	}

	/// <summary>Строка для процедуры скидок (pc_CalcDiscountsModel): T и хронометраж — для объёмной и пакетной скидки.</summary>
	public sealed class PcDiscountRow
	{
		public int MassmediaId { get; set; }
		public decimal TariffPrice { get; set; }
		public int DurationSec { get; set; }
		public bool Selected { get; set; }
	}

	public sealed class PcDiscounts
	{
		/// <summary>«Объёмная скидка» по станции.</summary>
		public Dictionary<int, decimal> Company { get; } = new Dictionary<int, decimal>();
		/// <summary>«Пакетная скидка» отмеченных станций; 1 — пакета нет.</summary>
		public decimal Package { get; set; } = 1m;
	}

	public sealed class PcRowResult
	{
		public PcRowInput Input { get; set; }
		public PcRowPrice Price { get; set; }
		public decimal CompanyDiscount { get; set; }
		/// <summary>T × «Объёмная скидка».</summary>
		public decimal TotalWithDiscount { get; set; }
		/// <summary>«Цена до пакетной скидки»: W × «Объёмная скидка».</summary>
		public decimal TotalBeforePackage { get; set; }
		/// <summary>«Пакетная скидка» и «Итог» — только у отмеченных строк.</summary>
		public decimal? PackageDiscount { get; set; }
		public decimal? Total { get; set; }
	}

	public sealed class PcResult
	{
		public List<PcRowResult> Rows { get; } = new List<PcRowResult>();
		/// <summary>«Итог:» — сумма «Итог» отмеченных строк.</summary>
		public decimal Total { get; set; }
		public decimal PackageDiscount { get; set; } = 1m;
		/// <summary>Выходов и секунд эфира у отмеченных строк — «Количество рекламных выпусков», «Хронометраж эфирного времени».</summary>
		public int Spots { get; set; }
		public int Seconds { get; set; }
		/// <summary>Дата, на которую считаются объёмная и пакетная скидка: первый выбранный день.</summary>
		public DateTime DiscountDate { get; set; }
	}

	/// <summary>
	/// Расчёт калькулятора цены без WinForms и базы — арифметика десктопной сетки <c>PriceCalculatorGrid</c> после этапа
	/// К.0 один в один (docs/tasks/web-price-calculator.md, §2.2): цена строки — сумма по сегментам прайс-листов с
	/// долями выбранных будней и выходных, наценкой за позицию и сезонным коэффициентом; скидки считает SQL одним вызовом
	/// (<see cref="PriceCalculatorData.Discounts"/>). Десктоп переводится на этот класс в К.6.
	/// </summary>
	public static class PriceCalculatorEngine
	{
		public const decimal MaxDuration = 3600m;
		public const int MaxSpots = 1000000;

		/// <summary>
		/// Выбранные дни периода: по дням недели (Пн … Вс) или по чётности числа месяца — как
		/// <c>PriceCalculatorForm.BuildSelectedDates</c>.
		/// </summary>
		public static List<DateTime> SelectDates(DateTime dateFrom, DateTime dateTo, bool useDaysOfWeek, bool[] daysOfWeek, bool evenDays)
		{
			var result = new List<DateTime>();
			for (var d = dateFrom.Date; d <= dateTo.Date; d = d.AddDays(1))
			{
				bool ok = useDaysOfWeek
					? daysOfWeek[((int)d.DayOfWeek + 6) % 7]
					: (evenDays ? d.Day % 2 == 0 : d.Day % 2 == 1);
				if (ok)
					result.Add(d);
			}
			return result;
		}

		public static bool IsWeekend(DateTime d)
		{
			return d.DayOfWeek == DayOfWeek.Saturday || d.DayOfWeek == DayOfWeek.Sunday;
		}

		/// <summary>
		/// Строка по шаблону выходов: количества за период = число выбранных будней (выходных) × «в день»; длительность и
		/// позиция — из шаблона. Отметка — переданная.
		/// </summary>
		public static PcRowInput TemplateRow(int massmediaId, IList<DateTime> dates, int duration,
			int primeWeekdayPerDay, int nonPrimeWeekdayPerDay, int primeWeekendPerDay, int nonPrimeWeekendPerDay,
			int position, bool selected)
		{
			int weekend = dates.Count(IsWeekend);
			int weekday = dates.Count - weekend;
			return new PcRowInput
			{
				MassmediaId = massmediaId,
				Selected = selected,
				Duration = duration,
				Position = position,
				PrimeWeekday = weekday * primeWeekdayPerDay,
				NonPrimeWeekday = weekday * nonPrimeWeekdayPerDay,
				PrimeWeekend = weekend * primeWeekendPerDay,
				NonPrimeWeekend = weekend * nonPrimeWeekendPerDay,
			};
		}

		/// <summary>
		/// Периоды коэффициента без промежутков: дни вне периодов UserDiscount — с коэффициентом 1 (К-1), коэффициент 0 и
		/// меньше — как 1; без периодов — один период на всё время с 1. Как <c>GetManagerDiscountPeriods</c> десктопа.
		/// </summary>
		public static List<PcRatioPeriod> FillRatioGaps(IEnumerable<PcRatioPeriod> periods)
		{
			var sorted = (periods ?? Enumerable.Empty<PcRatioPeriod>())
				.Select(p => new PcRatioPeriod(p.Start.Date, p.End.Date, p.Ratio > 0m ? p.Ratio : 1m))
				.OrderBy(p => p.Start)
				.ToList();
			if (sorted.Count == 0)
				return new List<PcRatioPeriod> { new PcRatioPeriod(DateTime.MinValue, DateTime.MaxValue, 1m) };

			var covered = new List<PcRatioPeriod>(sorted.Count * 2 + 1);
			DateTime gapStart = DateTime.MinValue.Date;
			bool tailOpen = true;
			foreach (var p in sorted)
			{
				if (tailOpen && p.Start > gapStart)
					covered.Add(new PcRatioPeriod(gapStart, p.Start.AddDays(-1), 1m));
				covered.Add(p);

				if (p.End >= DateTime.MaxValue.Date)
					tailOpen = false;
				else if (p.End.AddDays(1) > gapStart)
					gapStart = p.End.AddDays(1);
			}
			if (tailOpen)
				covered.Add(new PcRatioPeriod(gapStart, DateTime.MaxValue.Date, 1m));
			return covered;
		}

		/// <summary>
		/// «Цена кампании» строки и она же с сезонным коэффициентом. Один цикл сегмент × период: «одним числом» — один
		/// период с этим коэффициентом на всё время. Выходы строки делятся между сегментами дробно — по доле выбранных
		/// будней (выходных) сегмента; цена части = (цены × выходы) × длительность × (1 + наценка за позицию / 100).
		/// </summary>
		public static PcRowPrice Price(PcStation station, PcRowInput row, IList<DateTime> selectedDates, PcRatioRule rule)
		{
			var result = new PcRowPrice { ManagerRatio = 1m };
			if (rule.Single)
				result.ManagerRatio = rule.SingleRatio > 0m ? rule.SingleRatio : 1m;

			var weekdays = new List<DateTime>();
			var weekends = new List<DateTime>();
			foreach (var d in selectedDates.Select(x => x.Date).OrderBy(x => x))
				(IsWeekend(d) ? weekends : weekdays).Add(d);

			var segments = (station?.Segments ?? new List<PcSegment>()).OrderBy(s => s.Start).ToList();
			result.UncoveredDays = weekdays.Concat(weekends).Count(d => !segments.Any(s => s.Start.Date <= d && d <= s.End.Date));

			decimal duration = Clamp(row.Duration, 0m, MaxDuration);
			if (segments.Count == 0 || selectedDates.Count == 0 || duration <= 0m)
				return result;

			// выходов будних/выходных при отсутствии таких дней в выборке не бывает — их количества не участвуют
			decimal primeWd = weekdays.Count > 0 ? Clamp(row.PrimeWeekday, 0, MaxSpots) : 0;
			decimal nonPrimeWd = weekdays.Count > 0 ? Clamp(row.NonPrimeWeekday, 0, MaxSpots) : 0;
			decimal primeWe = weekends.Count > 0 ? Clamp(row.PrimeWeekend, 0, MaxSpots) : 0;
			decimal nonPrimeWe = weekends.Count > 0 ? Clamp(row.NonPrimeWeekend, 0, MaxSpots) : 0;

			var periods = rule.Single
				? new List<PcRatioPeriod> { new PcRatioPeriod(DateTime.MinValue, DateTime.MaxValue, result.ManagerRatio) }
				: FillRatioGaps(rule.Periods);

			decimal baseTotal = 0m;
			decimal withManager = 0m;
			foreach (var s in segments)
			{
				DateTime segStart = s.Start.Date;
				DateTime segEnd = s.End.Date;
				foreach (var p in periods)
				{
					DateTime partStart = segStart > p.Start ? segStart : p.Start;
					DateTime partEnd = segEnd < p.End ? segEnd : p.End;
					if (partStart > partEnd) continue;

					int wdInPart = CountDatesInRange(weekdays, partStart, partEnd);
					int weInPart = CountDatesInRange(weekends, partStart, partEnd);
					if (wdInPart == 0 && weInPart == 0) continue;

					decimal wdShare = weekdays.Count > 0 ? (decimal)wdInPart / weekdays.Count : 0m;
					decimal weShare = weekends.Count > 0 ? (decimal)weInPart / weekends.Count : 0m;

					decimal partAmount =
						(s.PrimeWeekday * (primeWd * wdShare) +
						 s.NonPrimeWeekday * (nonPrimeWd * wdShare) +
						 s.PrimeWeekend * (primeWe * weShare) +
						 s.NonPrimeWeekend * (nonPrimeWe * weShare))
						* duration;

					decimal extra = ExtraPercent(s, row.Position);
					if (extra != 0m)
						partAmount *= 1m + extra / 100m;

					baseTotal += partAmount;
					withManager += partAmount * p.Ratio;
				}
			}

			if (rule.Single)
			{
				// как десктоп «одним числом»: коэффициент к уже округлённой T
				result.TariffPrice = RoundMoney(baseTotal);
				result.WithManagerRatio = result.TariffPrice * result.ManagerRatio;
			}
			else
			{
				result.ManagerRatio = baseTotal > 0m ? withManager / baseTotal : 1m;
				result.TariffPrice = RoundMoney(baseTotal);
				result.WithManagerRatio = withManager;
			}
			return result;
		}

		/// <summary>Строки для процедуры скидок: T и хронометраж строки (длительность × выходы).</summary>
		public static PcDiscountRow DiscountRow(PcRowInput row, PcRowPrice price)
		{
			return new PcDiscountRow
			{
				MassmediaId = row.MassmediaId,
				TariffPrice = price.TariffPrice,
				DurationSec = Convert.ToInt32(Clamp(row.Duration, 0m, MaxDuration)) * row.Spots,
				Selected = row.Selected,
			};
		}

		/// <summary>Дата объёмной и пакетной скидки — первый выбранный день; без выбранных дней — переданная (начало периода).</summary>
		public static DateTime DiscountDate(IList<DateTime> selectedDates, DateTime fallback)
		{
			return selectedDates.Count > 0 ? selectedDates.Min().Date : fallback.Date;
		}

		/// <summary>
		/// Итоги по ценам строк и скидкам: «Цена до пакетной скидки» = W × объёмная, «Итог» отмеченной строки = она же ×
		/// пакетная, «Итог:» — сумма по отмеченным, выходы и секунды — по отмеченным.
		/// </summary>
		public static PcResult Finish(IList<PcRowInput> rows, IDictionary<int, PcRowPrice> prices, PcDiscounts discounts, DateTime discountDate)
		{
			var result = new PcResult { PackageDiscount = discounts.Package, DiscountDate = discountDate };
			decimal seconds = 0m;
			foreach (var row in rows)
			{
				PcRowPrice price = prices[row.MassmediaId];
				decimal company;
				if (!discounts.Company.TryGetValue(row.MassmediaId, out company))
					company = 1m;

				var r = new PcRowResult
				{
					Input = row,
					Price = price,
					CompanyDiscount = company,
					TotalWithDiscount = price.TariffPrice * company,
					TotalBeforePackage = price.WithManagerRatio * company,
				};
				if (row.Selected)
				{
					r.PackageDiscount = discounts.Package;
					r.Total = r.TotalBeforePackage * discounts.Package;
					result.Total += r.Total.Value;
					result.Spots += Clamp(row.Spots, 0, int.MaxValue);
					seconds += row.Spots * Clamp(row.Duration, 0m, MaxDuration);
				}
				result.Rows.Add(r);
			}
			result.Seconds = (int)Math.Round(seconds);
			return result;
		}

		/// <summary>
		/// Весь расчёт по модели: цены строк, скидки одним вызовом <paramref name="discounts"/> (в вебе —
		/// <see cref="PriceCalculatorData.Discounts"/>), итоги.
		/// </summary>
		public static PcResult Calculate(PcModel model, IList<PcRowInput> rows, IList<DateTime> selectedDates, PcRatioRule rule,
			Func<DateTime, IList<PcDiscountRow>, PcDiscounts> discounts)
		{
			var stations = model.Stations.ToDictionary(s => s.MassmediaId);
			var prices = new Dictionary<int, PcRowPrice>();
			var discountRows = new List<PcDiscountRow>();
			foreach (var row in rows)
			{
				PcStation station;
				stations.TryGetValue(row.MassmediaId, out station);
				var price = Price(station, row, selectedDates, rule);
				prices[row.MassmediaId] = price;
				discountRows.Add(DiscountRow(row, price));
			}

			DateTime date = DiscountDate(selectedDates, model.DateFrom);
			// без выбранных дней объёмная скидка в десктопе считалась на сегодня; строки при этом нулевые — результат тот же
			PcDiscounts d = discounts(date, discountRows);
			return Finish(rows, prices, d, date);
		}

		/// <summary>
		/// Цены за секунду для таблицы — наименьшая и наибольшая по сегментам станции (прежде показывалась только наибольшая,
		/// и период через Новый год выглядел по ценам следующего года, К-30).
		/// </summary>
		public static void PriceRange(PcStation station, Func<PcSegment, decimal> price, out decimal min, out decimal max)
		{
			min = max = 0m;
			if (station == null || station.Segments.Count == 0) return;
			min = station.Segments.Min(price);
			max = station.Segments.Max(price);
		}

		public static decimal RoundMoney(decimal value)
		{
			return Math.Round(value, 2, MidpointRounding.AwayFromZero);
		}

		private static decimal ExtraPercent(PcSegment s, int position)
		{
			if (position == (int)RollerPositions.First) return s.ExtraFirst;
			if (position == (int)RollerPositions.Second) return s.ExtraSecond;
			if (position == (int)RollerPositions.Last) return s.ExtraLast;
			return 0m;
		}

		private static int CountDatesInRange(List<DateTime> sortedDates, DateTime start, DateTime end)
		{
			int lo = LowerBound(sortedDates, start);
			int hi = UpperBound(sortedDates, end);
			return Math.Max(0, hi - lo);
		}

		private static int LowerBound(List<DateTime> a, DateTime x)
		{
			int l = 0, r = a.Count;
			while (l < r)
			{
				int m = (l + r) / 2;
				if (a[m] < x) l = m + 1; else r = m;
			}
			return l;
		}

		private static int UpperBound(List<DateTime> a, DateTime x)
		{
			int l = 0, r = a.Count;
			while (l < r)
			{
				int m = (l + r) / 2;
				if (a[m] <= x) l = m + 1; else r = m;
			}
			return l;
		}

		private static decimal Clamp(decimal value, decimal min, decimal max)
		{
			return value < min ? min : (value > max ? max : value);
		}

		private static int Clamp(int value, int min, int max)
		{
			return value < min ? min : (value > max ? max : value);
		}
	}
}
