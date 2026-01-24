CREATE     PROC [dbo].[MassmediaAgencies]
(
@massmediaID smallint = null
)
as
SET NOCOUNT on
SELECT 
	a.[agencyID], 
	a.[name],
	a.[isActive]
FROM 
	[Agency] a
	INNER JOIN AgencyMassmedia am ON a.agencyID = am.agencyID
WHERE
	am.massmediaID = @massmediaID
	and a.isActive = 1
