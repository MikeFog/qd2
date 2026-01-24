
CREATE  PROCEDURE [dbo].[StudioOrderBillIUD]
(
@billNo int,
@billDate datetime,
@actionID int = NULL,
@agencyID smallint = NULL,
@actionName varchar(32)
)
as
SET NOCOUNT ON
IF @actionName In ('AddItem', 'UpdateItem')  
begin
	delete from StudioOrderBill where actionID = @actionID and agencyID = @agencyID
	
	INSERT INTO [StudioOrderBill](billNo, billDate, actionID, agencyID)
	VALUES(@billNo, @billDate, @actionID, @agencyID)
end
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [StudioOrderBill] WHERE billNo = @billNo And billDate = @billDate

