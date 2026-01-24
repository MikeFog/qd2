CREATE              PROC [dbo].[agencies]
(
--@needStudioID smallint = null,
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
		/*
		union 
		select distinct o.agencyID from dbo.StudioOrder o
		*/
else
	INSERT INTO 
		#agency(agencyID)
	SELECT distinct
		ag.agencyID 
	FROM 
		[Agency] ag
		--LEFT JOIN StudioAgency sa on sa.agencyID = ag.[agencyID]
	WHERE 	
		ag.agencyID = COALESCE(@agencyID, ag.agencyID)
		and dbo.f_IsActiveChildFilter(@agencyID, ag.isActive, @ShowActive) = 1
		--and (@needStudioID is null or sa.studioID = @needStudioID)

EXEC sl_agencies
