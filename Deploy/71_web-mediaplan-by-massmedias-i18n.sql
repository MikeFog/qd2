-- Веб, страница акции (23.10.2026): пункт «Распечатать график размещения рекламной акции» в меню «⋯» шапки —
-- медиаплан раскладкой «по станциям» одной акции за весь срок (решение 22.10.2026, docs/tasks/web-mediaplan.md §2 п. 2;
-- в десктопе — галочка ActionForm при OK). Испанский перевод подписи пункта (1 строка); окно настроек печати, имя файла
-- и лист медиаплана переведены раньше. Только строки интерфейса: процедуры, таблицы и метаданные не меняются.
--
-- Без этого скрипта у пользователей с испанским языком всё работает, но новые надписи показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 71_web-mediaplan-by-massmedias-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'71: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'Распечатать график размещения рекламной акции', N'Imprimir plan de colocación de la campaña publicitaria')
        ) AS src ([source], [text])
       ON dst.lang = 'es' AND dst.context = ''
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES ('es', '', src.[source], src.[text]);
GO

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
BEGIN
    DECLARE @missing int = (SELECT COUNT(*) FROM (VALUES
        (N'Распечатать график размещения рекламной акции')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы пункта «Распечатать график размещения рекламной акции» (es) на месте.';
    ELSE
        RAISERROR(N'71: часть переводов пункта «Распечатать график размещения рекламной акции» не записана.', 16, 1);
END
GO
