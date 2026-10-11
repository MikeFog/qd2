-- Фактическое окно выпуска, этап 5 «Замена ролика» (docs/tasks/window-actual-switch.md §5.1, §5.5 п. 4; решения Р-3, О-8).
-- Замена ролика у выпуска, перенесённого трафиком, — по окну выхода (вариант А-9: только SQL, клиент и веб не меняются):
-- 1. RollerSubstitute: выпуск ищется в окне выхода; день — день модуля/пакета или день окна выхода; «прошлое» и «дедлайн» —
--    по этому дню; цена при ролике другой длины — по окну выхода, у окна без тарифа — по окну постановки (цена окна и
--    наценки); в режиме «один выпуск» ключ строится из самого выпуска (@originalWindowID — только признак режима);
--    «Незамененные ролики» — фактическое время окна выхода (колонка по-прежнему windowDateOriginal).
-- 2. RollerSubstitutionPassport: дерево «Заменить рекламный ролик» — дни выхода (у модулей/пакетов — день модуля/пакета) и
--    окна выхода с фактическим временем.
-- 3. RangeSlotIssues: веер отдаёт для Ctrl+R окно выхода и его день (колонки originalWindowID/windowDayOriginal — имена
--    исторические).
-- Три процедуры — один пакет: у них общий ключ #days; накат наполовину молча пропускает перенесённые выпуски.
--
-- Ставить после 26, 27 и 29 (несёт их правки этих процедур) — без них скрипт ничего не меняет. Клиент не нужен. Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 34_actual-window-roller-substitute.sql

SET NOCOUNT ON;
GO
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitute')) NOT LIKE N'%i.actualWindowID = @windowID%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.RangeSlotIssues')) NOT LIKE N'%INNER JOIN dbo.Issue i ON i.actualWindowID = tw.windowId%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitutionPassport')) LIKE N'%fn_GetTimeString%'
BEGIN
    RAISERROR(N'34: сначала Deploy/26, 27 и 29. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - error resolved
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - replace @moduleIssueID and @packModuleIssueID on @moduleID and @packModuleID
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 14.10.2008 - some optimization + add substitude for only one issue
*/
CREATE OR ALTER PROC [dbo].[RollerSubstitute]
(
@campaignID int,
@campaignTypeID tinyint,
@oldRollerID int,
@oldDuration int,
@newRollerID int,
@newDuration int,
@loggedUserId smallint,
@moduleID int = null,
@packModuleID int = null,
@originalWindowID int = null,
@issueID int = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
    if (object_id('tempdb..#days') is null and (@originalWindowID is null or @issueID is null))
    begin 
		print('Для работы процедуры необходима таблица #days (windowID int, issueDate datetime)')
		print('create table #days (windowID int, issueDate datetime)')
		return 
	end 
	else if @originalWindowID is not null
	begin 
		if object_id('tempdb..#days') is null
		begin 
			-- Один выпуск: ключ — окно выхода и день выпуска (день модуля/пакета или день окна выхода), из самого
			-- выпуска. @originalWindowID — только признак режима, его значение не используется (этап 5,
			-- docs/tasks/window-actual-switch.md): клиенты шлют Issue.originalWindowID.
			create table #days (windowID int, issueDate datetime)
			insert into [#days] (windowID,issueDate)
			select i.actualWindowID, coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal)
			from Issue i
				inner join TariffWindow tw on tw.windowId = i.actualWindowID
				left join ModuleIssue mi on mi.moduleIssueID = i.moduleIssueID
				left join PackModuleIssue pmi on pmi.packModuleIssueID = i.packModuleIssueID
			where i.issueID = @issueID
		end 
	end 

declare
	@rightForMinus bit,
	@RightToGoBack bit,
	@windowID int,
	@timeBonus int,
	@issuesDuration int,
	@deadline datetime,
	@position int,
	@newPrice decimal(18,2),
	@extraChargeFirst int, @extraChargeSecond int, @extraChargeLast int,
	@tariffWindowPrice decimal(18,2), 
	@diffDuration int,
	@isConfirmed bit,
	@date datetime,
	@msgError varchar(128),
	@timeadded int,
	@isAdmin bit,
	@isTrafficManager bit,
	@newRollerActionTypeID int

-- Длительности - из базы, а не от клиента (как и в IssueIUD): присланные могли устареть
select @oldDuration = duration From Roller where rollerID = @oldRollerID
select @newDuration = duration From Roller where rollerID = @newRollerID

set @diffDuration = @newDuration - @oldDuration
select @isConfirmed = a.isConfirmed From Action a Inner Join Campaign c On a.actionID = c.actionID Where c.campaignID = @campaignID
select @newRollerActionTypeID = rolActionTypeID From Roller where rollerID = @newRollerID

-- Заменой нельзя смешать политическую агитацию (тип 6) с другой рекламой в акции:
-- класс ролика (агитация / не агитация) при замене должен сохраняться
declare @oldRollerActionTypeID int
select @oldRollerActionTypeID = rolActionTypeID From Roller where rollerID = @oldRollerID
If (case when @newRollerActionTypeID = 6 then 1 else 0 end) <> (case when @oldRollerActionTypeID = 6 then 1 else 0 end) Begin
	RAISERROR('AgitationMixError', 16, 1)
	RETURN
End

If @newDuration = 0 Begin
	RAISERROR('Roller_NullDuration', 16, 1)
	RETURN 
End

-- в активированных акциях нельзя заменить на ролик без предмета рекламы
If Exists (Select 1 From Roller where rollerID = @newRollerID And advertTypeID Is Null And @isConfirmed = 1) Begin
	RAISERROR('WrongRollerForSubstitution', 16, 1)
	RETURN 
End

Exec hlp_GetMainUserCredentials
	@loggedUserId = @loggedUserId,
	@rightToGoBack = @rightToGoBack out,
	@isAdmin = @isAdmin out,
	@isTrafficManager = @isTrafficManager out,
	@rightForMinus = @rightForMinus OUT

declare @issues table (issueID int primary key, 
	newPrice decimal(18,2), 
	actualWindowID int, 
	date smalldatetime,
	isConfirmed bit, 
	msgError varchar(256))

declare cur_issues cursor local fast_forward
for
select
	i.issueID,
	i.actualWindowID,
	i.positionId,
	c.timeBonus,
	c.issuesDuration,
	p.extraChargeFirstRoller,
	p.extraChargeSecondRoller,
	p.extraChargeLastRoller,
	mm.deadline,
	p.price,
	dd.issueDay
from
	Issue i
	-- Этап 5 (docs/tasks/window-actual-switch.md): выпуск ищется в окне выхода; день — день модуля/пакета или
	-- день окна выхода; цена и наценки — по окну выхода, у окна без тарифа — по окну постановки (Р-3)
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
	inner join TariffWindow twO on i.originalWindowID = twO.windowId
	cross apply (select top 1 v.price, pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller
		from (values (1, tw.tariffId, tw.price), (2, twO.tariffId, twO.price)) v (ord, tariffId, price)
			inner join Tariff t on t.tariffID = v.tariffId
			inner join Pricelist pl on pl.pricelistID = t.pricelistID
		order by v.ord) p
	inner join Campaign c on i.campaignID = c.campaignID
	inner join MassMedia mm on tw.massmediaID = mm.massmediaID
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.priceListID = pmpl.priceListID
	cross apply (select issueDay = coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal)) dd
	inner join #days d on dd.issueDay = d.issueDate and (@campaignTypeID not in (1,2) or d.windowID = i.actualWindowID)
where
	i.campaignID = @campaignID
	and i.rollerID = @oldRollerID 
	and	(@moduleID is null or mi.moduleID = @moduleID) 
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
	and i.issueID = coalesce(@issueID, i.issueID)
	
open cur_issues
fetch next from cur_issues 
into @issueID, @windowID, @position, @timeBonus, @issuesDuration, @extraChargeFirst, @extraChargeSecond, @extraChargeLast,
	@deadline, @tariffWindowPrice,@date

set @timeadded = 0

while @@fetch_status = 0 
begin
	set @msgError = null

-- В окне может быть только один открывающий (тип 4 или 44) и один закрывающий (тип 5 или 55)
-- идентификатор СМИ и один анонс агитации (тип 7); 44/55/7 - авто-обвязка политической агитации
	If @newRollerActionTypeID In (4, 44, 5, 55, 7) And Exists (
		Select 1
		From
			Issue i
			Inner Join Roller r on r.rollerID = i.rollerID
		Where
			i.actualWindowID = @windowID
			And ((r.rolActionTypeID In (4, 44) And @newRollerActionTypeID In (4, 44))
				Or (r.rolActionTypeID In (5, 55) And @newRollerActionTypeID In (5, 55))
				Or (r.rolActionTypeID = 7 And @newRollerActionTypeID = 7))
			And i.issueID != @issueID
		)
		Begin
			If @newRollerActionTypeID In (4, 44)
				RAISERROR('RolType4AlreadyExistInWindow', 16, 1)
			Else If @newRollerActionTypeID In (5, 55)
				RAISERROR('RolType5AlreadyExistInWindow', 16, 1)
			Else
				RAISERROR('RolType7AlreadyExistInWindow', 16, 1)
			RETURN 1
		End

	-- Локальное промо (8/9) не позиционируется: выпуск с позицией на промо не меняем
	If @newRollerActionTypeID In (8, 9) And IsNull(@position, 0) <> 0
	begin
		select @msgError = 'PromoPositionForbidden'
	end

	If @isAdmin = 0 And	@isConfirmed = 1 And @isTrafficManager = 0 And @date <= IsNull(@deadLine, Convert(datetime, '19000101',112))
	begin
		select @msgError = 'DeadLineViolation'
	end 

	If	@isConfirmed = 1 And  @date <= dbo.ToShortDate(getdate()) And @RightToGoBack <> 1  And @isTrafficManager = 0
	begin
		select @msgError = 'DateInThePast'
	end 

	if @diffDuration > 0 begin
		if @rightForMinus <> 1 and exists(
			Select * From TariffWindow 
			Where windowId = @windowId 
				And [timeInUseConfirmed] + (@diffDuration) > duration) 
		begin
			select @msgError = 'WindowOverflow'
		end
			
		-- For sponsor campaign Time Bonus shouldn't be exceed
		IF @campaignTypeID = 2 
		begin		
			IF @timeBonus - @timeadded < @issuesDuration + @diffDuration 
				select @msgError = 'TimeBonusExceed'
			else 
				set @timeadded = @timeadded + @diffDuration
		end 
	end 

	set @newPrice = 0

	if @campaignTypeID = 1 and @msgError is null
	begin
		select @newPrice = dbo.fn_GetIssuePrice(
			@newDuration, @tariffWindowPrice, 1, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
	end
			
	insert into @issues (issueID,newPrice,actualWindowID, isConfirmed, msgError,date)
	values (@issueID,@newPrice,@windowID, @isConfirmed, @msgError,@date) 

	fetch next from cur_issues 
	into @issueID, @windowID, @position, @timeBonus, @issuesDuration, @extraChargeFirst, @extraChargeSecond, @extraChargeLast,@deadline, @tariffWindowPrice,@date
end 

close cur_issues
deallocate cur_issues

if @moduleID is not null or @packModuleID is not null 
begin 
	update i set i.msgError = 'Dependence' from @issues i 
		inner join @issues i2 on i.date = i2.date and i2.msgError is not null 
	where i.msgError is null 
end 

IF @campaignTypeID = 3
BEGIN 
	DECLARE cur_missues CURSOR local fast_forward
	FOR
	SELECT mi.[moduleIssueID], mpl.[price], mi.positionId,
		mpl.extraChargeFirstRoller, mpl.extraChargeSecondRoller, mpl.extraChargeLastRoller 
	FROM [ModuleIssue] mi 
		INNER JOIN [ModulePriceList] mpl ON mi.modulePricelistID = mpl.modulePricelistID
		inner join Pricelist pl on mpl.priceListID = pl.pricelistID
		inner join #days d on mi.issueDate = d.issueDate
		WHERE mi.[campaignID] = @campaignID
			and (@moduleID is null or  mi.moduleID = @moduleID) and mi.rollerID = @oldRollerID
			and not exists(select * from @issues i where i.date = mi.issueDate and i.msgError is not null)
			
	DECLARE @moduleIssueID int

	OPEN cur_missues
	FETCH NEXT FROM cur_missues 
	INTO @moduleIssueID, @tariffWindowPrice, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast
	
	WHILE @@fetch_status = 0 BEGIN
		IF @diffDuration = 0
			UPDATE [ModuleIssue] SET [rollerID] = @newRollerID
			WHERE [moduleIssueID] = @moduleIssueID
		ELSE
		BEGIN
			SELECT @newPrice = dbo.fn_GetIssuePrice(
				@newDuration, @tariffWindowPrice, 1, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)

			UPDATE [ModuleIssue]
			SET [rollerID] = @newRollerID, [tariffPrice] = @newPrice
			WHERE [moduleIssueID] = @moduleIssueID
		END

		FETCH NEXT FROM cur_missues
		INTO @moduleIssueID, @tariffWindowPrice, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast
	end
	
	close cur_missues
	deallocate cur_missues
END

IF @campaignTypeID = 4
BEGIN
	DECLARE cur_pmissues CURSOR LOCAL  fast_forward
	FOR 
	SELECT pmi.[packModuleIssueID], pmpl.[price], pmi.positionId, pmpl.extraChargeFirstRoller, pmpl.extraChargeSecondRoller, pmpl.extraChargeLastRoller FROM [PackModuleIssue] pmi 
		INNER JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
		inner join #days d on pmi.issueDate = d.issueDate
		WHERE pmi.[campaignID] = @campaignID
			and (@packModuleID is null or  pmpl.packModuleID = @packModuleID) and pmi.rollerID = @oldRollerID
			and not exists(select * from @issues i where i.date = pmi.issueDate and i.msgError is not null)
		
	DECLARE @packModuleIssueID INT
	
	OPEN cur_pmissues
	FETCH NEXT FROM cur_pmissues
	INTO @packModuleIssueID, @tariffWindowPrice, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast
	WHILE @@fetch_status = 0 BEGIN
		IF @diffDuration = 0
			UPDATE [PackModuleIssue] SET [rollerID] = @newRollerID
			WHERE [packModuleIssueID] = @packModuleIssueID
		ELSE
		BEGIN
			SELECT @newPrice = dbo.fn_GetIssuePrice(
				@newDuration, @tariffWindowPrice, 1, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)

			UPDATE [PackModuleIssue]
			SET [rollerID] = @newRollerID, [tariffPrice] = @newPrice
			WHERE [packModuleIssueID] = @packModuleIssueID
		END

		FETCH NEXT FROM cur_pmissues
		INTO @packModuleIssueID, @tariffWindowPrice, @position, @extraChargeFirst, @extraChargeSecond, @extraChargeLast
	end
	
	close cur_pmissues
	deallocate cur_pmissues
END

-- Замена на ролик той же длины (@diffDuration = 0): цена выпуска не меняется
-- (fn_GetIssuePrice от тех же аргументов дал бы то же значение), занятость окна
-- тоже. Пишем только rollerID и не трогаем tariffPrice — заодно не
-- «пересинхронизируем» её со сменившейся с момента размещения ценой окна.
if @diffDuration = 0
	update i set i.rollerID = @newRollerID
	from Issue i
		inner join @issues ii on i.issueID = ii.issueID and ii.msgError is null
else
begin
	update i set i.rollerID = @newRollerID,
		i.tariffPrice = ii.newPrice
	from Issue i
		inner join @issues ii on i.issueID = ii.issueID and ii.msgError is null

	Update
		TariffWindow
	Set
		timeInUseConfirmed =
			Case
				When [maxCapacity] = 0
					Then timeInUseConfirmed + coalesce(res.durC,0)
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed =
			Case
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed + coalesce(res.durU,0)
				Else timeInUseUnconfirmed
			End
	from
		(select i.actualWindowID,
				sum(case when i.isConfirmed = 1 then @diffDuration else 0 end) as durC,
				sum(case when i.isConfirmed = 0 then @diffDuration else 0 end) as durU
			from @issues i	where i.msgError is null
		 group by i.actualWindowID) as res
	where
		TariffWindow.windowId = res.actualWindowId
end

-- «Незамененные ролики»: фактическое время окна выхода. Имя колонки windowDateOriginal оставлено — его читают
-- атрибут сущности 212 и веб (ObjectActions.cs); переименование — этап 9
select row_number() over(order by tw.windowDateActual) as RowNum, tw.windowDateActual as windowDateOriginal, msg.message
from @issues i
	inner join iMessageToSubtitute msg on i.msgError = msg.msgError
	inner join Issue ii on i.issueID = ii.issueID
	inner join TariffWindow tw on ii.actualWindowID = tw.windowId
where i.msgError is not null
order by tw.windowDateActual
GO

/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - replace @moduleIssueID and @packModuleIssueID on @moduleID and @packModuleID
*/
CREATE OR ALTER PROC [dbo].[RollerSubstitutionPassport]
(
@campaignID int,
@campaignTypeID int,
@rollerID int,
@moduleID int = null,
@packModuleID int = null 
)
AS
SET NOCOUNT ON

SELECT COUNT(*) as issues FROM Issue i
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
WHERE i.campaignID = @campaignID AND i.rollerID = @rollerID 
	and (@moduleID is null or mi.moduleID = @moduleID) 
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)

-- Rollers
DECLARE @massmediaID smallint

SELECT DISTINCT 
	r.rollerID as [id],
	r.name
FROM 
	Roller r 
	inner join Roller ro on ro.rollerID = @rollerID and r.rollerID <> @rollerID
	INNER JOIN [Action] a ON r.firmID = a.firmID
	INNER JOIN Campaign c ON c.actionID = a.actionID and c.campaignID = @campaignID
where
	r.isEnabled = 1	AND 
	r.isMute = 0 And
	r.parentID Is Null
ORDER BY 
	r.[name]
	
-- Этап 5 (docs/tasks/window-actual-switch.md): дни и окна — по окну выхода; день — день модуля/пакета или день
-- окна выхода, как в RollerSubstitute (ключ #days должен совпадать с её курсором). Формат id не менять: его
-- разбирает RollerChangeAdvertTypeForm.
declare @days table (id varchar(20), parentID varchar(20), windowID int, timeString char(5), issueDate datetime, [image] varchar(50), [name] varchar(20))

insert into @days(id, issueDate,[image], [name])
select distinct
	convert(varchar, dd.issueDay, 104),
	dd.issueDay,
	'Day.png',
	convert(varchar, dd.issueDay, 104)
from
	Issue i
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
	cross apply (select issueDay = coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal)) dd
where
	i.[campaignID] = @campaignID AND
	i.rollerID = @rollerID
	and (@moduleID is null or mi.moduleID = @moduleID)
	and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
order by
	dd.issueDay

if (@campaignTypeID in (1,2))
begin 
	insert into @days(id, parentID, windowID,[image], [name],issueDate)
	select distinct
		-- id — ключ слота (время по расписанию окна выхода, без дублей); подпись — фактическое время выхода
		convert(varchar, tw.dayOriginal, 104) + CONVERT(varchar(5), tw.windowDateOriginal, 108),
		convert(varchar, tw.dayOriginal, 104),
		tw.windowId,
		'Issue.png',
		CONVERT(varchar(5), tw.windowDateActual, 108),
		tw.dayOriginal
	from
		Issue i
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
		left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
		left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
	where 
		i.[campaignID] = @campaignID AND
		i.rollerID = @rollerID  
		and (@moduleID is null or mi.moduleID = @moduleID) 
		and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
	order by CONVERT(varchar(5), tw.windowDateActual, 108)
end 

select * from @days order by issueDate
GO

-- =============================================
-- Description:	Фактическое содержимое одного или нескольких получасовых слотов веера:
--              выпуски акции по указанным кампаниям. В отличие от «Добавленных выпусков»
--              (пересечение слотов по всем выбранным кампаниям) показывает и
--              частичные слоты — те, где выпуск есть не во всех кампаниях
--              (в сетке они красные).
--              @issueDates — CSV дат в ISO 8601 с "T" (ГГГГ-ММ-ДДTчч:мм:сс), не через
--              дефис-пробел: на сервере с русским @@LANGUAGE 'YYYY-MM-DD HH:MM:SS'
--              парсится как 'YYYY-DD-MM' (день/месяц переставлены) — с "T" формат
--              однозначен независимо от языка сессии.
--              Один вызов — сразу все выделенные окна (было по одному @issueDate на
--              вызов; при выделении по Ctrl+R/Delete десятков окон это давало заметную
--              паузу перед диалогом подтверждения — по круговому запросу на окно).
--              Одна строка на выпуск; requestedIssueDate — какому из @issueDates эта
--              строка соответствует (нужно вызывающему, чтобы разложить обратно по
--              окнам). Группировку «ролик + позиция» собирает C#
--              (TariffWithRangeGrid.GetSlotIssueGroups).
--              originalWindowID/windowDayOriginal — для RollerSubstitute (массовая
--              замена ролика, TariffWithRangeGrid.GetSlotIssueRows): ключ её #days — окно
--              ВЫХОДА выпуска и его день (этап 5, docs/tasks/window-actual-switch.md). Имена
--              колонок исторические, их читает клиент; переименование — этап 9.
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[RangeSlotIssues]
(
	@actionID int,
	@issueDates varchar(max),
	-- Список кампаний акции (CSV campaignID). NULL/пусто — все линейные кампании акции.
	@campaignIDs varchar(max) = NULL
)
AS
BEGIN
	SET NOCOUNT ON;

	-- Даты слотов разбираются один раз во временную таблицу. Раньше STRING_SPLIT стоял
	-- прямо в соединении по диапазону времени, и сервер разбирал всю строку дат заново на
	-- каждый выпуск акции (выпуски × слоты): неделя веера у акции на 11 тыс. выпусков
	-- считалась 20–45 с и всё это время держала S-блокировку на всю таблицу Issue —
	-- чужие IssueIUD/RollerSubstitute ждали (прод, 29.09.2026).
	CREATE TABLE #requested (issueDate datetime NOT NULL, INDEX IX_requested CLUSTERED (issueDate));
	INSERT INTO #requested (issueDate)
	SELECT CONVERT(datetime, value, 126)
	FROM STRING_SPLIT(@issueDates, ',');

	SELECT
		req.issueDate AS requestedIssueDate,
		i.issueID,
		i.campaignID,
		i.rollerID,
		r.[name] AS rollerName,
		r.duration,
		dbo.fn_Int2Time(r.duration) AS durationString,
		i.positionId,
		tw.windowId AS originalWindowID,
		tw.dayOriginal AS windowDayOriginal
	FROM #requested req
		-- Слот определяется тем же окном, что и в MasterIssueDelete: выпуск ищется по окну выхода
		-- (actualWindowID), получас — по фактическому времени этого окна (правило 07.10.2026).
		INNER JOIN dbo.TariffWindow tw
			ON tw.windowDateActual BETWEEN req.issueDate AND DATEADD(second, -1, DATEADD(minute, 30, req.issueDate))
		INNER JOIN dbo.Issue i ON i.actualWindowID = tw.windowId
		INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
		INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
	WHERE c.actionID = @actionID
		AND c.campaignTypeID = 1
		AND (@campaignIDs IS NULL
			OR c.campaignID IN (SELECT CONVERT(int, value) FROM STRING_SPLIT(@campaignIDs, ',')))
	ORDER BY req.issueDate, i.rollerID, i.positionId, i.campaignID;
END
GO

-- Проверка: новые версии применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitute')) NOT LIKE N'%d.windowID = i.originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitute')) LIKE N'%d.windowID = i.actualWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitutionPassport')) NOT LIKE N'%i.originalWindowID = tw.windowId%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RangeSlotIssues')) NOT LIKE N'%twO.windowId%'
    PRINT N'ГОТОВО: замена ролика — по окну выхода (3 процедуры).';
ELSE
    RAISERROR(N'34: новая версия применена не ко всем процедурам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
