CREATE PROC [dbo].[hlp_GetMainUserCredentials]
(
@loggedUserId smallint,
@rightToGoBack bit out,
@isAdmin bit out,
@isTrafficManager bit out,
@rightForMinus bit OUT,
@grantorID SMALLINT = NULL
)
AS
BEGIN 
	SET NOCOUNT ON
	SELECT 
		@rightToGoBack = dbo.fn_IsRight2GoBack(@loggedUserId),
		@isAdmin = dbo.f_IsAdmin(@loggedUserId),
		@rightForMinus = dbo.fn_IsRightForMinus(@loggedUserId),
		@isTrafficManager = dbo.f_IsTrafficManager(@loggedUserId)
	IF @grantorID IS NOT NULL 
	BEGIN 
		IF @isAdmin = 0
			SET @isAdmin = dbo.f_IsAdmin(@grantorID)
		IF @rightToGoBack = 0
			SET @rightToGoBack = dbo.fn_IsRight2GoBack(@grantorID)
		IF @rightForMinus = 0
			SET @rightForMinus = dbo.fn_IsRightForMinus(@grantorID)
		IF @isTrafficManager = 0
			SET @isAdmin = dbo.f_IsTrafficManager(@grantorID)
	END 		
END 
