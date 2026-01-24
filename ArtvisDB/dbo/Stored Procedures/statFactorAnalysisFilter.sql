-- =============================================
-- Author:		Denis Gladkikh
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[statFactorAnalysisFilter]
(
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
BEGIN
	
	-- Massmedia
	exec massmediaList @loggedUserID = @loggedUserID, @ShowActive = 1

	-- PaymentType
	exec sl_LookupPaymentType

	-- CompanyType
	exec sl_LookupCampaignType

	--Roller types
	SELECT [rolTypeID] as id, [name] FROM [iRolType] ORDER BY [name]

	--Massmedia Group
	select mmg.massmediaGroupID as id, mmg.name from MassmediaGroup mmg

	exec UserListByRights	@loggedUserID = @loggedUserID
END
