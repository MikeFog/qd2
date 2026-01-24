-- =============================================
-- Author:		D.Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 11.01.2009
-- Description:	Группы менеджеров по правам
-- =============================================
CREATE PROCEDURE [dbo].[GroupListByRights]
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
		
	select distinct g.[name], g.groupID 
	from [Group] g 
		left join GroupMember gm on g.groupID = gm.groupID and gm.userID = @loggedUserID
	where (gm.userID is not null or @isRightToViewForeignActions = 1)
	order by g.[name]
END
