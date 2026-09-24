
/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE   PROC [dbo].[SponsorPricelistByDate]
(
@sponsorProgramID smallint,
@theDate datetime,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
IF EXISTS (
	SELECT * FROM [SponsorProgramPricelist] pl
	WHERE	sponsorProgramID = @sponsorProgramID AND	@theDate between pl.[startDate] AND pl.finishDate
	)
	SELECT 
		pl.[pricelistID], 
		pl.[sponsorProgramID],
		pl.[startDate],
		@tPricelistFrom + CONVERT(varchar(10), pl.[startDate], 104) as name,
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
		@tPricelistFrom + CONVERT(varchar(10), pl.[startDate], 104) as name,
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

