







CREATE         PROC [dbo].[PricelistByDate]
(
@massmediaID SMALLINT = null,
@theDate datetime,
@moduleID smallint = NULL,
@campaignID INT = null
)
AS
SET NOCOUNT ON
IF @campaignID IS NULL 
	SELECT 
		pl.*,
		'Прайс-лист от ' + CONVERT(varchar(10), pl.[startDate], 104) as name,
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
			'Прайс-лист от ' + CONVERT(varchar(10), pmpl.[startDate], 104) as name
		FROM [Campaign] c 
			INNER JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
			INNER JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
		WHERE
			@theDate BETWEEN pmpl.[startDate] AND pmpl.[finishDate]
END











