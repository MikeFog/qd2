CREATE PROCEDURE dbo.TransferLogFilter

AS
BEGIN
	SET NOCOUNT ON;

    select *, mg.massmediaGroupID as id
	from MassmediaGroup mg order by mg.name
END
