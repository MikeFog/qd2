CREATE  PROCEDURE [dbo].[FirmBrandID]
(
@firmID smallint,
@brandID smallint,
@actionName varchar(32)
)
as
set nocount on
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [FirmBrand](firmID, brandID)
	VALUES(@firmID, @brandID)

	END
ELSE IF @actionName = 'DetachItem'
	DELETE FROM [FirmBrand] WHERE firmID = @firmID And brandID = @brandID






