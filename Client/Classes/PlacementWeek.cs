using System;
using System.Collections.Generic;

namespace Merlin.Classes
{
	/// <summary>
	/// Признаки места в сетке размещения — в модели, а не в цвете ячейки, как у десктопных
	/// сеток (docs/tasks/web-tariffgrid.md, §2 п. 2). Как их показать, решает экран.
	/// </summary>
	[Flags]
	public enum PlacementFlags
	{
		None = 0,
		/// <summary>В месте есть выпуски текущей кампании (десктоп — синий текст).</summary>
		Mine = 1,
		/// <summary>Выпуски этой же фирмы в другой кампании станции (десктоп — бирюзовый текст).</summary>
		FirmHere = 2,
		/// <summary>Прайм-тайм: самая высокая цена окна в этот день (фон «Видеть прайм»).</summary>
		Prime = 4,
		/// <summary>Окно заблокировано (isDisabled): выпуски в него не ставятся.</summary>
		Disabled = 8,
		/// <summary>Окно отмечено (isMarked) — фон «Показать отмеченные окна».</summary>
		Marked = 16,
		/// <summary>Подходит под «Позиционирование» и «Предметы рекламы» (десктоп — жирный шрифт).</summary>
		Match = 32,
		/// <summary>
		/// Выпуски этой акции есть не у всех отмеченных кампаний (веер, десктоп — красный текст).
		/// Свои выпуски у всех отмеченных кампаний — <see cref="Mine"/>.
		/// </summary>
		Partial = 64,
		/// <summary>
		/// Выпуски этой фирмы в других акциях не на всех станциях места (веер, десктоп — оранжевый
		/// текст); на всех станциях — <see cref="FirmHere"/>.
		/// </summary>
		FirmSome = 128,
	}

	/// <summary>Вид периода сетки размещения: подписи навигации, выходные, ширина колонок дня.</summary>
	public enum PlacementPeriod
	{
		Week,
		Month,
	}

	/// <summary>
	/// Период сетки размещения: строки × дни (неделя — семь, месяц — до 31), ячейка — место (на
	/// вкладке «Рекламные окна» — рекламное окно станции). Модель строит источник (WindowsSource),
	/// отрисовывает общий компонент веба; ни один из них не проверяет тип кампании.
	/// </summary>
	public sealed class PlacementWeek
	{
		public const int DaysInWeek = 7;

		/// <summary>Неделя: семь дней с <see cref="FirstDay"/> (понедельника).</summary>
		public PlacementWeek() : this(PlacementPeriod.Week, DaysInWeek)
		{
		}

		/// <summary>
		/// Период из <paramref name="dayCount"/> дней с <see cref="FirstDay"/>; строки такого периода
		/// создаются с тем же числом дней (<see cref="PlacementRow"/> с dayCount).
		/// </summary>
		internal PlacementWeek(PlacementPeriod period, int dayCount)
		{
			Period = period;
			DayCount = dayCount;
			DayTotals = new int?[dayCount];
		}

		public PlacementPeriod Period { get; }

		/// <summary>Колонок дня в сетке; у недели — <see cref="DaysInWeek"/>.</summary>
		public int DayCount { get; }

		/// <summary>День первой колонки: понедельник недели, первое число месяца.</summary>
		public DateTime FirstDay { get; internal set; }

		/// <summary>Первый и последний показанный день — период, обрезанный сроком прайс-листа.</summary>
		public DateTime StartDate { get; internal set; }
		public DateTime FinishDate { get; internal set; }

		/// <summary>На эту дату у станции нет прайс-листа (ни действующего, ни будущего).</summary>
		public bool NoPricelist { get; internal set; }

		/// <summary>В строках есть цена (у слота веера её нет — колонка «Цена» не показывается).</summary>
		public bool HasPrices { get; internal set; } = true;

		/// <summary>Неделя строится по одному прайс-листу (подпись «Прайс-лист: … - …»); у веера их несколько.</summary>
		public bool HasPricelist { get; internal set; } = true;

		/// <summary>Своя надпись пустой недели вместо «рекламных окон нет»; null — стандартная.</summary>
		public string EmptyTitle { get; internal set; }
		public string EmptyHint { get; internal set; }

		/// <summary>
		/// Заголовочные колонки строки слева от дней (у модулей — «Радиостанция», «Модуль», «Тип оплаты»), тексты строки —
		/// <see cref="PlacementRow.Headers"/>. null — как у окон и веера: «Цена» (при <see cref="HasPrices"/>) и «Время».
		/// Задаётся до <see cref="Rows"/>.
		/// </summary>
		public IReadOnlyList<string> HeaderColumns { get; internal set; }

		/// <summary>Срок прайс-листа недели — подпись «Прайс-лист: … - …».</summary>
		public DateTime PricelistStart { get; internal set; }
		public DateTime PricelistFinish { get; internal set; }

		/// <summary>Строки периода; у каждой мест ровно <see cref="DayCount"/> — иначе сетка и выделение упадут на индексе.</summary>
		public IReadOnlyList<PlacementRow> Rows
		{
			get => _rows;
			internal set
			{
				foreach (PlacementRow row in value)
				{
					if (row.Cells.Length != DayCount)
						throw new ArgumentException($"Строка «{row.Time}»: мест {row.Cells.Length}, а дней в периоде {DayCount}.", nameof(value)); // i18n-ok: ошибка разработчика
					if (HeaderColumns != null && row.Headers?.Count != HeaderColumns.Count)
						throw new ArgumentException($"Строка «{row.Time}»: заголовков {row.Headers?.Count ?? 0}, а колонок {HeaderColumns.Count}.", nameof(value)); // i18n-ok: ошибка разработчика
				}
				_rows = value;
			}
		}

		private IReadOnlyList<PlacementRow> _rows = new List<PlacementRow>();

		/// <summary>Выпусков кампании за день, по колонкам (<see cref="DayCount"/>); null — день вне срока прайс-листа.</summary>
		public int?[] DayTotals { get; }

		/// <summary>Дата колонки <paramref name="dayIndex"/> — от <see cref="FirstDay"/>, а не от понедельника.</summary>
		public DateTime Day(int dayIndex) => FirstDay.AddDays(dayIndex);

		public bool IsInRange(int dayIndex)
		{
			DateTime day = Day(dayIndex);
			return day >= StartDate && day <= FinishDate;
		}

		/// <summary>Все места недели по строкам сверху вниз, дни слева направо.</summary>
		public IEnumerable<PlacementCell> Cells
		{
			get
			{
				foreach (PlacementRow row in Rows)
					foreach (PlacementCell cell in row.Cells)
						if (cell != null)
							yield return cell;
			}
		}

		public PlacementCell Find(int key)
		{
			foreach (PlacementCell cell in Cells)
				if (cell.Key == key)
					return cell;
			return null;
		}
	}

	/// <summary>
	/// Строка периода: тарифное время и цена (у окон) или тексты заголовочных колонок (у модулей), место на каждый день
	/// (null — места нет).
	/// </summary>
	public sealed class PlacementRow
	{
		/// <summary>Строка недели — семь мест.</summary>
		internal PlacementRow(string time, decimal price) : this(time, price, PlacementWeek.DaysInWeek)
		{
		}

		/// <summary>Строка периода из <paramref name="dayCount"/> мест — по <see cref="PlacementWeek.DayCount"/> модели.</summary>
		internal PlacementRow(string time, decimal price, int dayCount)
		{
			Time = time;
			Price = price;
			Cells = new PlacementCell[dayCount];
		}

		/// <summary>
		/// Строка с заголовочными колонками (<see cref="PlacementWeek.HeaderColumns"/>): <see cref="Time"/> — последний
		/// заголовок (для сообщений), цены нет.
		/// </summary>
		internal PlacementRow(IReadOnlyList<string> headers, int dayCount)
		{
			Headers = headers;
			Time = headers.Count > 0 ? headers[headers.Count - 1] : string.Empty;
			Cells = new PlacementCell[dayCount];
		}

		public string Time { get; }
		public decimal Price { get; }

		/// <summary>Тексты заголовочных колонок по <see cref="PlacementWeek.HeaderColumns"/>; null — у строк окон и веера.</summary>
		public IReadOnlyList<string> Headers { get; }

		/// <summary>Полоса строки (0/1) — «зебра» по радиостанциям у модулей (ComboModuleGrid.StripeMassmediaGroups); у остальных 0.</summary>
		public int Band { get; internal set; }

		public PlacementCell[] Cells { get; }
	}

	/// <summary>Одно место недели.</summary>
	public sealed class PlacementCell
	{
		/// <summary>
		/// Ключ места: у рекламного окна — windowId, у слота веера — номер получаса от 2000-01-01, у модуля — номер дня от
		/// 2000-01-01 × 100 000 + номер строки.
		/// </summary>
		public int Key { get; internal set; }

		/// <summary>Фактические дата и время выхода (у модуля — день, без времени).</summary>
		public DateTime Date { get; internal set; }

		/// <summary>Колонка сетки — номер дня от <see cref="PlacementWeek.FirstDay"/> (у недели 0 — понедельник).</summary>
		public int DayIndex { get; internal set; }

		/// <summary>Остаток времени (у штучного окна — «[осталось/всего]»).</summary>
		public string Text { get; internal set; }

		public PlacementFlags Flags { get; internal set; }

		public bool Has(PlacementFlags flag) => (Flags & flag) == flag;

		/// <summary>Подсказка к месту строками (веер: каких кампаний не хватает, чьи акции фирмы здесь); null — нет.</summary>
		public string Details { get; internal set; }

		/// <summary>Ролики текущей кампании в месте — в порядке выпусков, с повторами.</summary>
		public IReadOnlyList<int> OwnRollerIds { get; internal set; } = Array.Empty<int>();

		/// <summary>Ролики других кампаний этой фирмы в месте (бирюзовые), без повторов.</summary>
		public IReadOnlyList<int> FirmRollerIds { get; internal set; } = Array.Empty<int>();

		/// <summary>
		/// Номера роликов вместо остатка («Номера роликов»): сначала свои, затем ролики других
		/// кампаний фирмы, которых нет среди своих, по возрастанию — как RollerIssuesGrid3.
		/// null — ни одного известного номера (тогда в ячейке остаток).
		/// </summary>
		public string NumbersText(IReadOnlyDictionary<int, int> numbers)
		{
			List<string> result = new List<string>();
			HashSet<int> covered = new HashSet<int>();
			foreach (int rollerId in OwnRollerIds)
			{
				covered.Add(rollerId);
				if (numbers.TryGetValue(rollerId, out int number))
					result.Add(number.ToString());
			}

			List<int> others = new List<int>();
			foreach (int rollerId in FirmRollerIds)
				if (!covered.Contains(rollerId) && numbers.TryGetValue(rollerId, out int number))
					others.Add(number);
			others.Sort();
			foreach (int number in others)
				result.Add(number.ToString());

			return result.Count > 0 ? string.Join(", ", result) : null;
		}

		/// <summary>Окно ядра — для записи выпуска; строится из строки TariffWindowRetrieve.</summary>
		internal TariffWindowWithRollerIssues Window { get; set; }

		/// <summary>Выпуски отмеченных кампаний акции в слоте веера (RangeSlotIssues) — только у цветных слотов.</summary>
		internal IReadOnlyList<RangeSource.SlotIssue> SlotIssues { get; set; } = Array.Empty<RangeSource.SlotIssue>();

		/// <summary>Выпуски слота уже прочитаны (RangeSlotIssues): пустой <see cref="SlotIssues"/> тогда значит «выпусков нет».</summary>
		internal bool SlotIssuesLoaded { get; set; }

		/// <summary>Ролики фирмы в слоте веера из других акций (и модульных кампаний этой) — номера в бирюзовых и оранжевых слотах.</summary>
		internal IReadOnlyList<RangeSource.FirmRoller> FirmSlotRollers { get; set; } = Array.Empty<RangeSource.FirmRoller>();

		/// <summary>Модуль и кампания строки за день — у места вкладки «Модули» (ModuleDaySource), иначе null.</summary>
		internal ModuleDaySource.ModuleDay ModuleDay { get; set; }
	}
}
