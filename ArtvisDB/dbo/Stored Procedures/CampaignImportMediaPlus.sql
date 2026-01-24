-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 11.11.2008
-- Description:	Импорт кампании, для импорта необходима таблица #issues (duration smallint, date datetime, hour tinyint, isfirsthalf bit)
-- =============================================
CREATE procedure [dbo].[CampaignImportMediaPlus] 
(
	@actionID int,
	@massmediaID smallint,
	@agencyID smallint,
	@paymentTypeID smallint,
	@loggedUserID smallint,
	@rolTypeID smallint,
	@isConfirmed bit,
	@deadline smalldatetime,
	@extraChargeFirstRoller tinyint, 
	@extraChargeSecondRoller tinyint, 
	@extraChargeLastRoller tinyint
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;
    if (object_id('tempdb..#issues') is null)
    begin 
		print('Для работы процедуры необходима таблица #issues (duration smallint, date datetime, hour tinyint, isfirsthalf bit)')
		print('create table #issues (duration smallint, date datetime, hour tinyint, isfirsthalf bit)')
		return 
	end 
	
	------------------------------------------------------------------------
	-- Импорт обычной рекламной кампании
	
	declare @campaignID int
	
	select @campaignID = c.campaignID
	from Campaign c 
	where c.actionID = @actionID 
		and c.massmediaID = @massmediaID
		and c.agencyID = @agencyID
		and c.paymentTypeID = @paymentTypeID
		and c.campaignTypeID = 1 -- Обычная рекламная кампания
		
	if @campaignID is null 
	begin 
		exec CampaignIUD
			@campaignID = @campaignID output,
			@actionID = @actionID, 
			@campaignTypeID = 1,
			@massmediaID = @massmediaID,
			@paymentTypeID = @paymentTypeID,
			@agencyID = @agencyID,
			@loggedUserId = @loggedUserID, 
			@actionName = 'AddItem',
			@needShow = 0
	end 
	
	if @campaignID is null 
		return 
	
	declare @rightToGoBack bit, @isAdmin bit, @rightForMinus bit
	
	exec hlp_GetMainUserCredentials
		@loggedUserId, @rightToGoBack out, @isAdmin out, @rightForMinus out
	
	declare @duration smallint, @date datetime, @hour tinyint, @isfirsthalf bit, 
		@id int, @rollerID int, @windowID int, @tomorrow datetime, @issuePrice money,
		@tariffWindowPrice money, @activationdate datetime, @issueID int 
		
	if @isConfirmed = 1
		set @activationdate = getdate()
	else 
		set @activationdate = null
		
	set @tomorrow = dateadd(day, 1, Convert(datetime, Convert(varchar(8),getdate(), 112), 112))
	
	declare @log table(id int, duration int, date datetime, [hour] tinyint, isfirsthalf bit, issueID int)
	
	insert into @log (id,duration,date,[hour],isfirsthalf) 
	select row_number() over(order by i.date, i.hour, i.isfirsthalf), i.duration, i.date, i.hour, i.isfirsthalf 
	from #issues i
	
	declare cur_issues cursor local fast_forward
	for 
	select i.id, i.duration, i.date, i.hour, i.isfirsthalf  
	from @log i
	
	open cur_issues
	fetch next from cur_issues into @id,@duration,@date,@hour,@isfirsthalf
	
	while @@fetch_status = 0
	begin 
		select @windowID = null, @tariffWindowPrice = null
		
		select top 1 @windowID = tw.windowId, @tariffWindowPrice = tw.price
		from TariffWindow tw 
			inner join Tariff t on tw.tariffId = t.tariffID
			left join DisabledWindow dw on dw.massmediaID = @MassMediaId and tw.windowDateActual between dw.startDate And dw.finishDate
		where tw.massmediaID = @massmediaID
			and tw.dayOriginal = @date
			and datepart(hh,t.[time]) = @hour
			and ((@isfirsthalf = 1 and datepart(mi,t.[time]) < 30) or (@isfirsthalf = 0 and datepart(mi,t.[time]) >= 30))
			and (tw.isDisabled = 0 and dw.disabledWindowID is null )
			and (@deadLine is null or tw.dayActual > @deadLine) 
			and (@RightToGoBack = 1 or tw.dayActual >= @tomorrow)
			and tw.maxCapacity = 0 
			and (@rightForMinus = 1 or @isConfirmed = 0 or tw.[timeInUseConfirmed] + @duration <= tw.duration)
			and t.isForModuleOnly = 0
	
		if @windowID is not null 
		begin 
			exec GetMuteRoller @rolTypeID = @rolTypeID, @duration = @duration, @rollerID = @rollerID out, @withShow = 0
			set @issuePrice = dbo.fn_GetIssuePrice(@duration, @tariffWindowPrice, 1, 0, @extraChargeFirstRoller, @extraChargeSecondRoller, @extraChargeLastRoller)
			
			INSERT INTO [Issue](rollerID, actualWindowID, originalWindowId, campaignID, positionId, ratio, moduleIssueID, [packModuleIssueID], isConfirmed, [tariffPrice], grantorID, activationDate)
			VALUES(@rollerID, @windowID, @windowID, @campaignID, 0, 1, null, null, @isConfirmed, @issuePrice, null, @activationDate)

			if @@rowcount <> 1
			begin
				raiserror('InternalError', 16, 1)
				return 
			end 

			set @issueID = SCOPE_IDENTITY()
			
			update @log set issueID = @issueID where id = @id
		end 
		
		fetch next from cur_issues into @id,@duration,@date,@hour,@isfirsthalf
	end 
	
	close cur_issues 
	deallocate cur_issues
	
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then timeInUseConfirmed + t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed + t1.durationU
				Else timeInUseUnconfirmed
			End
	From
		(select i.actualWindowID as windowID, 
			sum(case when i.isConfirmed = 1 then l.duration else 0 end) as duration, 
			sum(case when i.isConfirmed = 0 then l.duration else 0 end) as durationU
		from 
			@log l inner join Issue i on l.issueID = i.issueID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	exec Campaigns	@actionID = @actionID,
					@campaignID = @campaignID,
					@massmediaID = @massmediaID 
	
	select row_number() over(order by l.date, l.hour) as RowNum, l.date as lDate, l.hour as lHour, l.isfirsthalf as lFirstHalf, dbo.fn_Int2Time(l.duration) as duration,
		cast(case when i.issueID is null then 0 else 1 end as bit) isImported, r.[name] as rName, tw.windowDateOriginal as twDate
	from @log l 
		left join Issue i on l.issueID = i.issueID
		left join Roller r on i.rollerID = r.rollerID
		left join TariffWindow tw on i.originalWindowID = tw.windowId
	
	------------------------------------------------------------------------
end
