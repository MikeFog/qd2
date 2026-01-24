


CREATE    PROC dbo.StudioPricelists
(
@studioID smallint = null,
@pricelistID smallint = null
)
AS
SET NOCOUNT ON
SELECT 
	StudioPricelist.*,
	'Прайс-лист от ' + Convert(varchar(8), startDate, 4) as name
FROM 
	[StudioPricelist]
WHERE
	studioID = Coalesce(@studioID, studioID)
	AND pricelistID = Coalesce(@pricelistID, pricelistID)
ORDER BY 
	startDate DESC



