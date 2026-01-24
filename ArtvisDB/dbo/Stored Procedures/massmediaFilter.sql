CREATE PROCEDURE [dbo].[massmediaFilter]
AS
BEGIN
	SET NOCOUNT ON;

    select *, mg.massmediaGroupID as id
	from MassmediaGroup mg order by mg.name
	
	select rolTypeID as id, name from dbo.iRolType
END
