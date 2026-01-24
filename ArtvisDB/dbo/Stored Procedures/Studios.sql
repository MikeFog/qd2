







CREATE         PROC [dbo].[Studios]
(
@studioID smallint = NULL,
@ShowActive bit = 0
)
as
SET NOCOUNT ON
SELECT 
	s.*,
	b.name as bankName
FROM 
	[vStudio] s
	LEFT JOIN Bank b ON s.bankID = b.BankID
WHERE
	s.[studioID] = COALESCE(@studioID, s.[studioID])
	and dbo.f_IsActiveChildFilter(@studioID, s.isActive, @ShowActive) = 1












