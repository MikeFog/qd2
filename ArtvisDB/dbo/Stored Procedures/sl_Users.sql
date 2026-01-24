



CREATE     PROC dbo.sl_Users
(
@groupID smallint
)
AS
SET NOCOUNT ON
SELECT DISTINCT
	u.*,
	u.lastName + space(1) + u.firstName as name,
	u.userId as id,
	@groupID as groupID		
FROM 
	[User] u
	INNER JOIN #User u2 ON u.userID = u2.userID
WHERE 
	u.userID <> 0	
ORDER BY 
	u.lastName, 
	u.firstName





