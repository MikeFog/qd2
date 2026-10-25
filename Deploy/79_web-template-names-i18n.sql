-- Веб, вкладка «Рекламные окна», диалог «Шаблон для внесения роликов» (24.10.2026): три режима вместо «Шаблон №1/№2/№3 для внесения
-- роликов» названы «В конкретное окно», «На интервале времени», «С указанием количества» (решение Миши). Испанские переводы
-- трёх новых названий (3 строки); заголовок диалога «Шаблон для внесения роликов» уже переведён. Там же — кнопка «Скачать все ({0})»
-- в окне просмотра документов (счёт, договор, эфирная справка), когда документов несколько, и «Проверить на акции» вместо «Примерить»
-- на экране «Шаблоны документов» (4 строки + отказ эфирной справки). Только строки интерфейса: процедуры,
-- таблицы и метаданные не меняются.
--
-- Без этого скрипта у пользователей с испанским языком всё работает, но новые надписи показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 79_web-template-names-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'79: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'В конкретное окно', N'En una ventana concreta'),
        (N'На интервале времени', N'En un intervalo de tiempo'),
        (N'С указанием количества', N'Indicando la cantidad'),
        (N'Скачать все ({0})', N'Descargar todo ({0})'),
        (N'Проверить на акции №', N'Comprobar en la campaña n.º'),
        (N'Проверить на акции…', N'Comprobar en una campaña…'),
        (N'Проверить на акции', N'Comprobar en una campaña'),
        (N'{0}: проверка на акции №{1}', N'{0}: comprobación en la campaña n.º {1}'),
        (N'У акции нет кампаний на одной радиостанции — эфирную справку проверить не на чем.', N'La campaña no tiene pautas en una sola emisora: no hay con qué comprobar el certificado de emisión.')
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
        (N'В конкретное окно'),
        (N'На интервале времени'),
        (N'С указанием количества'),
        (N'Скачать все ({0})'),
        (N'Проверить на акции №'),
        (N'Проверить на акции…'),
        (N'Проверить на акции'),
        (N'{0}: проверка на акции №{1}'),
        (N'У акции нет кампаний на одной радиостанции — эфирную справку проверить не на чем.')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы названий шаблонов (es) на месте.';
    ELSE
        RAISERROR(N'79: часть переводов названий шаблонов не записана.', 16, 1);
END
GO
