
CREATE  Proc [dbo].[ModuleIssueContentRetrieve]
(
@moduleIssueId int
)
As
Set Nocount On

SELECT 
	i.*,
	twOrigin.massmediaID,
	r.[name],
	dbo.fn_Int2Time(r.duration) as durationString,
	c.actionID,
	ip.[description] as issuePosition,
	issueDate = twOrigin.windowDateOriginal,
	advt.name as advertTypeName
FROM
	Issue i
	inner join Roller r on i.rollerID = r.rollerID 
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	Inner Join iIssuePosition ip On ip.positionId = i.positionId
	inner join TariffWindow twOrigin on twOrigin.windowID = i.originalWindowID
	LEFT JOIN AdvertType advt ON advt.advertTypeID = r.advertTypeID
where i.moduleIssueId = @moduleIssueId
ORDER BY 
	i.positionId

