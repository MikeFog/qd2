-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 19.09.2008
-- Description:	Insert, Update and Delete for special studio order actions
-- =============================================
CREATE procedure [dbo].[SpecialStudioOrderActionIUD] 
(
	@actionID int = NULL, 
	@firmID int,
	@userID int,
	@agencyID int,
	@price int,
	@paymentTypeID tinyint,
	@date datetime,
	@actionName varchar(32),
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

    if (@actionName = 'AddItem')
    begin 
		insert into StudioOrderAction (firmID,userID,createDate,finishDate,tariffPrice,totalPrice,contacts,orderStatusID,isSpecial) 
		values ( @firmID, @userID, getdate(), @date, @price, @price, NULL, 3, 1) 

		set @actionID = ident_current('StudioOrderAction')

		insert into StudioOrder (actionID,agencyID,rolstyleID,paymentTypeID,[name],price,rollerID,rollerDuration,createDate,isComplete,finishDate,ratio,tariffID,userID) 
		values (@actionID, @agencyID, null, @paymentTypeID, null, @price, null, null, getdate(), 1, @date, 1, null, @userID)

		exec [SpecialStudioOrderActions] @actionID = @actionID, @loggedUserID = @loggedUserID
	end 
	else if (@actionName = 'UpdateItem')
    begin 
		update StudioOrderAction
		set firmID = @firmID, userID = @userID, finishDate = @date, tariffPrice = @price, totalPrice = @price
		where actionID = @actionID and isSpecial = 1

		if @@rowcount = 1 and @@error = 0
			update StudioOrder
			set agencyID = @agencyID,
				price = @price,
				finishDate = @date,
				userID = @userID
			where actionID = @actionID 

		exec [SpecialStudioOrderActions] @actionID = @actionID, @loggedUserID = @loggedUserID
	end 
	else if (@actionName = 'DeleteItem')
    begin 
		delete from StudioOrder where @actionID = actionID
		if @@rowcount = 1 and @@error = 0
			delete from StudioOrderAction where actionID = @actionID
	end 
end
