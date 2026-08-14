-- Выпуски модулей всей акции - панель «Добавленные выпуски» формы размещения
-- комбо-модулями.
--
-- Существующие ModuleIssueRetrieve и CampaignModuleIssuesRetrieve работают в разрезе
-- одной кампании, а здесь кампаний столько же, сколько модулей в комбо-модуле.
CREATE PROC [dbo].[ComboModuleIssuesRetrieve]
(
@actionID int
)
AS
SET NOCOUNT ON

SELECT
	mi.moduleIssueID,
	mi.campaignID,
	mi.moduleID,
	mi.issueDate,
	mm.[name] AS massmediaName,
	m.[name] AS moduleName,
	r.[name] AS rollerName,
	ip.[description] AS issuePosition,
	CONVERT(varchar(10), mi.issueDate, 104) + ' - ' + mm.[name] + ' - ' + r.[name] AS [name]
FROM
	[ModuleIssue] mi
	INNER JOIN [Campaign] c ON c.campaignID = mi.campaignID
	INNER JOIN [Module] m ON m.moduleID = mi.moduleID
	INNER JOIN [vMassmedia] mm ON mm.massmediaID = m.massmediaID
	INNER JOIN [vRoller] r ON r.rollerID = mi.rollerID
	LEFT JOIN [iIssuePosition] ip ON ip.positionId = mi.positionId
WHERE
	c.actionID = @actionID
ORDER BY
	mi.issueDate,
	mm.[name],
	m.[name]
