







CREATE         PROC [dbo].[PricelistByDate]
(
@massmediaID SMALLINT = null,
@theDate datetime,
@moduleID smallint = NULL,
@campaignID INT = null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
IF @campaignID IS NULL 
	SELECT 
		pl.*,
		@tPricelistFrom + CONVERT(varchar(10), pl.[startDate], 104) as name,
		@moduleID as moduleID
	FROM 
		[Pricelist] pl
	WHERE
		pl.pricelistID = dbo.fn_GetPricelistIDByDate(@massmediaID, @theDate, default)
ELSE
BEGIN
	DECLARE @campaignTypeID SMALLINT
	SELECT @campaignTypeID = campaignTypeID FROM [Campaign] WHERE [campaignID] = @campaignID
	
	IF @campaignTypeID = 4
		SELECT 
			pmpl.*, 
			@tPricelistFrom + CONVERT(varchar(10), pmpl.[startDate], 104) as name
		FROM [Campaign] c 
			INNER JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
			INNER JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
		WHERE
			@theDate BETWEEN pmpl.[startDate] AND pmpl.[finishDate]
END











