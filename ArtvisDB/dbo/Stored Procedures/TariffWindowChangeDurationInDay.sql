
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

	-- Продолжительность не может быть больше полной (нулевая полная продолжительность означает «не задана»)
	if exists(select *
		from TariffWindow tw
		where tw.massmediaID = @massmediaId
			And tw.[windowDateOriginal] >= @startDate
			And tw.[windowDateOriginal] <= @finishDate
			And tw.duration_total > 0 And @newDuration > tw.duration_total)
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
end
