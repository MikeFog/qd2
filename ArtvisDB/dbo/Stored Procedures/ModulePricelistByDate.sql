







CREATE    PROC [dbo].[ModulePricelistByDate]
(
@massmediaID smallint,
@theDate datetime,
@moduleID SMALLINT = NULL
)
AS
SET NOCOUNT ON
SELECT 
	mpl.[modulePriceListID], 
	mpl.[priceListID],
	mpl.[price],
	mpl.[moduleID],
	mpl.startDate,
	mpl.[finishDate],
	'Прайс-лист от ' + CONVERT(varchar(10), mpl.startDate, 104) as name
FROM 
	[ModulePricelist] mpl 
	INNER JOIN [Pricelist] pl ON mpl.[priceListID] = pl.[pricelistID]
WHERE
	mpl.moduleID = ISNULL(@moduleID, mpl.moduleID) AND
	@theDate BETWEEN mpl.[startDate] AND mpl.[finishDate] AND
	pl.[massmediaID] = @massmediaID












