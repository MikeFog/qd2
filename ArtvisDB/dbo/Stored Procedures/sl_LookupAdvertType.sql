
CREATE PROC [dbo].[sl_LookupAdvertType]
as
SET NOCOUNT ON
select AdvertTypeId as Id, name from AdvertType order by name


