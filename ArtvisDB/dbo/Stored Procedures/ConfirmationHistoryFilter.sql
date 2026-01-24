CREATE PROC [dbo].[ConfirmationHistoryFilter]
(
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
exec UserListByRights @loggedUserID = @loggedUserID

SELECT confirmationTypeID as id, name FROM iConfirmationType ORDER BY name
