

CREATE   PROC dbo.rpt_StudioOrderAct
(
@actionID int,
@agencyID smallint
)
AS
SET NOCOUNT ON

SELECT
	rs.name,
	SUM(so.finalPrice) as price,
	CAST('' as VARCHAR(1024)) as priceString
FROM 
	StudioOrder so
	INNER JOIN RolStyle rs ON rs.rolstyleID = so.rolstyleID
WHERE
	so.actionID = @actionID	
	AND so.agencyID = @agencyID
GROUP BY 
	rs.name

