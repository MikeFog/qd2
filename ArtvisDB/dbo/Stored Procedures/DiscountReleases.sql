

CREATE   PROC [dbo].[DiscountReleases]
(
@massmediaID smallint = NULL,
@discountReleaseID smallint = NULL,
@hideDiscountsInThePast bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
as
set nocount on
DECLARE @tDiscountsFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Скидки от ');
DECLARE @tTo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' до ');
SELECT 
	dr.*,
	@tDiscountsFrom + Convert(varchar(10), dr.[startDate], 104) + CASE WHEN dr.[finishDate] IS NOT NULL THEN @tTo + Convert(varchar(10), dr.[finishDate], 104) ELSE '' END as name
FROM 
	[DiscountRelease] dr
WHERE
	dr.[massmediaID] = Coalesce(@massmediaID, dr.[massmediaID])
	AND dr.[discountReleaseID] = Coalesce(@discountReleaseID, dr.[discountReleaseID])
	And (@hideDiscountsInThePast = 0 or dr.finishDate >= CAST(GETDATE() AS date))
ORDER BY
	dr.startDate DESC
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DiscountReleases] TO PUBLIC
    AS [dbo];

