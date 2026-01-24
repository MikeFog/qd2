
CREATE      PROC [dbo].[ProcedureConfigurationRetrieve]
as
set nocount on 
SELECT DISTINCT
	sp.storedProcedureID,
	sp.name,
	sp.procedureType,
	sp.isTransactionRequired,
	sp.cachingTime,
	mp.[entityID],
	an.name as actionName, 
	mp.[moduleID] AS sys_moduleid, 
	mp.[connectionTimeout] 
FROM 
	[iModuleProcedure] mp
	INNER JOIN iStoredProcedure sp ON sp.storedProcedureID = mp.storedProcedureID
	INNER JOIN iActionName an ON an.actionNameID = mp.actionNameID
	
SELECT 
	[storedProcedureID], [name], [position] 
FROM 
	[iTableAlias]
ORDER BY
	[position]

SELECT DISTINCT
	sp.storedProcedureID,
	sp.name,
	sp.procedureType,
	sp.isTransactionRequired,
	sp.cachingTime,
	30 as connectionTimeout
FROM 
	iStoredProcedure sp

