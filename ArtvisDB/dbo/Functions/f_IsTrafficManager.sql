CREATE  FUNCTION [dbo].[f_IsTrafficManager]
(
@userID smallint
)
RETURNS bit
AS
BEGIN
IF EXISTS (
	SELECT * 
	FROM [User] 
	WHERE userID = @userID AND isTrafficManager = 1
	)
	RETURN 1

RETURN 0	

END