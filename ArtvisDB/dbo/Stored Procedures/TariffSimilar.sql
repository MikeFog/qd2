-- «Похожие тарифы» для массового редактирования: тарифы того же прайс-листа с той же минутой выхода
-- и полностью совпадающими остальными атрибутами (включая набор дней недели) - то есть отличающиеся
-- от исходного тарифа только часом. Исходный тариф входит в результат.
-- hasWindows - у тарифа есть сгенерированные окна (TariffIUD не даст его править),
-- inUnion - тариф входит в цепочку объединения (TariffUnion) в любой роли.
CREATE PROCEDURE [dbo].[TariffSimilar]
(
@tariffID int
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SELECT
	t.tariffID,
	DATEPART(hour, t.[time]) AS [hour],
	CAST(CASE WHEN EXISTS (SELECT 1 FROM TariffWindow w WHERE w.tariffID = t.tariffID) THEN 1 ELSE 0 END AS bit) AS hasWindows,
	CAST(CASE WHEN EXISTS (SELECT 1 FROM TariffUnion u WHERE u.tariffID = t.tariffID OR u.tariffUnionID = t.tariffID) THEN 1 ELSE 0 END AS bit) AS inUnion
FROM Tariff s
	INNER JOIN Tariff t ON t.pricelistID = s.pricelistID
		AND DATEPART(minute, t.[time]) = DATEPART(minute, s.[time])
		AND t.price = s.price
		AND t.duration = s.duration
		AND t.duration_total = s.duration_total
		AND t.maxCapacity = s.maxCapacity
		AND t.isForModuleOnly = s.isForModuleOnly
		AND t.needExt = s.needExt
		AND t.needInJingle = s.needInJingle
		AND t.needOutJingle = s.needOutJingle
		AND ISNULL(t.blockTypeID, 0) = ISNULL(s.blockTypeID, 0)
		AND t.notEarly = s.notEarly
		AND t.notLater = s.notLater
		AND t.openBlock = s.openBlock
		AND t.openPhonogram = s.openPhonogram
		AND t.monday = s.monday AND t.tuesday = s.tuesday AND t.wednesday = s.wednesday
		AND t.thursday = s.thursday AND t.friday = s.friday AND t.saturday = s.saturday AND t.sunday = s.sunday
		AND ISNULL(t.comment, '') = ISNULL(s.comment, '')
		AND ISNULL(t.suffix, '') = ISNULL(s.suffix, '')
WHERE s.tariffID = @tariffID
ORDER BY t.[time]
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[TariffSimilar] TO PUBLIC
    AS [dbo];
