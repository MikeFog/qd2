


CREATE FUNCTION [dbo].[fn_FindTariffIDForChain]
(
@tariffID int,
@pricelistID int
)
RETURNS INT
BEGIN
	Declare 
		@monday bit,
		@tuesday bit,
		@wednesday bit,
		@thursday bit,
		@friday bit,
		@saturday bit,
		@sunday bit,
		@time smalldatetime,
		@broadcastStart smalldatetime,
		@hour int

	Select 		
		@monday = monday,
		@tuesday = tuesday,
		@wednesday = [wednesday],
		@thursday = thursday,
		@friday = friday,
		@saturday = saturday,
		@sunday = sunday,
		@time = time
	From 
		Tariff
	Where
		tariffID = @tariffId
	Select @broadcastStart = broadcastStart From [dbo].[Pricelist] Where [pricelistID] = @pricelistID
	Set @hour = DATEPART(hour, @time)
	Set @time = dbo.fn_GetTariffTimesWithBroadcast(@time, @broadcastStart)

	-- Найдём ближайший следующий тариф, где есть совпадение хотя бы по одному "активному" дню недели
	Declare @nextTariffId int
	SELECT 
		TOP 1 @nextTariffId = tariffID 
	FROM 
		Tariff 
	WHERE 
		pricelistID = @pricelistID 
		And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) > @time
		And (
			(DATEPART(hour, time) >= @hour And
			((@monday = monday And @monday = 1)  Or (@tuesday = tuesday And @tuesday = 1) Or (@wednesday = [wednesday] And @wednesday = 1) 
			Or (@thursday = thursday And @thursday = 1) Or (@friday = friday And @friday = 1) Or (@saturday = saturday And @saturday = 1) 
			Or (@sunday = sunday And @sunday = 1))
			) 
			OR
			(
			DATEPART(hour, time) < @hour And
			((@monday = tuesday And @monday = 1)  Or (@tuesday = wednesday And @tuesday = 1) Or (@wednesday = thursday And @wednesday = 1) 
			Or (@thursday = friday And @thursday = 1) Or (@friday = saturday And @friday = 1) Or (@saturday = sunday And @saturday = 1) 
			Or (@sunday = monday And @sunday = 1)))
			)
	ORDER BY 
		dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) ASC

	-- теперь убедимся, что в этом тарифе совпаюадют все дни
	If Exists (
		select 1
		from Tariff t	
			inner join Pricelist pl on t.pricelistID = pl.pricelistID
		where t.pricelistID = @pricelistID
			And t.tariffID = @nextTariffId 
			And @monday = monday And @tuesday = tuesday And	@wednesday = [wednesday] And @thursday = thursday And @friday = friday And @saturday = saturday And	@sunday = sunday
		)
		Return @nextTariffId

	Return NULL
END



