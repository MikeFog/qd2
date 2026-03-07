
CREATE    PROC [dbo].[RollerStylePassport]
AS
Set Nocount On
SELECT	RolTypeID as Id, [name] FROM iRolType ORDER BY [Name]
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[RollerStylePassport] TO PUBLIC
    AS [dbo];

