CREATE   Proc
[dbo].[RollersWithoutAdvertype]
(
@actionId int
)
AS
Set Nocount On

Select distinct
	r.*, 
	dbo.fn_Int2Time(r.[duration]) as durationString, 
	f.name as firmName
From 
	vRoller r
	Inner Join Issue i on i.rollerID = r.rollerID
	Inner Join Campaign c on c.campaignID = i.campaignID
	LEFT JOIN Firm f ON f.firmID = r.firmID
Where 
	c.actionID = @actionId
	And r.advertTypeID Is Null


SELECT 
	pi.*,
	sp.name
FROM 
	[ProgramIssue] pi
	INNER JOIN Campaign c ON c.campaignID = pi.campaignID
	INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
WHERE
	c.actionID = @actionId
	And pi.advertTypeID Is Null
ORDER BY
	pi.issueDate DESC