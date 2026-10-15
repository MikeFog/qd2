-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 14.04.2009
-- Description:	Веерное размещение: один и тот же ролик на всех (выбранных) линейных кампаниях акции в
--              получасе @issueDate — на каждой станции в одном её окне.
--
--              Какое окно станции берётся (Д-15, docs/action-forms.md): из окон получаса, подходящих по
--              условиям ниже, — сначала те, где IssueIUD НЕ отклонит позицию @positionId, потом остальные;
--              внутри каждой группы — самое свободное по времени. Позицию отклоняют: подтверждённый выпуск
--              с этой позицией в окне (признак TariffWindow.is{First|Second|Last}PositionOccupied,
--              hlp_IssueVerify -> FirstLastIssueError) и выпуск ТОЙ ЖЕ АКЦИИ с этой позицией в окне
--              (IssueIUD -> PositionErrorForTheSameAction). Раньше брали просто самое свободное окно, и занятая
--              в нём позиция роняла слот целиком, хотя в соседнем окне того же получаса она была свободна.
--              Окно, куда ролик не помещается (timeInUseConfirmed + длительность > duration — как WindowOverflow в
--              hlp_IssueVerify, который IssueIUD применяет только к подтверждённой акции без права «минус», и в
--              ActionActivate), при позиции тоже идёт в конец: иначе отказ по позиции в лучшем окне подменялся бы
--              переполнением тесного соседнего (клиенты в этом случае повторяют вызов без позиции только после отказа
--              по позиции), а у черновика ролик молча переполнял бы окно.
--              Без позиции (@positionId = 0) и когда ни одно окно не подходит выбор прежний (самое свободное),
--              отказ даёт IssueIUD с тем же текстом.
-- =============================================
CREATE   procedure [dbo].[AddRangeIssues] 
(
	@issueDate datetime, 
	@rollerID int = NULL,
	@rollerDuration smallint = NULL,
	@positionId int = 0,
	@loggedUserId smallint,
	@actionID int,
	@grantorID SMALLINT = NULL,
	@considerUnconfirmed bit = 0,
	@ignoreWindowsWithTheSameFirmIssue bit = 0,
	-- Список кампаний акции (CSV campaignID), с которыми работает веер. NULL/пусто —
	-- все линейные кампании акции (прежнее поведение для вызовов без выбора).
	@campaignIDs varchar(max) = NULL
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

	-- Веер ставит точечные выпуски в рекламные окна — это операция только для линейных
	-- кампаний (campaignTypeID = 1). Модульные/спонсорские/пакетно-модульные кампании акции
	-- в курсор не берём (иначе линейный Issue уехал бы в кампанию с модульным ценообразованием).
	declare cur_massmedias cursor local fast_forward for
	select c.massmediaID, c.campaignID from dbo.Campaign c
	where c.actionID = @actionID and c.campaignTypeID = 1
		and (@campaignIDs is null
			or c.campaignID in (select convert(int, value) from string_split(@campaignIDs, ',')))

	declare @massmediaID smallint, @campaignID int, @windowID int, @price decimal(18,2), @windowDateActual datetime, @firmID int
	
	select @firmID = a.firmID
	from dbo.[Action] a
	where a.actionID = @actionID

	-- Длительность ролика — из базы, а не от клиента (так же делает IssueIUD при AddItem).
	declare @dur int
	select @dur = convert(int, r.duration) from dbo.Roller r where r.rollerID = @rollerID
	
	open cur_massmedias
	fetch next from cur_massmedias into @massmediaID, @campaignID
	
	while @@fetch_status = 0
	begin 
		select @windowID = null, @price = null
		
		select top 1 @windowID = tw.windowId, @price = tw.price, @windowDateActual = tw.windowDateActual
		from TariffWindow tw
			inner join dbo.Tariff t on tw.tariffId = t.tariffID
		where tw.massmediaID = @massmediaID and tw.maxCapacity = 0 and tw.isDisabled = 0 and t.isForModuleOnly = 0
			and tw.windowDateActual between @issueDate and dateadd(second, -1, dateadd(minute, 30, @issueDate))
			and (
				@ignoreWindowsWithTheSameFirmIssue = 0
				or not exists
				(
					select 1
					from dbo.Issue i
						inner join dbo.Campaign c on c.campaignID = i.campaignID
						inner join dbo.[Action] a on a.actionID = c.actionID
					where i.actualWindowID = tw.windowId
						and a.firmID = @firmID
						and (i.isConfirmed = 1 or (a.deleteDate is null and @considerUnconfirmed = 1))
				)
			)
		order by
			-- Д-15: при позиции в последнюю очередь идут окна, где (а) позицию отклонят — она занята подтверждённым выпуском
			-- или выпуском этой же акции, как у hlp_IssueVerify и IssueIUD, и (б) ролик не помещается. Переполнение IssueIUD
			-- отклоняет только у подтверждённой акции без права «минус», но молча переполнять окно у черновика нельзя
			-- (ActionActivate откажет позже), а отказ по позиции не должен подменяться переполнением тесного окна
			CASE
				WHEN isnull(@positionId, 0) = 0 THEN 0
				WHEN @positionId = -20 AND tw.isFirstPositionOccupied = 1 THEN 1
				WHEN @positionId = -10 AND tw.isSecondPositionOccupied = 1 THEN 1
				WHEN @positionId = 10 AND tw.isLastPositionOccupied = 1 THEN 1
				WHEN exists
				(
					select 1
					from dbo.Issue ps_i
						inner join dbo.Campaign ps_c on ps_c.campaignID = ps_i.campaignID
					where ps_i.actualWindowID = tw.windowId
						and ps_i.positionId = @positionId
						and ps_c.actionID = @actionID
				) THEN 1
				WHEN @dur is not null AND tw.timeInUseConfirmed + @dur > tw.duration THEN 1
				ELSE 0
			END,
			CASE
				WHEN @considerUnconfirmed = 0 THEN tw.duration - tw.timeInUseConfirmed
				ELSE tw.duration - tw.timeInUseConfirmed - tw.timeInUseUnconfirmed
			END
			desc
	
		if @windowID is null or @price is null 
		begin 
			if @ignoreWindowsWithTheSameFirmIssue = 1
				and exists
				(
					select 1
					from dbo.TariffWindow tw
						inner join dbo.Tariff t on tw.tariffId = t.tariffID
						inner join dbo.Issue i on i.actualWindowID = tw.windowId
						inner join dbo.Campaign c on c.campaignID = i.campaignID
						inner join dbo.[Action] a on a.actionID = c.actionID
					where tw.massmediaID = @massmediaID and tw.maxCapacity = 0 and tw.isDisabled = 0 and t.isForModuleOnly = 0
						and tw.windowDateActual between @issueDate and dateadd(second, -1, dateadd(minute, 30, @issueDate))
						and a.firmID = @firmID
						and (i.isConfirmed = 1 or a.deleteDate is null)
				)
				raiserror('IssueWithTheSameFirmExists', 16, 1)
			else
				raiserror('CannotAddRangeIssues', 16, 1)
			return 
		end 
	
		exec dbo.IssueIUD
			@rollerID = @rollerID,
			@rollerDuration = @rollerDuration,
			@issueDate = @windowDateActual,
			@windowID = @windowID, 
			@tariffWindowPrice = @price,
			@campaignID = @campaignID,
			@positionId = @positionId, 
			@ratio = 1,
			@loggedUserId = @loggedUserId,
			@massmediaID = @massmediaID,
			@actionName = 'AddItem',
			@grantorID = @grantorID
	
		if @@error <> 0 
			return 
	
		fetch next from cur_massmedias into @massmediaID, @campaignID
	end 

end