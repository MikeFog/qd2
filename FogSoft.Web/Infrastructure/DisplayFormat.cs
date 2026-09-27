using System.Globalization;
using System.Text.RegularExpressions;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Показ дат и дней недели по культуре установки (App.config, <c>Culture</c>),
/// а не по жёстко заданному «dd.MM.yyyy». Для ru-RU результат тот же, что и был.
///
/// Только для показа. Строки дат, которые уходят в базу или в имя файла, свой
/// формат не меняют: там важна неизменность, а не привычность.
/// docs/tasks/web-i18n.md.
/// </summary>
public static class DisplayFormat
{
	private static DateTimeFormatInfo Info => CultureInfo.CurrentCulture.DateTimeFormat;

	/// <summary>24.09.2026</summary>
	public static string Date(DateTime value) => value.ToString(Info.ShortDatePattern);

	/// <summary>24.09.2026 09:05</summary>
	public static string DateAndTime(DateTime value) => value.ToString(Info.ShortDatePattern + " HH:mm");

	/// <summary>
	/// Значение даты из базы — для грида, окна выбора, паспорта. Секунд нигде:
	/// полночь — только дата, время суток (datetime с фиктивной датой 01.01.1900,
	/// как хранит PageFieldTime) — только «HH:mm», иначе дата и «HH:mm».
	/// Десктопный грид даёт то же самое без явного формата — через DateTimeConverter
	/// (кроме 01.01.1900: там он показал бы и фиктивную дату).
	/// <paramref name="dateOnly"/> — атрибут размечен как дата (date/date2).
	/// </summary>
	public static string Value(DateTime value, bool dateOnly = false)
	{
		if (dateOnly)
			return Date(value);
		// Раньше полуночи: тариф на 00:00 — это «00:00», а не «01.01.1900».
		if (value.Date == TimeOfDayBase)
			return value.ToString("HH:mm");
		return value.TimeOfDay == TimeSpan.Zero ? Date(value) : DateAndTime(value);
	}

	private static readonly DateTime TimeOfDayBase = new(1900, 1, 1);

	/// <summary>24.09</summary>
	public static string DayMonth(DateTime value) => value.ToString(DayMonthPattern);

	/// <summary>24.09 09:05</summary>
	public static string DayMonthTime(DateTime value) => value.ToString(DayMonthPattern + " HH:mm");

	/// <summary>Краткие названия дней с понедельника: Пн, Вт, … Вс.</summary>
	public static string[] WeekDaysFromMonday()
	{
		string[] names = Info.AbbreviatedDayNames; // с воскресенья
		var result = new string[7];
		for (int i = 0; i < 7; i++)
		{
			string name = names[(i + 1) % 7];
			result[i] = name.Length == 0 ? name : char.ToUpper(name[0], CultureInfo.CurrentCulture) + name.Substring(1);
		}
		return result;
	}

	// Краткая дата без года: из «dd.MM.yyyy» — «dd.MM», из «d/M/yyyy» — «d/M».
	private static string DayMonthPattern =>
		Regex.Replace(Info.ShortDatePattern, @"[^dM]*y+[^dM]*", "");
}
