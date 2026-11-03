





CREATE       PROCEDURE [dbo].[sl_Agencies]
as
SET NOCOUNT ON
SELECT 
	ag.*,
	ag.agencyID as id,
	bn.name as bankName,
	ab.account
FROM
	[Agency] ag
	INNER JOIN #Agency ta ON ta.agencyID = ag.agencyID
	LEFT JOIN bank bn ON bn.bankID = ag.bankID
	LEFT JOIN AgencyBank ab ON ab.agencyID = ag.agencyID AND ab.bankID = ag.bankID
ORDER BY
	ag.[name]







