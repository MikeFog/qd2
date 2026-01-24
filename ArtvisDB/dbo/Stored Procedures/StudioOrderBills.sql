
CREATE  PROC dbo.StudioOrderBills
(
@actionID int,
@agencyID int
)
AS
SET NOCOUNT ON
SELECT 
	sob.*
FROM 
	[StudioOrderBill] sob
WHERE
	sob.actionID = @actionID And
	sob.agencyID = @agencyID

