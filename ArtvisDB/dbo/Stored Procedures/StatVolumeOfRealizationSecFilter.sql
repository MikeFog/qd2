-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 24.11.2008
-- Description:	╘шы№ЄЁ: ╬с·хь ЁхрышчрЎшш яю ёхъєэфрь
-- =============================================
CREATE procedure [dbo].[StatVolumeOfRealizationSecFilter] 
(
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

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
	
	exec UserListByRights @loggedUserID = @loggedUserID

end
