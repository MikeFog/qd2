
CREATE  PROC [dbo].[StudioOrderActionPriceForAgency]
(
@actionID int,
@agencyID smallint,
@summa money out
)
as
SET NOCOUNT ON
SELECT @summa = Sum([finalPrice]) FROM [StudioOrder]
WHERE actionID = @actionID And agencyID = @agencyID


