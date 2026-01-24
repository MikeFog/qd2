




CREATE    PROCEDURE [dbo].[PaymentTypesLoad]
(
@ShowActive bit = 0
)
WITH EXECUTE AS OWNER
as
set nocount on 
exec PaymentTypes @ShowActive


