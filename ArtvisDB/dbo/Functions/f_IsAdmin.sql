
CREATE  FUNCTION [dbo].[f_IsAdmin]
(
@userID smallint
)
RETURNS bit
AS
BEGIN
IF EXISTS (
	SELECT * 
	FROM [User] 
	WHERE userID = @userID AND isAdmin = 1
	)
	RETURN 1

RETURN 0	

END


