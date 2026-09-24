

CREATE   PROC dbo.DiscountValues
(
@discountReleaseID smallint = Null,
@discountValueID smallint = Null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tDiscountForSumsOver NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Скидка для сумм более ')
DECLARE @tRub NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'р.')
SELECT 
	[discountValueID], 
	[discountReleaseID], 
	[summa], 
	[discount],
	@tDiscountForSumsOver + LTrim(Str([summa])) + @tRub as name
FROM 
	[DiscountValue]
WHERE
	[discountReleaseID] = Coalesce(@discountReleaseID, [discountReleaseID])
	AND [discountValueID] = Coalesce(@discountValueID, [discountValueID])
ORDER BY
	summa DESC
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DiscountValues] TO PUBLIC
    AS [dbo];

