using System;
using System.Collections.Generic;
using System.Linq;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.DataAccess;

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

		/// <summary>Предел периода — десять лет: запуск идёт на сервере, без отмены и без окна прогресса.</summary>
		public const int MaxPeriodDays = 3660;

		/// <summary>
		/// Проверки диалога Шаблонов 2 и 3 (десктоп — FrmTemplate2.btnOk_Click): текст первой найденной ошибки или null.
		/// Десктоп показывает ошибки по очереди сообщениями; здесь — одним текстом в диалоге. Сверх десктопа: отмечен ли
		/// хотя бы один день недели (иначе запуск молча ничего не делает), границы периода (итератор не переносит верхнюю
		/// границу даты, а запуск на сервере без отмены не должен быть бесконечным).
		/// </summary>
		public string IntervalProblem()
		{
			if (Quantity == 0 && QuantityPrime + QuantityOffPrime == 0)
				return Tr.T(Properties.Resources.PrimeAndNonPrimeQuantityMissing);
			if (StartDate.Date > FinishDate.Date)
				return MessageAccessor.GetMessage("TemplateStartFinishDateError");
			if (StartTime >= FinishTime)
				return MessageAccessor.GetMessage("TemplateStartFinishTimeError");
			return PeriodProblem();
		}

		/// <summary>
		/// Проверки диалога Шаблона 1 (десктоп — FrmTemplate.btnOk_Click проверяет только «добавить / удалить», в вебе режима
		/// нет). Сверх десктопа — порядок дат и те же границы периода, что у Шаблонов 2 и 3.
		/// </summary>
		public string SingleProblem()
		{
			if (StartDate.Date > FinishDate.Date)
				return MessageAccessor.GetMessage("TemplateStartFinishDateError");
			return PeriodProblem();
		}

		private string PeriodProblem()
		{
			if (FinishDate.Date > new DateTime(9998, 12, 31))
				return Tr.T("Дата окончания периода слишком большая.");
			if (StartDate.Date < new DateTime(1900, 1, 1))
				return Tr.T("Дата начала периода слишком ранняя.");
			if ((FinishDate.Date - StartDate.Date).TotalDays > MaxPeriodDays)
				return Tr.T("Период шаблона слишком длинный: не больше 10 лет.");
			if (!ByParity && !WeekDays.Any(on => on))
				return Tr.T("Отметьте хотя бы один день недели.");
			return null;
		}

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
