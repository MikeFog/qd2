
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

	update tw 
	set tw.duration = @newDuration
	from TariffWindow tw 
	where
		tw.massmediaID = @massmediaId
		And tw.[windowDateOriginal] >= @startDate
		And tw.[windowDateOriginal] <= @finishDate  
end
