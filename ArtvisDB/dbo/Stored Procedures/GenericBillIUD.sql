CREATE  PROCEDURE [dbo].[GenericBillIUD]
(
@billNo int,
@billDate datetime,
@actionID int = NULL,
@agencyID smallint = NULL,
@actionName varchar(32)
)
as
SET NOCOUNT on
IF @actionName In ('AddItem', 'UpdateItem')  
begin
	delete from Bill where actionID = @actionID and agencyID = @agencyID

	INSERT INTO [Bill](billNo, billDate, actionID, agencyID)
	VALUES(@billNo, @billDate, @actionID, @agencyID)
end
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [Bill] WHERE billNo = @billNo And billDate = @billDate

