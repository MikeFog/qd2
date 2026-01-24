CREATE  PROC [dbo].[statVolumeOfRealizationForRollersFilter]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

-- PaymentType
exec sl_LookupPaymentType

-- Manager
exec UserListByRights
	@loggedUserID = @loggedUserID, --  smallint
	@forStudioOrders = 1 --  bit

-- Studio
exec Studios

-- roltype
exec sl_LookupRolType
