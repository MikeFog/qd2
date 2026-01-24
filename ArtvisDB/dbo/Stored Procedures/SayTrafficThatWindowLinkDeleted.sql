
CREATE   procedure [dbo].[SayTrafficThatWindowLinkDeleted] 
(
	@userId int,
	@massmediaId int,
	@windowDate1 datetime,
	@windowDate2 datetime
)
as 
begin 
	set nocount on;

	declare @msg varchar(255)
	
	select @msg = 'Удалено объединение рекламных окон в ''' + dbo.ToDateTimeString(@windowDate1) + ' и '''+ dbo.ToDateTimeString(@windowDate2) + ''' на радиостанции ' + [name]  + ' (' + groupName  +').' 
	from vMassmedia where massmediaID = @massmediaId

    insert into Announcement (toUserID, subject, fromUserID) 
	Select userID, @msg, @userId From [User] Where [isTrafficManager] = 1
end
