





CREATE      PROC [dbo].[PaymentCommonPassport]
(
@paymentID int = NULL
)
WITH EXECUTE AS OWNER
as
set nocount on 
-- agency
SELECT 	
	a.[agencyID],
	a.[agencyID] as id, 
	a.[name],
	a.isActive
FROM 	
	[Agency] a
	LEFT JOIN Payment p on p.agencyID = a.agencyID and p.paymentID = @paymentID
WHERE	
	dbo.f_IsActiveChildFilter(p.agencyID, a.isActive, 1) = 1
ORDER BY 
	a.name

--EXEC sl_LookupPaymentType
--Active PaymentTypes
SELECT pt.[paymentTypeID] as id, pt.[name] 
FROM 	[PaymentType] pt
		LEFT JOIN Payment p on p.paymentTypeID = pt.paymentTypeID and p.paymentID = @paymentID
WHERE	dbo.f_IsActiveChildFilter(p.paymentTypeID, pt.isActive, 1) = 1
ORDER BY name

--EXEC Firms








