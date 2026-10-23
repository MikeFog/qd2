-- Веб, вкладка «Рекламные окна» страницы акции (23.10.2026, решение В-15, docs/tasks/web-action-forms.md): кнопка «Справка» —
-- файл заказчика Client/Help/veer.html как есть, в новой вкладке браузера. Испанские переводы подписи и подсказки кнопки
-- (2 строки, дословно из десктопа — CampaignForm.tbbHelp); сама справка только на русском (файл ведёт заказчик). Только строки
-- интерфейса: процедуры, таблицы и метаданные не меняются.
--
-- Без этого скрипта у пользователей с испанским языком всё работает, но новые надписи показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 73_web-veer-help-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'73: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'Справка', N'Ayuda'),
        (N'Открыть справку по этому экрану', N'Abrir la ayuda de esta pantalla')
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
        (N'Справка'),
        (N'Открыть справку по этому экрану')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы кнопки «Справка» (es) на месте.';
    ELSE
        RAISERROR(N'73: часть переводов кнопки «Справка» не записана.', 16, 1);
END
GO
