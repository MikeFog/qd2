


CREATE   procedure [dbo].[ActionAgencies] 
(
@actionId INT
) 
WITH EXECUTE AS OWNER
AS
set nocount on 
CREATE TABLE #agency (agencyID smallint)

INSERT INTO [#agency] ([agencyID]) 
SELECT DISTINCT 
	c.[agencyID]
FROM
	[Campaign] c
WHERE 
	c.[actionID] = @actionId
	
EXEC sl_agencies	

/*
select 	distinct a.*
from	Campaign c
		join AgencyMassMedia m
			on c.massmediaId = m.massmediaId
		join Agency a
			on a.agencyId = m.agencyid
where
		c.actionId = @actionId
*/



