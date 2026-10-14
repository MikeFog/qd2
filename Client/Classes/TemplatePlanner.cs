using System;
using System.Collections.Generic;
using System.Linq;

namespace Merlin.Classes
{
	/// <summary>Окно линейной сетки или слот веера — кандидат на выпуск шаблона.</summary>
	public sealed class TemplateWindow
	{
		/// <summary>Номер окна (линейка) или слота (веер) — как его знает читатель; планировщик его не разбирает.</summary>
		public int Key { get; set; }

		/// <summary>Начало окна (дата и время).</summary>
		public DateTime Start { get; set; }

		/// <summary>Остаток времени с неподтверждёнными выпусками, секунды: чем больше, тем раньше окно берётся.</summary>
		public int FreeSeconds { get; set; }

		public bool IsPrime { get; set; }
	}

	public enum TemplateGroup
	{
		/// <summary>Выпуски вперемешку (задано «в день» без разбивки).</summary>
		All,
		Prime,
		OffPrime,
	}

	/// <summary>Окна, выбранные для одной группы дня, в порядке постановки.</summary>
	public sealed class TemplatePick
	{
		internal TemplatePick() { }

		public TemplateGroup Group { get; internal set; }

		/// <summary>Сколько выпусков просили.</summary>
		public int Wanted { get; internal set; }

		public List<TemplateWindow> Windows { get; internal set; }

		/// <summary>Окон нашлось меньше, чем просили.</summary>
		public bool IsShort => Windows.Count < Wanted;
	}

	/// <summary>
	/// Выбор окон дня для Шаблонов 1–3 — чистая логика без базы, одна на линейку и веер. Правила — десктопные
	/// (FrmGenerator.AddSimpleIssues, TariffWithRangeGrid.AddIssuesRangeTimePeriod): сначала самые свободные
	/// окна, среди равных — случайное; задано «в день» — берутся первые N, иначе отдельно прайм и офф-прайм.
	/// Случайность — внедряемый <see cref="Random"/>: с зерном выбор воспроизводим (стенд tools/template-planner-check).
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public static class TemplatePlanner
	{
		/// <summary>
		/// Окна с началом в интервале [start; finish] минут от полуночи. Конец включается у линейки
		/// (FrmGenerator: <c>(hour*60+min) &lt;= finish</c>) и исключается у веера
		/// (TariffWithRangeGrid: <c>TimeOfDay &lt; finish</c>) — паритет с десктопом, решение 14.10.2026.
		/// </summary>
		public static List<TemplateWindow> InTimeRange(IEnumerable<TemplateWindow> windows, int startMinute, int finishMinute, bool finishInclusive)
		{
			return windows.Where(w =>
			{
				int minute = w.Start.Hour * 60 + w.Start.Minute;
				return minute >= startMinute && (finishInclusive ? minute <= finishMinute : minute < finishMinute);
			}).ToList();
		}

		/// <summary>
		/// Окна дня для запуска. <paramref name="quantity"/> &gt; 0 — одна группа <see cref="TemplateGroup.All"/>;
		/// иначе группы прайм и офф-прайм (с нулевым заданием не возвращаются). Прайм у кандидата уже определён читателем:
		/// у линейки — цена равна максимальной за день (GetMaxPriceByDay), у веера — признак слота.
		/// </summary>
		public static List<TemplatePick> PickDay(IEnumerable<TemplateWindow> candidates, int quantity, int quantityPrime, int quantityOffPrime, Random rnd)
		{
			List<TemplateWindow> all = candidates.ToList();
			List<TemplatePick> picks = new List<TemplatePick>();
			if (quantity > 0)
			{
				picks.Add(Pick(TemplateGroup.All, quantity, Order(all, rnd)));
				return picks;
			}

			// Порядок розыгрыша как у десктопной линейки (FrmGenerator): сначала весь прайм-список, потом офф-прайм.
			// Веер десктопа тянет жребий по всему дню и делит на прайм после сортировки — распределение то же, последовательность по зерну другая.
			List<TemplateWindow> prime = Order(all.Where(w => w.IsPrime), rnd);
			List<TemplateWindow> offPrime = Order(all.Where(w => !w.IsPrime), rnd);
			if (quantityPrime > 0)
				picks.Add(Pick(TemplateGroup.Prime, quantityPrime, prime));
			if (quantityOffPrime > 0)
				picks.Add(Pick(TemplateGroup.OffPrime, quantityOffPrime, offPrime));
			return picks;
		}

		private static TemplatePick Pick(TemplateGroup group, int wanted, List<TemplateWindow> ordered) =>
			new TemplatePick { Group = group, Wanted = wanted, Windows = ordered.Take(wanted).ToList() };

		/// <summary>Самые свободные первыми, среди равных — случайно (жребий каждому окну в порядке списка, как у десктопа).</summary>
		private static List<TemplateWindow> Order(IEnumerable<TemplateWindow> windows, Random rnd) =>
			windows.Select(w => new { w, rand = rnd.Next() })
				.OrderByDescending(x => x.w.FreeSeconds)
				.ThenBy(x => x.rand)
				.Select(x => x.w)
				.ToList();
	}
}
