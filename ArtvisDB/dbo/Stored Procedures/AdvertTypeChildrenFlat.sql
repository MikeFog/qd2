





CREATE      PROC [dbo].[AdvertTypeChildrenFlat]
(
@AdvertTypeId smallint = null,
@parentID smallint = NULL
)
as
SET NOCOUNT ON
SELECT 
	a2.*,
	a1.name as groupName
FROM 
	[AdvertType] a2
	INNER JOIN [AdvertType] a1 On a2.parentID = a1.advertTypeID
WHERE 
	a2.parentID = ISNULL(@parentID, a2.parentID)
	And a2.AdvertTypeId = IsNull(@AdvertTypeId, a2.AdvertTypeId)
ORDER BY 
	a1.[name],
	a2.name
