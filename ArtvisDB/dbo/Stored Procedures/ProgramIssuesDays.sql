/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE   PROC [dbo].[ProgramIssuesDays]
(
@campaignID INT,
@programID AS SMALLINT = NULL
)
as
SET NOCOUNT ON
SELECT DISTINCT	
	pi.[campaignID], 
	CONVERT(varchar(10), DATEADD(mi, -DATEPART(mi, spp.broadcastStart), DATEADD(hh, -DATEPART(hh, spp.broadcastStart), pi.[issueDate])), 104) as [name],
	CONVERT(datetime, CONVERT(varchar(10), DATEADD(mi, -DATEPART(mi, spp.broadcastStart), DATEADD(hh, -DATEPART(hh, spp.broadcastStart), pi.[issueDate])), 104), 104) as issueDate,
	@programID AS programID,
	spp.[bonus],
	a.deleteDate
FROM 
	[ProgramIssue] pi
	INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
	INNER JOIN [SponsorProgramPricelist] spp ON sp.[sponsorProgramID] = spp.[sponsorProgramID]
		and dbo.ToShortDate(pi.issueDate - spp.broadcastStart) between spp.startDate and spp.finishDate
	INNER JOIN Campaign c On pi.campaignID = c.campaignID
	INNER JOIN Action a On a.actionID = c.actionID
WHERE
	pi.[campaignID] = @campaignID 
	AND (@programID IS NULL OR @programID = [programID])
ORDER BY
	CONVERT(datetime, CONVERT(varchar(10), DATEADD(mi, -DATEPART(mi, spp.broadcastStart), DATEADD(hh, -DATEPART(hh, spp.broadcastStart), pi.[issueDate])), 104), 104)
