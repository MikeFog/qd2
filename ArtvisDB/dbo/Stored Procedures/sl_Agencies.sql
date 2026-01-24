





CREATE       PROCEDURE [dbo].[sl_Agencies]
as
SET NOCOUNT ON
SELECT 
	ag.*,
	ag.agencyID as id,
	bn.name as bankName
FROM 
	[Agency] ag
	INNER JOIN #Agency ta ON ta.agencyID = ag.agencyID
	LEFT JOIN bank bn ON bn.bankID = ag.bankID
	--LEFT JOIN StudioAgency sa on sa.agencyID = ag.[agencyID]
ORDER BY
	ag.[name]







