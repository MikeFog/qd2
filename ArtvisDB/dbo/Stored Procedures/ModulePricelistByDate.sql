







CREATE    PROC [dbo].[ModulePricelistByDate]
(
@massmediaID smallint,
@theDate datetime,
@moduleID SMALLINT = NULL,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
SELECT 
	mpl.[modulePriceListID], 
	mpl.[priceListID],
	mpl.[price],
	mpl.[moduleID],
	mpl.startDate,
	mpl.[finishDate],
	@tPricelistFrom + CONVERT(varchar(10), mpl.startDate, 104) as name
FROM 
	[ModulePricelist] mpl 
	INNER JOIN [Pricelist] pl ON mpl.[priceListID] = pl.[pricelistID]
WHERE
	mpl.moduleID = ISNULL(@moduleID, mpl.moduleID) AND
	@theDate BETWEEN mpl.[startDate] AND mpl.[finishDate] AND
	pl.[massmediaID] = @massmediaID












