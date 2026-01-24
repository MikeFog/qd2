


CREATE    FUNCTION dbo.fn_GetPricelistIDByDate
(
@massmediaID smallint,
@theDate datetime,
@simpleSearchFlag bit = 0 -- this flag inicate should function search nearest pricelist
				  -- if pricelist for theDate doesn't exist	
)
RETURNS SMALLINT
AS
BEGIN
DECLARE @pricelistID smallint
IF EXISTS (
	SELECT * FROM [Pricelist] pl
	WHERE	massmediaID = @massmediaID AND	@theDate between pl.[startDate] AND pl.finishDate
	)
	SELECT 
		@pricelistID = pl.[pricelistID] 
	FROM 
		[Pricelist] pl
	WHERE
		massmediaID = @massmediaID AND
		@theDate between pl.[startDate] AND pl.finishDate
ELSE IF @simpleSearchFlag = 0
	SELECT TOP 1
		@pricelistID = pl.[pricelistID]
	FROM 
		[Pricelist] pl
	WHERE
		massmediaID = @massmediaID AND
		@theDate < pl.[startDate] 
	ORDER BY
		pl.[startDate]

RETURN @pricelistID
END




