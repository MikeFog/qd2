CREATE          PROC [dbo].[ModuleList]
(
@massmediaID smallint = NULL,
@moduleId smallint = NULL,
@theDate datetime = NULL
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
IF @theDate IS NULL
	SELECT 
		m.[moduleID],  
		m.[massmediaID],
		m.[name],
		m.[path],
		mm.[name] as massmediaName,
		mm.groupName
	FROM 
		[Module] m
		inner join vMassmedia mm on m.massmediaID = mm.massmediaID
	WHERE
		m.[massmediaID] = Coalesce(@massmediaID, m.[massmediaID]) And
		m.[moduleID] = Coalesce(@moduleId, m.[moduleId])
	ORDER BY
		m.[name]
ELSE
	SELECT 
		m.[moduleID],  
		m.[massmediaID],
		m.[name],
		m.[path],
		mm.[name] as massmediaName,
		mm.groupName,
		mpl.startDate as closestPriceListStartDate
	FROM 
		[Module] m
		INNER JOIN (
			SELECT TOP 1 WITH TIES moduleID, finishDate, startDate
			FROM ModulePriceList
			WHERE @theDate <= finishDate
			ORDER BY ROW_NUMBER() OVER (PARTITION BY moduleID ORDER BY finishDate ASC)
		) mpl ON mpl.moduleID = m.moduleID
		inner join vMassmedia mm on m.massmediaID = mm.massmediaID
	WHERE
		m.[massmediaID] = Coalesce(@massmediaID, m.[massmediaID]) And
		m.[moduleID] = Coalesce(@moduleId, m.[moduleId])
	ORDER BY
		m.[name]










