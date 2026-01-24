/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE    PROC [dbo].[SponsorCampaignPrograms]
(
@campaignID int,
@issueDate datetime = NULL
)
as
SET NOCOUNT ON
SELECT DISTINCT
	pi.[campaignID], 
	pi.[programID],
	@issueDate as issueDate,
	sp.NAME,
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
	pi.campaignID = @campaignID AND
	(@issueDate is null or pi.issueDate between DATEADD(mi, DATEPART(mi, spp.broadcastStart), DATEADD(hh, DATEPART(hh, spp.broadcastStart), @issueDate)) AND dateadd(ss, -1, DATEADD(mi, DATEPART(mi, spp.broadcastStart), DATEADD(hh, DATEPART(hh, spp.broadcastStart), dateadd(day, 1, @issueDate)))))
--group by pi.[campaignID], pi.[programID],issueDate, sp.NAME,spp.[bonus]
