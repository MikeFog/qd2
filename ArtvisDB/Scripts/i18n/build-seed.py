"""
Собирает скрипт загрузки переводов в iTranslation из TSV. docs/tasks/web-i18n.md, этап 5.

    python ArtvisDB/Scripts/i18n/build-seed.py es

Вход:  ArtvisDB/Scripts/i18n/<lang>.tsv — колонки: where, source, text (табы и переводы
       строк внутри значений записаны как \\t и \\n). Файл удобно вычитывать в Excel.
Выход: ArtvisDB/Scripts/web-i18n-<lang>-seed.sql — идемпотентный скрипт: вставляет
       новые переводы и ОБНОВЛЯЕТ изменившиеся (источник правды — TSV в репозитории).
"""
import io
import os
import re
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
BATCH = 500


def unescape(s):
    out, i = [], 0
    while i < len(s):
        if s[i] == '\\' and i + 1 < len(s):
            out.append({'n': '\n', 'r': '\r', 't': '\t', '\\': '\\'}.get(s[i + 1], '\\' + s[i + 1]))
            i += 2
        else:
            out.append(s[i])
            i += 1
    return ''.join(out)


def sql_literal(s):
    # Переводы строк — через NCHAR(13)/NCHAR(10): файл скрипта в CRLF, и символы
    # внутри литерала исказились бы. Ключ перевода должен совпасть байт в байт.
    parts = re.split(r'(\r|\n)', s.replace("'", "''"))
    return ' + '.join({'\r': 'NCHAR(13)', '\n': 'NCHAR(10)'}.get(p, "N'" + p + "'")
                      for p in parts if p != '')


def main(lang):
    rows = []
    with io.open(os.path.join(HERE, lang + '.tsv'), encoding='utf-8-sig') as f:
        header = f.readline()
        for no, line in enumerate(f, 2):
            line = line.rstrip('\r\n')
            if not line:
                continue
            cols = line.split('\t')
            if len(cols) != 3:
                sys.exit(f'{lang}.tsv:{no}: ожидается 3 колонки, а их {len(cols)}')
            source, text = unescape(cols[1]), unescape(cols[2])
            if text:
                rows.append((source, text))

    out = io.StringIO()
    out.write(f"""/*
    ДЕПЛОЙ: переводы веб-версии на язык «{lang}» ({len(rows)} строк). docs/tasks/web-i18n.md, этап 5.
    СГЕНЕРИРОВАН из ArtvisDB/Scripts/i18n/{lang}.tsv скриптом build-seed.py — руками не править.

    ПРЕДУСЛОВИЕ     накачен web-i18n-translation-deploy.sql (таблица iTranslation).
    ИДЕМПОТЕНТНОСТЬ повторный запуск безопасен: новые строки вставляются, изменённые — обновляются.
    КЛИЕНТ          веб перезапустить (переводы кэшируются в памяти). Десктоп не затронут.
*/

SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE TABLE #t ([source] NVARCHAR(4000) NOT NULL, [text] NVARCHAR(4000) NOT NULL);
GO
""")
    for i in range(0, len(rows), BATCH):
        out.write('INSERT INTO #t ([source], [text]) VALUES\n')
        out.write(',\n'.join(f'({sql_literal(s)}, {sql_literal(t)})' for s, t in rows[i:i + BATCH]))
        out.write(';\nGO\n')
    out.write(f"""
BEGIN TRANSACTION;

MERGE [dbo].[iTranslation] AS dst
USING (SELECT '{lang}' AS [lang], '' AS [context], [source], [text] FROM #t) AS src
   ON dst.[lang] = src.[lang] AND dst.[context] = src.[context]
  AND dst.[sourceHash] = CONVERT(binary(32), hashbytes('SHA2_256', src.[source]))
WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
    UPDATE SET [text] = src.[text]
WHEN NOT MATCHED BY TARGET THEN
    INSERT ([lang], [context], [source], [text]) VALUES (src.[lang], src.[context], src.[source], src.[text]);

PRINT CONCAT('Вставлено или обновлено переводов: ', @@ROWCOUNT);

COMMIT TRANSACTION;
GO

DROP TABLE #t;
GO
""")
    path = os.path.join(HERE, '..', f'web-i18n-{lang}-seed.sql')
    with io.open(path, 'w', encoding='utf-8-sig', newline='\r\n') as f:
        f.write(out.getvalue())
    print(f'{len(rows)} строк -> {os.path.normpath(path)}')


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else 'es')
