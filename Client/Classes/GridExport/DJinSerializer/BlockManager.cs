using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Merlin.Classes.GridExport.DJinSerializer
{
    public static class BlockManager
    {
        // Метки типов роликов, которые проставляет DJinExportDocument.PrintRoller.
        // Нужны только для сортировки внутри блока; в готовый файл метки новых
        // типов не попадают (см. NormalizeAgitationMarker).
        private const string TypeLocalSmi = "c-type-4";
        private const string TypeFederalSmi = "c-type-5";
        private const string TypeAgitLocalSmi = "c-type-44";
        private const string TypeAgitFederalSmi = "c-type-55";
        private const string TypeAgitAnnounce = "c-type-7";
        private const string TypeAgitation = "c-type-6";

        private class Block
        {
            public List<string> Lines = new List<string>();
        }

        public static void ProcessFile(string filePath)
        {
            var blocks = ParseBlocks(filePath);
            var outputLines = new List<string>();

            foreach (var block in blocks)
            {
                var processed = ProcessBlock(block);
                if (processed == null)
                    continue;

                outputLines.AddRange(processed.Lines);
            }

            File.WriteAllLines(filePath, outputLines, DJinParam.Encoding);
        }

        // ---------------- DataTable -> группы ----------------

        private static List<Block> ParseBlocks(string path)
        {
            var blocks = new List<Block>();
            Block current = null;
            foreach (var line in File.ReadAllLines(path, DJinParam.Encoding))
            {
                if (line.StartsWith("\"B", StringComparison.OrdinalIgnoreCase))
                {
                    current = new Block();
                    current.Lines.Add(line);
                }
                else if (current != null)
                {
                    current.Lines.Add(line);
                    if (line.StartsWith("\"E\"", StringComparison.OrdinalIgnoreCase))
                    {
                        blocks.Add(current);
                        current = null;
                    }
                }
            }
            return blocks;
        }

        private static Block ProcessBlock(Block original)
        {
            var lines = original.Lines;

            if (lines.Count == 0)
                return original;

            // Buscamos B y E
            int btIndex = lines.FindIndex(l => l.StartsWith("\"B", StringComparison.OrdinalIgnoreCase));
            int eIndex = lines.FindIndex(l => l.StartsWith("\"E\"", StringComparison.OrdinalIgnoreCase));

            if (btIndex == -1 || eIndex == -1 || eIndex <= btIndex)
            {
                // Bloque mal formado, lo devolvemos tal cual
                return original;
            }

            var btLine = lines[btIndex];
            var eLine = lines[eIndex];

            // Todas las líneas "intermedias" (entre BT y E, excluyendo BT y E)
            var middle = lines.Skip(btIndex + 1).Take(eIndex - btIndex - 1).ToList();

            // Пустой блок (BT сразу за которым E, без строк между ними) - не включаем в результат
            if (middle.Count == 0)
            {
                return null;
            }

            // Identificar c-type-4, c-type-5 y las demás
            string cType4Line = null;
            string cType5Line = null;
            string agitLocalLine = null;
            string agitFederalLine = null;
            string agitAnnounceLine = null;
            var agitationLines = new List<string>();
            var otherLines = new List<string>();

            foreach (var line in middle)
            {
                string col1 = GetTypeMarker(line);

                if (col1 == TypeLocalSmi)
                    cType4Line = line;
                else if (col1 == TypeFederalSmi)
                    cType5Line = line;
                else if (col1 == TypeAgitLocalSmi)
                    agitLocalLine = line;
                else if (col1 == TypeAgitFederalSmi)
                    agitFederalLine = line;
                else if (col1 == TypeAgitAnnounce)
                    agitAnnounceLine = line;
                else if (col1 == TypeAgitation)
                    agitationLines.Add(line);
                else
                    otherLines.Add(line);
            }

            bool hasAgitation = agitLocalLine != null || agitFederalLine != null
                                || agitAnnounceLine != null || agitationLines.Count > 0;

            // Si el bloque NO tiene c-type-4 ni c-type-5 ni política → dejar todo igual
            if (cType4Line == null && cType5Line == null && !hasAgitation)
            {
                return original;
            }

            // ¿Hay otras líneas que empiecen con "c" (aparte de c-type-4/5)?
            // Ролик агитации - обычный рекламный ролик, поэтому тоже считается;
            // идентификаторы СМИ и анонс - нет, как и ручные 4/5
            bool hasRealRollers = agitationLines.Count > 0 || otherLines.Any(line =>
                !GetTypeMarker(line).StartsWith("j", StringComparison.OrdinalIgnoreCase));

            // Si no hay otras "c" y sí hay c-type-4 o c-type-5,
            // entonces eliminamos todas las líneas con "j"
            if (!hasRealRollers)
            {
                otherLines = otherLines
                    .Where(line => GetTypeMarker(line) != "j")
                    .ToList();
            }

            // Порядок внутри блока:
            // 1) BT (вне этого списка)
            // 2) c-type-4 - ручной идентификатор локального СМИ, если есть
            // 3) джингл влёта
            // 4) обычные ролики в исходном порядке (позиционирование не трогаем)
            // 5) политическая часть: локальное СМИ (44) -> анонс (7) -> ролики
            //    агитации (6) -> федеральное СМИ (55).
            //    44/55 обрамляют только агитацию; их не будет, если блок уже
            //    обрамлён ручными 4/5 (тогда агитация идёт перед закрывающим 5)
            // 6) джингл аута и музыкальная добивка - остаются в конце блока
            // 7) c-type-5 - ручной идентификатор федерального СМИ, если есть
            // 8) E (вне этого списка)

            // Служебные строки (джингл влёта в начале, джингл аута и добивка в
            // конце) остаются на своих местах: политическая часть встаёт между
            // обычными роликами и аутом, а не после него.
            // Хвост - всё, что идёт после последнего обычного ролика.
            int tailStart = otherLines.Count;
            while (tailStart > 0 && IsServiceLine(otherLines[tailStart - 1]))
                tailStart--;

            if (tailStart == 0)
            {
                // Обычных роликов в блоке нет (окно целиком под агитацию). Голова -
                // ровно те служебные строки, что стояли в начале блока. Считаем их
                // по исходному порядку, а не считаем первую строку джинглом влёта:
                // у блока может не быть влёта (тариф с needInJingle = 0), и тогда
                // единственная служебная строка - это аут, его место в конце
                int headCount = 0;
                while (headCount < middle.Count && IsServiceLine(middle[headCount]))
                    headCount++;

                tailStart = Math.Min(headCount, otherLines.Count);
            }

            var newMiddle = new List<string>();

            if (cType4Line != null)
                newMiddle.Add(cType4Line);

            // Mantener orden original en las "otras"
            for (int i = 0; i < tailStart; i++)
            {
                newMiddle.Add(otherLines[i]);
            }

            if (agitLocalLine != null)
                newMiddle.Add(agitLocalLine);

            if (agitAnnounceLine != null)
                newMiddle.Add(agitAnnounceLine);

            foreach (var line in agitationLines)
            {
                newMiddle.Add(line);
            }

            if (agitFederalLine != null)
                newMiddle.Add(agitFederalLine);

            for (int i = tailStart; i < otherLines.Count; i++)
            {
                newMiddle.Add(otherLines[i]);
            }

            if (cType5Line != null)
                newMiddle.Add(cType5Line);

            // Reconstruimos el bloque
            var result = new Block();
            result.Lines.Add(btLine);
            result.Lines.AddRange(newMiddle.Select(NormalizeAgitationMarker));
            result.Lines.Add(eLine);

            return result;
        }

        private static string GetTypeMarker(string line)
        {
            var cols = line.Split(',');
            return cols.Length > 1 ? cols[1].Trim('"') : "";
        }

        /// <summary>
        /// Джингл (влёт/аут) или музыкальная добивка - строки, обрамляющие блок.
        /// Рекламные ролики между ними, поэтому политическая часть не должна
        /// оказаться за аутом.
        /// </summary>
        private static bool IsServiceLine(string line)
        {
            string marker = GetTypeMarker(line);
            return marker.StartsWith(DJinParam.strJingle, StringComparison.OrdinalIgnoreCase)
                   || marker == DJinParam.strEtc;
        }

        /// <summary>
        /// Убирает служебные метки политической обвязки: они нужны только для
        /// сортировки выше. В файл все они пишутся как обычные рекламные ролики -
        /// метки 4/5 не ставим специально, чтобы DJin не принял авто-обвязку за
        /// ручные идентификаторы СМИ.
        /// </summary>
        private static string NormalizeAgitationMarker(string line)
        {
            string marker = GetTypeMarker(line);

            if (marker == TypeAgitLocalSmi || marker == TypeAgitFederalSmi
                || marker == TypeAgitAnnounce || marker == TypeAgitation)
                return ReplaceTypeMarker(line, DJinParam.strRoller);

            return line;
        }

        private static string ReplaceTypeMarker(string line, string newMarker)
        {
            var cols = line.Split(',');
            if (cols.Length < 2)
                return line;

            cols[1] = string.Format("\"{0}\"", newMarker);
            return string.Join(",", cols);
        }
    }
}