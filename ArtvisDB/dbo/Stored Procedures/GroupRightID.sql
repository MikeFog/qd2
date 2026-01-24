CREATE PROCEDURE [dbo].[GroupRightID]
(
@groupID smallint,
@entityActionID smallint,
@actionName varchar(32)
)
as
begin 
SET NOCOUNT on
	IF @actionName IN ('AddItem', 'UpdateItem') BEGIN
		INSERT INTO [GroupRight](groupID, entityActionID)
		VALUES(@groupID, @entityActionID)
		END
	ELSE IF @actionName = 'DeleteItem'
		DELETE FROM [GroupRight] WHERE groupID = @groupID And entityActionID = @entityActionID
end 

