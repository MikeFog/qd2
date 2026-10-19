-- Тексты отказов, которых не было в iMessage (docs/tasks/side-defects.md, «Мелочи этапа 6.0»; docs/business-errors.md).
-- Без строки в iMessage десктоп показывает «В программе произошла ошибка.», а веб — сырой ключ.
--
-- 1. TariffUnionErrorAlreadyUsed — TariffIUD (карточка тарифа: «Объединить с блоком», выбранный блок уже продолжение
--    другого тарифа). В iMessage был текст под ключом с опечаткой TariffUnionErrorAlwaysUsed, который никто не поднимает;
--    новая строка — тот же текст (испанский перевод у него уже есть).
-- 2. InvalidDiscountInterval — UserDiscountIUD (период сезонного коэффициента менеджера: конец раньше начала).
-- 3. Испанский для нового текста 2 и для общего текста «В программе произошла ошибка.», который веб теперь показывает,
--    когда ограничение нарушено, а строки в iMessage для него нет (раньше — пустое сообщение).
--
-- Порядок: в любое время, независимо. После наката перезапустить qd2 и веб (iMessage и переводы читаются при старте).
-- Идемпотентен. Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 60_missing-error-messages.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

DECLARE @unionText nvarchar(4000) = ISNULL((SELECT [message] FROM dbo.iMessage WHERE name = 'TariffUnionErrorAlwaysUsed'),
    N'Блок на объединение уже используется в другом блоке. Операция прервана.');
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'TariffUnionErrorAlreadyUsed')
    UPDATE dbo.iMessage SET [message] = @unionText WHERE name = 'TariffUnionErrorAlreadyUsed' AND [message] <> @unionText;
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('TariffUnionErrorAlreadyUsed', @unionText);

DECLARE @intervalText nvarchar(4000) = N'Дата окончания периода не может быть раньше даты начала. Операция прервана.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'InvalidDiscountInterval')
    UPDATE dbo.iMessage SET [message] = @intervalText WHERE name = 'InvalidDiscountInterval' AND [message] <> @intervalText;
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('InvalidDiscountInterval', @intervalText);
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'60: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'В программе произошла ошибка.', N'Se produjo un error en el programa.'),
        (N'Дата окончания периода не может быть раньше даты начала. Операция прервана.', N'La fecha de fin del período no puede ser anterior a la fecha de inicio. Operación cancelada.')
        ) AS src ([source], [text])
       ON dst.lang = 'es' AND dst.context = ''
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES ('es', '', src.[source], src.[text]);
GO

-- Проверка. Таблицу переводов упоминаем только там, где она есть: иначе пакет не скомпилируется (ошибка 208).
DECLARE @ok bit = CASE WHEN EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'TariffUnionErrorAlreadyUsed')
                        AND EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'InvalidDiscountInterval') THEN 1 ELSE 0 END;
IF @ok = 1 AND OBJECT_ID('dbo.iTranslation') IS NOT NULL
    EXEC sp_executesql N'SELECT @ok = CASE WHEN (SELECT COUNT(*) FROM dbo.iTranslation WHERE lang = ''es'' AND context = ''''
            AND sourceHash IN (CONVERT(binary(32), HASHBYTES(''SHA2_256'', N''В программе произошла ошибка.'')),
                               CONVERT(binary(32), HASHBYTES(''SHA2_256'', N''Дата окончания периода не может быть раньше даты начала. Операция прервана.'')))) = 2
        THEN 1 ELSE 0 END', N'@ok bit OUTPUT', @ok OUTPUT;
IF @ok = 1
    PRINT N'ГОТОВО: тексты TariffUnionErrorAlreadyUsed и InvalidDiscountInterval, испанские переводы.';
ELSE
    RAISERROR(N'60: тексты или переводы записаны не полностью.', 16, 1);
GO
