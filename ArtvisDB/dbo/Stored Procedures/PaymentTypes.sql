



CREATE     PROC dbo.PaymentTypes
(
@ShowActive bit = 0
)
AS
SET NOCOUNT ON
SELECT 
	pt.*
FROM 
	[PaymentType] pt
WHERE
	dbo.f_IsActiveFilter(pt.IsActive, @ShowActive) = 1
ORDER BY 
	pt.[name]




