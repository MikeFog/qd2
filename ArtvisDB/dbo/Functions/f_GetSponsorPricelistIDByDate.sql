


CREATE    FUNCTION f_GetSponsorPricelistIDByDate
(
@sponsorProgramID smallint,
@theDate datetime
)
RETURNS SMALLINT
AS
BEGIN
DECLARE @pricelistID smallint

SELECT 
	@pricelistID = pl.[pricelistID] 
FROM 
	[SponsorProgramPricelist] pl
WHERE
	sponsorProgramID = @sponsorProgramID AND
	@theDate between pl.[startDate] AND pl.finishDate

RETURN @pricelistID
END




