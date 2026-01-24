CREATE   PROC [dbo].[statVolumeOfRealizationMonthFilter]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
-- Manager
exec UserListByRights
	@loggedUserID = @loggedUserID
