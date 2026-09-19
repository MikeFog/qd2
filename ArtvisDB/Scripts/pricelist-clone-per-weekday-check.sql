/*
    ПРОВЕРКА (без побочных эффектов): клон прайс-листа по дням недели.
    Клонирует прайс в BEGIN TRAN ... ROLLBACK на далёкие даты (2090 год) и показывает,
    чем клон отличается от исходного прайса. Ничего не сохраняется, кроме «сгоревших»
    значений IDENTITY.

    Задать @old (ID прайс-листа) вручную или оставить — возьмётся прайс, по которому
    сгенерировано самое свежее окно.

    ЧТО СМОТРЕТЬ
      - tariffs: клонов не меньше, чем тарифов (больше — значит, дни недели различаются).
      - unions: связей-продолжений не меньше, чем было.
      - «Отличия клона от исходного»: строки, которых нет в исходном прайсе, —
        это как раз разделившиеся / изменившиеся тарифы (проверить глазами: время, цена,
        набор дней).
*/
SET NOCOUNT ON;
DECLARE @old smallint = NULL;   -- <— ID прайс-листа
IF @old IS NULL
    SELECT TOP 1 @old = t.pricelistID FROM TariffWindow w JOIN Tariff t ON t.tariffID = w.tariffId ORDER BY w.windowId DESC;

DECLARE @mm smallint, @new smallint;
SELECT @mm = massmediaID FROM Pricelist WHERE pricelistID = @old;
PRINT 'Исходный прайс-лист: ' + CAST(@old AS varchar(10)) + ', станция ' + CAST(@mm AS varchar(10));

BEGIN TRAN;
    SET @new = @old;
    EXEC PricelistIUD @pricelistID = @new OUTPUT, @massmediaID = @mm, @startDate = '01.01.2090', @finishDate = '31.01.2090',
        @broadcastStart = '01.01.1900', @extraChargeFirstRoller = 0, @extraChargeSecondRoller = 0, @extraChargeLastRoller = 0,
        @actionName = 'Clone';

    SELECT 'tariffs' AS what,
        (SELECT COUNT(*) FROM Tariff WHERE pricelistID = @old) AS [old],
        (SELECT COUNT(*) FROM Tariff WHERE pricelistID = @new) AS [new];
    SELECT 'unions' AS what,
        (SELECT COUNT(*) FROM TariffUnion u JOIN Tariff t ON t.tariffID = u.tariffID WHERE t.pricelistID = @old) AS [old],
        (SELECT COUNT(*) FROM TariffUnion u JOIN Tariff t ON t.tariffID = u.tariffID WHERE t.pricelistID = @new) AS [new];

    SELECT 'Отличия клона от исходного' AS what, x.[time], x.price, x.duration, x.duration_total,
        x.monday, x.tuesday, x.wednesday, x.thursday, x.friday, x.saturday, x.sunday
    FROM (
        SELECT [time], price, duration, duration_total, monday, tuesday, wednesday, thursday, friday, saturday, sunday FROM Tariff WHERE pricelistID = @new
        EXCEPT
        SELECT [time], price, duration, duration_total, monday, tuesday, wednesday, thursday, friday, saturday, sunday FROM Tariff WHERE pricelistID = @old
    ) x
    ORDER BY x.[time];
ROLLBACK;

SELECT 'после ROLLBACK: тарифов в исходном прайсе' AS what, COUNT(*) AS cnt FROM Tariff WHERE pricelistID = @old;
