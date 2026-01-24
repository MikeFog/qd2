CREATE PROCEDURE [dbo].[AnnouncementPassport]
(
	@announcementID int = null
)
AS
begin
SET NOCOUNT ON
	select
		a.*,
		fromUserName = uf.[userName],
		toUserName = ut.userName
	from		
		Announcement a
		inner join [User] uf on a.fromUserID = uf.userID
		inner join [User] ut on a.toUserID = ut.userID
	where	
		a.announcementID = @announcementID

END