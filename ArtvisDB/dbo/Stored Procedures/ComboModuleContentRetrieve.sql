CREATE PROC [dbo].[ComboModuleContentRetrieve]
(
@comboModuleID smallint = NULL,
@comboModuleContentID smallint = NULL
)
AS
SET NOCOUNT ON

SELECT
	cmc.*,
	m.[name],
	m.[name] as moduleName,
	mm.nameWithGroup as massmediaName,
	mm.[massmediaID],
	mm.groupName
FROM
	[ComboModuleContent] cmc
	INNER JOIN Module m ON m.moduleID = cmc.moduleID
	INNER JOIN vMassmedia mm ON mm.massmediaID = m.massmediaID
WHERE
	cmc.comboModuleID = Coalesce(@comboModuleID, cmc.comboModuleID)
	AND cmc.comboModuleContentID = Coalesce(@comboModuleContentID, cmc.comboModuleContentID)
ORDER BY
	massmediaName,
	moduleName
