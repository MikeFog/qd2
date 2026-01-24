

CREATE PROC [dbo].[sl_PaymentsCommon]
AS
SET NOCOUNT ON
SELECT
	p.*,
	'Платёж от фирмы ''' + f.name + '''' as name,
	f.name AS firmName,
	hc.name as headCompanyName,
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
	#PaymentsCommon p2
	INNER JOIN [Payment] p ON p.paymentID = p2.paymentID
	INNER JOIN firm f ON f.firmID = p.firmID
	Inner Join HeadCompany hc on hc.headCompanyID = f.headCompanyID
	INNER JOIN agency a ON a.agencyID = p.agencyID
	INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
	INNER JOIN [user] u ON u.userID = p.userID
	LEFT JOIN [PaymentAction] pa ON pa.paymentID = p.paymentID
GROUP BY 
	f.name, a.name, pt.name, u.LastName, u.firstName, p.[agencyID], p.[firmID], p.[isEnabled], 
	p.[paymentDate], p.[paymentID], p.[paymentTypeID], p.[userID], p.[summa], hc.name
ORDER BY
	p.paymentDate DESC



