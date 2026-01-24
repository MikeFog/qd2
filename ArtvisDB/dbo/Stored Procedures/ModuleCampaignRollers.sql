
CREATE  PROC dbo.ModuleCampaignRollers
(
@campaignID int,
@moduleID smallint
)
AS
Set Nocount On
SELECT 
	mi.[moduleIssueID], 
	mi.[campaignID], 
	mi.[moduleID], 
	mi.[issueDate], 
	mi.[rollerID], 
	r.name 
FROM 
	[ModuleIssue] mi
	INNER JOIN Roller r ON r.rollerID = mi.rollerID


