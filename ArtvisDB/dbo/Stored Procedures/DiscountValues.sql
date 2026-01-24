

CREATE   PROC dbo.DiscountValues
(
@discountReleaseID smallint = Null,
@discountValueID smallint = Null
)
AS
SET NOCOUNT ON
SELECT 
	[discountValueID], 
	[discountReleaseID], 
	[summa], 
	[discount],
	'Скидка для сумм более ' + LTrim(Str([summa])) + 'р.' as name
FROM 
	[DiscountValue]
WHERE
	[discountReleaseID] = Coalesce(@discountReleaseID, [discountReleaseID])
	AND [discountValueID] = Coalesce(@discountValueID, [discountValueID])
ORDER BY
	summa DESC


