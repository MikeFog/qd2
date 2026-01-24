CREATE PROC [dbo].[Groups]
	@groupID INT = null
AS
SET NOCOUNT on
SELECT 
	* 
FROM 
	[Group] g
WHERE
	g.[groupID] = ISNULL(@groupID, g.[groupID])
ORDER BY name
