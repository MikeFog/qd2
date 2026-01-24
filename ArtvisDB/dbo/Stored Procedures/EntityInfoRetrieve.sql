CREATE  PROCEDURE [dbo].[EntityInfoRetrieve]
(
@userID smallint,
@entityID smallint = Null,
@entityName varchar(32) = Null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

IF @entityID IS null and @entityName is not null 
	SELECT @entityID = entityID FROM iEntity WHERE codeName = @entityName	

-- 1.
SELECT
	ent.*,
	ent.codeName as entityName
FROM	
	iEntity ent
WHERE
	ent.entityID = coalesce(@entityID, ent.entityID)

-- 2.
SELECT 
	ent.entityID,
	ent.codeName  as entityName,
	ea.name, 
	ea.alias,
	ea.selector,
	ea.dataType
FROM	
	iEntityAttribute ea
	INNER JOIN iEntity ent ON ea.entityID = ent.entityID
WHERE
	ent.entityID = coalesce(@entityID, ent.entityID)
ORDER BY
	ent.entityID, ea.ordinal_position

-- 3.
SELECT
	ent.entityID,
	ent.codeName  as entityName,
	COALESCE(ea.name, SPACE(1)) as name,
	ea.alias,
	ea.parentID,
	isHidden,
	ea.[entityActionID],
	ea.imgResourceName,
	dbo.IsActionEnabled(@userID, ea.entityActionID) as isActionEnabled
FROM 
	iEntityAction ea
	INNER JOIN iEntity ent ON ea.entityID = ent.entityID
WHERE
	ent.entityID = coalesce(@entityID, ent.entityID)
ORDER BY
	ent.entityID, ea.ordinal_position

-- 4.
SELECT 	
	cols.table_name, cols.column_name, 
	cols.column_default, 
	cols.is_nullable, cols.is_nullable, 
	IsNull(cols.domain_name, cols.data_type)  as data_type,
	cols.CHARACTER_MAXIMUM_LENGTH ,
	ent.entityID,
	ent.codeName  as entityName
FROM 		
	INFORMATION_SCHEMA.COLUMNS cols
	INNER JOIN iEntity ent ON cols.table_name = ent.tableName
WHERE
	ent.entityID = coalesce(@entityID, ent.entityID)
