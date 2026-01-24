





CREATE      PROC [dbo].[AdvertTypes]
(
@parentID smallint = NULL,
@AdvertTypeId smallint = null
)
as
SET NOCOUNT ON
SELECT 
	*
FROM 
	[AdvertType]
WHERE
	(
	(@AdvertTypeId Is Null and ([parentID] = @parentID OR ([parentID] IS NULL AND @parentID IS NULL)))
	or
	(@AdvertTypeId Is Not Null and AdvertTypeId = @AdvertTypeId)
	)
ORDER BY 
	[name]







