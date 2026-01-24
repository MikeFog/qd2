-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 
-- Description:	
-- =============================================
create procedure UserGroupD 
(
	@userID smallint, 
	@groupID int,
	@actionName varchar(16)
)
as 
begin 
	set nocount on;

	if @actionName = 'DeleteItem'
		delete from GroupMember where userID = @userID and groupID = @groupID
end
