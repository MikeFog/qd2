CREATE     PROC [dbo].[statBalanceFilter]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

-- firms
EXEC Firms

-- PaymentType
exec sl_LookupPaymentType

-- Manager
exec [UserListByRights] @loggedUserID = @loggedUserID
