CREATE     PROC [dbo].[NewCampaignPassport]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on
-- Table #1 campaign_type
SELECT [campaignTypeID] as id, [name] FROM [iCampaignType]

-- Table #2 payment_type
SELECT [paymentTypeID] as id, [name], [isHidden] FROM [PaymentType] where isActive = 1

-- Table #3 massmedia
exec massmediaList @loggedUserID = @loggedUserID, @ShowActive = 1, @checkCanAdd = 1

-- Table #4 massmedia Audio
--exec massmediaList @loggedUserID = @loggedUserID, @ShowActive = 1, @checkCanAdd = 1, @rollerTypeID = 1

-- RolTypes
--select rolTypeID as id, name from dbo.iRolType
-- Table #4 massmedia group
exec sl_LookupMassmediaGroupd
