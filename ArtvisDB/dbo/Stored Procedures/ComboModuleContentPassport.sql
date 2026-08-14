CREATE PROCEDURE [dbo].[ComboModuleContentPassport]
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
-- 1. Massmedia
SELECT massmediaID as [id], nameWithGroup as [name] FROM vMassmedia where isActive = 1 ORDER BY [name]

-- 2. Modules
EXEC ModuleList
