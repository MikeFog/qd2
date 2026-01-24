






CREATE      PROC [dbo].[PackModulePricelists]
(
@packModuleID smallint = null,
@pricelistID smallint = null
)
as

SET NOCOUNT ON
SELECT DISTINCT
	pl.[pricelistID], 
	pl.[packModuleID],
	pl.[startDate],
	'Прайс-лист от ' + CONVERT(varchar(10), pl.[startDate], 104) + ' до ' + CONVERT(varchar(10), pl.finishDate, 104) as name,
	pl.[finishDate],
	pl.[price],
	pl.[extraChargeFirstRoller],
	pl.[extraChargeSecondRoller],
	pl.[extraChargeLastRoller],
	pl.rollerID,
	mm.[roltypeID]
FROM 
	[PackModulePriceList] pl
	left JOIN [PackModuleContent] pmc ON pl.[priceListID] = pmc.[pricelistID]
	left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
	left JOIN [MassMedia] mm ON m.[massmediaID] = mm.[massmediaID]
WHERE
	pl.packModuleID = Coalesce(@packModuleID, pl.packModuleID) And
	pl.[pricelistID] = Coalesce(@pricelistID, pl.[pricelistID])
ORDER BY 
	pl.[startDate] DESC








