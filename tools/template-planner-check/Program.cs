using System;
using System.Collections.Generic;
using System.Linq;
using Merlin.Classes;

// Проверки планировщика Шаблонов 1–3 (docs/tasks/web-action-forms.md, этап 5) на инвариантах, а не на «как десктоп»:
// у десктопа жребий без зерна, побитовое совпадение недостижимо. Без ключей — только чистая логика (даты, интервал,
// выбор окон дня); с ключом --db — ещё очередь роликов Шаблона 3 на настоящих роликах ArtvisDev; с ключом --load — только замер времени запуска на длинных периодах.
internal static class Program
{
	private static int failed;

	private static int Main(string[] args)
	{
		if (args.Contains("--load"))
		{
			LoadCases();
			Console.WriteLine(failed == 0 ? "OK: замер выполнен" : $"ОШИБОК: {failed}");
			return failed == 0 ? 0 : 1;
		}

		DatesCases();
		TimeRangeCases();
		PickCases();
		if (args.Contains("--db"))
		{
			QueueCases();
			RunCases();
			RangeCases();
			RangeTemplateCases();
		}

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


	// ---------- Запуск шаблона на ArtvisDev ----------

	/// <summary>
	/// Шаблон 2 целиком: макет акции 186408, кампания 412530 (линейка, Авторадио Переславль), три дня внутри её срока.
	/// Проверяется то, что можно проверить через публичный API: выпусков в день, нехватка окон, откат через «Отменить»
	/// (числа выпусков по дням недели и цена акции возвращаются). Всё снимается в finally.
	/// </summary>
	private static void RunCases()
	{
		InitDb();
		ActionOnMassmedia action = ActionOnMassmedia.GetActionById(186408);
		WindowsSource.OpenCampaign c = WindowsSource.Open(action, 412530);
		Check("шаблон: кампания открыта и правка разрешена", c != null && c.CanEdit);
		if (c == null || !c.CanEdit)
			return;
		System.Data.DataTable rollers = WindowsSource.LoadRollers(c);
		int rollerId = WindowsSource.RollerIdOf(rollers.Rows.Cast<System.Data.DataRow>().First(r => !WindowsSource.IsDummy(r)));

		DateTime monday = new DateTime(2026, 10, 12);
		WindowsSource.WeekView view = new WindowsSource.WeekView();
		Func<int[]> totals = () =>
		{
			PlacementWeek week = WindowsSource.LoadWeek(c, monday, view);
			return Enumerable.Range(0, 7).Select(d => week.DayTotals[d] ?? 0).ToArray();
		};
		int[] before = totals();

		List<WindowsSource.PlacedIssue> created = new List<WindowsSource.PlacedIssue>();
		try
		{
			// Четверг–суббота, по два выпуска в день на интервале 06:00–21:00
			PlacementTemplate t = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
				StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 2,
			};
			Check("шаблон: проверки диалога пропускают", t.IntervalProblem() == null, t.IntervalProblem());
			// проверки диалога: каждая даёт свой текст, исправная — null
			Func<Action<PlacementTemplate>, string> problem = change =>
			{
				PlacementTemplate x = new PlacementTemplate
				{
					StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
					StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 2,
				};
				change(x);
				return x.IntervalProblem();
			};
			Check("проверки: конец периода раньше начала", problem(x => x.FinishDate = new DateTime(2026, 10, 14)) != null);
			Check("проверки: конец интервала не позже начала", problem(x => x.FinishTime = x.StartTime) != null);
			Check("проверки: деление на прайм без количества", problem(x => x.Quantity = 0) != null);
			Check("проверки: деление на прайм с количеством", problem(x => { x.Quantity = 0; x.QuantityPrime = 1; }) == null);
			Check("проверки: ни одного дня недели", problem(x => Array.Clear(x.WeekDays, 0, 7)) != null);
			Check("проверки: ни одного дня недели, но по чётности — можно", problem(x => { Array.Clear(x.WeekDays, 0, 7); x.ByParity = true; }) == null);
			Check("проверки: дата за пределами DateTime", problem(x => x.FinishDate = DateTime.MaxValue) != null);
			TemplateSource.TemplateResult r1 = TemplateSource.RunInterval(c, t, rollerId, 0);
			created.AddRange(r1.Created);
			int[] after1 = totals();
			Check("шаблон: 3 дня по 2 выпуска", r1.Done == 6 && r1.Created.Count == 6 && r1.Days == 3, $"done={r1.Done} created={r1.Created.Count} days={r1.Days}");
			Check("шаблон: нет замечаний", r1.NotAdded == 0 && r1.Errors.Rows.Count == 0, string.Join("; ", r1.Errors.Rows.Cast<System.Data.DataRow>().Select(x => x[0] + " " + x[1])));
			Check("шаблон: по два выпуска в четверг, пятницу и субботу, остальные дни те же",
				Enumerable.Range(0, 7).All(d => after1[d] - before[d] == (d >= 3 && d <= 5 ? 2 : 0)), string.Join(",", after1.Zip(before, (a, b) => a - b)));
			Check("шаблон: окна разные", r1.Created.Select(x => x.WindowId).Distinct().Count() == 6);

			// Деление на прайм и офф-прайм: цена всех окон станции одна, значит все окна «прайм», офф-прайма нет —
			// по одному выпуску в день и три строки «нехватка окон офф прайм»
			PlacementTemplate split = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
				StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 0, QuantityPrime = 1, QuantityOffPrime = 2,
			};
			TemplateSource.TemplateResult r2 = TemplateSource.RunInterval(c, split, rollerId, 0);
			created.AddRange(r2.Created);
			Check("шаблон: прайм — по выпуску в день", r2.Done == 3, "done=" + r2.Done);
			Check("шаблон: нехватка офф-прайма — по строке на день, не добавлено 6",
				r2.Errors.Rows.Count == 3 && r2.NotAdded == 6, $"rows={r2.Errors.Rows.Count} notAdded={r2.NotAdded}");

			// Дни вне прайс-листов: «ближайший будущий» (2020 — раньше первого) и «нет вовсе» (2035) — по одной строке на подряд
			// идущие дни, без записи; прайс-лист спрашивается не на каждый день
			foreach (DateTime from in new[] { new DateTime(2020, 3, 2), new DateTime(2035, 3, 2) })
			{
				PlacementTemplate far = new PlacementTemplate
				{
					StartDate = from, FinishDate = from.AddDays(5),
					StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 2,
				};
				TemplateSource.TemplateResult none = TemplateSource.RunInterval(c, far, rollerId, 0);
				Check("вне прайс-листов " + from.Year + ": ничего не добавлено", none.Done == 0 && none.Created.Count == 0, "done=" + none.Done);
				Check("вне прайс-листов " + from.Year + ": одна строка на шесть дней, не добавлено 12",
					none.Errors.Rows.Count == 1 && none.NotAdded == 12, $"rows={none.Errors.Rows.Count} notAdded={none.NotAdded}");
			}
			Check("проверки: период длиннее десяти лет",
				new PlacementTemplate { StartDate = new DateTime(2000, 1, 1), FinishDate = new DateTime(2030, 1, 1), StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 2 }.IntervalProblem() != null);
			Check("проверки: начало раньше 1900",
				new PlacementTemplate { StartDate = new DateTime(26, 10, 15), FinishDate = new DateTime(30, 10, 15), StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 2 }.IntervalProblem() != null);

			// Отмена сотен выпусков идёт пачками по 10 в своих транзакциях: 27 выпусков — три пачки
			PlacementTemplate big = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
				StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 9, IgnoreFirmWindows = false,
			};
			TemplateSource.TemplateResult r3 = TemplateSource.RunInterval(c, big, rollerId, 0);
			created.AddRange(r3.Created);
			Check("шаблон: 3 дня по 9 выпусков", r3.Done == 27 && r3.Created.Count == 27, $"done={r3.Done} notAdded={r3.NotAdded}");

			// Шаблон 1: по выпуску в день в окно на время; окна 10:45 есть в сетке станции каждый день
			PlacementTemplate one = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
				StartTime = new TimeSpan(10, 45, 0), FinishTime = new TimeSpan(10, 45, 0), Quantity = 1, IgnoreFirmWindows = false,
			};
			Check("шаблон 1: проверки диалога пропускают", one.SingleProblem() == null, one.SingleProblem());
			TemplateSource.TemplateResult s1 = TemplateSource.RunSingle(c, one, rollerId, 0);
			created.AddRange(s1.Created);
			Check("шаблон 1: по выпуску в день, все в окне 10:45", s1.Done == 3 && s1.NotAdded == 0 && s1.Created.All(x => x.Name.Contains(" 10:45 ")),
				$"done={s1.Done} notAdded={s1.NotAdded} names={string.Join(";", s1.Created.Select(x => x.Name))}");

			// теперь в этих окнах выпуски фирмы: с флагом «не использовать окна, где есть ролики фирмы» — три отказа по правилам десктопа
			one.IgnoreFirmWindows = true;
			TemplateSource.TemplateResult s2 = TemplateSource.RunSingle(c, one, rollerId, 0);
			created.AddRange(s2.Created);
			Check("шаблон 1: окна с роликами фирмы пропускаются", s2.Done == 0 && s2.Errors.Rows.Count == 3 && s2.NotAdded == 3,
				$"done={s2.Done} rows={s2.Errors.Rows.Count} notAdded={s2.NotAdded}");

			// время без окна: «Рекламное окно не найдено.» по строке на день
			PlacementTemplate nowhere = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
				StartTime = new TimeSpan(3, 7, 0), FinishTime = new TimeSpan(3, 7, 0), Quantity = 1, IgnoreFirmWindows = false,
			};
			TemplateSource.TemplateResult s3 = TemplateSource.RunSingle(c, nowhere, rollerId, 0);
			created.AddRange(s3.Created);
			Check("шаблон 1: время без окна — одна строка на три дня", s3.Done == 0 && s3.Errors.Rows.Count == 1 && s3.NotAdded == 3,
				$"done={s3.Done} rows={s3.Errors.Rows.Count} notAdded={s3.NotAdded} first={(s3.Errors.Rows.Count > 0 ? s3.Errors.Rows[0][1] : "")}");

			Check("проверки шаблона 1: конец раньше начала", new PlacementTemplate { StartDate = new DateTime(2026, 10, 18), FinishDate = new DateTime(2026, 10, 15) }.SingleProblem() != null);

			// Шаблон 3: два ролика по 3 выхода на 3 дня по 2 в день — каждый ролик ровно 3 раза, в каждый день оба
			int rollerId2 = WindowsSource.RollerIdOf(rollers.Rows.Cast<System.Data.DataRow>().Where(r => !WindowsSource.IsDummy(r)).Skip(1).First());
			PlacementTemplate multi = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
				StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 2, IgnoreFirmWindows = false,
			};
			Check("шаблон 3: общее количество выходов — 6", multi.ExpectedTotal() == 6, multi.ExpectedTotal() + "");
			var quotas = new List<KeyValuePair<int, int>> { new KeyValuePair<int, int>(rollerId, 3), new KeyValuePair<int, int>(rollerId2, 3) };
			TemplateSource.TemplateResult m1 = TemplateSource.RunMulti(c, multi, quotas, 0);
			created.AddRange(m1.Created);
			Check("шаблон 3: 6 выпусков, замечаний нет", m1.Done == 6 && m1.NotAdded == 0 && m1.Errors.Rows.Count == 0, $"done={m1.Done} notAdded={m1.NotAdded} rows={m1.Errors.Rows.Count}");
			Check("шаблон 3: квоты выполнены точно", m1.Created.Count(x => x.RollerId == rollerId) == 3 && m1.Created.Count(x => x.RollerId == rollerId2) == 3);
			Check("шаблон 3: в каждый день оба разных ролика",
				m1.Created.GroupBy(x => x.Name.Substring(0, 10)).All(g => g.Count() == 2 && g.Select(x => x.RollerId).Distinct().Count() == 2));

			// Окон не хватает: интервал 10:45–10:46 — одно окно в день при двух выходах; к концу периода в очереди остаётся 3
			PlacementTemplate tight = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
				StartTime = new TimeSpan(10, 45, 0), FinishTime = new TimeSpan(10, 46, 0), Quantity = 2, IgnoreFirmWindows = false,
			};
			TemplateSource.TemplateResult m2 = TemplateSource.RunMulti(c, tight, quotas, 0);
			created.AddRange(m2.Created);
			Check("шаблон 3: нехватка окон — по выпуску в день, остаток 3 в очереди", m2.Done == 3 && m2.NotAdded == 3, $"done={m2.Done} notAdded={m2.NotAdded}");
			Check("шаблон 3: три строки нехватки по дням и одна — за весь период", m2.Errors.Rows.Count == 4, $"rows={m2.Errors.Rows.Count}");

			// «Отменить» возвращает всё
			int createdTotal = created.Count;
			WindowsSource.BatchResult undo = WindowsSource.Undo(c, created);
			created.Clear();
			Check("отмена: сняты все пачками по 10", undo.Done == createdTotal && createdTotal >= 48 && undo.Failed == 0 && undo.Unprocessed.Count == 0, $"done={undo.Done} total={createdTotal} failed={undo.Failed}");
			Check("отмена: числа выпусков по дням вернулись", totals().SequenceEqual(before), string.Join(",", totals()));
		}
		finally
		{
			if (created.Count > 0)
			{
				WindowsSource.BatchResult cleanup = WindowsSource.Undo(c, created);
				Console.WriteLine($"уборка после сбоя: снято {cleanup.Done}, не снято {cleanup.Failed}");
			}
		}
	}

	// ---------- Веер на ArtvisDev ----------

	/// <summary>
	/// «Отменить последнее» у веера: макет акции 186408, все шесть станций, 15–17.10. Клик по слоту, пачка слотов и «до полного
	/// пересечения» (выпуски на трёх станциях из шести, остальным добавляются) — выпуски находятся разностью RangeSlotIssues;
	/// отмена возвращает числа выпусков по дням и цену акции. Всё снимается в finally.
	/// </summary>
	private static void RangeCases()
	{
		InitDb();
		int[] campaignIds = { 412530, 412531, 412532, 412533, 412534, 412535 };
		Func<int[], RangeSource.OpenRange> open = ids =>
			RangeSource.Open(ActionOnMassmedia.GetActionById(186408), ids.Select(id => new KeyValuePair<int, string>(id, "К" + id)).ToList());
		RangeSource.OpenRange r = open(campaignIds);
		Check("веер: открыт и правка разрешена", r != null && r.CanEdit);
		if (r == null || !r.CanEdit)
			return;
		System.Data.DataTable rollers = WindowsSource.LoadRollers(r);
		int rollerId = WindowsSource.RollerIdOf(rollers.Rows.Cast<System.Data.DataRow>().First(row => !WindowsSource.IsDummy(row)));

		DateTime monday = new DateTime(2026, 10, 12);
		WindowsSource.WeekView view = new WindowsSource.WeekView();
		Func<RangeSource.OpenRange, PlacementWeek> load = scope => RangeSource.LoadWeek(scope, monday, view);
		Func<int[]> totals = () => { PlacementWeek w = load(r); return Enumerable.Range(0, 7).Select(d => w.DayTotals[d] ?? 0).ToArray(); };
		Func<decimal> price = () => ActionOnMassmedia.GetActionById(186408).TotalPrice;
		int[] before = totals();
		decimal priceBefore = price();

		const PlacementFlags busy = PlacementFlags.Mine | PlacementFlags.Partial | PlacementFlags.FirmHere | PlacementFlags.FirmSome | PlacementFlags.Disabled;
		List<PlacementCell> free = load(r).Cells
			.Where(c => (c.Flags & busy) == 0 && c.Date >= new DateTime(2026, 10, 15) && c.Date < new DateTime(2026, 10, 18) && c.Date.Hour >= 7 && c.Date.Hour <= 20)
			.GroupBy(c => c.Date.Hour).Select(g => g.First()).Take(5).ToList();
		Check("веер: свободные слоты для проверки найдены", free.Count == 5, "слотов: " + free.Count);
		if (free.Count < 5)
			return;

		List<WindowsSource.PlacedIssue> created = new List<WindowsSource.PlacedIssue>();
		try
		{
			// клик по слоту: по выпуску на каждой из шести станций
			IReadOnlyList<WindowsSource.PlacedIssue> one = RangeSource.PlaceSlot(r, free[0], rollerId, 0, view);
			created.AddRange(one);
			Check("слот: по выпуску на каждой станции", one.Count == 6 && one.Select(x => x.CampaignId).Distinct().Count() == 6 && one.All(x => x.RollerId == rollerId),
				string.Join(";", one.Select(x => x.Name)));
			Check("слот: подписи — слот, кампания, ролик", one.All(x => x.Name.StartsWith(free[0].Date.ToString("dd.MM.yyyy HH:mm")) && x.Name.Contains("К41253")), one.Count > 0 ? one[0].Name : "");
			Check("слот: цена акции изменилась", price() != priceBefore);

			// пачка слотов: Done — число слотов, выпусков по шесть на слот
			WindowsSource.BatchResult batch = RangeSource.PlaceSlots(r, free.Skip(1).Take(2).ToList(), rollerId, 0, view);
			created.AddRange(batch.Created);
			Check("пачка: два слота, двенадцать выпусков", batch.Done == 2 && batch.Failed == 0 && batch.Created.Count == 12, $"done={batch.Done} failed={batch.Failed} created={batch.Created.Count}");

			// «до полного пересечения»: слот на трёх станциях из шести, остальным недостающие добавляются
			RangeSource.OpenRange half = open(campaignIds.Take(3).ToArray());
			half.Rollers = rollers;
			IReadOnlyList<WindowsSource.PlacedIssue> three = RangeSource.PlaceSlot(half, free[3], rollerId, 0, view);
			created.AddRange(three);
			Check("слот на трёх станциях: три выпуска", three.Count == 3 && three.All(x => campaignIds.Take(3).Contains(x.CampaignId)), "создано " + three.Count);
			PlacementCell partial = load(r).Cells.FirstOrDefault(c => c.Key == free[3].Key);
			Check("слот на трёх станциях: у шести он частичный", partial != null && partial.Has(PlacementFlags.Partial));
			RangeSource.AdditionPlan plan = RangeSource.PlanAdditions(r, RangeSource.ReadSlots(r, new[] { partial }));
			Check("до пересечения: недостающие — три", plan.Count == 3, "план " + plan.Count);
			WindowsSource.BatchResult fill = RangeSource.FillToIntersection(r, plan, view, new Dictionary<int, int>(), out int withoutPosition);
			created.AddRange(fill.Created);
			Check("до пересечения: добавлено три выпуска, все записаны", fill.Done == 3 && fill.Created.Count == 3 && fill.Created.All(x => campaignIds.Skip(3).Contains(x.CampaignId)),
				$"done={fill.Done} created={fill.Created.Count}");

			Check("выпуски записи — все разные", created.Select(x => x.IssueId).Distinct().Count() == created.Count && created.Count == 6 + 12 + 3 + 3);

			// отмена чужого набора: выпуск кампании вне веера не снимается
			WindowsSource.BatchResult foreign = null;
			try
			{
				foreign = RangeSource.Undo(half, created);
			}
			catch (InvalidOperationException)
			{
			}
			Check("отмена: записи с кампаниями вне веера — отказ", foreign == null);

			// отмена: пачками по 10, числа по дням и цена возвращаются
			int total = created.Count;
			WindowsSource.BatchResult undo = RangeSource.Undo(r, created);
			created.Clear();
			Check("отмена: сняты все", undo.Done == total && undo.Failed == 0 && undo.Unprocessed.Count == 0, $"done={undo.Done} total={total} failed={undo.Failed}");
			Check("отмена: числа выпусков по дням вернулись", totals().SequenceEqual(before), string.Join(",", totals()));
			Check("отмена: цена акции вернулась", price() == priceBefore, $"{price()} против {priceBefore}");

			// повторная отмена: выпусков уже нет — по строке отказа на каждый, ничего не падает
			WindowsSource.BatchResult again = RangeSource.Undo(r, one.Concat(three).ToList());
			Check("повторная отмена: «Выпуска уже нет» по каждому", again.Done == 0 && again.Failed == 9, $"done={again.Done} failed={again.Failed}");
			Console.WriteLine("веер проверен: слот, пачка, до пересечения, отмена");
		}
		finally
		{
			if (created.Count > 0)
			{
				WindowsSource.BatchResult cleanup = RangeSource.Undo(r, created);
				Console.WriteLine($"уборка веера после сбоя: снято {cleanup.Done}, не снято {cleanup.Failed}");
			}
		}
	}

	/// <summary>
	/// Шаблоны 1–3 на веере: макет акции 186408, шесть станций, 15–17.10. Проверяются инварианты: сколько слотов занято, по выпуску
	/// на каждой станции слота, квоты Шаблона 3, нехватка слотов, дни без слотов, отмена возвращает числа по дням и цену.
	/// </summary>
	private static void RangeTemplateCases()
	{
		InitDb();
		int[] campaignIds = { 412530, 412531, 412532, 412533, 412534, 412535 };
		RangeSource.OpenRange r = RangeSource.Open(ActionOnMassmedia.GetActionById(186408),
			campaignIds.Select(id => new KeyValuePair<int, string>(id, "К" + id)).ToList());
		Check("веер-шаблоны: открыт и правка разрешена", r != null && r.CanEdit);
		if (r == null || !r.CanEdit)
			return;
		System.Data.DataTable rollers = WindowsSource.LoadRollers(r);
		List<int> rollerIds = rollers.Rows.Cast<System.Data.DataRow>().Where(row => !WindowsSource.IsDummy(row)).Select(WindowsSource.RollerIdOf).ToList();
		int rollerId = rollerIds[0], rollerId2 = rollerIds[1];

		DateTime monday = new DateTime(2026, 10, 12);
		WindowsSource.WeekView view = new WindowsSource.WeekView();
		Func<int[]> totals = () => { PlacementWeek w = RangeSource.LoadWeek(r, monday, view); return Enumerable.Range(0, 7).Select(d => w.DayTotals[d] ?? 0).ToArray(); };
		Func<decimal> price = () => ActionOnMassmedia.GetActionById(186408).TotalPrice;
		int[] before = totals();
		decimal priceBefore = price();
		Func<TemplateSource.TemplateResult, string> rows = res => string.Join(" | ", res.Errors.Rows.Cast<System.Data.DataRow>().Select(x => x[0] + " " + x[1]));
		Func<PlacementTemplate> interval = () => new PlacementTemplate
		{
			StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
			StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(21), Quantity = 2, IgnoreFirmWindows = true,
		};

		List<WindowsSource.PlacedIssue> created = new List<WindowsSource.PlacedIssue>();
		try
		{
			// Шаблон 2: три дня по два слота — по выпуску на каждой из шести станций
			TemplateSource.TemplateResult t2 = TemplateRangeSource.RunInterval(r, interval(), rollerId, 0, view);
			created.AddRange(t2.Created);
			Check("веер Ш.2: шесть слотов, замечаний нет", t2.Done == 6 && t2.NotAdded == 0 && t2.Errors.Rows.Count == 0 && t2.Days == 3, $"done={t2.Done} notAdded={t2.NotAdded} days={t2.Days} {rows(t2)}");
			Check("веер Ш.2: по выпуску на станции в каждом слоте (36)", t2.Created.Count == 36 && t2.Created.GroupBy(x => x.CampaignId).All(g => g.Count() == 6), "создано " + t2.Created.Count);
			Check("веер Ш.2: по два слота в день, слоты разные",
				t2.Created.Select(x => x.Name.Substring(0, 16)).Distinct().Count() == 6 && t2.Created.GroupBy(x => x.Name.Substring(0, 10)).Count() == 3);
			int[] after2 = totals();
			Check("веер Ш.2: в четверг-субботу прибавилось по 12 выпусков (2 слота × 6 станций)",
				Enumerable.Range(0, 7).All(d => after2[d] - before[d] == (d >= 3 && d <= 5 ? 12 : 0)), string.Join(",", after2.Zip(before, (a, b) => a - b)));

			// прайм / офф-прайм: всего 3 слота в день, число просьб — 9; сумма «поставлено + не добавлено» сходится
			PlacementTemplate split = interval();
			split.Quantity = 0; split.QuantityPrime = 1; split.QuantityOffPrime = 2; split.IgnoreFirmWindows = false;
			TemplateSource.TemplateResult t2s = TemplateRangeSource.RunInterval(r, split, rollerId, 0, view);
			created.AddRange(t2s.Created);
			Check("веер Ш.2: прайм и офф-прайм — поставлено и не добавлено в сумме 9", t2s.Done + t2s.NotAdded == 9 && t2s.Done > 0, $"done={t2s.Done} notAdded={t2s.NotAdded} {rows(t2s)}");

			// Шаблон 1: время 09:45 округляется до 09:30 — по слоту в день
			PlacementTemplate one = new PlacementTemplate
			{
				StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 10, 17),
				StartTime = new TimeSpan(9, 45, 0), FinishTime = new TimeSpan(9, 45, 0), Quantity = 1, IgnoreFirmWindows = false,
			};
			TemplateSource.TemplateResult t1 = TemplateRangeSource.RunSingle(r, one, rollerId, 0, view);
			created.AddRange(t1.Created);
			Check("веер Ш.1: по слоту в день, все на 09:30", t1.Done == 3 && t1.NotAdded == 0 && t1.Created.Count == 18 && t1.Created.All(x => x.Name.Contains(" 09:30 ")),
				$"done={t1.Done} created={t1.Created.Count} {rows(t1)} names={string.Join(";", t1.Created.Take(2).Select(x => x.Name))}");

			// теперь в этих слотах выпуски фирмы; процедура сама отказывает при «не использовать окна, где есть ролики фирмы»
			one.IgnoreFirmWindows = true;
			TemplateSource.TemplateResult t1b = TemplateRangeSource.RunSingle(r, one, rollerId, 0, view);
			created.AddRange(t1b.Created);
			Check("веер Ш.1: слоты с роликами фирмы — отказ по каждому дню", t1b.Done == 0 && t1b.Errors.Rows.Count == 3 && t1b.NotAdded == 3, $"done={t1b.Done} rows={t1b.Errors.Rows.Count} {rows(t1b)}");

			// Шаблон 3: два ролика по 3 выхода на три дня по 2 в день
			PlacementTemplate multi = interval();
			multi.IgnoreFirmWindows = false;
			var quotas = new List<KeyValuePair<int, int>> { new KeyValuePair<int, int>(rollerId, 3), new KeyValuePair<int, int>(rollerId2, 3) };
			TemplateSource.TemplateResult t3 = TemplateRangeSource.RunMulti(r, multi, quotas, 0, view);
			created.AddRange(t3.Created);
			Check("веер Ш.3: шесть слотов, замечаний нет", t3.Done == 6 && t3.NotAdded == 0 && t3.Errors.Rows.Count == 0, $"done={t3.Done} notAdded={t3.NotAdded} {rows(t3)}");
			Check("веер Ш.3: квоты точно — по 3 слота (18 выпусков) каждого ролика",
				t3.Created.Count(x => x.RollerId == rollerId) == 18 && t3.Created.Count(x => x.RollerId == rollerId2) == 18);
			Check("веер Ш.3: в каждый день оба разных ролика",
				t3.Created.GroupBy(x => x.Name.Substring(0, 10)).All(g => g.Select(x => x.RollerId).Distinct().Count() == 2));

			// слотов не хватает: интервал 09:30–09:31 — один слот в день при двух выходах; остаток 3 в очереди
			PlacementTemplate tight = interval();
			tight.StartTime = new TimeSpan(9, 30, 0); tight.FinishTime = new TimeSpan(9, 31, 0); tight.IgnoreFirmWindows = false;
			TemplateSource.TemplateResult t3t = TemplateRangeSource.RunMulti(r, tight, quotas, 0, view);
			created.AddRange(t3t.Created);
			Check("веер Ш.3: нехватка слотов — по слоту в день, остаток 3 в очереди", t3t.Done == 3 && t3t.NotAdded == 3, $"done={t3t.Done} notAdded={t3t.NotAdded} {rows(t3t)}");
			Check("веер Ш.3: три строки нехватки по дням и одна — за весь период", t3t.Errors.Rows.Count == 4, $"rows={t3t.Errors.Rows.Count}");

			// граница интервала: конец исключается — слот 09:30 при интервале 08:30–09:30 не берётся
			PlacementTemplate edge = interval();
			edge.StartTime = new TimeSpan(8, 30, 0); edge.FinishTime = new TimeSpan(9, 30, 0); edge.Quantity = 1; edge.IgnoreFirmWindows = false;
			TemplateSource.TemplateResult te = TemplateRangeSource.RunInterval(r, edge, rollerId, 0, view);
			created.AddRange(te.Created);
			Check("веер: конец интервала исключается — в 08:30–09:30 слот 08:30", te.Done == 3 && te.Created.All(x => x.Name.Contains(" 08:30 ")),
				$"done={te.Done} names={string.Join(";", te.Created.Take(2).Select(x => x.Name))}");

			// дни вне прайс-листов: одна строка на подряд идущие дни
			PlacementTemplate far = interval();
			far.StartDate = new DateTime(2035, 3, 2); far.FinishDate = new DateTime(2035, 3, 7);
			TemplateSource.TemplateResult tf = TemplateRangeSource.RunInterval(r, far, rollerId, 0, view);
			created.AddRange(tf.Created);
			Check("веер: дни без слотов — одна строка на шесть дней, не добавлено 12", tf.Done == 0 && tf.Errors.Rows.Count == 1 && tf.NotAdded == 12, $"done={tf.Done} rows={tf.Errors.Rows.Count} notAdded={tf.NotAdded}");

			// «Отменить» возвращает всё
			int total = created.Count;
			WindowsSource.BatchResult undo = RangeSource.Undo(r, created);
			created.Clear();
			Check("веер: отмена шаблонов снимает всё", undo.Done == total && undo.Failed == 0 && undo.Unprocessed.Count == 0, $"done={undo.Done} total={total} failed={undo.Failed}");
			Check("веер: числа выпусков по дням вернулись", totals().SequenceEqual(before), string.Join(",", totals()));
			Check("веер: цена акции вернулась", price() == priceBefore, $"{price()} против {priceBefore}");
			Console.WriteLine($"веер: шаблоны проверены, снято {total} выпусков");
		}
		finally
		{
			if (created.Count > 0)
			{
				WindowsSource.BatchResult cleanup = RangeSource.Undo(r, created);
				Console.WriteLine($"уборка веера после сбоя: снято {cleanup.Done}, не снято {cleanup.Failed}");
			}
		}
	}

	// ---------- Замер нагрузки (ключ --load) ----------

	/// <summary>
	/// Время запуска шаблонов на длинных периодах (5.7): линейка — макет 178224, кампания 392053 (283 дня), 15.10–31.12 по 5 выпусков в день;
	/// веер — макет 176784, три станции, 15.10–31.12 по 2 слота в день. Печатает миллисекунды запуска и отмены и снимает всё в finally.
	/// Только на ArtvisDev, в нерабочее время: запуск держит блокировки окон на время дня.
	/// </summary>
	private static void LoadCases()
	{
		InitDb();
		PlacementTemplate Tpl(int quantity) => new PlacementTemplate
		{
			StartDate = new DateTime(2026, 10, 15), FinishDate = new DateTime(2026, 12, 31),
			StartTime = TimeSpan.FromHours(6), FinishTime = TimeSpan.FromHours(23), Quantity = quantity, IgnoreFirmWindows = true,
		};

		// линейка
		{
			ActionOnMassmedia action = ActionOnMassmedia.GetActionById(178224);
			WindowsSource.OpenCampaign c = WindowsSource.Open(action, 392053);
			Check("нагрузка, линейка: кампания открыта и правка разрешена", c != null && c.CanEdit);
			if (c != null && c.CanEdit)
			{
				System.Data.DataTable rollers = WindowsSource.LoadRollers(c);
				int rollerId = WindowsSource.RollerIdOf(rollers.Rows.Cast<System.Data.DataRow>().First(r => !WindowsSource.IsDummy(r)));
				decimal priceBefore = ActionOnMassmedia.GetActionById(178224).TotalPrice;
				List<WindowsSource.PlacedIssue> created = new List<WindowsSource.PlacedIssue>();
				try
				{
					System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
					TemplateSource.TemplateResult res = TemplateSource.RunInterval(c, Tpl(5), rollerId, 0);
					created.AddRange(res.Created);
					long run = sw.ElapsedMilliseconds;
					Console.WriteLine($"нагрузка, линейка: Ш.2 78 дней × 5 — {res.Done} выпусков, не добавлено {res.NotAdded}, строк замечаний {res.Errors.Rows.Count}, {run} мс ({(res.Done == 0 ? 0 : run / res.Done)} мс на выпуск)");
					sw.Restart();
					WindowsSource.BatchResult undo = WindowsSource.Undo(c, created);
					created.Clear();
					Console.WriteLine($"нагрузка, линейка: отмена {undo.Done} выпусков, не снято {undo.Failed}, {sw.ElapsedMilliseconds} мс");
					Check("нагрузка, линейка: цена акции вернулась", ActionOnMassmedia.GetActionById(178224).TotalPrice == priceBefore, $"{ActionOnMassmedia.GetActionById(178224).TotalPrice} против {priceBefore}");
				}
				finally
				{
					if (created.Count > 0)
						Console.WriteLine($"уборка после сбоя: снято {WindowsSource.Undo(c, created).Done}");
				}
			}
		}

		// веер
		{
			int[] campaignIds = { 388478, 388479, 388480 };
			RangeSource.OpenRange r = RangeSource.Open(ActionOnMassmedia.GetActionById(176784),
				campaignIds.Select(id => new KeyValuePair<int, string>(id, "К" + id)).ToList());
			Check("нагрузка, веер: открыт и правка разрешена", r != null && r.CanEdit);
			if (r != null && r.CanEdit)
			{
				System.Data.DataTable rollers = WindowsSource.LoadRollers(r);
				int rollerId = WindowsSource.RollerIdOf(rollers.Rows.Cast<System.Data.DataRow>().First(row => !WindowsSource.IsDummy(row)));
				WindowsSource.WeekView view = new WindowsSource.WeekView();
				decimal priceBefore = ActionOnMassmedia.GetActionById(176784).TotalPrice;
				List<WindowsSource.PlacedIssue> created = new List<WindowsSource.PlacedIssue>();
				try
				{
					System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
					TemplateSource.TemplateResult res = TemplateRangeSource.RunInterval(r, Tpl(2), rollerId, 0, view);
					created.AddRange(res.Created);
					long run = sw.ElapsedMilliseconds;
					Console.WriteLine($"нагрузка, веер: Ш.2 78 дней × 2 слота × 3 станции — {res.Done} слотов ({res.Created.Count} выпусков), не добавлено {res.NotAdded}, строк замечаний {res.Errors.Rows.Count}, {run} мс ({(res.Created.Count == 0 ? 0 : run / res.Created.Count)} мс на выпуск)");
					sw.Restart();
					WindowsSource.BatchResult undo = RangeSource.Undo(r, created);
					created.Clear();
					Console.WriteLine($"нагрузка, веер: отмена {undo.Done} выпусков, не снято {undo.Failed}, {sw.ElapsedMilliseconds} мс");
					Check("нагрузка, веер: цена акции вернулась", ActionOnMassmedia.GetActionById(176784).TotalPrice == priceBefore, $"{ActionOnMassmedia.GetActionById(176784).TotalPrice} против {priceBefore}");
				}
				finally
				{
					if (created.Count > 0)
						Console.WriteLine($"уборка веера после сбоя: снято {RangeSource.Undo(r, created).Done}");
				}
			}
		}
	}

	// ---------- Очередь роликов (Шаблон 3) ----------

	private static bool dbReady;

	/// <summary>Как веб при старте: метаданные ссылаются на сборки десктопа; пользователь — администратор sveta (id 3).</summary>
	private static void InitDb()
	{
		if (dbReady)
			return;
		dbReady = true;
		AppDomain.CurrentDomain.AssemblyResolve += (_, e) =>
			new[] { "Merlin", "FogSoft.WinForm" }.Contains(new System.Reflection.AssemblyName(e.Name).Name)
				? typeof(FogSoft.WinForm.Classes.PresentationObject).Assembly
				: null;
		FogSoft.WinForm.DataAccess.DataAccessor.LoadProcedureConfig();
		var storage = new TestUserStorage();
		FogSoft.WinForm.Classes.SecurityManager.SetLoggedUserStorage(storage);
		storage.User = FogSoft.WinForm.Classes.SecurityManager.GetUser(3);

	}

	private sealed class TestUserStorage : FogSoft.WinForm.Classes.SecurityManager.ILoggedUserStorage
	{
		public FogSoft.WinForm.Classes.SecurityManager.User User { get; set; }
	}

	private static void QueueCases()
	{
		InitDb();

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
