
CREATE  Proc [dbo].[ModuleIssueContentRetrieve]
(
@moduleIssueId int
)
As
Set Nocount On

SELECT 
	i.*,
	tw.massmediaID,
	r.[name],
	dbo.fn_Int2Time(r.duration) as durationString,
	c.actionID,
	ip.[description] as issuePosition,
	issueDate = tw.windowDateActual,   -- время выхода
	advt.name as advertTypeName
FROM
	Issue i
	inner join Roller r on i.rollerID = r.rollerID 
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	Inner Join iIssuePosition ip On ip.positionId = i.positionId
	inner join TariffWindow tw on tw.windowID = i.actualWindowID   -- окно выхода
	LEFT JOIN AdvertType advt ON advt.advertTypeID = r.advertTypeID
where i.moduleIssueId = @moduleIssueId
ORDER BY 
	i.positionId

