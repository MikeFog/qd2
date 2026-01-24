CREATE    PROC [dbo].[StudioOrderActionsFilter]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

-- 1. payment_type
SELECT [paymentTypeID] as id, [name] FROM [PaymentType] ORDER BY [name]

-- 2. roller_type
SELECT [rolTypeID] as id, [name] FROM [iRolType] ORDER BY [name]

-- 3. firm
EXEC FirmWithOrder

-- 4. user
exec UserListByRights @loggedUserID, @isRightToViewForeignActions, @isRightToViewGroupActions, @forStudioOrders = 1

-- 5. studio
SELECT [studioID] as id, [name] 
FROM [Studio]
	join Agency on [Studio].StudioId = Agency.AgencyId
ORDER BY [name]

-- 6. agency
EXEC dbo.Agencies @ShowActive = 1

-- 7. actionStatus
SELECT [statusID] AS id, [name] FROM [iStudioOrderActionStatus]

-- 8. group managers
exec GroupListByRights @loggedUserID, @isRightToViewForeignActions, @isRightToViewGroupActions, @forStudioOrders = 1
