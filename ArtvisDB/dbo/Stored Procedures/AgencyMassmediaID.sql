
CREATE  PROCEDURE dbo.AgencyMassmediaID
(
@agencyID smallint,
@massmediaID smallint,
@actionName varchar(32)
)
AS
SET NOCOUNT ON

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [AgencyMassmedia](agencyID, massmediaID)
	VALUES(@agencyID, @massmediaID)

	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [AgencyMassmedia] WHERE agencyID = @agencyID AND massmediaID = @massmediaID


