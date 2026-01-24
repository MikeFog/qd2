Create procedure [dbo].[ActionIssues]
(
@actionID int
)
as

SELECT 
	i.*,
	r.[name],
	r.duration,
	tw.massmediaID,
	tw.tariffId,
	dbo.fn_Int2Time(r.duration) as durationString,
	c.actionID,
	ip.[description] as issuePosition,
	tw.windowDateOriginal as issueDate
FROM
	Issue i
	inner join Roller r on i.rollerID = r.rollerID
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	Inner Join iIssuePosition ip On ip.positionId = i.positionId
WHERE
	c.actionID = @actionID
ORDER BY 
	tw.windowDateOriginal