CREATE PROC [dbo].[SponsorPrograms]
(
@massmediaID smallint = null,
@sponsorProgramID smallint = null,
@activeOnly bit = 0
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
ORDER BY 
	sp.name

