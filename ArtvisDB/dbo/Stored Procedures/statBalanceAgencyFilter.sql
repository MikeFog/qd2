




CREATE PROC [dbo].[statBalanceAgencyFilter]
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

-- PaymentType
exec sl_LookupPaymentType


