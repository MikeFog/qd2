
/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE   PROC [dbo].[SponsorPricelistByDate]
(
@sponsorProgramID smallint,
@theDate datetime
)
AS
SET NOCOUNT ON
IF EXISTS (
	SELECT * FROM [SponsorProgramPricelist] pl
	WHERE	sponsorProgramID = @sponsorProgramID AND	@theDate between pl.[startDate] AND pl.finishDate
	)
	SELECT 
		pl.[pricelistID], 
		pl.[sponsorProgramID],
		pl.[startDate],
		'Прайс-лист от ' + CONVERT(varchar(10), pl.[startDate], 104) as name,
		pl.[finishDate],
		pl.bonus,
		pl.broadcastStart
	FROM 
		[SponsorProgramPricelist] pl
	WHERE
		pl.sponsorProgramID = @sponsorProgramID AND
		@theDate between pl.[startDate] AND pl.finishDate
ELSE
	SELECT TOP 1
		pl.[pricelistID], 
		pl.[sponsorProgramID],
		pl.[startDate],
		'Прайс-лист от ' + CONVERT(varchar(10), pl.[startDate], 104) as name,
		pl.[finishDate],
		pl.bonus,
		pl.broadcastStart
	FROM 
		[SponsorProgramPricelist] pl
	WHERE
		pl.sponsorProgramID = @sponsorProgramID AND
		@theDate < pl.[startDate] 
	ORDER BY
		pl.[startDate]

