



CREATE PROCEDURE [dbo].[AnnouncementCheckForNew]
(
	@date datetime,
	@userID smallint
)
WITH EXECUTE AS OWNER
AS
begin
	SET NOCOUNT ON
	exec CreateNotActivatedActionsAnnouncement
	exec CreateActionMuteRollersAlerts 
	
	select	top 1 1
	from		
		Announcement a
		Inner join [User] u On a.toUserID = u.UserID
	where	
		a.dateConfirmed is null
		and a.toUserID = @userID
	
END







