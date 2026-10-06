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
	}

	/// <summary>
	/// Неделя сетки размещения: строки × семь дней, ячейка — место (на вкладке «Рекламные окна»
	/// — рекламное окно станции). Модель строит источник (WindowsSource), отрисовывает общий
	/// компонент веба; ни один из них не проверяет тип кампании.
	/// </summary>
	public sealed class PlacementWeek
	{
		public const int DaysInWeek = 7;

		/// <summary>Понедельник недели.</summary>
		public DateTime Monday { get; internal set; }

		/// <summary>Первый и последний показанный день — неделя, обрезанная сроком прайс-листа.</summary>
		public DateTime StartDate { get; internal set; }
		public DateTime FinishDate { get; internal set; }

		/// <summary>На эту дату у станции нет прайс-листа (ни действующего, ни будущего).</summary>
		public bool NoPricelist { get; internal set; }

		/// <summary>Срок прайс-листа недели — подпись «Прайс-лист: … - …».</summary>
		public DateTime PricelistStart { get; internal set; }
		public DateTime PricelistFinish { get; internal set; }

		public IReadOnlyList<PlacementRow> Rows { get; internal set; } = new List<PlacementRow>();

		/// <summary>Выпусков кампании за день (Пн…Вс); null — день вне срока прайс-листа.</summary>
		public int?[] DayTotals { get; internal set; } = new int?[DaysInWeek];

		public DateTime Day(int dayIndex) => Monday.AddDays(dayIndex);

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

	/// <summary>Строка недели: тарифное время и цена (у окон), семь мест (null — места нет).</summary>
	public sealed class PlacementRow
	{
		internal PlacementRow(string time, decimal price)
		{
			Time = time;
			Price = price;
			Cells = new PlacementCell[PlacementWeek.DaysInWeek];
		}

		public string Time { get; }
		public decimal Price { get; }
		public PlacementCell[] Cells { get; }
	}

	/// <summary>Одно место недели.</summary>
	public sealed class PlacementCell
	{
		/// <summary>Ключ места: у рекламного окна — windowId.</summary>
		public int Key { get; internal set; }

		/// <summary>Фактические дата и время выхода.</summary>
		public DateTime Date { get; internal set; }

		/// <summary>День недели места (0 — понедельник) — колонка сетки.</summary>
		public int DayIndex { get; internal set; }

		/// <summary>Остаток времени (у штучного окна — «[осталось/всего]»).</summary>
		public string Text { get; internal set; }

		public PlacementFlags Flags { get; internal set; }

		public bool Has(PlacementFlags flag) => (Flags & flag) == flag;

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
	}
}
