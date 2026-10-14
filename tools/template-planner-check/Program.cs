using System;
using System.Collections.Generic;
using System.Linq;
using Merlin.Classes;

// Проверки планировщика Шаблонов 1–3 (docs/tasks/web-action-forms.md, этап 5) на инвариантах, а не на «как десктоп»:
// у десктопа жребий без зерна, побитовое совпадение недостижимо. Без ключей — только чистая логика (даты, интервал,
// выбор окон дня); с ключом --db — ещё очередь роликов Шаблона 3 на настоящих роликах ArtvisDev.
internal static class Program
{
	private static int failed;

	private static int Main(string[] args)
	{
		DatesCases();
		TimeRangeCases();
		PickCases();
		if (args.Contains("--db"))
			QueueCases();

		Console.WriteLine(failed == 0 ? "OK: все проверки прошли" : $"ОШИБОК: {failed}");
		return failed == 0 ? 0 : 1;
	}

	private static void Check(string name, bool ok, string details = null)
	{
		if (!ok)
		{
			failed++;
			Console.WriteLine($"ПРОВАЛ: {name}{(details == null ? "" : " — " + details)}");
		}
	}

	// ---------- Даты периода ----------

	private static void DatesCases()
	{
		// Эталон — определение из диалога: день недели отмечен / число месяца нужной чётности, концы включены.
		Random rnd = new Random(1);
		int bad = 0;
		for (int i = 0; i < 3000; i++)
		{
			PlacementTemplate t = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 1, 1).AddDays(rnd.Next(0, 600)),
				ByParity = rnd.Next(3) == 0,
				Odd = rnd.Next(2) == 0,
			};
			t.FinishDate = t.StartDate.AddDays(rnd.Next(-3, 120));
			for (int d = 0; d < 7; d++)
				t.WeekDays[d] = rnd.Next(2) == 0;

			List<DateTime> expected = new List<DateTime>();
			for (DateTime day = t.StartDate; day <= t.FinishDate; day = day.AddDays(1))
				if (t.ByParity ? (day.Day % 2 == 1) == t.Odd : t.WeekDays[((int)day.DayOfWeek + 6) % 7])
					expected.Add(day);

			if (!expected.SequenceEqual(t.Dates()))
			{
				bad++;
				if (bad <= 3)
					Console.WriteLine($"  даты: {t.StartDate:d}–{t.FinishDate:d} parity={t.ByParity} odd={t.Odd} days={string.Join("", t.WeekDays.Select(x => x ? 1 : 0))}: ждали {expected.Count}, вышло {t.Dates().Count}");
			}
		}
		Check("даты: 3000 случайных периодов совпали с определением", bad == 0, bad + " расхождений");

		// Опорные числа, чтобы сверка выше не была пустой: две рабочие недели; нечётные и чётные числа октября 2026
		PlacementTemplate workWeeks = new PlacementTemplate { StartDate = new DateTime(2026, 10, 12), FinishDate = new DateTime(2026, 10, 25) };
		workWeeks.WeekDays[5] = workWeeks.WeekDays[6] = false;
		Check("даты: две рабочие недели — 10", workWeeks.Dates().Count == 10);
		PlacementTemplate october = new PlacementTemplate { StartDate = new DateTime(2026, 10, 1), FinishDate = new DateTime(2026, 10, 31), ByParity = true, Odd = true };
		Check("даты: нечётные числа октября — 16", october.Dates().Count == 16);
		october.Odd = false;
		Check("даты: чётные числа октября — 15", october.Dates().Count == 15);

		// 29 февраля високосного года в обоих режимах (случайная сверка выше лежит в невисокосных 2026–2027)
		PlacementTemplate leap = new PlacementTemplate { StartDate = new DateTime(2028, 2, 25), FinishDate = new DateTime(2028, 3, 5), ByParity = true, Odd = true };
		Check("даты: нечётные через 29.02.2028", leap.Dates().Select(d => d.ToString("dd.MM")).SequenceEqual(new[] { "25.02", "27.02", "29.02", "01.03", "03.03", "05.03" }),
			string.Join(",", leap.Dates().Select(d => d.ToString("dd.MM"))));
		leap.Odd = false;
		Check("даты: чётные через 29.02.2028", leap.Dates().Select(d => d.ToString("dd.MM")).SequenceEqual(new[] { "26.02", "28.02", "02.03", "04.03" }),
			string.Join(",", leap.Dates().Select(d => d.ToString("dd.MM"))));
		PlacementTemplate leapDays = new PlacementTemplate { StartDate = new DateTime(2028, 2, 28), FinishDate = new DateTime(2028, 3, 1) };
		Check("даты: дни недели через 29.02.2028", leapDays.Dates().Count == 3);

		PlacementTemplate one = new PlacementTemplate { StartDate = new DateTime(2026, 10, 14), FinishDate = new DateTime(2026, 10, 14) };
		Check("даты: один день", one.Dates().SequenceEqual(new[] { new DateTime(2026, 10, 14) }));
		PlacementTemplate empty = new PlacementTemplate { StartDate = new DateTime(2026, 10, 14), FinishDate = new DateTime(2026, 10, 14) };
		Array.Clear(empty.WeekDays, 0, 7);
		Check("даты: ни одного дня недели — пусто", empty.Dates().Count == 0);
		PlacementTemplate reversed = new PlacementTemplate { StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 14) };
		Check("даты: конец раньше начала — пусто", reversed.Dates().Count == 0);
		PlacementTemplate withTime = new PlacementTemplate { StartDate = new DateTime(2026, 10, 14, 15, 0, 0), FinishDate = new DateTime(2026, 10, 14, 9, 0, 0) };
		Check("даты: время в датах не мешает", withTime.Dates().Count == 1);
	}

	// ---------- Интервал времени ----------

	private static void TimeRangeCases()
	{
		List<TemplateWindow> day = Enumerable.Range(0, 48)
			.Select(i => new TemplateWindow { Key = i, Start = new DateTime(2026, 10, 14).AddMinutes(i * 30) }).ToList();
		// 10:00–12:00: линейка включает конец (5 окон), веер — нет (4)
		List<int> linear = TemplatePlanner.InTimeRange(day, 10 * 60, 12 * 60, true).Select(w => w.Key).ToList();
		List<int> range = TemplatePlanner.InTimeRange(day, 10 * 60, 12 * 60, false).Select(w => w.Key).ToList();
		Check("интервал: линейка включает конец", linear.SequenceEqual(new[] { 20, 21, 22, 23, 24 }), string.Join(",", linear));
		Check("интервал: веер исключает конец", range.SequenceEqual(new[] { 20, 21, 22, 23 }), string.Join(",", range));
		Check("интервал: начало всегда включено", TemplatePlanner.InTimeRange(day, 10 * 60 + 1, 12 * 60, true).First().Key == 21);
		Check("интервал: пустой при конце раньше начала", TemplatePlanner.InTimeRange(day, 12 * 60, 10 * 60, true).Count == 0);
	}

	// ---------- Выбор окон дня ----------

	private static List<TemplateWindow> Candidates(Random rnd, int count)
	{
		// остатков мало разных — чтобы были равные и жребий работал
		return Enumerable.Range(0, count).Select(i => new TemplateWindow
		{
			Key = i,
			Start = new DateTime(2026, 10, 14).AddMinutes(i * 30),
			FreeSeconds = rnd.Next(0, 6) * 30,
			IsPrime = rnd.Next(4) == 0,
		}).ToList();
	}

	private static void PickCases()
	{
		Random gen = new Random(7);
		int orderBad = 0, setBad = 0, primeBad = 0, shortBad = 0, groupBad = 0;
		for (int i = 0; i < 2000; i++)
		{
			List<TemplateWindow> candidates = Candidates(gen, gen.Next(0, 40));
			int quantity = gen.Next(0, 3) == 0 ? 0 : gen.Next(1, 20);
			int prime = gen.Next(0, 5), offPrime = gen.Next(0, 8);
			List<TemplatePick> picks = TemplatePlanner.PickDay(candidates, quantity, prime, offPrime, new Random(i));

			foreach (TemplatePick pick in picks)
			{
				IEnumerable<TemplateWindow> pool = pick.Group == TemplateGroup.Prime ? candidates.Where(w => w.IsPrime)
					: pick.Group == TemplateGroup.OffPrime ? candidates.Where(w => !w.IsPrime) : candidates;
				List<TemplateWindow> poolList = pool.ToList();

				// столько, сколько просили, или все, что есть
				if (pick.Windows.Count != Math.Min(pick.Wanted, poolList.Count)) shortBad++;
				if (pick.IsShort != (poolList.Count < pick.Wanted)) shortBad++;
				// в порядке убывания остатка
				for (int k = 1; k < pick.Windows.Count; k++)
					if (pick.Windows[k - 1].FreeSeconds < pick.Windows[k].FreeSeconds) orderBad++;
				// взяты самые свободные: не взятые не свободнее ни одного из взятых
				if (pick.Windows.Count > 0)
				{
					int worstPicked = pick.Windows.Min(w => w.FreeSeconds);
					if (poolList.Except(pick.Windows).Any(w => w.FreeSeconds > worstPicked)) setBad++;
				}
				// группа не вышла за свой прайм
				if (pick.Group == TemplateGroup.Prime && pick.Windows.Any(w => !w.IsPrime)) primeBad++;
				if (pick.Group == TemplateGroup.OffPrime && pick.Windows.Any(w => w.IsPrime)) primeBad++;
				// без повторов окна
				if (pick.Windows.Select(w => w.Key).Distinct().Count() != pick.Windows.Count) setBad++;
			}

			if (quantity > 0)
			{
				if (!(picks.Count == 1 && picks[0].Group == TemplateGroup.All && picks[0].Wanted == quantity)) groupBad++;
			}
			else if (!picks.Select(p => p.Group).SequenceEqual(new[] { TemplateGroup.Prime, TemplateGroup.OffPrime }.Where(g => g == TemplateGroup.Prime ? prime > 0 : offPrime > 0)))
				groupBad++;

			// прайм-окна офф-прайм-группы и наоборот не пересекаются между собой
			if (picks.Count == 2 && picks[0].Windows.Select(w => w.Key).Intersect(picks[1].Windows.Select(w => w.Key)).Any())
				setBad++;
		}
		Check("выбор: порядок по убыванию остатка", orderBad == 0, orderBad + "");
		Check("выбор: берутся самые свободные, без повторов", setBad == 0, setBad + "");
		Check("выбор: прайм и офф-прайм не смешиваются", primeBad == 0, primeBad + "");
		Check("выбор: нехватка окон считается верно", shortBad == 0, shortBad + "");
		Check("выбор: группы дня — «все» либо прайм / офф-прайм без нулевых", groupBad == 0, groupBad + "");

		// Зерно: тот же ввод — тот же выбор; разные зёрна при равных остатках дают разные наборы.
		List<TemplateWindow> equal = Enumerable.Range(0, 20).Select(i => new TemplateWindow { Key = i, FreeSeconds = 100 }).ToList();
		string a = string.Join(",", TemplatePlanner.PickDay(equal, 5, 0, 0, new Random(42))[0].Windows.Select(w => w.Key));
		string b = string.Join(",", TemplatePlanner.PickDay(equal, 5, 0, 0, new Random(42))[0].Windows.Select(w => w.Key));
		Check("выбор: с одним зерном повторяется", a == b);
		int different = Enumerable.Range(0, 30)
			.Select(seed => string.Join(",", TemplatePlanner.PickDay(equal, 5, 0, 0, new Random(seed))[0].Windows.Select(w => w.Key)))
			.Distinct().Count();
		Check("выбор: при равных остатках жребий меняет набор", different > 10, different + " разных из 30");
		// Равномерность: из 20 равных окон берём 5, 4000 зёрен — каждое окно ~1000 раз (ожидание 1000; допуск ±20 %)
		int[] taken = new int[20];
		for (int seed = 0; seed < 4000; seed++)
			foreach (TemplateWindow w in TemplatePlanner.PickDay(equal, 5, 0, 0, new Random(seed))[0].Windows)
				taken[w.Key]++;
		Check("выбор: жребий равномерен среди равных окон", taken.All(n => n >= 800 && n <= 1200), string.Join(",", taken));
		Check("выбор: нет кандидатов — пустая группа с недобором", TemplatePlanner.PickDay(new TemplateWindow[0], 3, 0, 0, new Random(1))[0].IsShort);
	}

	// ---------- Очередь роликов (Шаблон 3) ----------

	private sealed class TestUserStorage : FogSoft.WinForm.Classes.SecurityManager.ILoggedUserStorage
	{
		public FogSoft.WinForm.Classes.SecurityManager.User User { get; set; }
	}

	private static void QueueCases()
	{
		// Как веб при старте: метаданные ссылаются на сборки десктопа.
		AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
			new[] { "Merlin", "FogSoft.WinForm" }.Contains(new System.Reflection.AssemblyName(e.Name).Name)
				? typeof(FogSoft.WinForm.Classes.PresentationObject).Assembly
				: null;
		FogSoft.WinForm.DataAccess.DataAccessor.LoadProcedureConfig();
		var storage = new TestUserStorage();
		FogSoft.WinForm.Classes.SecurityManager.SetLoggedUserStorage(storage);
		storage.User = FogSoft.WinForm.Classes.SecurityManager.GetUser(3);

		// Ролики фирм акций ArtvisDev (хватит любых четырёх с длительностью): те же строки, что у списка «Ролики» в вебе.
		List<Roller> rollers = new List<Roller>();
		foreach (int actionId in new[] { 186408, 186415, 176888, 185313 })
			rollers.AddRange(new Firm(ActionOnMassmedia.GetActionById(actionId).FirmID).GetRollers().Rows.Cast<System.Data.DataRow>()
				.Select(row => new Roller(row)).Where(r => r.Duration > 0 && !r.IsDummy));
		rollers = rollers.GroupBy(r => r[Roller.ParamNames.RollerId].ToString()).Select(g => g.First()).Take(4).ToList();
		List<int> ids = rollers.Select(r => int.Parse(r[Roller.ParamNames.RollerId].ToString())).ToList();
		Check("очередь: ролики для проверки найдены", ids.Count == 4, "годных роликов: " + ids.Count);
		if (ids.Count < 4)
			return;
		Func<int, Roller> roller = i => rollers[i];
		Func<Roller, int> idOf = r => int.Parse(r[Roller.ParamNames.RollerId].ToString());

		// равные квоты 3: порция в число роликов — всегда все разные (раунд)
		int roundBad = 0, totalBad = 0, adjacentBad = 0;
		for (int seed = 0; seed < 100; seed++)
		{
			var queue = new RollerAllocationQueue(Enumerable.Range(0, 4).Select(i => (roller(i), 3)), new Random(seed));
			List<int> all = new List<int>();
			for (int day = 0; day < 3; day++)
			{
				List<int> portion = queue.TakeForToday(4).Select(idOf).ToList();
				if (portion.Distinct().Count() != 4) roundBad++;
				all.AddRange(portion);
			}
			if (queue.Count != 0 || ids.Any(id => all.Count(x => x == id) != 3)) totalBad++;
		}
		Check("очередь: порция равных квот — все разные ролики", roundBad == 0, roundBad + "");
		Check("очередь: квоты выданы точно", totalBad == 0, totalBad + "");

		// неравные квоты 5/3/2: сумма точна; в порции нет соседей-повторов, пока самый частый ≤ половины
		for (int seed = 0; seed < 100; seed++)
		{
			var queue = new RollerAllocationQueue(new[] { (roller(0), 5), (roller(1), 3), (roller(2), 2) }, new Random(seed));
			List<int> all = new List<int>();
			while (queue.Count > 0)
			{
				List<int> portion = queue.TakeForToday(4).Select(idOf).ToList();
				int most = portion.GroupBy(x => x).Max(g => g.Count());
				if (most * 2 <= portion.Count)
					for (int k = 1; k < portion.Count; k++)
						if (portion[k] == portion[k - 1]) adjacentBad++;
				all.AddRange(portion);
			}
			if (all.Count(x => x == ids[0]) != 5 || all.Count(x => x == ids[1]) != 3 || all.Count(x => x == ids[2]) != 2) totalBad++;
		}
		Check("очередь: неравные квоты выданы точно", totalBad == 0, totalBad + "");
		Check("очередь: нет подряд идущих повторов, если самый частый ≤ половины порции", adjacentBad == 0, adjacentBad + "");

		// возврат в начало: что вернули, то следующим и выдаётся
		var q = new RollerAllocationQueue(new[] { (roller(0), 2), (roller(1), 2) }, new Random(5));
		List<Roller> first = q.TakeForToday(3);
		q.PutBackToFront(first.Skip(1).ToList());
		Check("очередь: возврат остатка в начало", q.Count == 3 && q.TakeForToday(2).Select(idOf).OrderBy(x => x).SequenceEqual(first.Skip(1).Select(idOf).OrderBy(x => x)));

		// то же зерно — та же очередь
		Func<string> run = () =>
		{
			var queue = new RollerAllocationQueue(Enumerable.Range(0, 4).Select(i => (roller(i), 2)), new Random(11));
			return string.Join(",", queue.TakeForToday(8).Select(idOf));
		};
		Check("очередь: с одним зерном повторяется", run() == run());
		Console.WriteLine($"очередь роликов проверена на {ids.Count} роликах ArtvisDev");
	}
}
