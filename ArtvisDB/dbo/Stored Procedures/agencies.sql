CREATE              PROC [dbo].[agencies]
(
@agencyID smallint = null,
@ShowActive bit = 1,
@showUsed bit = 0
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

CREATE TABLE #agency (agencyID smallint)

if @showUsed = 1
 insert into #agency ( agencyID )
		select distinct c.agencyID from dbo.Campaign c
else
	INSERT INTO
		#agency(agencyID)
	SELECT distinct
		ag.agencyID
	FROM
		[Agency] ag
	WHERE
		ag.agencyID = COALESCE(@agencyID, ag.agencyID)
		and dbo.f_IsActiveChildFilter(@agencyID, ag.isActive, @ShowActive) = 1

EXEC sl_agencies
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[agencies] TO PUBLIC
    AS [dbo];

