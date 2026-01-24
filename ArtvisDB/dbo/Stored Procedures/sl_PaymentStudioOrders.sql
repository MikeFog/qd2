
CREATE  PROC [dbo].[sl_PaymentStudioOrders]
AS
SET NOCOUNT ON
SELECT 
	p.[agencyID], p.[firmID], p.[isEnabled], p.[paymentDate], p.[paymentID], p.[paymentTypeID], p.[summa],
	'Платёж от фирмы ''' + f.name + '''' as name,
	f.name AS firmName,
	a.name as agencyName,
	pt.name as paymentTypeName,
	u.LastName + Space(1) + u.firstName as userName,
	CASE 
		WHEN SUM(pa.summa) > 0 THEN	SUM(pa.summa)
		ELSE 0
	END AS consumed
	,p.summa - CASE 
		WHEN SUM(pa.summa) > 0 THEN	(SUM(pa.summa)) 
		ELSE 0
	END AS remainder
FROM 
	#PaymentStudioOrder p2
	INNER JOIN [PaymentStudioOrder] p ON p.paymentID = p2.paymentID
	INNER JOIN firm f ON f.firmID = p.firmID
	INNER JOIN agency a ON a.agencyID = p.agencyID
	INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
	INNER JOIN [user] u ON u.userID = p.userID
	LEFT JOIN [PaymentStudioOrderAction] pa ON p.[paymentID] = pa.[paymentID]
GROUP BY 
	f.name, a.name, pt.name, u.LastName, u.firstName, p.[agencyID], p.[firmID], p.[isEnabled], p.[paymentDate], p.[paymentID], p.[paymentTypeID], p.[userID], p.[summa]
ORDER BY 
	p.paymentDate desc

