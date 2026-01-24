
CREATE PROC [dbo].[sl_LookupMassmediaGroupd]
as
SET NOCOUNT ON
select 0 as id, 'Показать все' as [name]
union
SELECT [massmediaGroupID] as id, name FROM [dbo].[MassmediaGroup]

