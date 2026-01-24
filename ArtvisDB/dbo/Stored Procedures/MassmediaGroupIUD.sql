
CREATE PROCEDURE [dbo].[MassmediaGroupIUD]
(
	@massmediaGroupID int = null out,
	@name varchar(250) = null,
	@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

    IF @actionName = 'AddItem' BEGIN
		INSERT INTO MassmediaGroup([name]) VALUES(@name)

		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @massmediaGroupID = SCOPE_IDENTITY()

		EXEC MassmediaGroups @massmediaGroupID = @massmediaGroupID
	END
	ELSE IF @actionName = 'DeleteItem' BEGIN
		DELETE FROM MassmediaGroup WHERE massmediaGroupID = @massmediaGroupID
	END
	ELSE IF @actionName = 'UpdateItem' BEGIN
		update MassmediaGroup set [name] = @name where massmediaGroupID = @massmediaGroupID

		EXEC MassmediaGroups @massmediaGroupID = @massmediaGroupID
	end 
END


