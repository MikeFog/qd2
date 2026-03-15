

CREATE   PROC [dbo].[DiscountReleases]
(
@massmediaID smallint = NULL,
@discountReleaseID smallint = NULL,
@hideDiscountsInThePast bit = 0
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
	And (@hideDiscountsInThePast = 0 or dr.finishDate > GETDATE() or dr.finishDate Is Null)
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DiscountReleases] TO PUBLIC
    AS [dbo];

