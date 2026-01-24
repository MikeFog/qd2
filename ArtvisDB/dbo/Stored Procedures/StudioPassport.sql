
CREATE        PROC [dbo].[StudioPassport]
(
@studioID smallint = Null
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
exec banks

SELECT 
	ag.*,
	bn.name as bankName,
	Cast(
		CASE
			WHEN sa.agencyID IS NULL THEN 0
			ELSE	1
		END as Bit) AS isObjectSelected
FROM 
	[Agency] ag
	LEFT JOIN bank bn ON bn.bankID = ag.bankID
	LEFT JOIN StudioAgency sa ON sa.agencyID = ag.agencyID
		AND sa.studioID = @studioID
WHERE
	(ag.[IsActive] = 1 or sa.agencyID Is Not Null)
ORDER BY
	isObjectSelected desc, ag.[name]








