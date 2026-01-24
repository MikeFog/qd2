-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 26.11.2008
-- Description:	Экспорт информации в формат Media Plus
-- =============================================
CREATE procedure [dbo].[ExportMediaPlusFormat] 
(
	@campaignID int
)
as 
begin 
	set nocount on;
	
    select cast(1 as float) - c.managerDiscount as managerDiscount, cast(1 as float) - c.discount as volumeDiscount, cast(1 as float) - a.discount as packDiscount, mm.mediaPlusMassmediaID
    from Campaign c 
		inner join [Action] a on c.actionID = a.actionID 
		inner join MassMedia mm on c.massmediaID = mm.massmediaID
	where c.campaignID = @campaignID
	
	select r.duration as format, 
		left(convert(varchar,tw.dayOriginal,20),10) as date,
		datepart(hh,tw.windowDateOriginal) as [hour], 
		cast(case when datepart(minute, tw.windowDateOriginal) > 30 then 2 else 1 end as int) as hhour,
		i.tariffPrice as tariff
	from Issue i 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
		inner join Roller r on i.rollerID = r.rollerID
	where i.campaignID = @campaignID
end
