CREATE          PROC [dbo].[Users]
(
@groupId smallint = null,
@userId smallint = null,
@activeOnly bit = 0
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

CREATE TABLE #User(userID smallint)
INSERT INTO #User(userID)
SELECT DISTINCT 
	u.userID
FROM 
	[User] u
	LEFT JOIN GroupMember gm ON gm.userID = u.userID
WHERE 
	u.userID <> 0	
	AND (gm.groupId = @groupId OR @groupId IS NULL)
	And u.userId = Coalesce(@userId, u.userId)
	and (@activeOnly = 0 or u.isActive = 1)

EXEC sl_Users @groupId
