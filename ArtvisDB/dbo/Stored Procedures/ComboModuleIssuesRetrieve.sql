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
	-- меню выпуска строится по сущности 130 (ModuleIssue жёстко берёт её в
	-- конструкторе), поэтому нужны те же колонки, что отдаёт ModuleIssueRetrieve:
	-- позиционирование читает positionId и modulePriceListID, замена ролика - rollerID
	mi.positionId,
	mi.modulePriceListID,
	mi.rollerID,
	mm.massmediaID,
	mm.nameWithGroup AS massmediaName,
	m.[name] AS moduleName,
	r.[name] AS rollerName,
	ip.[description] AS issuePosition,
	-- нужен CampaignPart.IsMarkedAsDeleted: он читает его прямо из параметров
	-- объекта, без него контекстное меню в списке выпусков падает
	a.deleteDate,
	CONVERT(varchar(10), mi.issueDate, 104) + ' - ' + mm.nameWithGroup + ' - ' + r.[name] AS [name]
FROM
	[ModuleIssue] mi
	INNER JOIN [Campaign] c ON c.campaignID = mi.campaignID
	INNER JOIN [Action] a ON a.actionID = c.actionID
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
