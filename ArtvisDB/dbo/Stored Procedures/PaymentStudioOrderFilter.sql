CREATE  PROC [dbo].[PaymentStudioOrderFilter]
WITH EXECUTE AS OWNER
as
set nocount on 
Exec dbo.LookupUsedAgency
EXEC sl_LookupPaymentType
EXEC Firms
