using System.Data;
using System.Globalization;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using FogSoft.WinForm;
using FogSoft.WinForm.Classes;

namespace FogSoft.Web.Infrastructure;

/// <summary>
/// Выгрузка списка в .xlsx: колонки и строки на входе, файл на выходе. Веб-аналог
/// десктопного ExportManager.ExportExcel(DataGridView, Entity) — выгружает ровно
/// то, что показано, а не заново прочитанные данные.
///
/// Пишется потоком (OpenXmlWriter), а не деревом объектов: журнал удалённых
/// выпусков — десятки тысяч строк, и DOM держал бы их все в памяти разом.
/// Живёт в вебе, а не в ядре: этап 4 плана заменит весь слой выгрузок десктопа,
/// и этот код с ним смешиваться не должен.
///
/// Правила формата списаны с ExportManager:
///   — первая строка — подписи колонок (Entity.Attribute.Alias), жирным, с рамкой;
///   — boolean → «Да»/«Нет» (в самом списке это галочка, а десктопный экспорт
///     пишет текст);
///   — деньги (ColumnInfo.IsMoneyData) — число с денежным форматом;
///   — decimal/float (ColumnInfo.IsFloatData) — число, два знака;
///   — атрибут time и date2 — свои форматы времени и даты;
///   — остальные числа — числами, даты — датами (не строками), иначе в Excel
///     нельзя суммировать и сортировать;
///   — ширины колонок — по длине содержимого (десктоп: SetAutoFitCells).
/// Тип ячейки решается признаками метаданных (ColumnInfo.Is*Data), как в
/// ObjectList.Display; по значению выбирается только способ записи (число,
/// дата, строка), а не смысл колонки.
/// </summary>
public static class GridExcelExport
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    // Индексы стилей (cellXfs) — порядок задан в BuildStyles.
    private const uint StHeader = 1, StText = 2, StNumber = 3, StMoney = 4, StFloat = 5,
                       StTime = 6, StDate = 7, StDateTime = 8;

    // Форматы — те же, что ставит десктоп (MSDocumentSheet.SetFormatForCell).
    private const uint FmtMoney = 164, FmtTime = 165, FmtDate = 166, FmtDateTime = 167;

    // Предел ширины колонки в знаках: как и в гриде, длинный текст не растягивает
    // колонку на весь экран.
    private const double MaxWidth = 60;

    // Даты до этого срока Excel не хранит числом (1900 — начало его календаря).
    private static readonly DateTime MinExcelDate = new(1900, 1, 1);

    private const string BoolYes = "Да", BoolNo = "Нет";

    /// <summary>
    /// Собирает книгу. <paramref name="columns"/> — колонки в порядке показа,
    /// <paramref name="rows"/> — строки в порядке показа (с учётом сортировки).
    /// Поток возвращается в начале.
    /// </summary>
    public static MemoryStream Build(
        Entity entity, IReadOnlyList<Entity.Attribute> columns, IReadOnlyList<DataRow> rows)
    {
        var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            WorkbookPart workbook = document.AddWorkbookPart();

            var stylesPart = workbook.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = BuildStyles();
            stylesPart.Stylesheet.Save();

            var strings = new SharedStrings();
            var sheetPart = workbook.AddNewPart<WorksheetPart>();
            var kinds = columns.Select(c => KindOf(entity, c)).ToArray();
            WriteSheet(sheetPart, columns, kinds, rows, strings);

            // Таблица строк пишется после листа: индексы известны, только когда
            // просмотрены все ячейки.
            var stringsPart = workbook.AddNewPart<SharedStringTablePart>();
            strings.Write(stringsPart);

            workbook.Workbook = new Workbook(
                new Sheets(new Sheet { Id = workbook.GetIdOfPart(sheetPart), SheetId = 1, Name = "Лист1" }));
            workbook.Workbook.Save();
        }

        stream.Position = 0;
        return stream;
    }

    /// <summary>
    /// Имя файла: «заголовок экрана дата.xlsx». Символы, недопустимые в имени
    /// файла, заменяются дефисом — «Предметы рекламы / Предмет рекламы» не должно
    /// терять смысл на слэше.
    /// </summary>
    public static string FileName(string? title, DateTime date)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var name = new StringBuilder();
        foreach (char ch in string.IsNullOrWhiteSpace(title) ? "Список" : title.Trim())
            name.Append(Array.IndexOf(invalid, ch) >= 0 ? '-' : ch);

        // Windows не любит точку и пробел в конце имени; длинное имя режем —
        // заголовок дерева склеен из двух названий.
        string safe = name.ToString();
        if (safe.Length > 100)
            safe = safe[..100];
        safe = safe.TrimEnd(' ', '.');

        return $"{safe} {date:dd.MM.yyyy}.xlsx";
    }

    // ---------- Лист ----------

    private enum Kind { Text, Boolean, Money, Float, Time, Date }

    private static Kind KindOf(Entity entity, Entity.Attribute attribute)
    {
        entity.ColumnsInfo.TryGetValue(attribute.Name, out ColumnInfo? info);

        // Порядок как в ExportManager.SetColumnFormat, boolean — первым: у него
        // своё правило (текст), а не формат числа.
        if (ColumnInfo.IsBooleanData(info, attribute)) return Kind.Boolean;
        if (ColumnInfo.IsMoneyData(info, attribute)) return Kind.Money;
        if (ColumnInfo.IsFloatData(info, attribute)) return Kind.Float;
        if (attribute.DataType == "time") return Kind.Time;
        if (attribute.DataType == "date2") return Kind.Date;
        return Kind.Text;
    }

    private static void WriteSheet(
        WorksheetPart part, IReadOnlyList<Entity.Attribute> columns, Kind[] kinds,
        IReadOnlyList<DataRow> rows, SharedStrings strings)
    {
        // Только колонки, которые есть в строках: как и в гриде, атрибут без
        // колонки в результате процедуры не выгружается (ObjectList.Columns это
        // уже отфильтровал, но защита дешёвая).
        DataColumnCollection? present = rows.Count > 0 ? rows[0].Table.Columns : null;
        bool[] exists = columns.Select(c => present != null && present.Contains(c.Name)).ToArray();

        double[] widths = new double[columns.Count];
        for (int i = 0; i < columns.Count; i++)
            widths[i] = (columns[i].Alias?.Length ?? 0) * 1.2 + 2;   // подпись жирная

        MeasureWidths(columns, kinds, exists, rows, widths);

        using OpenXmlWriter writer = OpenXmlWriter.Create(part);
        writer.WriteStartElement(new Worksheet());

        writer.WriteStartElement(new Columns());
        for (int i = 0; i < columns.Count; i++)
            writer.WriteElement(new Column
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = Math.Min(widths[i], MaxWidth),
                CustomWidth = true,
            });
        writer.WriteEndElement();

        writer.WriteStartElement(new SheetData());

        writer.WriteStartElement(new Row());
        foreach (Entity.Attribute a in columns)
            writer.WriteElement(StringCell(strings, a.Alias ?? a.Name, StHeader));
        writer.WriteEndElement();

        foreach (DataRow row in rows)
        {
            writer.WriteStartElement(new Row());
            for (int i = 0; i < columns.Count; i++)
                writer.WriteElement(exists[i]
                    ? ValueCell(strings, row[columns[i].Name], kinds[i])
                    : new Cell { StyleIndex = StText });
            writer.WriteEndElement();
        }

        writer.WriteEndElement();   // sheetData
        writer.WriteEndElement();   // worksheet
    }

    /// <summary>
    /// Приблизительная ширина по содержимому — аналог SetAutoFitCells. Строки
    /// измеряются по длине, числа и даты — по типичной ширине их формата:
    /// форматировать каждое значение ради ширины было бы дороже самой выгрузки.
    /// </summary>
    private static void MeasureWidths(
        IReadOnlyList<Entity.Attribute> columns, Kind[] kinds, bool[] exists,
        IReadOnlyList<DataRow> rows, double[] widths)
    {
        for (int i = 0; i < columns.Count; i++)
        {
            if (!exists[i])
                continue;

            string name = columns[i].Name;
            double w = widths[i];
            foreach (DataRow row in rows)
            {
                object v = row[name];
                if (v == DBNull.Value)
                    continue;

                double len = kinds[i] switch
                {
                    Kind.Boolean => 3,
                    Kind.Money => 16,
                    Kind.Float => 12,
                    _ => v switch
                    {
                        string s => s.Length + 2,
                        DateTime dt => dt.TimeOfDay == TimeSpan.Zero ? 12 : 20,
                        TimeSpan => 10,
                        _ => 12,
                    },
                };
                if (len > w)
                    w = len;
                if (w >= MaxWidth)
                    break;
            }
            widths[i] = w;
        }
    }

    // ---------- Ячейки ----------

    private static Cell StringCell(SharedStrings strings, string text, uint style) => new()
    {
        DataType = CellValues.SharedString,
        StyleIndex = style,
        CellValue = new CellValue(strings.IndexOf(text).ToString(CultureInfo.InvariantCulture)),
    };

    private static Cell NumberCell(string invariant, uint style) => new()
    {
        DataType = CellValues.Number,
        StyleIndex = style,
        CellValue = new CellValue(invariant),
    };

    private static Cell ValueCell(SharedStrings strings, object v, Kind kind)
    {
        if (v == DBNull.Value)
            return new Cell { StyleIndex = StText };   // пустая, но с рамкой

        if (kind == Kind.Boolean || v is bool)
            return StringCell(strings, ParseHelper.GetBooleanFromObject(v, false) ? BoolYes : BoolNo, StText);

        // Атрибут-время: часть суток. Значение приходит и как TimeSpan (тип time
        // в БД), и как DateTime (тип datetime, из которого берётся время).
        if (kind == Kind.Time)
        {
            if (v is TimeSpan ts)
                return NumberCell(Invariant(ts.TotalDays), StTime);
            if (v is DateTime t)
                return NumberCell(Invariant(t.TimeOfDay.TotalDays), StTime);
        }

        if (v is DateTime date)
        {
            if (date < MinExcelDate)
                return StringCell(strings, date.ToString(CultureInfo.CurrentCulture), StText);

            bool dateOnly = kind == Kind.Date || date.TimeOfDay == TimeSpan.Zero;
            return NumberCell(Invariant(date.ToOADate()), dateOnly ? StDate : StDateTime);
        }

        if (v is TimeSpan span)
            return NumberCell(Invariant(span.TotalDays), StTime);

        if (TryNumber(v, out string? number))
        {
            uint style = kind switch { Kind.Money => StMoney, Kind.Float => StFloat, _ => StNumber };
            return NumberCell(number!, style);
        }

        return StringCell(strings, Convert.ToString(v, CultureInfo.CurrentCulture) ?? "", StText);
    }

    private static bool TryNumber(object v, out string? text)
    {
        switch (v)
        {
            case byte or sbyte or short or ushort or int or uint or long or ulong:
                text = Convert.ToString(v, CultureInfo.InvariantCulture);
                return true;
            case decimal m:
                text = m.ToString(CultureInfo.InvariantCulture);
                return true;
            case float f when float.IsFinite(f):
                text = f.ToString("R", CultureInfo.InvariantCulture);
                return true;
            case double d when double.IsFinite(d):
                text = d.ToString("R", CultureInfo.InvariantCulture);
                return true;
            default:
                text = null;
                return false;
        }
    }

    private static string Invariant(double d) => d.ToString("R", CultureInfo.InvariantCulture);

    // ---------- Таблица общих строк ----------

    /// <summary>
    /// Общие строки: одинаковое значение (название станции, тип оплаты) хранится
    /// один раз, а ячейки ссылаются на него по номеру — файл журнала на десятки
    /// тысяч строк получается в разы меньше.
    /// </summary>
    private sealed class SharedStrings
    {
        private readonly Dictionary<string, int> _index = new(StringComparer.Ordinal);
        private readonly List<string> _items = new();

        public int IndexOf(string text)
        {
            text = Sanitize(text);
            if (!_index.TryGetValue(text, out int i))
            {
                i = _items.Count;
                _index[text] = i;
                _items.Add(text);
            }
            return i;
        }

        public void Write(SharedStringTablePart part)
        {
            using OpenXmlWriter writer = OpenXmlWriter.Create(part);
            writer.WriteStartElement(new SharedStringTable
            {
                Count = (uint)_items.Count,
                UniqueCount = (uint)_items.Count,
            });
            foreach (string s in _items)
            {
                writer.WriteStartElement(new SharedStringItem());
                // Пробелы по краям XML по умолчанию отбрасывает — без xml:space
                // значение «Москва » вернулось бы из файла без пробела.
                writer.WriteElement(new Text(s) { Space = SpaceProcessingModeValues.Preserve });
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Управляющие символы (кроме табуляции и перевода строки) недопустимы в
        /// XML: один такой в названии — и писатель бросил бы исключение на всём
        /// файле. Заменяем пробелом.
        /// </summary>
        private static string Sanitize(string s)
        {
            StringBuilder? sb = null;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                bool bad = c < 0x20 && c != '\t' && c != '\n' && c != '\r'
                           || c == 0xFFFE || c == 0xFFFF
                           || (char.IsSurrogate(c) && !IsPairedSurrogate(s, i));
                if (bad)
                {
                    sb ??= new StringBuilder(s);
                    sb[i] = ' ';
                }
            }
            return sb?.ToString() ?? s;
        }

        private static bool IsPairedSurrogate(string s, int i) =>
            char.IsHighSurrogate(s[i])
                ? i + 1 < s.Length && char.IsLowSurrogate(s[i + 1])
                : i > 0 && char.IsHighSurrogate(s[i - 1]);
    }

    // ---------- Стили ----------

    /// <summary>
    /// Шрифт как в десктопе (Tahoma 10), рамка на всех ячейках — десктоп
    /// обводит весь выгружаемый диапазон, а не только шапку.
    /// </summary>
    private static Stylesheet BuildStyles()
    {
        static Font Tahoma(bool bold) => bold
            ? new Font(new Bold(), new FontSize { Val = 10 }, new FontName { Val = "Tahoma" })
            : new Font(new FontSize { Val = 10 }, new FontName { Val = "Tahoma" });

        static Border Thin() => new(
            new LeftBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
            new RightBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
            new TopBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
            new BottomBorder(new Color { Auto = true }) { Style = BorderStyleValues.Thin },
            new DiagonalBorder());

        static CellFormat Xf(uint numFmt, uint font, uint border) => new()
        {
            NumberFormatId = numFmt,
            FontId = font,
            FillId = 0,
            BorderId = border,
            ApplyNumberFormat = numFmt != 0,
            ApplyFont = true,
            ApplyBorder = border != 0,
        };

        return new Stylesheet(
            new NumberingFormats(
                new NumberingFormat { NumberFormatId = FmtMoney, FormatCode = "#,##0.00\\ \"₽\"" },
                new NumberingFormat { NumberFormatId = FmtTime, FormatCode = "hh:mm:ss" },
                new NumberingFormat { NumberFormatId = FmtDate, FormatCode = "dd/mm/yyyy" },
                new NumberingFormat { NumberFormatId = FmtDateTime, FormatCode = "dd/mm/yyyy\\ hh:mm:ss" }),
            new Fonts(Tahoma(false), Tahoma(true)),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
            new Borders(new Border(), Thin()),
            new CellStyleFormats(new CellFormat { NumberFormatId = 0, FontId = 0, FillId = 0, BorderId = 0 }),
            new CellFormats(
                Xf(0, 0, 0),             // 0 — по умолчанию
                Xf(0, 1, 1),             // 1 — подпись колонки: жирный, рамка
                Xf(0, 0, 1),             // 2 — текст
                Xf(0, 0, 1),             // 3 — число «как есть» (формат «Общий»)
                Xf(FmtMoney, 0, 1),      // 4 — деньги
                Xf(2, 0, 1),             // 5 — два знака (встроенный «0.00»)
                Xf(FmtTime, 0, 1),       // 6 — время
                Xf(FmtDate, 0, 1),       // 7 — дата
                Xf(FmtDateTime, 0, 1))); // 8 — дата и время
    }
}
