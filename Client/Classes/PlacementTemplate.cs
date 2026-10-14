using System;
using System.Collections.Generic;
using System.Linq;

namespace Merlin.Classes
{
	/// <summary>
	/// Параметры Шаблонов 1–3 (docs/tasks/web-action-forms.md, этап 5): период, дни, интервал времени и
	/// сколько выпусков в день. Даты периода перебирает десктопный <see cref="IssueTemplate"/> — тот же
	/// итератор, что у FrmGenerator (дни недели или чётность числа месяца), поэтому набор дат совпадает.
	///
	/// Только веб: в Client.csproj не входит.
	/// </summary>
	public sealed class PlacementTemplate
	{
		public DateTime StartDate { get; set; }
		public DateTime FinishDate { get; set; }

		/// <summary>Дни недели Пн … Вс; по умолчанию все. Действуют, пока не включена чётность.</summary>
		public bool[] WeekDays { get; } = Enumerable.Repeat(true, 7).ToArray();

		/// <summary>true — дни по чётности числа месяца (вместо дней недели), см. <see cref="Odd"/>.</summary>
		public bool ByParity { get; set; }

		/// <summary>При <see cref="ByParity"/>: true — нечётные числа, false — чётные.</summary>
		public bool Odd { get; set; }

		/// <summary>«Не использовать окна, где есть ролики данной фирмы»; как в десктопных диалогах, включён по умолчанию.</summary>
		public bool IgnoreFirmWindows { get; set; } = true;

		/// <summary>Начало интервала (Шаблон 2, 3) или время выпуска (Шаблон 1); важны часы и минуты.</summary>
		public TimeSpan StartTime { get; set; }

		/// <summary>Конец интервала (Шаблон 2, 3).</summary>
		public TimeSpan FinishTime { get; set; }

		/// <summary>Выпусков в день вперемешку; 0 — считать по <see cref="QuantityPrime"/> и <see cref="QuantityOffPrime"/>.</summary>
		public int Quantity { get; set; }

		public int QuantityPrime { get; set; }

		public int QuantityOffPrime { get; set; }

		/// <summary>Даты периода по возрастанию, как их обходит FrmGenerator (с началом и концом включительно).</summary>
		public List<DateTime> Dates()
		{
			// Время даты не нужно: StartDate/FinishDate десктопного итератора — сутки (Шаблон 1 добавляет
			// время к дате отдельно). Со временем в датах охранная проверка итератора «начало > конец» отсекла бы
			// период в один день, когда время начала позже времени конца.
			IssueTemplate template = new IssueTemplate();
			template.StartDate = StartDate.Date;
			template.FinishDate = FinishDate.Date;
			template.WeekDays = (bool[])WeekDays.Clone();
			template.Day2AddMode = ByParity ? Day2AddMode.OddEvenDays : Day2AddMode.WeekDays;
			template.IsOdd = Odd;

			List<DateTime> dates = new List<DateTime>();
			template.Reset();
			while (template.MoveNext())
				dates.Add(template.CurrentDate.Date);
			return dates;
		}
	}
}
