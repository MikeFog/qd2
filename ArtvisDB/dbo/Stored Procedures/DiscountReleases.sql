

CREATE   PROC [dbo].[DiscountReleases]
(
@massmediaID smallint = NULL,
@discountReleaseID smallint = NULL
)
as
set nocount on
SELECT 
	dr.*,
	'Скидки от ' + Convert(varchar(10), dr.[startDate], 104) + CASE WHEN dr.[finishDate] IS NOT NULL THEN ' до ' + Convert(varchar(10), dr.[finishDate], 104) ELSE '' END as name
FROM 
	[DiscountRelease] dr
WHERE
	dr.[massmediaID] = Coalesce(@massmediaID, dr.[massmediaID])
	AND dr.[discountReleaseID] = Coalesce(@discountReleaseID, dr.[discountReleaseID])


