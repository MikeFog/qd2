CREATE PROC [dbo].[PackModuleIssueContentRetrieve]
(
@packModuleIssueId int = null,
@issueID int = null
)
AS
SET NOCOUNT ON 

SELECT 
	i.[issueID],
	r.[name],
	tw.windowDateOriginal as [issueDate],
	dbo.fn_Int2Time(r.duration) as durationString,
	ip.[description] AS issuePosition,
	m.[name] AS massmediaName,
	m.groupName,
	r.advertTypeName
FROM 
	[Issue] i
	inner join vRoller r on i.rollerID = r.rollerID
	INNER JOIN [iIssuePosition] ip ON i.[positionId] = ip.[positionId]
	INNER JOIN [TariffWindow] tw ON tw.[windowId] = i.originalWindowID
	INNER JOIN [vMassMedia] m ON tw.[massmediaID] = m.[massmediaID]
WHERE 
	i.[packModuleIssueID] = coalesce(@packModuleIssueId, i.[packModuleIssueID])
	and i.issueID = coalesce(@issueID, i.issueID)
ORDER BY
	m.[name], tw.windowDateActual

