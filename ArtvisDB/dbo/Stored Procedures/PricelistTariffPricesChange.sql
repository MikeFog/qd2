-- «Сменить цену» прайс-листа: замена цен тарифов по списку «старая:новая».
-- @prices - пары через запятую, десятичный разделитель - точка: '1500.00:1650.00,2000:2200'.
-- Замены применяются одновременно, не цепочкой: при 100:200 и 200:300 тариф за 100 станет 200.
-- Тарифы с сгенерированными окнами не меняются (TariffIUD тоже не даёт - TariffInUse): цену
-- окон берут из самих окон, тариф с новой ценой разошёлся бы с ними.
-- Результаты: 1) changedCount - сколько тарифов изменено; 2) пропущенные тарифы (с окнами).
CREATE PROCEDURE [dbo].[PricelistTariffPricesChange]
(
@pricelistID smallint,
@prices varchar(max)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @map TABLE (oldPrice decimal(18,2) PRIMARY KEY, newPrice decimal(18,2) NOT NULL)

INSERT INTO @map (oldPrice, newPrice)
SELECT
	CONVERT(decimal(18,2), LEFT(s.value, CHARINDEX(':', s.value) - 1)),
	CONVERT(decimal(18,2), SUBSTRING(s.value, CHARINDEX(':', s.value) + 1, 50))
FROM STRING_SPLIT(@prices, ',') s
WHERE s.value <> ''

DELETE FROM @map WHERE oldPrice = newPrice

UPDATE t
SET price = m.newPrice
FROM Tariff t
	INNER JOIN @map m ON m.oldPrice = t.price
WHERE t.pricelistID = @pricelistID
	AND NOT EXISTS (SELECT 1 FROM TariffWindow tw WHERE tw.tariffId = t.tariffID)

SELECT @@ROWCOUNT AS changedCount

SELECT t.tariffID, t.[time], t.price
FROM Tariff t
	INNER JOIN @map m ON m.oldPrice = t.price
WHERE t.pricelistID = @pricelistID
	AND EXISTS (SELECT 1 FROM TariffWindow tw WHERE tw.tariffId = t.tariffID)
ORDER BY t.[time], t.tariffID
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[PricelistTariffPricesChange] TO PUBLIC
    AS [dbo];
