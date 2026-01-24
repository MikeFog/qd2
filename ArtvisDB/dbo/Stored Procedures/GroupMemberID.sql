CREATE PROC dbo.GroupMemberID
(
@userID SMALLINT, 
@groupID SMALLINT,
@actionName varchar(32)
)
AS

SET NOCOUNT ON

IF @actionName = 'AddItem'
	INSERT INTO [GroupMember]([groupID], [userID]) VALUES(@groupID, @userID)		
ELSE IF @actionName = 'DetachItem'
	DELETE [GroupMember] WHERE UserID = @UserID AND groupID = @groupID


