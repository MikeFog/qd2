-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 
-- Description:	
-- =============================================
CREATE procedure [dbo].[UserGroups] 
(
	@userID smallint = 0
)
as 
begin 
	set nocount on;

    select g.*,
		@userID as userID
    from 
		[Group] g inner join GroupMember gm on g.groupID = gm.groupID
	where 
		gm.userID = @userID
end
