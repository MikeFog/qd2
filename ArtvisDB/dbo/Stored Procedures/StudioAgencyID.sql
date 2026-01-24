
CREATE  PROCEDURE [dbo].[StudioAgencyID]
(
@studioID smallint = NULL,
@agencyID smallint = NULL,
@actionName varchar(32)
)
as
SET NOCOUNT ON
IF @actionName = 'AddItem' BEGIN
	INSERT INTO [StudioAgency](studioID, agencyID)
	VALUES(@studioID, @agencyID)
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [StudioAgency] WHERE studioID = @studioID And agencyID = @agencyID



