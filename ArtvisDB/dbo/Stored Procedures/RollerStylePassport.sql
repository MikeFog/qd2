
CREATE    PROC [dbo].[RollerStylePassport]
AS
Set Nocount On
SELECT	RolTypeID as Id, [name] FROM iRolType ORDER BY [Name]


