using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace Merlin.Classes.GridExport.DJinSerializer
{
    public static class BlockManager
    {
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
            int btIndex = lines.FindIndex(l => l.StartsWith("\"B\"", StringComparison.OrdinalIgnoreCase));
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

            // Identificar c-type-4, c-type-5 y las demás
            string cType4Line = null;
            string cType5Line = null;
            var otherLines = new List<string>();

            foreach (var line in middle)
            {
                var cols = line.Split(',');
                string col1 = cols.Length > 1 ? cols[1].Trim('"') : "";

                if (col1 == "c-type-4")
                {
                    cType4Line = line;
                }
                else if (col1 == "c-type-5")
                {
                    cType5Line = line;
                }
                else
                {
                    otherLines.Add(line);
                }
            }

            // Si el bloque NO tiene c-type-4 ni c-type-5 → dejar todo igual
            if (cType4Line == null && cType5Line == null)
            {
                return original;
            }

            // ¿Hay otras líneas que empiecen con "c" (aparte de c-type-4/5)?
            bool hasRealRollers = otherLines.Any(line =>
            {
                var cols = line.Split(',');
                string col1 = cols.Length > 1 ? cols[1].Trim('"') : "";
                return !col1.StartsWith("j", StringComparison.OrdinalIgnoreCase);
            });

            // Si no hay otras "c" y sí hay c-type-4 o c-type-5,
            // entonces eliminamos todas las líneas con "j"
            if (!hasRealRollers && (cType4Line != null || cType5Line != null))
            {
                otherLines = otherLines
                    .Where(line =>
                    {
                        var cols = line.Split(',');
                        string col1 = cols.Length > 1 ? cols[1].Trim('"') : "";
                        return col1 != "j";
                    })
                    .ToList();
            }

            // Construimos el nuevo "middle" en el orden correcto:
            // 1) BT (fuera de aquí)
            // 2) c-type-4 (si existe)
            // 3) otras líneas (en el orden original, filtradas)
            // 4) c-type-5 (si existe)
            // 5) E (fuera de aquí)

            var newMiddle = new List<string>();

            if (cType4Line != null)
                newMiddle.Add(cType4Line);

            // Mantener orden original en las "otras"
            foreach (var line in otherLines)
            {
                newMiddle.Add(line);
            }

            if (cType5Line != null)
                newMiddle.Add(cType5Line);

            // Reconstruimos el bloque
            var result = new Block();
            result.Lines.Add(btLine);
            result.Lines.AddRange(newMiddle);
            result.Lines.Add(eLine);

            return result;
        }
    }
}