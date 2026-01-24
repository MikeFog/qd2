-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 
-- Description:	
-- =============================================
CREATE procedure UserDiscountIUD 
(
	@discountID int = null out,
	@userID smallint, 
	@startDate datetime,
	@finishDate datetime,
	@maxRatio float = 1,
	@actionName varchar(16)
)
as 
begin 
	set nocount on;

	if @actionName in ('AddItem','UpdateItem')
	begin 
		if exists(
			select * 
			from UserDiscount ud 
			where ud.userID = @userID
				and (@discountID is null or ud.discountID <> @discountID)
				and ((ud.startDate between @startDate and @finishDate)
					or  (ud.finishDate between @startDate and @finishDate) ))
		begin
			raiserror('CannotAddUserDiscount', 16, 1)
			return 
		end 
	end

    if @actionName = 'AddItem'
    begin
		insert into UserDiscount (
			userID,
			startDate,
			finishDate,
			maxRatio
		) values ( 
			@userID,
			@startDate,
			@finishDate,
			@maxRatio ) 
		
		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @discountID = SCOPE_IDENTITY()
	end
	else if @actionName = 'UpdateItem'
	begin 
		update UserDiscount
			set startDate = @startDate,
				finishDate = @finishDate,
				maxRatio = @maxRatio
			where discountID = @discountID
	end
	else if @actionName = 'DeleteItem'
	begin 
		delete from UserDiscount where discountID = @discountID
	end
end
