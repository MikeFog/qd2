-- «Сменить цену» прайс-листа: все разные цены тарифов прайс-листа.
-- tariffsCount - тарифов с этой ценой, withWindowsCount - из них с сгенерированными окнами
-- (их цену PricelistTariffPricesChange не меняет).
CREATE PROCEDURE [dbo].[PricelistTariffPrices]
(
@pricelistID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SELECT
	t.price,
	COUNT(*) AS tariffsCount,
	SUM(w.hasWindows) AS withWindowsCount
FROM Tariff t
	CROSS APPLY (SELECT CASE WHEN EXISTS (SELECT 1 FROM TariffWindow tw WHERE tw.tariffId = t.tariffID) THEN 1 ELSE 0 END AS hasWindows) w
WHERE t.pricelistID = @pricelistID
GROUP BY t.price
ORDER BY t.price
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[PricelistTariffPrices] TO PUBLIC
    AS [dbo];
