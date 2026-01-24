-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 30.10.2008
-- Description:	Создание новых сообщений о том что ролики не созданы
-- =============================================
CREATE procedure [dbo].[CreateActionMuteRollersAlerts] 
as 
begin 
	set nocount on;
	SET DATEFIRST 1
	
	declare @today datetime, @weekday tinyint 
	set @today = dbo.ToShortDate(getdate())
	Set @weekday = DatePart(dw, @today)

	declare @actions table (actionID int primary key, userID smallint, adminToo bit)

	insert into @actions 
    select distinct c.actionID, a.userID, max(case when datediff(day, @today, tw.dayActual) <= 1 then 1 else 0 end)
    from Issue i 
		inner join Campaign c on i.campaignID = c.campaignID
		inner join [Action] a on c.actionID = a.actionID and a.isConfirmed = 1
		inner join Roller r on i.rollerID = r.rollerID and r.isMute = 1
		inner join TariffWindow tw on i.actualWindowID = tw.windowId and tw.dayActual >= @today
		left join ActionMuteRollerAlert aa on c.actionID = aa.actionID and aa.alertDate = @today
	where aa.alertDate is null
		and (datediff(day, @today, tw.dayActual) > 0 and 
				(datediff(day, @today, tw.dayActual) <= 3
					or (@weekday = 5 and  datediff(day, @today, tw.dayActual) <= 5 )
					or (@weekday = 6 and  datediff(day, @today, tw.dayActual) <= 4 )))
	group by c.actionID, a.userID
	
	insert into ActionMuteRollerAlert (actionID,alertDate)
	select a.actionID, @today from @actions a
		left join ActionMuteRollerAlert amr on a.actionID = amr.actionID and amr.alertDate = @today
	where amr.actionID is null 
	
	insert into Announcement (dateCreated, isConfirmationRequired,fromUserID,toUserID,[subject]) 
	select getdate(), 0, a.userID, a.userID, 'Акция №' + cast(a.actionID as nvarchar) + ' имеет ролики-пустышки в выпусках, выходящих менее чем через 3 дня.'
	from @actions a 
		
	insert into Announcement (dateCreated, isConfirmationRequired,fromUserID,toUserID,[subject]) 
	select getdate(), 0, 0, a.userID, 'Акция №' + cast(a.actionID as nvarchar) + ' имеет ролики-пустышки в выпусках, выходящих менее чем через 1 день.'
	from @actions a where a.adminToo = 1
	
	delete from ActionMuteRollerAlert where alertDate < @today
end
