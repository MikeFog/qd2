-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 18.12.2008
-- Description:	Импорт кампании, для импорта необходимы дополнительные временные таблицы
-- =============================================
CREATE procedure [dbo].[CampaignImportGrammofon] 
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
	@extraChargeLastRoller tinyint,
	@grammofonMistake tinyint
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

    if (object_id('tempdb..#issues') is null or object_id('tempdb..#rollers') is null)
    begin 
		print('Для работы процедуры  необходимы дополнительные временные таблицы')
		print('create table #issues(date datetime, rollerID int)')
		print('create table #rollers([rollerID] Int, [name] VarChar(4000) COLLATE Cyrillic_General_CI_AS, [duration] Int, [realID] Int)')
		return 
	end 
	------------------------------------------------------------------------
	-- Импорт обычной рекламной кампании
	-- Находим или создаем рекламную кампанию
	declare @campaignID int, @activationdate datetime
	
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
		
	if @isConfirmed = 1
		set @activationdate = getdate()
	else 
		set @activationdate = null
	
	--------------------------------------------------------------------------
	-- Главные пользовательские привелегии
	declare @rightToGoBack bit, @isAdmin bit, @rightForMinus bit
	
	exec hlp_GetMainUserCredentials
		@loggedUserId, @rightToGoBack out, @isAdmin out, @rightForMinus out
	
	--------------------------------------------------------------------------
	-- Находим ролики для импортируемых
	-- Сначала по имени
	update ri set realID = r.rollerID
	from #rollers ri 
		inner join Roller r on ri.name = r.name 
	
	declare @rollerID int, @duration int, @realRollerID int
	
	-- Подставляем для не найденных ролики пустышки
	declare cur_rollers cursor local fast_forward
	for 
	select ri.rollerID, ri.duration from #rollers ri where ri.realID is null
	
	open cur_rollers
	fetch next from cur_rollers into @rollerID, @duration
	
	while @@fetch_status = 0
	begin
		exec [GetMuteRoller] @rolTypeID, @duration, @realRollerID out, 0
		
		update #rollers set realID = @realRollerID where @rollerID = rollerID
		
		fetch next from cur_rollers into @rollerID, @duration
	end 
	
	close cur_rollers
	deallocate cur_rollers
	--------------------------------------------------------------------------
	-- Импорт
	
	declare @tomorrow datetime, @id int, @date datetime, @issueID int, @tariffWindowPrice money, @windowID int, @issuePrice money
	
	set @tomorrow = dateadd(day, 1, Convert(datetime, Convert(varchar(8),getdate(), 112), 112))
	
	declare @log table(id int, duration int, rollerID int, date datetime, issueID int)
	
	insert into @log (id, duration, rollerID, date, issueID) 
	select row_number() over (order by i.date), r.duration, r.realID, i.date, null
	from #rollers r
		inner join #issues i on i.rollerID = r.rollerID
	
	declare cur_issues cursor local fast_forward
	for
	select l.id, l.duration, l.rollerID, l.date, tw.windowId, tw.price
	from @log l
		inner join TariffWindow tw on tw.massmediaID = @massmediaID and 
					tw.windowDateOriginal between dateadd(mi, -@grammofonMistake, l.date) 
									and dateadd(mi, @grammofonMistake, l.date)
		inner join Tariff t on tw.tariffId = t.tariffID
		left join DisabledWindow dw on dw.massmediaID = @MassMediaId and tw.windowDateActual between dw.startDate And dw.finishDate
	where (tw.isDisabled = 0 and dw.disabledWindowID is null )
		and (@deadLine is null or tw.dayActual > @deadLine) 
		and (@RightToGoBack = 1 or tw.dayActual >= @tomorrow)
		and tw.maxCapacity = 0 
		and (@rightForMinus = 1 or @isConfirmed = 0 or tw.[timeInUseConfirmed] + l.duration <= tw.duration)
		and t.isForModuleOnly = 0
			
	open cur_issues
	fetch next from cur_issues into @id,@duration,@rollerID,@date,@windowID,@tariffWindowPrice
	
	while @@fetch_status = 0
	begin 
		-- могут выбраться два окна, потому проверяем что уже не выбрано
		if not exists(select * from @log where id = @id and issueID is not null)
		begin 
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
		fetch next from cur_issues into @id,@duration,@rollerID,@date,@windowID,@tariffWindowPrice
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
	
	select row_number() over(order by l.date) as RowNum, l.date as lDate, dbo.fn_Int2Time(l.duration) as duration,
		cast(case when i.issueID is null then 0 else 1 end as bit) isImported, r.[name] as rName, tw.windowDateOriginal as twDate
	from @log l 
		left join Issue i on l.issueID = i.issueID
		left join Roller r on i.rollerID = r.rollerID
		left join TariffWindow tw on i.originalWindowID = tw.windowId
	------------------------------------------------------------------------
end
