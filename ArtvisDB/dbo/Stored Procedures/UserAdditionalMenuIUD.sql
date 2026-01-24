-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 31.10.2008
-- Description:	
-- =============================================
create procedure UserAdditionalMenuIUD 
(
	@userID smallint, 
	@menuID smallint,
	@actionName varchar(32)
)
as 
begin 
	set nocount on;

	if (dbo.f_IsAdmin(@userID) = 1)
	begin 
		raiserror('CannotChangeMenusForAdmin', 16, 1)
		return
	end

    if @actionName in ('UpdateItem','AddItem')
    begin 
		delete from UserAdditionMenu where userID = @userID and menuID = @menuID and isGrant = 0
		
		if not exists(select * from GroupMember gm
						inner join GroupMenu gr on gm.groupID = gr.groupID
						where gm.userID = @userID and gr.menuID = @menuID)
		begin 
			insert into UserAdditionMenu(userID,menuID,isGrant) values (@userID,@menuID,1) 
		end 
    end 
    else if @actionName = 'DeleteItem'
    begin 
		delete from UserAdditionMenu where userID = @userID and menuID = @menuID and isGrant = 1
		
		if exists(select * from GroupMember gm
						inner join GroupMenu gr on gm.groupID = gr.groupID
						where gm.userID = @userID and gr.menuID = @menuID)
		begin 
			insert into UserAdditionMenu(userID,menuID,isGrant) values (@userID,@menuID,0) 
		end 
    end
end
