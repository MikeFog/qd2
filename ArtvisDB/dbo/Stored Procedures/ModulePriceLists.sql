

CREATE        PROC [dbo].[ModulePriceLists]
(
@moduleID smallint = NULL,
@modulePriceListID smallint = NULL
)

AS
SET NOCOUNT ON
SELECT 
	mpl.*, 
	pl.broadcastStart,
	'Модуль ' + CONVERT(varchar(10), mpl.startDate, 104) + ' - ' + CONVERT(varchar(10), mpl.finishDate, 104) + ' (прайс от ' + CONVERT(varchar(10), pl.startDate, 104) + ')' as NAME,
	mm.[roltypeID]
FROM 
	[ModulePriceList] mpl
	INNER JOIN PriceList pl ON pl.priceListID = mpl.priceListID
	INNER JOIN [MassMedia] mm ON pl.[massmediaID] = mm.[massmediaID]
WHERE
	mpl.moduleID = Coalesce(@moduleID, mpl.moduleID) And
	mpl.modulePriceListID = Coalesce(@modulePriceListID, mpl.modulePriceListID)
ORDER BY
	mpl.startDate ASC











