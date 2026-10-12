
CREATE   procedure [dbo].[TariffWindowChangeDurationInDay] 
(
	@newDuration int,
	@startDate datetime,
	@finishDate datetime,
	@massmediaId int
)
as 
begin 
	set nocount on;

	-- Меняется только продолжительность, полная у каждого окна своя. Окна профилактики передатчика (0/0 - так
	-- заказчик её помечает) пропускаются: массовой правкой профилактика не снимается (решение Миши 12.10.2026)
	-- Продолжительность не может быть больше полной (без исключения «0 = не задана»)
	if exists(select *
		from TariffWindow tw
		where tw.massmediaID = @massmediaId
			And tw.[windowDateOriginal] >= @startDate
			And tw.[windowDateOriginal] <= @finishDate
			And Not (tw.duration = 0 And tw.duration_total = 0)
			And @newDuration > tw.duration_total)
	begin
		raiserror('DurationExceedsTotal', 16, 1)
		return
	end

	update tw
	set tw.duration = @newDuration
	from TariffWindow tw 
	where
		tw.massmediaID = @massmediaId
		And tw.[windowDateOriginal] >= @startDate
		And tw.[windowDateOriginal] <= @finishDate  
		And Not (tw.duration = 0 And tw.duration_total = 0)
end
