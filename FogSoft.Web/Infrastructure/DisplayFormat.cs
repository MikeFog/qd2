using System.Globalization;
using System.Text.RegularExpressions;
using FogSoft.WinForm.Classes;

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

	/// <summary>
	/// Деньги и дроби в ячейке списка — по типу колонки, как десктопный SmartGrid.CreateDataGridColumn: Style.Format "c"
	/// (валюта, с разделителем тысяч и знаком ₽) для money-колонок и "f" (два знака после запятой) для decimal/float.
	/// Признак — ColumnInfo.IsMoneyData/IsFloatData: сначала тип атрибута из метаданных, потом тип колонки БД.
	/// null — не деньги и не дробь, значение показывает вызывающий. Общее для ObjectList, ObjectSelector и
	/// selector-грида карточки (решение Миши 29.10.2026 — правило веба по типу колонки, а не разметка метаданных).
	/// </summary>
	public static string? Number(object value, Entity entity, Entity.Attribute attribute)
	{
		if (value is not IConvertible)
			return null;
		entity.ColumnsInfo.TryGetValue(attribute.Name, out ColumnInfo? columnInfo);
		if (ColumnInfo.IsMoneyData(columnInfo, attribute))
			return Convert.ToDecimal(value).ToString("C");
		if (ColumnInfo.IsFloatData(columnInfo, attribute))
			return Convert.ToDecimal(value).ToString("F");
		return null;
	}

	/// <summary>
	/// Число в поле карточки по типу одноимённой колонки таблицы сущности — тот же признак, что у списков
	/// (<see cref="Number"/>), но без округления: из поля ввода значение уходит обратно в базу. Деньги — на чтение
	/// «5 360,00 ₽», в поле ввода «5360,00»; прочие дроби — без лишних нулей («1,0000000000» → «1», «0,8000» → «0,8»).
	/// null — не число из базы (в том числе уже введённый текст) или у поля нет колонки: показать как есть.
	/// </summary>
	public static string? FieldNumber(object? value, Entity entity, string column, bool readOnly)
	{
		if (value is not (decimal or double or float))
			return null;
		// Тип атрибута списка важнее типа колонки (ColumnInfo.IsSQLType): «Сумма» порога — decimal(18,2), но в списке деньги.
		Entity.Attribute attribute = entity.SortedAttributes.Find(a => string.Equals(a.Name, column, StringComparison.OrdinalIgnoreCase))
			?? new Entity.Attribute(column, column, string.Empty);
		entity.ColumnsInfo.TryGetValue(column, out ColumnInfo? columnInfo);
		decimal number = Convert.ToDecimal(value);
		if (ColumnInfo.IsMoneyData(columnInfo, attribute))
		{
			if (readOnly)
				return number.ToString("C");
			if (number == decimal.Round(number, 2))
				return number.ToString("F2");
		}
		else if (!ColumnInfo.IsFloatData(columnInfo, attribute))
			return null;
		return number.ToString("0.############################");
	}

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
