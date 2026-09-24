
/*
Modified by: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008
*/
CREATE          PROC [dbo].[SponsorPricelists]
(
@sponsorProgramID smallint = NULL,
@pricelistID smallint = NULL,
@hideSponsorPLInThePast bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
DECLARE @tTo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' до ')
SELECT 
	spp.*,
	dbo.fn_Int2Time(spp.bonus) as bonusString,
	@tPricelistFrom + Convert(varchar(8), startDate, 4) + @tTo + Convert(varchar(8), finishDate, 4)  as name
FROM 
	[SponsorProgramPricelist] spp
WHERE
	spp.sponsorProgramID = COALESCE(@sponsorProgramID, spp.sponsorProgramID) AND
	spp.pricelistID = COALESCE(@pricelistID, spp.pricelistID) 
	And (@hideSponsorPLInThePast = 0  Or spp.finishDate > GETDATE())
ORDER BY
	spp.finishDate DESC
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[SponsorPricelists] TO PUBLIC
    AS [dbo];

