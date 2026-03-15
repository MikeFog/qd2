
CREATE      PROC [dbo].[as_relationScenarios]
as
set nocount on
SELECT 
    s.name AS "@name",
    s.startingEntityID AS "@startingEntityID",
    s.filter AS "@filter", -- Esto crea el elemento <filter> dentro de <scenario>
    (
        SELECT 
            r.parentEntityID AS "@parentEntityID",
            r.childEntityID AS "@childEntityID",
            r.selector AS "@selector",
            r.isChildNodeExpandable AS "@isChildNodeExpandable"
        FROM iEntityRelation r
        WHERE r.relationScenarioID = s.relationScenarioID
        FOR XML PATH('relation'), TYPE
    )
FROM iRelationScenario s
ORDER BY s.name
FOR XML PATH('scenario')