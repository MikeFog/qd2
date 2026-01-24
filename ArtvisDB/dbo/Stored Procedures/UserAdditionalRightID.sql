-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 20.10.2008
-- Description:	
-- =============================================
CREATE procedure [dbo].[UserAdditionalRightID] 
(
	@userID smallint, 
	@entityActionID smallint,
	@actionName varchar(32)
)
as 
begin 
	set nocount on;

	if (dbo.f_IsAdmin(@userID) = 1)
	begin 
		raiserror('CannotChangeRightsForAdmin', 16, 1)
		return
	end

    if @actionName in ('UpdateItem','AddItem')
    begin 
		delete from UserAdditionRight where userID = @userID and entityActionID = @entityActionID and isGrant = 0
		
		if not exists(select * from GroupMember gm
						inner join GroupRight gr on gm.groupID = gr.groupID
						where gm.userID = @userID and gr.entityActionID = @entityActionID)
		begin 
			insert into UserAdditionRight(userID,entityActionID,isGrant) values (@userID,@entityActionID,1) 
		end 
    end 
    else if @actionName = 'DeleteItem'
    begin 
		delete from UserAdditionRight where userID = @userID and entityActionID = @entityActionID and isGrant = 1
		
		if exists(select * from GroupMember gm
						inner join GroupRight gr on gm.groupID = gr.groupID
						where gm.userID = @userID and gr.entityActionID = @entityActionID)
		begin 
			insert into UserAdditionRight(userID,entityActionID,isGrant) values (@userID,@entityActionID,0) 
		end 
    end
end

