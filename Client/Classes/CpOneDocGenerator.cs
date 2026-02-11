using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Merlin.Classes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace Merlin.Cp
{
    // ============================================================
    // 1) ГРУППИРОВКА КП-1 (кластеры по одинаковому Header+RowPattern)
    //    ВАЖНО: "кластеры" могут быть и из 1 снапшота.
    //    Remainder = только НЕ-КП1-совместимые (Rows не одинаковы).
    // ============================================================

    public static class Cp1Grouping
    {
        public static List<Cp1Group> FindCp1Groups(List<CampaignCalcSnapshot> saved)
        {
            if (saved == null || saved.Count == 0) return new List<Cp1Group>();

            var normalized = saved
                .Select(Normalize)
                .Where(x => x != null)
                .ToList();

            return normalized
                .GroupBy(x => new Cp1Key(x.Header, x.RowPattern))
                .Select(g => new Cp1Group
                {
                    Header = g.First().Header,
                    RowPattern = g.First().RowPattern,
                    Snapshots = g.Select(x => x.Source).ToList()
                })
                .ToList();
        }

        /// <summary>
        /// КП-1 совместимость: в одном снапшоте все строки Rows имеют одинаковый "паттерн"
        /// (споты/ролик/позиция одинаковы).
        /// </summary>
        public static bool IsCp1Compatible(CampaignCalcSnapshot s)
        {
            return Normalize(s) != null;
        }

        private static Normalized Normalize(CampaignCalcSnapshot s)
        {
            if (s?.Rows == null || s.Rows.Count == 0) return null;

            var p0 = BuildRowPattern(s.Rows[0]);
            for (int i = 1; i < s.Rows.Count; i++)
            {
                var pi = BuildRowPattern(s.Rows[i]);
                if (!p0.Equals(pi))
                    return null;
            }

            var h = new Cp1Header
            {
                DateFrom = s.DateFrom.Date,
                DateTo = s.DateTo.Date,
                TotalDays = s.TotalDays,
            };

            return new Normalized { Source = s, Header = h, RowPattern = p0 };
        }

        private static Cp1RowPattern BuildRowPattern(CampaignCalcRow r)
        {
            return new Cp1RowPattern
            {
                PrimeTotalSpotsWeekday = r.PrimeTotalSpotsWeekday,
                NonPrimeTotalSpotsWeekday = r.NonPrimeTotalSpotsWeekday,
                PrimeTotalSpotsWeekend = r.PrimeTotalSpotsWeekend,
                NonPrimeTotalSpotsWeekend = r.NonPrimeTotalSpotsWeekend,
                RollerDuration = r.RollerDuration,
                Position = r.Position
            };
        }

        // ===== DTO =====

        public class Cp1Group
        {
            public Cp1Header Header { get; set; }
            public Cp1RowPattern RowPattern { get; set; }
            public List<CampaignCalcSnapshot> Snapshots { get; set; } = new List<CampaignCalcSnapshot>();
        }

        public sealed class Cp1Header : IEquatable<Cp1Header>
        {
            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }
            public int TotalDays { get; set; }

            public bool Equals(Cp1Header o)
            {
                if (o == null) return false;
                // Для КП-1 ключа сейчас достаточно периода (ты так и хотел)
                return DateFrom == o.DateFrom && DateTo == o.DateTo;
            }

            public override bool Equals(object obj) => Equals(obj as Cp1Header);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = 17;
                    h = h * 23 + DateFrom.GetHashCode();
                    h = h * 23 + DateTo.GetHashCode();
                    return h;
                }
            }
        }

        public sealed class Cp1RowPattern : IEquatable<Cp1RowPattern>
        {
            public int PrimeTotalSpotsWeekday { get; set; }
            public int NonPrimeTotalSpotsWeekday { get; set; }
            public int PrimeTotalSpotsWeekend { get; set; }
            public int NonPrimeTotalSpotsWeekend { get; set; }

            public int RollerDuration { get; set; }
            public int Position { get; set; }

            public bool Equals(Cp1RowPattern o)
            {
                if (o == null) return false;
                return PrimeTotalSpotsWeekday == o.PrimeTotalSpotsWeekday
                       && NonPrimeTotalSpotsWeekday == o.NonPrimeTotalSpotsWeekday
                       && PrimeTotalSpotsWeekend == o.PrimeTotalSpotsWeekend
                       && NonPrimeTotalSpotsWeekend == o.NonPrimeTotalSpotsWeekend
                       && RollerDuration == o.RollerDuration
                       && Position == o.Position;
            }

            public override bool Equals(object obj) => Equals(obj as Cp1RowPattern);

            public override int GetHashCode()
            {
                unchecked
                {
                    int h = 17;
                    h = h * 23 + PrimeTotalSpotsWeekday;
                    h = h * 23 + NonPrimeTotalSpotsWeekday;
                    h = h * 23 + PrimeTotalSpotsWeekend;
                    h = h * 23 + NonPrimeTotalSpotsWeekend;
                    h = h * 23 + RollerDuration;
                    h = h * 23 + Position;
                    return h;
                }
            }
        }

        private sealed class Cp1Key : IEquatable<Cp1Key>
        {
            public Cp1Header Header { get; }
            public Cp1RowPattern Pattern { get; }

            public Cp1Key(Cp1Header header, Cp1RowPattern pattern)
            {
                Header = header;
                Pattern = pattern;
            }

            public bool Equals(Cp1Key o) => o != null && Equals(Header, o.Header) && Equals(Pattern, o.Pattern);
            public override bool Equals(object obj) => Equals(obj as Cp1Key);
            public override int GetHashCode()
            {
                unchecked { return (Header.GetHashCode() * 397) ^ Pattern.GetHashCode(); }
            }
        }

        private sealed class Normalized
        {
            public CampaignCalcSnapshot Source { get; set; }
            public Cp1Header Header { get; set; }
            public Cp1RowPattern RowPattern { get; set; }
        }
    }

    // ============================================================
    // 2) РАЗБИЕНИЕ: КП-1 кластеры + детальный остаток
    //    Remainder = ТОЛЬКО те, кто НЕ подходит под КП-1.
    // ============================================================

    public static class Cp1Split
    {
        public sealed class SplitResult
        {
            public List<Cp1Grouping.Cp1Group> Groups { get; set; } = new List<Cp1Grouping.Cp1Group>();
            public List<CampaignCalcSnapshot> Remainder { get; set; } = new List<CampaignCalcSnapshot>();
        }

        public static SplitResult SplitGroupsAndRemainder(List<CampaignCalcSnapshot> saved)
        {
            var res = new SplitResult();
            if (saved == null || saved.Count == 0) return res;

            // КП-1 группы (включая из 1 элемента)
            res.Groups = Cp1Grouping.FindCp1Groups(saved)
                // можно сортировать по размеру группы или оставить как есть
                .OrderByDescending(g => g.Snapshots.Count)
                .ToList();

            // Детальный остаток = только не совместимые с КП-1
            res.Remainder = saved.Where(s => !Cp1Grouping.IsCp1Compatible(s)).ToList();

            return res;
        }
    }

    // ============================================================
    // 3) ГЕНЕРАЦИЯ ОДНОГО DOCX
    //    - не ломаем колонтитулы: НЕ удаляем SectionProperties
    //    - сквозная нумерация блоков
    //    - группы (КП-1) + детальные (только remainder)
    // ============================================================

    public static class CpOneDocGenerator
    {
        private sealed class VariantRow
        {
            public string StationsSet { get; set; }
            public decimal Price { get; set; }
            public CampaignCalcSnapshot Snapshot { get; set; }
        }

        public static string GenerateOneDoc(
            List<CampaignCalcSnapshot> saved,
            string templatePath,
            string outputPath,
            string clientName,
            DateTime docDate,
            string directorName,
            string contactName,
            string contactEmail,
            string contactPhone)
        {
            if (saved == null || saved.Count == 0)
                throw new Exception("Нет данных для формирования КП.");

            var split = Cp1Split.SplitGroupsAndRemainder(saved);

            File.Copy(templatePath, outputPath, true);

            using (var doc = WordprocessingDocument.Open(outputPath, true))
            {
                var body = doc.MainDocumentPart.Document.Body;
                
                // Collect shared groupNames from all groups
                var sharedGroupNames = split.Groups
                    .Select(g => GetSharedGroupName(g))
                    .Where(gn => !string.IsNullOrWhiteSpace(gn))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                // Determine the groupName to use for the placeholder
                string groupNameValue = sharedGroupNames.Count == 1 ? sharedGroupNames[0] : string.Empty;

                // 0). Подстановка текстовых плейсхолдеров

                ReplaceTextPlaceholders2(doc, new Dictionary<string, string>
                {
                    ["{{CLIENT_NAME}}"] = clientName,
                    ["{{DOC_DATE}}"] = docDate.ToShortDateString(),
                    ["{{DIRECTOR_NAME}}"] = directorName,
                    ["{{CONTACT_NAME}}"] = contactName,
                    ["{{CONTACT_EMAIL}}"] = contactEmail,
                    ["{{CONTACT_PHONE}}"] = contactPhone,
                    ["{{groupName}}"] = groupNameValue
                });

                ReplaceTextPlaceholdersPreserveStylesEverywhere(doc, new Dictionary<string, string>
                {
                    ["{{CLIENT_NAME}}"] = clientName,
                    ["{{DOC_DATE}}"] = docDate.ToString("dd.MM.yyyy"),
                    ["{{DIRECTOR_NAME}}"] = directorName,
                    ["{{CONTACT_NAME}}"] = contactName,
                    ["{{CONTACT_EMAIL}}"] = contactEmail,
                    ["{{CONTACT_PHONE}}"] = contactPhone,
                    ["{{groupName}}"] = groupNameValue
                });


                // 1) Ищем абзац-якорь с {{CONTENT}}
                var anchorParagraph = FindAnchorParagraph(body, "{{CONTENT}}");
                if (anchorParagraph == null)
                    throw new Exception("Не найден якорь {{CONTENT}} в шаблоне Word. Вставь {{CONTENT}} отдельным абзацем в нужное место.");

                // 2) Генерим контент в список элементов (ничего не вставляем в документ по ходу генерации)
                var generated = new List<OpenXmlElement>();
                Action<OpenXmlElement> emit = e => generated.Add(e);

                int blockNo = 1;

                // КП-1 группы (включая группы из 1 варианта)
                foreach (var g in split.Groups)
                {
                    if (blockNo > 1) AppendPageBreak(emit);

                    AppendGroupBlock(
                        emit,
                        blockNo,
                        g,
                        clientName,
                        docDate,
                        directorName,
                        contactName,
                        contactEmail,
                        contactPhone);

                    blockNo++;
                }

                // Детальные (ТОЛЬКО не-совместимые с КП-1)
                foreach (var s in split.Remainder)
                {
                    if (blockNo > 1) AppendPageBreak(emit);

                    AppendDetailedBlock(emit, blockNo, s);
                    blockNo++;
                }

                // 3) Вклеиваем элементы ПОСЛЕ якорного абзаца, сохраняя порядок
                OpenXmlElement cursor = anchorParagraph;
                foreach (var el in generated)
                {
                    cursor.InsertAfterSelf(el);
                    cursor = el;
                }

                // 4) Удаляем якорь (целиком абзац)
                anchorParagraph.Remove();

                doc.MainDocumentPart.Document.Save();
            }

            return outputPath;
        }

        private static Paragraph FindAnchorParagraph(Body body, string anchorToken)
        {
            // Ищем в тексте (Text) внутри Paragraph
            return body.Descendants<Paragraph>()
                .FirstOrDefault(p => p.Descendants<Text>().Any(t => (t.Text ?? "").Contains(anchorToken)));
        }


        // -------------------- БЛОК: ГРУППА КП-1 --------------------

        private static void AppendGroupBlock(
            Action<OpenXmlElement> emit,
            int blockNo,
            Cp1Grouping.Cp1Group group,
            string clientName,
            DateTime validTill,
            string directorName,
            string contactName,
            string contactEmail,
            string contactPhone)
        {
            emit(MakeParagraph($"{blockNo}) Линейная реклама на радио", bold: true, fontSize: "28"));
            emit(MakeParagraph(string.Empty, bold: false));

            var pw = group.RowPattern.PrimeTotalSpotsWeekday;
            var npw = group.RowPattern.NonPrimeTotalSpotsWeekday;
            var pwe = group.RowPattern.PrimeTotalSpotsWeekend;
            var npwe = group.RowPattern.NonPrimeTotalSpotsWeekend;

            var rollerSec = group.RowPattern.RollerDuration;
            var positionId = group.RowPattern.Position;

            var spotsPerStation = pw + npw + pwe + npwe;
            var durationPerStation = TimeSpan.FromSeconds(spotsPerStation * rollerSec);

            emit(MakeParagraph("Параметры расчёта одинаковые для каждой станции:", bold: false));
            emit(MakeParagraph($"Продолжительность ролика: {rollerSec} сек.", bold: false));
            emit(MakeParagraph($"Период: с {group.Header.DateFrom:dd.MM.yyyy} до {group.Header.DateTo:dd.MM.yyyy}", bold: false));
            emit(MakeParagraph($"Количество дней рекламной акции: {group.Header.TotalDays}", bold: false));
            emit(MakeParagraph($"Количество ежедневных выпусков в будни: прайм - {pw}, не прайм - {npw}", bold: false));
            emit(MakeParagraph($"Количество ежедневных выпусков в выходные: прайм - {pwe}, не прайм - {npwe}", bold: false));
            emit(MakeParagraph($"Общее количество выпусков в день: {spotsPerStation}", bold: false));
            emit(MakeParagraph($"Хронометраж эфирного времени: {durationPerStation:hh\\:mm\\:ss}", bold: false));
            emit(MakeParagraph($"Позиционирование в рекламном блоке: {MapPosition(positionId)}", bold: false));
            emit(MakeParagraph(string.Empty, bold: false));

            var rows = group.Snapshots
                .Select(s => new VariantRow { 
                    StationsSet = BuildStationsSetText(s), 
                    Price = s.GrandTotal,
                    Snapshot = s 
                })
                .OrderByDescending(x => x.Price)
                .ToList();

            emit(BuildVariantsTable(rows));

            emit(MakeParagraph(string.Empty, bold: false));
        }

        private static Table BuildVariantsTable(List<VariantRow> variants)
        {
            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableStyle { Val = "TableGrid" },
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

            // Заголовок
            var headerRow = new TableRow();
            headerRow.AppendChild(BuildCell("Радиостанции", bold: true));
            headerRow.AppendChild(BuildCell("Стоимость рекламной акции", bold: true, right: true));
            table.AppendChild(headerRow);

            foreach (var v in variants)
            {
                var row = new TableRow();
                row.AppendChild(BuildCell(v.StationsSet ?? string.Empty, bold: false));
                row.AppendChild(BuildCell(FormatRub(v.Price), bold: true, right: true));
                table.AppendChild(row);
            }

            return table;
        }

        // -------------------- БЛОК: ДЕТАЛЬНЫЙ (только remainder) --------------------

        private static void AppendDetailedBlock(Action<OpenXmlElement> emit, int blockNo, CampaignCalcSnapshot s)
        {
            emit(MakeParagraph($"{blockNo}) Линейная реклама на радио в Ярославле", bold: true, fontSize: "28"));
            emit(MakeParagraph($"Период: с {s.DateFrom:dd.MM.yyyy} до {s.DateTo:dd.MM.yyyy}", bold: false));

            var totalDur = TimeSpan.FromSeconds(Math.Max(0, s.TotalDuration));
            emit(MakeParagraph($"Суммарный хронометраж эфирного времени: {totalDur:hh\\:mm\\:ss}", bold: false));

            emit(MakeParagraph("", bold: false));
            emit(BuildDetailedTable(s));
            emit(MakeParagraph("", bold: false));
            emit(MakeRightAlignedParagraph($"Итого: {FormatRub(s.GrandTotal)}", bold: true));
        }

        private static Table BuildDetailedTable(CampaignCalcSnapshot s)
        {
            var table = new Table();
            table.AppendChild(new TableProperties(
                new TableStyle { Val = "TableGrid" },
                new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" },
                new TableBorders(
                    new TopBorder { Val = BorderValues.Single, Size = 4 },
                    new BottomBorder { Val = BorderValues.Single, Size = 4 },
                    new LeftBorder { Val = BorderValues.Single, Size = 4 },
                    new RightBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                    new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 })));

            table.AppendChild(BuildHeaderRow(new[]
            {
                "Радиостанции",
                "Ролик\n(сек)",
                "Кол-во\nвыходов\nбудни\nпрайм",
                "Кол-во\nвыходов\nбудни\nне прайм",
                "Кол-во\nвыходов\nвыходные\nпрайм",
                "Кол-во\nвыходов\nвыходные\nне прайм",
                "Всего\nвыходов",
                "Стоимость\nрекламной\nакции"
            }));

            foreach (var r in (s.Rows ?? new List<CampaignCalcRow>()))
            {
                var totalSpots =
                    r.PrimeTotalSpotsWeekday +
                    r.NonPrimeTotalSpotsWeekday +
                    r.PrimeTotalSpotsWeekend +
                    r.NonPrimeTotalSpotsWeekend;

                table.AppendChild(BuildDataRow(new[]
                {
                    r.StationName ?? "",
                    r.RollerDuration.ToString(),
                    r.PrimeTotalSpotsWeekday.ToString(),
                    r.NonPrimeTotalSpotsWeekday.ToString(),
                    r.PrimeTotalSpotsWeekend.ToString(),
                    r.NonPrimeTotalSpotsWeekend.ToString(),
                    totalSpots.ToString(),
                    FormatRub(r.TotalAfterPackage)
                }));
            }

            return table;
        }

        // -------------------- УТИЛИТЫ --------------------

        private static void AppendPageBreak(Action<OpenXmlElement> emit)
        {
            emit(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
        }

        private static string GetSharedGroupName(Cp1Grouping.Cp1Group group)
        {
            if (group?.Snapshots == null || group.Snapshots.Count == 0)
                return null;

            // Get all distinct groupNames from all rows in all snapshots
            var allGroupNames = group.Snapshots
                .SelectMany(s => s.Rows ?? new List<CampaignCalcRow>())
                .Where(r => !string.IsNullOrWhiteSpace(r.GroupName))
                .Select(r => r.GroupName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Return the shared groupName if all rows share exactly one groupName
            return allGroupNames.Count == 1 ? allGroupNames[0] : null;
        }

        private static string BuildStationsSetText(CampaignCalcSnapshot s)
        {
            if (s?.Rows == null || s.Rows.Count == 0)
                return string.Empty;

            // Check if all rows have the same groupName (and it's not null/empty)
            var allGroupNames = s.Rows
                .Where(r => !string.IsNullOrWhiteSpace(r.GroupName))
                .Select(r => r.GroupName.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            bool useSameGroup = allGroupNames.Count == 1;

            IEnumerable<string> names;
            if (useSameGroup)
            {
                // Use shortName when all rows share the same groupName
                names = s.Rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.ShortName))
                    .Select(r => r.ShortName.Trim());
            }
            else
            {
                // Use StationName (full name with group) otherwise
                names = s.Rows
                    .Where(r => !string.IsNullOrWhiteSpace(r.StationName))
                    .Select(r => r.StationName.Trim());
            }

            return string.Join(" + ", names
                .Distinct(StringComparer.CurrentCulture)
                .OrderBy(x => x, StringComparer.CurrentCulture));
        }

        private static string MapPosition(int position)
        {
            // Используем твой справочник
            return Issue.GetRollerPositionItems().FirstOrDefault(x => x.Key == position).Value ?? "без позиции";
        }

        private static string FormatRub(decimal price)
        {
            var s = string.Format(new CultureInfo("ru-RU"), "{0:N0}", price);
            return s + "\u00A0руб.";
        }

        private static Paragraph MakeParagraph(string text, bool bold, string fontSize = null)
        {
            var runProps = new RunProperties();
            if (bold) runProps.AppendChild(new Bold());
            if (!string.IsNullOrWhiteSpace(fontSize))
                runProps.AppendChild(new FontSize { Val = fontSize });

            var run = new Run(runProps, new Text(text ?? "") { Space = SpaceProcessingModeValues.Preserve });
            return new Paragraph(run);
        }

        private static Paragraph MakeRightAlignedParagraph(string text, bool bold)
        {
            var p = MakeParagraph(text, bold);
            p.ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Right });
            return p;
        }

        private static TableRow BuildHeaderRow(string[] headers)
        {
            var row = new TableRow();
            foreach (var h in headers)
                row.AppendChild(BuildCell(h, bold: true, center: true));
            return row;
        }

        private static TableRow BuildDataRow(string[] values)
        {
            var row = new TableRow();
            for (int i = 0; i < values.Length; i++)
            {
                bool center = i != 0 && i != values.Length - 1;
                bool right = i == values.Length - 1;
                row.AppendChild(BuildCell(values[i], bold: false, center: center, right: right));
            }
            return row;
        }

        private static TableCell BuildCell(string text, bool bold, bool center = false, bool right = false)
        {
            var p = MakeParagraph(text ?? "", bold);

            if (center)
                p.ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Center });
            if (right)
                p.ParagraphProperties = new ParagraphProperties(new Justification { Val = JustificationValues.Right });

            var cell = new TableCell(p);
            cell.AppendChild(new TableCellProperties(new TableCellVerticalAlignment { Val = TableVerticalAlignmentValues.Center }));
            return cell;
        }



        private static void ReplaceTextPlaceholders2(WordprocessingDocument doc, Dictionary<string, string> map)
        {
            foreach (var text in doc.MainDocumentPart.Document.Descendants<Text>())
            {
                if (string.IsNullOrEmpty(text.Text))
                    continue;

                foreach (var kv in map)
                {
                    if (text.Text.Contains(kv.Key))
                        text.Text = text.Text.Replace(kv.Key, kv.Value ?? string.Empty);
                }
            }
        }

        private static void ReplaceTextPlaceholdersPreserveStylesEverywhere(
            WordprocessingDocument doc,
            Dictionary<string, string> map)
        {
            // Основной документ
            ReplaceInRootPreserveStyles(doc.MainDocumentPart.Document, map);
            doc.MainDocumentPart.Document.Save();

            // Колонтитулы (если есть)
            foreach (var hp in doc.MainDocumentPart.HeaderParts)
            {
                ReplaceInRootPreserveStyles(hp.Header, map);
                hp.Header.Save();
            }

            foreach (var fp in doc.MainDocumentPart.FooterParts)
            {
                ReplaceInRootPreserveStyles(fp.Footer, map);
                fp.Footer.Save();
            }
        }

        private sealed class TextSpan
        {
            public Text TextNode { get; set; }
            public int Start { get; set; }   // start index in paragraph full string
            public int Length { get; set; }
        }

        private static void ReplaceInRootPreserveStyles(OpenXmlElement root, Dictionary<string, string> map)
        {
            var paragraphs = root.Descendants<Paragraph>().ToList();
            foreach (var p in paragraphs)
                ReplaceInParagraphPreserveStyles(p, map);
        }

        private static void ReplaceInParagraphPreserveStyles(Paragraph p, Dictionary<string, string> map)
        {
            var texts = p.Descendants<Text>().ToList();
            if (texts.Count == 0) return;

            // 1) строим полную строку абзаца + карту "индексы -> Text-ноды"
            var spans = new List<TextSpan>(texts.Count);
            int pos = 0;
            foreach (var t in texts)
            {
                var s = t.Text ?? string.Empty;
                spans.Add(new TextSpan { TextNode = t, Start = pos, Length = s.Length });
                pos += s.Length;
            }

            string full = string.Concat(texts.Select(t => t.Text ?? string.Empty));
            if (full.Length == 0) return;

            // 2) Для каждого токена ищем ВСЕ вхождения и заменяем с конца (чтобы индексы не "плыли")
            foreach (var kv in map)
            {
                var token = kv.Key;
                var replacement = kv.Value ?? string.Empty;

                // find all occurrences
                var occ = new List<int>();
                int idx = 0;
                while (true)
                {
                    idx = full.IndexOf(token, idx, StringComparison.Ordinal);
                    if (idx < 0) break;
                    occ.Add(idx);
                    idx += token.Length;
                }

                if (occ.Count == 0) continue;

                // replace from end to start
                for (int k = occ.Count - 1; k >= 0; k--)
                {
                    int start = occ[k];
                    int end = start + token.Length;

                    // find first and last TextSpan that overlap [start, end)
                    var first = spans.LastOrDefault(s => s.Start <= start && (s.Start + s.Length) >= start);
                    var last = spans.LastOrDefault(s => s.Start < end && (s.Start + s.Length) >= end);

                    if (first == null || last == null)
                        continue; // что-то экзотическое, пропустим

                    int firstLocal = start - first.Start;
                    int lastLocalEnd = end - last.Start;

                    // Берём prefix из first text, suffix из last text
                    var firstText = first.TextNode.Text ?? string.Empty;
                    var lastText = last.TextNode.Text ?? string.Empty;

                    string prefix = firstLocal > 0 ? firstText.Substring(0, firstLocal) : string.Empty;
                    string suffix = lastLocalEnd < lastText.Length ? lastText.Substring(lastLocalEnd) : string.Empty;

                    // Вписываем replacement в первый Text
                    first.TextNode.Text = prefix + replacement + suffix;

                    // Обнуляем все Text между first и last (включая last, если это другой)
                    bool inRange = false;
                    foreach (var s in spans)
                    {
                        if (ReferenceEquals(s, first)) inRange = true;

                        if (inRange)
                        {
                            if (!ReferenceEquals(s, first))
                                s.TextNode.Text = string.Empty;
                        }

                        if (ReferenceEquals(s, last)) break;
                    }

                    // Обновляем full (только чтобы последующие токены искались корректно внутри этого абзаца)
                    // безопасно, т.к. замены идут с конца
                    full = full.Substring(0, start) + replacement + full.Substring(end);
                }
            }
        }
    }
}
