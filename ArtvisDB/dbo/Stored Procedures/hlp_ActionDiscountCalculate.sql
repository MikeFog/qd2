CREATE PROC [dbo].[hlp_ActionDiscountCalculate]
(
@actionID int,
@startDate datetime,
@discountValue float output
)
AS
SET NOCOUNT on

DECLARE @avgDuration float, @campaignsCount tinyint, @priceByCampaigns money

Select @priceByCampaigns = Sum([price]) From Campaign where actionID = @actionID

SELECT @avgDuration=AVG(CAST(c.issuesDuration AS float)), @campaignsCount=COUNT(DISTINCT c.massmediaID)
FROM Campaign c
WHERE c.actionID=@actionID and c.campaignTypeID < 4

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
		GROUP BY 
			m.packageDiscountPriceListID
		HAVING 
			count(DISTINCT m.massmediaID)=@campaignsCount
		) t
	JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID=t.packageDiscountPriceListID
	JOIN PackageDiscount d ON d.packageDiscountId=pl.packageDiscountID
WHERE 
	d.count = t.campaignsCount
	AND @startDate BETWEEN pl.startDate AND pl.finishDate
	and pl.value <= @priceByCampaigns
