
CREATE      PROC [dbo].[as_relationScenarios]
as
set nocount on
SELECT 
	scenario.name,
	scenario.startingEntityID,
	relation.parentEntityID,
	relation.childEntityID,
	relation.selector,
	relation.isChildNodeExpandable
FROM 
	iRelationScenario scenario 
	LEFT JOIN iEntityRelation relation ON scenario.relationScenarioID = relation.relationScenarioID
ORDER BY 
	scenario.name
FOR XML AUTO

