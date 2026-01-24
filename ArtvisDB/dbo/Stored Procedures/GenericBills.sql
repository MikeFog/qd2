

Create  PROC dbo.GenericBills
(
@actionID int,
@agencyID int
)
AS
SET NOCOUNT ON
SELECT
	bill.*
FROM
	[Bill] bill
WHERE
	bill.actionID = @actionID And
	bill.agencyID = @agencyID


