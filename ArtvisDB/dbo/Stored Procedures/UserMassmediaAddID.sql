-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 14.01.2009
-- Description:	
-- =============================================
create procedure UserMassmediaAddID 
(
	@userID smallint, 
	@massmediaID smallint,
	@actionName varchar(32)
)
as 
begin 
	set nocount on;

    IF @actionName = 'AddItem' 
	begin
		if exists(select * from UserMassmedia where massmediaID = @massmediaID and userID = @userID)
			update UserMassmedia set canAdd = 1
			where userID = @userID and massmediaID = @massmediaID
		else
			insert into UserMassmedia (userID,	massmediaID, canAdd) 
			values (@userID, @massmediaID,1) 
	end
	ELSE IF @actionName = 'DeleteItem' BEGIN
		update UserMassmedia set canAdd = 0
			WHERE userID = @userID and massmediaID = @massmediaID
	END
end
