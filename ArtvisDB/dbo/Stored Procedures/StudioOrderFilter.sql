
CREATE  PROC dbo.StudioOrderFilter
AS
SET NOCOUNT ON
SELECT [studioID] as id, [name] FROM [vStudio] ORDER BY [name]

