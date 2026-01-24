CREATE PROC [dbo].[ActionsFilter]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
-- 1. payment_type
Exec PaymentTypes

-- 2. campaign_type
Exec CampaignTypes

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

-- 3. group managers
--exec GroupListByRights @loggedUserID, @isRightToViewForeignActions, @isRightToViewGroupActions
Select * From MassmediaGroup order by name

-- 4. users
exec UserListByRights @loggedUserID, @isRightToViewForeignActions, @isRightToViewGroupActions
