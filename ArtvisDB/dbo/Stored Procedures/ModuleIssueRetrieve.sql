CREATE   PROC [dbo].[ModuleIssueRetrieve]
(
@campaignID INT = NULL,
@issueDate datetime = NULL,
@showUnconfirmed BIT = 1,
@moduleIssueId INT = NULL,
@windowID INT = NULL
)
AS
SET NOCOUNT on

if @windowID is not null
	SELECT DISTINCT
		mi.moduleIssueID,
		mi.[campaignID], 
		mi.[moduleID],
		mi.issueDate,
		mi.modulePriceListID,
		mi.positionID,
		mi.rollerID,
		m.name as moduleName,
		r.name as rollerName,
		m.name + ' - ' + r.name as NAME,
		f.[name] AS firmName,
		c.[actionID],
		a.[isConfirmed],
		r.advertTypeName,
		a.deleteDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join vRoller r on r.rollerId = mi.rollerId
		INNER JOIN [Campaign] c ON c.[campaignID] = mi.[campaignID]
		INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		INNER JOIN [Issue] i ON mi.[moduleIssueID] = i.[moduleIssueID] 
	WHERE
		(@campaignID is null or mi.campaignID = @campaignID)
		And (@issueDate is null or mi.issueDate = @issueDate)
		AND (@showUnconfirmed = 1 OR mi.[isConfirmed] = 1)
		AND (@moduleIssueId is null or mi.[moduleIssueID] = @moduleIssueId)
		AND i.originalWindowID = @windowID
	Order By 
		m.name, r.name
else
	SELECT DISTINCT
		mi.moduleIssueID,
		mi.[campaignID], 
		mi.[moduleID],
		mi.issueDate,
		mi.modulePriceListID,
		mi.positionID,
		mi.rollerID,
		m.name as moduleName,
		r.name as rollerName,
		m.name + ' - ' + r.name as NAME,
		f.[name] AS firmName,
		c.[actionID],
		a.[isConfirmed],
		ip.[description] as issuePosition,
		r.advertTypeName,
		a.deleteDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join vRoller r on r.rollerId = mi.rollerId
		INNER JOIN [Campaign] c ON c.[campaignID] = mi.[campaignID]
		INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		Inner Join iIssuePosition ip On ip.positionId = mi.positionId
	WHERE
		(@campaignID is null or mi.campaignID = @campaignID)
		And (@issueDate is null or mi.issueDate = @issueDate)
		AND (@showUnconfirmed = 1 OR mi.[isConfirmed] = 1)
		AND (@moduleIssueId is null or mi.[moduleIssueID] = @moduleIssueId)
	Order By 
		m.name, r.name
