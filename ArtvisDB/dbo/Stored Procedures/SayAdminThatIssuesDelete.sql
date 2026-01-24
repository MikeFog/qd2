-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	Сообщение администраторам о том что выпуски удалили
-- =============================================
CREATE procedure [dbo].[SayAdminThatIssuesDelete] 
(
	@userID smallint,
	@actionID int 
)
as 
begin 
	set nocount on;

	declare @msg varchar(255)
	
	select @msg = 'Удалил выпуски, дата выхода которых раньше чем через ' + cast(dbo.f_SysParamsDaysLog() as varchar) + ' дней у акции №' + cast(@actionID as varchar)  + '.'
	
    insert into Announcement (toUserID, fromUserID,subject) 
    Select  UserID, @userID, @msg From [User] Where IsAdmin = 1
end
