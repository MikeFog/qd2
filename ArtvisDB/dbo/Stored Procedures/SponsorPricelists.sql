
/*
Modified by: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008
*/
CREATE          PROC [dbo].[SponsorPricelists]
(
@sponsorProgramID smallint = NULL,
@pricelistID smallint = NULL,
@hideSponsorPLInThePast bit = 0
)
AS
SET NOCOUNT ON
SELECT 
	spp.*,
	dbo.fn_Int2Time(spp.bonus) as bonusString,
	'Прайс-лист от ' + Convert(varchar(8), startDate, 4) + ' до ' + Convert(varchar(8), finishDate, 4)  as name
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

