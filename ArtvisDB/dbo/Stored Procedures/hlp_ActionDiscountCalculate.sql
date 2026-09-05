CREATE PROC [dbo].[hlp_ActionDiscountCalculate]
(
@actionID int,
@startDate datetime,
@discountValue decimal(9,4) output
)
AS
SET NOCOUNT on

DECLARE @avgDuration float, @campaignsCount tinyint, @priceByCampaigns decimal(18,2)

Select @priceByCampaigns = Sum([price]) From Campaign where actionID = @actionID

-- ИСПРАВЛЕНО: COUNT(*) вместо COUNT(DISTINCT c.massmediaID)
-- Это гарантирует, что каждая кампания (включая тип 3) учитывается отдельно
-- ИСПРАВЛЕНО: пустые кампании (без единого размещения) в расчёте пакета не участвуют -
-- иначе они тянут вниз avgDuration и завышают @campaignsCount, срывая пакетную скидку.
-- Предикат тот же, что ActionRecalculate использует для oldTotalCount.
SELECT @avgDuration=AVG(CAST(c.issuesDuration AS float)), @campaignsCount=COUNT(*)
FROM Campaign c
WHERE c.actionID=@actionID and c.campaignTypeID < 4
	and (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0

SELECT @avgDuration=COALESCE(@avgDuration,0), @campaignsCount=COALESCE(@campaignsCount,0)

SELECT @discountValue=COALESCE(MIN(pl.discount),1) FROM (
		SELECT m.packageDiscountPriceListID, count(DISTINCT c.massmediaID) AS campaignsCount
		FROM Campaign c
			JOIN (
				PackageDiscountMassmedia m 
					JOIN PackageDiscountPriceList p ON p.packageDiscountPriceListID=m.packageDiscountPriceListID
				) ON c.massmediaID = m.massmediaID
														AND (
																(c.campaignTypeID=1 AND m.isForType1=1)
																OR (c.campaignTypeID=2 AND m.isForType2=1)
																OR (c.campaignTypeID=3 AND m.isForType3=1)
																)
														AND CAST(c.issuesDuration as float) >= @avgDuration*p.eachVolume/100
		WHERE
			c.actionID=@actionID
			-- пустые кампании исключаем и здесь, чтобы HAVING сравнивал только реальные
			and (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0
		GROUP BY
			m.packageDiscountPriceListID
		-- ИСПРАВЛЕНО: count(c.massmediaID) вместо count(DISTINCT m.massmediaID)
		-- Считаем количество сопоставлённых кампаний, а не уникальных massmedia
		HAVING 
			count(c.massmediaID)=@campaignsCount
		) t
	JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID=t.packageDiscountPriceListID
	JOIN PackageDiscount d ON d.packageDiscountId=pl.packageDiscountID
WHERE 
	d.count = t.campaignsCount
	AND @startDate BETWEEN pl.startDate AND pl.finishDate
	and pl.value <= @priceByCampaigns