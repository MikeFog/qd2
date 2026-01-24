

CREATE  PROCEDURE [dbo].[GroupRightsAssignedRetrieve]
	@groupID INT
AS
BEGIN
	SET NOCOUNT ON;

	SELECT 
		ea.entityActionID as ID,
		ea.entityActionID as entityActionID,
		e.name as entityName,
		ea.[alias] AS ActionName,
		e.name + ' - ' + ea.alias as [NAME],
		Cast(
			CASE
				WHEN gr.groupID is null THEN 0
				ELSE 1
			END as Bit)
		 as isObjectSelected,
		@groupID as groupID
	FROM 
		[iEntityAction] ea
		INNER JOIN iEntity e ON e.entityID = ea.entityID
		LEFT JOIN GroupRight gr ON gr.entityActionID = ea.entityActionID
			AND gr.groupID = @groupID
	WHERE
		e.isGrantingAllowed = 1 And
		ea.isGrantingAllowed = 1 And
		NOT ea.name IS null
		and not gr.groupID is null
		and e.isObsolete = 0
	ORDER BY
		e.name,
		ea.[alias]
END

