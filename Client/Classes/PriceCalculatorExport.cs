using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FogSoft.WinForm.Classes;
using FogSoft.WinForm.Classes.Export;

namespace Merlin.Classes
{
	/// <summary>
	/// Выгрузка калькулятора цены в Excel без Excel (docs/tasks/web-price-calculator.md, этап К.1): тот же лист, что
	/// десктопная «Excel» (<c>PriceCalculatorForm.ExportSelectedToExcel</c>) — блок «Параметры расчета:», таблица отмеченных
	/// строк, «Итог:», — на <see cref="OpenXmlExportDocument"/>. Колонки цен — по правилу веба: две, если у всех станций
	/// цены будней и выходных совпадают, иначе четыре; разные по сегментам цены — диапазоном «x – y» (К-30).
	/// </summary>
	public static class PriceCalculatorExport
	{
		private const string MoneyFormat = "#,##0.00";
		private const string RatioFormat = "0.00";

		public sealed class Column
		{
			public string Header { get; set; }
			/// <summary>Формат чисел колонки; null — общий.</summary>
			public string NumberFormat { get; set; }
			public Func<PcRowResult, PcStation, object> Value { get; set; }
		}

		/// <summary>Колонки таблицы и выгрузки — в порядке десктопа, цены — по правилу «2 или 4».</summary>
		public static List<Column> Columns(PcModel model)
		{
			var positions = Positions();
			var columns = new List<Column>
			{
				new Column { Header = Tr.T("Радиостанция"), Value = (r, s) => s != null ? s.Name : null },
				new Column { Header = Tr.T("Пр-ть ролика"), Value = (r, s) => r.Input.Duration },
				new Column
				{
					Header = Tr.T("Позиция"),
					Value = (r, s) =>
					{
						string name;
						return positions.TryGetValue(r.Input.Position, out name) ? name : r.Input.Position.ToString(CultureInfo.CurrentCulture);
					},
				},
			};

			if (WeekdayEqualsWeekend(model.Stations))
			{
				columns.Add(PriceColumn(Tr.T("Цена прайм"), x => x.PrimeWeekday));
				columns.Add(PriceColumn(Tr.T("Цена офф прайм"), x => x.NonPrimeWeekday));
			}
			else
			{
				columns.Add(PriceColumn(Tr.T("Цена будни прайм"), x => x.PrimeWeekday));
				columns.Add(PriceColumn(Tr.T("Цена будни офф прайм"), x => x.NonPrimeWeekday));
				columns.Add(PriceColumn(Tr.T("Цена выходные прайм"), x => x.PrimeWeekend));
				columns.Add(PriceColumn(Tr.T("Цена выходные офф прайм"), x => x.NonPrimeWeekend));
			}

			columns.Add(new Column { Header = Tr.T("Кол-во выходов будни прайм"), Value = (r, s) => r.Input.PrimeWeekday });
			columns.Add(new Column { Header = Tr.T("Кол-во выходов будни офф прайм"), Value = (r, s) => r.Input.NonPrimeWeekday });
			columns.Add(new Column { Header = Tr.T("Кол-во выходов выходные прайм"), Value = (r, s) => r.Input.PrimeWeekend });
			columns.Add(new Column { Header = Tr.T("Кол-во выходов выходные офф прайм"), Value = (r, s) => r.Input.NonPrimeWeekend });
			columns.Add(new Column { Header = Tr.T("Цена кампании"), NumberFormat = MoneyFormat, Value = (r, s) => r.Price.TariffPrice });
			columns.Add(new Column { Header = Tr.T("Объёмная скидка"), NumberFormat = RatioFormat, Value = (r, s) => r.CompanyDiscount });
			columns.Add(new Column { Header = Tr.T("Сезонный коэфф."), NumberFormat = RatioFormat, Value = (r, s) => r.Price.ManagerRatio });
			columns.Add(new Column { Header = Tr.T("Цена до пакетной скидки"), NumberFormat = MoneyFormat, Value = (r, s) => r.TotalBeforePackage });
			columns.Add(new Column { Header = Tr.T("Пакетная скидка"), NumberFormat = RatioFormat, Value = (r, s) => r.PackageDiscount });
			columns.Add(new Column { Header = Tr.T("Итог"), NumberFormat = MoneyFormat, Value = (r, s) => r.Total });
			return columns;
		}

		/// <summary>Цены будней и выходных совпадают у всех станций во всех сегментах — тогда хватает двух колонок цен.</summary>
		public static bool WeekdayEqualsWeekend(IEnumerable<PcStation> stations)
		{
			return stations.SelectMany(s => s.Segments)
				.All(x => x.PrimeWeekday == x.PrimeWeekend && x.NonPrimeWeekday == x.NonPrimeWeekend);
		}

		/// <summary>Цена за секунду станции: одно число или «x – y», если по сегментам периода она разная.</summary>
		public static object PriceValue(PcStation station, Func<PcSegment, decimal> price)
		{
			decimal min, max;
			PriceCalculatorEngine.PriceRange(station, price, out min, out max);
			if (min == max)
				return min;
			// неразрывные пробелы: диапазон не переносится внутри ячейки с переносом текста
			return string.Format(CultureInfo.CurrentCulture, "{0:N2} – {1:N2}", min, max);
		}

		/// <summary>«Город:» — города (группы) отмеченных станций через запятую, как десктоп и КП.</summary>
		public static string CityDescription(PcModel model, PcResult result)
		{
			var stations = model.Stations.ToDictionary(s => s.MassmediaId);
			return string.Join(", ", result.Rows
				.Where(r => r.Input.Selected)
				.Select(r =>
				{
					PcStation s;
					return stations.TryGetValue(r.Input.MassmediaId, out s) && s.GroupName != null ? s.GroupName.Trim() : null;
				})
				.Where(g => !string.IsNullOrEmpty(g))
				.Distinct()
				.OrderBy(g => g));
		}

		/// <summary>«График:» — дни недели («Пн, Вт») или «Чётные дни» / «Нечётные дни».</summary>
		public static string ScheduleDescription(PcTemplate template)
		{
			if (template.UseDaysOfWeek)
			{
				string[] names = { "Пн", "Вт", "Ср", "Чт", "Пт", "Сб", "Вс" }; // i18n-ok: ключи, переводятся Tr.T(n) ниже
				var active = names.Where((n, i) => template.DaysOfWeek[i]).Select(n => Tr.T(n)).ToList();
				return active.Count > 0 ? string.Join(", ", active) : "—";
			}
			return template.EvenDays ? Tr.T("Чётные дни") : Tr.T("Нечётные дни");
		}

		/// <summary>Хронометраж часами без переноса в сутки: 93 000 с — «25:50:00» (К-8).</summary>
		public static string FormatDuration(int totalSeconds)
		{
			return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}:{2:00}",
				totalSeconds / 3600, totalSeconds % 3600 / 60, totalSeconds % 60);
		}

		/// <summary>
		/// Книга с листом «Отмеченные», как десктопная «Excel»: параметры расчёта, отмеченные строки, «Итог:»; формат денег
		/// и коэффициентов по колонкам, перенос заголовков по словам, ширина — по данным и самому длинному слову заголовка.
		/// </summary>
		public static byte[] Excel(PcTemplate template, PcModel model, PcResult result)
		{
			var stations = model.Stations.ToDictionary(s => s.MassmediaId);
			var rows = result.Rows.Where(r => r.Input.Selected).ToList();
			var columns = Columns(model);

			var doc = new OpenXmlExportDocument();
			IDocumentSheet sheet = doc.GetNewSheet(Tr.T("Отмеченные"), "Arial", 10);

			int currentRow = 1;
			sheet.SetCellValue(currentRow, 1, Tr.T("Параметры расчета:"));
			sheet.SetBoldForRange(currentRow, 1, currentRow, 1);
			currentRow++;
			sheet.SetCellValue(currentRow++, 1, Tr.T("Город:") + " " + CityDescription(model, result));
			sheet.SetCellValue(currentRow++, 1, string.Format(CultureInfo.CurrentCulture, Tr.T("Период: {0:d} - {1:d}"),
				template.PeriodFrom, template.PeriodTo));
			sheet.SetCellValue(currentRow++, 1, Tr.T("График:") + " " + ScheduleDescription(template));
			sheet.SetCellValue(currentRow++, 1, Tr.T("Количество рекламных выпусков:") + result.Spots.ToString(CultureInfo.CurrentCulture));
			sheet.SetCellValue(currentRow++, 1, Tr.T("Хронометраж эфирного времени:") + FormatDuration(result.Seconds));
			currentRow += 1;

			int headerRow = currentRow;
			for (int c = 0; c < columns.Count; c++)
			{
				sheet.SetCellValue(headerRow, c + 1, columns[c].Header);
				if (columns[c].NumberFormat != null)
					sheet.SetColumnNumberFormat(c + 1, columns[c].NumberFormat);
			}
			currentRow = headerRow + 1;

			foreach (var r in rows)
			{
				PcStation station;
				stations.TryGetValue(r.Input.MassmediaId, out station);
				for (int c = 0; c < columns.Count; c++)
					sheet.SetCellValue(currentRow, c + 1, columns[c].Value(r, station));
				currentRow++;
			}

			int cols = columns.Count;
			sheet.SetBoldForRange(headerRow, 1, headerRow, cols);
			sheet.SetBoldForRange(headerRow, cols, headerRow + rows.Count, cols);
			sheet.SetBordersStyles(headerRow, 1, headerRow + rows.Count, cols, true);
			int totalRow = currentRow;
			sheet.SetCellValue(totalRow, cols - 1, Tr.T("Итог:") + " ");
			sheet.SetCellValue(totalRow, cols, result.Total);
			sheet.SetBoldForRange(totalRow, cols, totalRow, cols);
			int lastRow = totalRow;

			// перенос по словам — только у заголовков (десктоп включал его на всю таблицу, и «Не определен» уходило в две строки);
			// ширина колонок — по данным, не уже самого длинного слова заголовка
			sheet.SetWrapText(headerRow, 1, headerRow, cols, true);
			sheet.SetAutoFitCells(1, cols);
			for (int c = 0; c < cols; c++)
			{
				int longestWord = columns[c].Header
					.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
					.DefaultIfEmpty(string.Empty)
					.Max(w => w.Length);
				if (longestWord == 0) continue;
				// как десктоп: не уже самого длинного слова заголовка, с запасом на ширину кириллицы
				sheet.SetColumnWidth(c + 1, Math.Max(sheet.GetColumnWidth(c + 1), Math.Ceiling(longestWord * 1.2) + 2));
			}
			sheet.SetAutoFitRows(headerRow, headerRow);
			return doc.ToArray();
		}

		/// <summary>Позиции калькулятора с подписями: «Не определен», «Первый», «Второй», «Последний».</summary>
		public static Dictionary<int, string> Positions()
		{
			return Issue.GetRollerPositionItems().ToDictionary(kv => kv.Key, kv => kv.Value);
		}

		private static Column PriceColumn(string header, Func<PcSegment, decimal> price)
		{
			return new Column { Header = header, NumberFormat = MoneyFormat, Value = (r, s) => PriceValue(s, price) };
		}
	}
}
