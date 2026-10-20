
CREATE PROCEDURE [dbo].[TariffPassport]
(
	@pricelistID smallint = null,
	@tariffId int = null,
	@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @tMo NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'пн');
	DECLARE @tTu NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'вт');
	DECLARE @tWe NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'ср');
	DECLARE @tTh NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'чт');
	DECLARE @tFr NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'пт');
	DECLARE @tSa NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'сб');
	DECLARE @tSu NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'вс');



Declare @nextTariffId int
Set @nextTariffId = dbo.fn_FindTariffIDForChain(@tariffId, @pricelistID)

select 
	(CONVERT(varchar(5), t.[time], 108) 
	+ case t.monday when 1 then @tMo else '' end 
	+ case t.tuesday when 1 then @tTu else '' end 
	+ case t.wednesday when 1 then @tWe else '' end 
	+ case t.thursday when 1 then @tTh else '' end 
	+ case t.friday when 1 then @tFr else '' end 
	+ case t.saturday when 1 then @tSa else '' end 
	+ case t.sunday when 1 then @tSu else '' end 
	+ case when t.pricelistID = @pricelistID then '' else ' (' + mm.[name] + ')' end) as name, 
	t.tariffID as id 
from Tariff t	
	inner join Pricelist pl on t.pricelistID = pl.pricelistID
	inner join vMassmedia mm on pl.massmediaID = mm.massmediaID
where t.pricelistID = @pricelistID
	And t.tariffID = @nextTariffId 
		

Select blockTypeId as Id, [name] + ' (' + code + ')' as name, blockTypeId From BlockType
END

