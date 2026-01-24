CREATE PROCEDURE [dbo].[sp_CheckFirmINN]
	(
		@inn varchar(20),
		@firmID int = null 
	)
AS
BEGIN
	SET NOCOUNT ON;

	select [name] 
	from Firm 
	where (((@inn is null or @inn = space(0)) and inn is null)
		or (@inn is not null and @inn != space(0) and inn like @inn + '%')) and (@firmID is null or firmID <> @firmID)
END
