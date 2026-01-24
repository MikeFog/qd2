
CREATE PROC [dbo].[sl_LookupRolType]
as
SET NOCOUNT ON
SELECT RolTypeID as id, name FROM iRolType ORDER BY name

