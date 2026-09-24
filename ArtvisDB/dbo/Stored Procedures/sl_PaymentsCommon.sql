

CREATE PROC [dbo].[sl_PaymentsCommon] (@languageCode VARCHAR(10) = 'ru') -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
AS
SET NOCOUNT ON
DECLARE @tFirmPayment NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Платёж от фирмы ''');
SELECT
	p.*,
	@tFirmPayment + f.name + '''' as name,
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



