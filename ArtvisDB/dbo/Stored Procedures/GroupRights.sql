
CREATE     PROC [dbo].[GroupRights]
(
@groupID smallint
)
as
SET NOCOUNT on

SELECT 
	ea.entityActionID,
	e.name as entityName,
	IsNull(ea2.alias+ '\','') + ea.[alias] as alias,
	Cast(
		CASE
			WHEN gr.groupID is null THEN 0
			ELSE 1
		END as Bit)
	 as isObjectSelected
FROM 
	[iEntityAction] ea
	left join iEntityAction ea2 on ea.parentID = ea2.entityActionID
	INNER JOIN iEntity e ON e.entityID = ea.entityID
	LEFT JOIN GroupRight gr ON gr.entityActionID = ea.entityActionID
		AND gr.groupID = @groupID
WHERE
	e.isGrantingAllowed = 1 And
	ea.isGrantingAllowed = 1 And
	NOT ea.name IS NULL
	And e.isObsolete = 0
ORDER BY
	e.name,
	alias

