-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 17.09.2008
-- Description:	Add, Update and Delete Special Action
-- Modification: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - Need agency with payment Type
-- =============================================
CREATE procedure [dbo].[SpecialActionIUD] 
(
	@actionID int = null, 
	@actionName varchar(32),
	@firmID int,
	@userID int,
	@date datetime,
	@price money,
	@agencyID int,
	@paymentTypeID tinyint,
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

    if @actionName = 'AddItem'
    begin 
		insert into [Action] (firmID,startDate,finishDate,discount,userID,tariffPrice,priceSumByCampaigns,createDate,modDate,isSpecial,isConfirmed,totalPrice,isAlerted) 
		values(@firmID,@date,@date,1,@userID,@price,@price,getdate(),getdate(),1,1,@price,1) 
		
		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @actionID = SCOPE_IDENTITY()
		
		insert into Campaign (actionID,startDate,finishDate,discount,tariffPrice,finalPrice,paymentTypeID,massmediaID,campaignTypeID,issuesCount,issuesDuration,modTime,modUser,agencyID,timeBonus,programsCount,billNo,billDate,managerDiscount,contractNo) 
		values (@actionID, @date, @date, 1, @price, @price, @paymentTypeID, null, 1, 0, 0, getdate(),@userID,@agencyID,0,0,null,null,1,null)
		
		exec [SpecialActions] @actionID = @actionID, @loggedUserID = @loggedUserID
    end 
    else if @actionName = 'UpdateItem'
    begin 
		update [Action] 
		set userID = @userID,
			startDate = @date,
			finishDate = @date,
			firmID = @firmID,
			tariffPrice = @price,
			priceSumByCampaigns = @price,
			totalPrice = @price,
			modDate = getdate()
		where actionID = @actionID and isSpecial = 1
		
		update c
		set c.modUser = @userID,
			c.agencyID = @agencyID,
			c.paymentTypeID = @paymentTypeID,
			c.startDate = @date,
			c.finishDate = @date,
			c.modTime = getdate(),
			c.tariffPrice = @price,
			c.finalPrice = @price
		from Campaign c inner join [Action] a on c.actionID = a.actionID
		where a.actionID = @actionID and a.isSpecial = 1
		
		exec [SpecialActions] @actionID, @loggedUserID = @loggedUserID
    end 
    else if @actionName = 'DeleteItem'
    begin 
		delete from c from Campaign c inner join [Action] a on c.actionID = a.actionID where a.actionID = @actionID and a.isSpecial = 1
		delete from [Action] where actionID = @actionID and isSpecial = 1
    end 
end
