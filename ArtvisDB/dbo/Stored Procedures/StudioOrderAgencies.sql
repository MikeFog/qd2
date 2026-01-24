
CREATE  PROCEDURE [dbo].[StudioOrderAgencies]
(
@actionID int
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

CREATE TABLE #agency (agencyID smallint)

INSERT INTO 
	#agency(agencyID)
SELECT DISTINCT
	so.agencyID FROM StudioOrder so
WHERE 	
	so.actionID = @actionID

EXEC sl_agencies


