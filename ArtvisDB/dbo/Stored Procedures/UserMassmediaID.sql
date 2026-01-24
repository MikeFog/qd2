CREATE PROCEDURE [dbo].[UserMassmediaID]
(
	@userID int,
	@massmediaID smallint,
	@actionName varchar(32)
)	
AS
BEGIN
	SET NOCOUNT ON;

	IF @actionName = 'AddItem' 
	begin 
		if exists(select * from UserMassmedia where massmediaID = @massmediaID and userID = @userID)
			update UserMassmedia set canWork = 1
			where userID = @userID and massmediaID = @massmediaID
		else
			insert into UserMassmedia (userID,	massmediaID, canWork) 
			values (@userID, @massmediaID, 1) 
	end
	ELSE IF @actionName = 'DeleteItem' 
	begin
		update UserMassmedia set canWork = 0
			where userID = @userID and massmediaID = @massmediaID
	end
END
