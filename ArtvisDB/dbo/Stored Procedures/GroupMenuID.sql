CREATE PROCEDURE [dbo].[GroupMenuID]
(
@groupID smallint = NULL,
@menuID smallint = NULL,
@actionName varchar(32)
)
as
SET NOCOUNT on
IF @actionName IN ('AddItem', 'UpdateItem') 
begin
	if not exists(select * from GroupMenu where groupID = @groupID and menuID = @menuID)
		INSERT INTO [GroupMenu](groupID, menuID) VALUES(@groupID, @menuID)
END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [GroupMenu] WHERE GroupID = @GroupID AND menuID = @menuID



