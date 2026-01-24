



CREATE     PROC [dbo].[FirmBalanceStudioOrderOnLoad]
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

-- 1. firms
Create Table #firm(firmID int)
Insert Into #firm(firmID)
(Select Distinct firmID From StudioOrderAction
UNION
SELECT DISTINCT firmID FROM [PaymentStudioOrder])
Exec sl_Firms

-- 2. Managers
CREATE TABLE #User(userID smallint)
Insert Into #User(userID)
(Select Distinct userID From StudioOrderAction
UNION 
SELECT DISTINCT userID FROM [PaymentStudioOrder])
Exec sl_Users null

-- 3. Payment types
((SELECT DISTINCT
	pt.*
FROM 
	[PaymentType] pt
	INNER JOIN StudioOrder so ON so.paymentTypeID = pt.paymentTypeID)
UNION
(SELECT DISTINCT
	pt.*
FROM 
	[PaymentType] pt
	INNER JOIN [PaymentStudioOrder] so ON so.paymentTypeID = pt.paymentTypeID))
ORDER BY pt.name

-- 4. Agencies
CREATE TABLE #Agency(agencyID smallint)
Insert Into #Agency(agencyID)
(Select Distinct agencyID From StudioOrder
UNION
Select Distinct agencyID From PaymentStudioOrder)
Exec sl_Agencies



