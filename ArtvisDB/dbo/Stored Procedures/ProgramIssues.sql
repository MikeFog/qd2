/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008
*/
CREATE     PROC [dbo].[ProgramIssues]
(
@issueID INT = NULL,
@campaignID int = null,
@issueDate datetime = null,
@windowDate datetime = null,
@programID smallint = null,
@massmediaID smallint = null,
@firmID smallint = null,
@userID smallint = null,
@startDate datetime = null,
@finishDate datetime = null,
@showUncorfirmed bit = true,
@showDeleted bit = 0
)
AS

SET NOCOUNT ON

set @issueDate = dbo.ToShortDate(@issueDate) -- to work object refresh

SELECT 
	pi.*,
	sp.name,
	f.name as firmName,
	u.userName,
	a.deleteDate,
	adv.name as advertTypeName
FROM 
	[ProgramIssue] pi
	INNER JOIN Campaign c ON c.campaignID = pi.campaignID
	INNER JOIN [Action] a ON a.actionID = c.actionID
	INNER JOIN Firm f ON f.firmID = a.firmID
	INNER JOIN [User] u ON a.userID = u.userID
	INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
	inner join SponsorTariff st on pi.tariffID = st.tariffID
	inner join SponsorProgramPricelist pl on st.pricelistID = pl.priceListID
	left join AdvertType adv On adv.advertTypeID = pi.advertTypeID
WHERE
	pi.campaignID = COALESCE(@campaignID, pi.campaignID) and
	pi.issueDate = coalesce(@windowDate, pi.issueDate) and
	(@issueDate is null or pi.issueDate >= DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @issueDate))) And
	(@issueDate is null or pi.issueDate < DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), dateadd(day, 1, @issueDate)))) And
	pi.programID = COALESCE(@programID, pi.programID) AND
	sp.massmediaID = COALESCE(@massmediaID, sp.massmediaID) And
	f.firmID = Coalesce(@firmID, f.firmID) And
	u.userID = Coalesce(@userID, u.userID) AND
	pi.[issueID] = COALESCE(@issueID, pi.[issueID]) and 
	(@startDate is null or @finishDate is null or (  pi.issueDate between  DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @startDate)) and DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), dateadd(ss, -1, dateadd(day, 1, @finishDate)) ))     ))
	and (@showUncorfirmed = 1 or pi.isConfirmed = 1)
	and (a.deleteDate Is Null or @showDeleted = 1)
ORDER BY
	pi.issueDate DESC
