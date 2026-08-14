CREATE PROC [dbo].[ComboModules]
(
@comboModuleID smallint = NULL
)
AS
SET NOCOUNT ON

SELECT
	cm.[comboModuleID], cm.[name]
FROM
	[ComboModule] cm
WHERE
	cm.comboModuleID = Coalesce(@comboModuleID, cm.comboModuleID)
ORDER BY
	cm.[name]
