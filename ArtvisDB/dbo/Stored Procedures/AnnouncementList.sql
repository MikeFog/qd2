




CREATE PROCEDURE [dbo].[AnnouncementList]
(
	@userID int = null,
	@fromUserID int = null,
	@toUserID int = null,
	@isConfirmed bit = null,
	@ShowUnConfirmed bit = null,
	@ShowFromMe bit = null,
	@ShowForMe bit = null,
	@StartDay datetime = null,
	@FinishDay datetime = null,
	@announcementID int = null
)
AS
begin
SET NOCOUNT ON
	select distinct
		a.*,
		"to" = ut.userName,
		"from" = uf.userName,
		isConfirmed = case when a.dateConfirmed is null then 0 else 1 end,
		fromUserName = uf.userName, 
		toUserName = ut.userName
	from		
		Announcement a
		inner join [User] ut on ut.userID = a.toUserID
		inner join [User] uf on uf.userID = a.fromUserID
	where	
		(@fromUserID is null or @fromUserID = a.fromUserID) and
		(@toUserID is null or @toUserID = a.toUserID) and
		(isnull(@ShowUnConfirmed, 0) = 0 or (@ShowUnConfirmed = 1 and a.dateConfirmed is null)) and
		(@StartDay is null or (a.dateCreated >= dateadd(day, -1, @startDay))) and
		(@FinishDay is null or (a.dateCreated <= dateadd(day, 1, @finishDay))) and
		(IsNull(@ShowFromMe, 0) = 0 or (@ShowForMe = 1 or (@ShowFromMe = 1 and uf.userID = @userID))) and
		(IsNull(@ShowForMe, 0) = 0 or (@ShowFromMe = 1 or (@ShowForMe = 1 and ut.userID = @userID))) and
		(@UserID is null or (@userId in (ut.UserID, uf.UserId))) and
		(@announcementID is null or a.announcementID = @announcementID) 
	order by 
		a.dateCreated desc
				
END








