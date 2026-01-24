CREATE  PROC [dbo].[BalanceStudioOrderFilter]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
set nocount on
Exec dbo.LookupUsedAgency
Exec Firms
Exec sl_LookupPaymentType
Exec UserListByRights @loggedUserID, @forStudioOrders = 1
