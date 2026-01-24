


/*
Modified by Denis Gladkikh (dgladkikh@fogsoft.ru) - add moduleID and packModuleID for roller subtitude
*/

CREATE       PROC [dbo].[CampaignRollers]
(
@campaignID int,
@massmediaID smallint,
@issueDate datetime = null,
@moduleIssueID int = null,
@packModuleIssueID int = null,
@rollerId int = null
)
AS
SET NOCOUNT ON

SELECT DISTINCT
	i.[campaignID], 
	i.[rollerID],
	c.massmediaID,
	@issueDate as issueDate,
	r.NAME,
	dbo.fn_Int2Time(r.[duration]) as durationString,
	COUNT(i.[issueID]) AS [count],
	r.duration,
	r.[path],
	case when @moduleIssueID is null then null else i.moduleIssueID end as moduleIssueID,
	case when @packModuleIssueID is null then null else i.packModuleIssueID end as packModuleIssueID,
	mi.moduleID as moduleID,
	pmpl.packModuleID as packModuleID,
	r.isMute,
	r.advertTypeName,
	mi.modulePricelistID,
	a.deleteDate
FROM 
	[Issue] i
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	Inner Join Action a On a.actionID = c.actionID
	inner join vRoller r on i.rollerID = r.rollerID
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID 
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
										and	tw.dayOriginal between pmpl.startDate and pmpl.finishDate
WHERE
	i.campaignID = @campaignID and 
	(@moduleIssueID is null or i.moduleIssueID = @moduleIssueID) and 
	(@packModuleIssueID is null or i.packModuleIssueID = @packModuleIssueID) and 
	(@issueDate is null or tw.dayOriginal = @issueDate)
	and (@rollerId is null or i.rollerID = @rollerId)
GROUP BY i.[campaignID], i.[rollerID], c.[massmediaID], r.[name], r.[duration], r.[path],
	case when @moduleIssueID is null then null else i.moduleIssueID end, case when @packModuleIssueID is null then null else i.packModuleIssueID end, 
	mi.moduleID, pmpl.packModuleID,r.isMute, r.advertTypeName,	mi.modulePricelistID,
	a.deleteDate
ORDER BY
	r.name


