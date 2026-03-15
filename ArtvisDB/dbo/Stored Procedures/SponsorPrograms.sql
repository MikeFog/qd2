CREATE PROC [dbo].[SponsorPrograms]
(
@massmediaID smallint = null,
@sponsorProgramID smallint = null,
@activeOnly bit = 0,
@hideSponsorPLInThePast bit = 0
)
as
SET NOCOUNT ON
SELECT
	sp.*,
	mm.name as massmedia
FROM 
	SponsorProgram sp 
	inner join vMassmedia mm on sp.massmediaID = mm.massmediaID
WHERE
	sp.massmediaID = Coalesce(@massmediaID, sp.massmediaID) And
	sp.sponsorProgramID = Coalesce(@sponsorProgramID, sp.sponsorProgramID) And
	(@activeOnly = 0 Or sp.isActive = 1)
	And (@hideSponsorPLInThePast = 0 
		Or Exists(Select 1 From [SponsorProgramPricelist] pl Where pl.sponsorProgramID = sp.sponsorProgramID And pl.finishDate > GETDATE()))
ORDER BY 
	sp.name
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[SponsorPrograms] TO PUBLIC
    AS [dbo];

