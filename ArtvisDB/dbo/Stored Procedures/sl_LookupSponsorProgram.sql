





CREATE  PROCEDURE [dbo].[sl_LookupSponsorProgram]
as
SET NOCOUNT ON
SELECT 
	SponsorProgram.sponsorProgramId as Id,
	SponsorProgram.name
FROM 
	SponsorProgram
ORDER BY
	SponsorProgram.name







