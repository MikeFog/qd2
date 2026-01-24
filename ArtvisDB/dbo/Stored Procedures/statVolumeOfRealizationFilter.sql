CREATE    PROC [dbo].[statVolumeOfRealizationFilter]
(
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
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

exec UserListByRights
	@loggedUserID = @loggedUserID
