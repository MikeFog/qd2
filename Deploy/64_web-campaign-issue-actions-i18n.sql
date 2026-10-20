-- Веб, действия над выпусками в дереве кампании (docs/tasks/web-menu-inventory.md §11, 20.10.2026): «Изменить
-- позиционирование» у кампании, ролика, модуля и роликов спонсорской — испанские переводы новых строк итога (3).
-- «Удалить рекламные выпуски», «Удалить» у узлов дерева и «Изменить предмет рекламы» новых строк не дали. Только строки
-- интерфейса: процедуры, таблицы и метаданные не меняются.
--
-- Без этого скрипта пункт у пользователей с испанским языком работает, но эти три строки показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 64_web-campaign-issue-actions-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'64: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'Изменено выпусков: {0}, не изменено: {1}', N'Emisiones modificadas: {0}, no modificadas: {1}'),
        (N'У окна выхода нет тарифа: позиция не меняется, иначе цена выпуска станет нулевой.', N'La ventana de emisión no tiene tarifa: la posición no se cambia, porque el precio de la emisión quedaría en cero.'),
        (N'Изменение позиционирования прервано: {0} Выпуски этого дня не изменены, выпуски прежних дней изменены. Не обработано дней: {1}.', N'Cambio de posicionamiento interrumpido: {0} Las emisiones de este día no se modificaron; las de los días anteriores sí. Días sin procesar: {1}.')
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
        (N'Изменено выпусков: {0}, не изменено: {1}'),
        (N'У окна выхода нет тарифа: позиция не меняется, иначе цена выпуска станет нулевой.'),
        (N'Изменение позиционирования прервано: {0} Выпуски этого дня не изменены, выпуски прежних дней изменены. Не обработано дней: {1}.')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы действий над выпусками в дереве кампании (es) на месте.';
    ELSE
        RAISERROR(N'64: часть переводов действий над выпусками не записана.', 16, 1);
END
GO
