
-- =============================================
-- Author:		D.Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 11.01.2009
-- Description:	Пользователи по группе
-- =============================================
CREATE PROCEDURE UserListByRights
(
	@loggedUserID smallint,
	@isRightToViewForeignActions bit = null,
	@isRightToViewGroupActions bit = null,
	@forStudioOrders bit = 0
)
AS
BEGIN
	set nocount on;

	if @isRightToViewForeignActions is null
	begin 
		if @forStudioOrders = 1
			set @isRightToViewForeignActions = dbo.[fn_IsRightToEditForeignSOActions](@loggedUserID)
		else
			set @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID)
	end 
	
	if @isRightToViewGroupActions is null
	begin 
		if @forStudioOrders = 1
			set @isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)
		else
			set @isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)
	end
		
	select distinct	u.*, u.lastName + space(1) + u.firstName as [name],u.userId as id
	from [User] u
		left join GroupMember gm on u.userID = gm.userID
		left join GroupMember gmu on gm.groupID = gmu.groupID and gmu.userID = @loggedUserID
	where u.userID <> 0	and (gmu.userID is not null or @isRightToViewForeignActions = 1 or u.userID = @loggedUserID)
		and u.isActive = 1
	order by u.lastName, u.firstName
END

