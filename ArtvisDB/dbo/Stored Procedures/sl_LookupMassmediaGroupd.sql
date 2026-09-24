
CREATE PROC [dbo].[sl_LookupMassmediaGroupd] (@languageCode VARCHAR(10) = 'ru') -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
as
SET NOCOUNT ON
DECLARE @tShowAll NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Показать все');
select 0 as id, @tShowAll as [name]
union
SELECT [massmediaGroupID] as id, name FROM [dbo].[MassmediaGroup]

