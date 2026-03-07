




CREATE    PROCEDURE [dbo].[PaymentTypesLoad]
(
@ShowActive bit = 0
)
WITH EXECUTE AS OWNER
as
set nocount on 
exec PaymentTypes @ShowActive
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[PaymentTypesLoad] TO PUBLIC
    AS [dbo];

