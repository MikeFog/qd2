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
	tw.windowDateActual as issueDate,   -- время выхода
	-- нужен CampaignPart.IsMarkedAsDeleted: он читает его прямо из параметров
	-- объекта, без него контекстное меню в журнале клонированных выпусков падает
	a.deleteDate
FROM
	Issue i
	inner join Roller r on i.rollerID = r.rollerID
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	INNER JOIN [Action] a ON a.actionID = c.actionID
	Inner Join iIssuePosition ip On ip.positionId = i.positionId
WHERE
	c.actionID = @actionID
ORDER BY 
	tw.windowDateActual