-- Клон рекламной акции одной датой (29.10.2026, решение заказчика): вместо даты у каждой кампании — одно поле «Начало новой
-- акции», все кампании сдвигаются на одно число дней (самая ранняя из отмеченных начинается в этот день). Десктоп и веб; в вебе
-- «Клонировать» у акции сделан впервые (партия 4 журнала акций). Испанские переводы надписей веб-окна клонирования и двух отказов
-- проверки агентства (Д-46), которые раньше показывал только десктоп (11 строк). Только строки интерфейса: процедуры,
-- таблицы и метаданные не меняются.
--
-- Без этого скрипта у пользователей с испанским языком всё работает, но новые надписи показываются по-русски
-- (ключ перевода — русский текст). Идемпотентен: повторный запуск ничего не меняет.
-- После наката перезапустить веб (переводы читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 85_action-clone-one-date-i18n.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.iTranslation') IS NULL
    PRINT N'85: таблицы iTranslation нет (многоязычность — Deploy/08), переводы не нужны.';
ELSE
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'Клонировать рекламную акцию № {0}', N'Clonar la campaña publicitaria n.º {0}'),
        (N'Начало новой акции', N'Inicio de la nueva campaña'),
        (N'Все кампании сдвигаются на одно число дней: самая ранняя из отмеченных начнётся в этот день.', N'Todas las pautas se desplazan el mismo número de días: la más temprana de las marcadas empezará ese día.'),
        (N'Было: {0} – {1}, станет: {2} – {3}', N'Antes: {0} – {1}, después: {2} – {3}'),
        (N'Агентство в новой акции:', N'Agencia en la nueva campaña:'),
        (N'У станции нет действующего агентства: клонировать нельзя', N'La emisora no tiene ninguna agencia vigente: no se puede clonar'),
        (N'{0} (прежнего «{1}» у станции уже нет)', N'{0} (la emisora ya no tiene la anterior, «{1}»)'),
        (N'Укажите начало новой акции.', N'Indique el inicio de la nueva campaña.'),
        (N'У радиостанции кампании «{0}» нет действующего агентства: снимите с неё галочку.', N'La emisora de la pauta «{0}» no tiene ninguna agencia vigente: desmárquela.'),
        (N'Для кампании «{0}» выберите агентство: прежнего агентства у радиостанции уже нет.', N'Elija una agencia para la pauta «{0}»: la emisora ya no tiene la agencia anterior.'),
        (N'После замены агентства кампании «{0}» и «{1}» совпали бы по радиостанции, типу и типу оплаты. Оставьте в клоне только одну из них.', N'Tras cambiar la agencia, las pautas «{0}» y «{1}» coincidirían en emisora, tipo y tipo de pago. Deje solo una de ellas en el clon.')
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
        (N'Клонировать рекламную акцию № {0}'),
        (N'Начало новой акции'),
        (N'Все кампании сдвигаются на одно число дней: самая ранняя из отмеченных начнётся в этот день.'),
        (N'Было: {0} – {1}, станет: {2} – {3}'),
        (N'Агентство в новой акции:'),
        (N'У станции нет действующего агентства: клонировать нельзя'),
        (N'{0} (прежнего «{1}» у станции уже нет)'),
        (N'Укажите начало новой акции.'),
        (N'У радиостанции кампании «{0}» нет действующего агентства: снимите с неё галочку.'),
        (N'Для кампании «{0}» выберите агентство: прежнего агентства у радиостанции уже нет.'),
        (N'После замены агентства кампании «{0}» и «{1}» совпали бы по радиостанции, типу и типу оплаты. Оставьте в клоне только одну из них.')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0
        PRINT N'ГОТОВО: переводы окна клонирования акции (es) на месте.';
    ELSE
        RAISERROR(N'85: часть переводов окна клонирования акции не записана.', 16, 1);
END
GO
