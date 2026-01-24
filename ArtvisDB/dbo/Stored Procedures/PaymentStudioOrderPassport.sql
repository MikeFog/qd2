




CREATE      PROC [dbo].[PaymentStudioOrderPassport]
(
@paymentID int = NULL
)
WITH EXECUTE AS OWNER
as
set nocount on 
--EXEC sl_LookupAgency
-- Active Agencies
SELECT 	
	a.[agencyID] as id, 
	a.[name]
FROM 	
	[Agency] a
	LEFT JOIN PaymentStudioOrder so on so.agencyID = a.agencyID and so.paymentID = @paymentID
WHERE	
	dbo.f_IsActiveChildFilter(so.agencyID, a.isActive, 1) = 1
ORDER BY 
	a.name
--EXEC sl_LookupPaymentType
--Active PaymentTypes
SELECT pt.[paymentTypeID] as id, pt.[name] 
FROM 	[PaymentType] pt
		LEFT JOIN PaymentStudioOrder p on p.paymentTypeID = pt.paymentTypeID and p.paymentID = @paymentID
WHERE	dbo.f_IsActiveChildFilter(p.paymentTypeID, pt.isActive, 1) = 1
ORDER BY name

--EXEC Firms







