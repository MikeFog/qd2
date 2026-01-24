CREATE PROC [dbo].[sl_LookupPaymentType]
as
SET NOCOUNT ON
SELECT paymentTypeID as id, name FROM PaymentType ORDER BY name

