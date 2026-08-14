-- Модули, размещённые в акции, - строки грида при редактировании готовой акции
-- из её карточки (кнопка «Размещение комбо-модулями»).
--
-- Связи кампании с модулем в схеме нет, она выводится через выпуски - так же
-- устроена CampaignModulesRetrieve. Поэтому модульная кампания без выпусков сюда
-- не попадёт: неизвестно, какой в ней модуль.
--
-- Колонки совпадают с ComboModuleContentRetrieve в той части, которую читает грид.
CREATE PROC [dbo].[ComboModuleActionModulesRetrieve]
(
@actionID int
)
AS
SET NOCOUNT ON

SELECT DISTINCT
	mi.moduleID,
	m.[name] AS moduleName,
	mm.massmediaID,
	mm.[name] AS massmediaName,
	mm.nameWithGroup AS massmediaNameWithGroup,
	mm.groupName
FROM
	[ModuleIssue] mi
	INNER JOIN [Campaign] c ON c.campaignID = mi.campaignID
	INNER JOIN [Module] m ON m.moduleID = mi.moduleID
	INNER JOIN [vMassmedia] mm ON mm.massmediaID = m.massmediaID
WHERE
	c.actionID = @actionID
ORDER BY
	massmediaNameWithGroup,
	moduleName
