CREATE  PROC [dbo].[BalanceFilter]
(
	@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
set nocount on
Exec LookupUsedAgency
Exec Firms
Exec sl_LookupPaymentType
Exec UserListByRights @loggedUserID
