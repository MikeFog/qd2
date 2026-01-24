
CREATE PROCEDURE [dbo].[TariffPassport]
(
	@pricelistID smallint = null,
	@tariffId int = null 
)
AS
BEGIN
	SET NOCOUNT ON;



Declare @nextTariffId int
Set @nextTariffId = dbo.fn_FindTariffIDForChain(@tariffId, @pricelistID)

select 
	(dbo.fn_GetTimeString(pl.broadcastStart, t.[time]) 
	+ case t.monday when 1 then ',пн' else '' end 
	+ case t.wednesday when 1 then ',вт' else '' end 
	+ case t.tuesday when 1 then ',ср' else '' end 
	+ case t.thursday when 1 then ',чт' else '' end 
	+ case t.friday when 1 then ',пт' else '' end 
	+ case t.saturday when 1 then ',сб' else '' end 
	+ case t.sunday when 1 then ',вс' else '' end 
	+ case when t.pricelistID = @pricelistID then '' else ' (' + mm.[name] + ')' end) as name, 
	t.tariffID as id 
from Tariff t	
	inner join Pricelist pl on t.pricelistID = pl.pricelistID
	inner join vMassmedia mm on pl.massmediaID = mm.massmediaID
where t.pricelistID = @pricelistID
	And t.tariffID = @nextTariffId 
		

Select blockTypeId as Id, [name] + ' (' + code + ')' as name, blockTypeId From BlockType
END

