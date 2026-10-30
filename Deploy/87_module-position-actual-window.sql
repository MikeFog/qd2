-- Р-16 / Д-24 (29.10.2026, docs/tasks/window-actual-open-questions.md): смена позиции выпуска модуля
-- («Сделать первым / вторым / последним», «Убрать порядок», «Изменить позиционирование» у модульной кампании и модуля —
-- десктоп и веб, все через ModuleIssueIUD UpdateItem) проверяла «позиция занята» в окнах тарифов модуля в день модуля,
-- а позицию ставила всем строкам выпуска, в том числе перенесённым трафиком в чужие окна: в окне выхода могли оказаться
-- две «первых». Теперь проверка (hlp_IssueVerify, модульная ветка) при смене позиции идёт по окнам, где строки выпуска
-- стоят на самом деле (Issue.actualWindowID) — правило «всё по фактическому окну» от 07.10.2026.
--
--   1) hlp_IssueVerify: новый необязательный параметр @moduleIssueID (умолчание NULL — у остальных вызовов всё как было);
--      окна модульной ветки собираются в табличную переменную: без @moduleIssueID — прежний отбор по тарифам модуля
--      и дню, с ним — окна выхода строк выпуска. Сами проверки (CASE) не менялись.
--   2) ModuleIssueIUD: UpdateItem передаёт @moduleIssueID. Постановка (AddItem) — без изменений.
--
-- На данных: на ArtvisDev у 349 выпусков модулей есть перенесённые строки, все в прошлом — сегодня поведение не меняется.
-- Проверено на ArtvisDev в откатываемой транзакции: строка будущего выпуска перенесена в окно с занятой первой позицией —
-- до правки «Сделать первым» прошло (позиция -20 и в чужом окне), после — отказ FirstLastIssueError; без переноса
-- «Сделать первым» по-прежнему проходит.
--
-- Когда: в любое время, на Artvis и Tumen; клиент и веб не нужны, перезапуск не нужен. Идемпотентен (CREATE OR ALTER).
-- Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 87_module-position-actual-window.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [dbo].[hlp_IssueVerify]
(
@issueID int,
@actionName varchar(32),
@massmediaID smallint,
@DeadLine datetime,
@windowID int,
@issueDate datetime,
@rollerDuration int,
@rightToGoBack bit,
@isAdmin bit,
@isTrafficManager bit,
@rightForMinus bit,
@campaignFinishDate datetime,
@campaignTypeID tinyint,
@isConfirmed bit,
@positionId smallint,
@timeBonus int,
@issuesDuration int,
@sumCapacity INT, 
@rollerActionTypeID TINYINT,
@msgError varchar(64) out,
@modulePricelistID int = null,
@needByTypeVerify bit = 0,
@packModulePriceListId int = null,
@moduleIssueID int = null      -- смена позиции выпуска модуля: проверять окна, где его строки стоят на самом деле (Р-16)
)
AS
SET NOCOUNT ON

declare @tomorrow datetime
select @msgError = null, @tomorrow = dateadd(day, 1, Convert(datetime, Convert(varchar(8),getdate(), 112), 112)), @issueDate = Convert(datetime, Convert(varchar(8),@issueDate, 112), 112)

IF @sumCapacity IS NULL 
 set @sumCapacity = 1
 
-- check Disabled Windows 
if ((@needByTypeVerify = 0 or @campaignTypeID in (1,2,3)) 
		and exists (select * 
						from DisabledWindow dw 
							inner join TariffWindow tw on tw.windowID = @windowID and tw.massmediaID = @MassMediaId
								and dw.massmediaID = @MassMediaId and tw.windowDateActual between dw.startDate And dw.finishDate)
	or (@needByTypeVerify = 1 and @campaignTypeID = 4 and exists(select * from [PackModuleContent]  pmc
																	INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
																	INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
																	INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID] and tw.dayOriginal = @issueDate
																	inner join DisabledWindow dw on tw.windowDateActual between dw.startDate And dw.finishDate and tw.massmediaID = dw.massmediaID
																	where pmc.pricelistID = @packModulePriceListId ) ) )
begin
	set @msgError = 'DisabledWindowInsert'
	return 1
end


--set @msgError = @rollerActionTypeID
--RETURN 1

-- «Отбивка политической агитации» (7), «Локальное СМИ (агитация)» (44) и «Федеральное СМИ (агитация)» (55)
-- ставит только система (AgitationFraming пишет Issue напрямую, мимо этой проверки); человеку - отказ
-- (заказчик, 11.10.2026)
If @rollerActionTypeID In (7, 44, 55)
	Begin
		set @msgError = 'AgitationServiceRollerManual'
		RETURN 1
	End

-- В окне может быть только один открывающий (тип 4 или 44) и один закрывающий (тип 5 или 55)
-- идентификатор СМИ; 44/55 - авто-обрамление политической агитации, пары к ручным 4/5
If @rollerActionTypeID In (4, 44) And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID In (4, 44)
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType4AlreadyExistInWindow'
		RETURN 1
	End

If @rollerActionTypeID In (5, 55) And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID In (5, 55)
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType5AlreadyExistInWindow'
		RETURN 1
	End

-- Размещение агитации сразу подтверждённым выпуском (в уже активированной акции)
-- требует заполненных роликов обвязки в карточке станции - иначе обвязку не создать.
-- В черновике проверка не нужна: её выполнит активация (ActionActivate).
If @rollerActionTypeID = 6 And @isConfirmed = 1
	And Exists (
		Select 1 From MassMedia mm
		Where mm.massmediaID = @MassMediaId
			And (mm.agitationLocalRollerID Is Null
				Or mm.agitationAnnounceRollerID Is Null
				Or mm.agitationFederalRollerID Is Null)
	)
	Begin
		set @msgError = 'AgitationStationRollersNotSet'
		RETURN 1
	End

-- Анонс политической агитации (тип 7) в окне может быть только один
If @rollerActionTypeID = 7 And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID = 7
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType7AlreadyExistInWindow'
		RETURN 1
	End

-- Для политической агитации и её авто-обрамления спецпозиционирование не применяется
If @rollerActionTypeID In (6, 7, 44, 55) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'AgitationPositionForbidden'
		RETURN 1
	End

-- Локальное промо (8 - со спонсором, 9 - без спонсора) не позиционируется:
-- место в блоке ему задаёт тип ролика при выгрузке
If @rollerActionTypeID In (8, 9) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'PromoPositionForbidden'
		RETURN 1
	End

-- «Локальное СМИ» (4) и «Федеральное СМИ» (5) не позиционируются (заказчик, 11.10.2026):
-- место в блоке им задаёт тип ролика при выгрузке
If @rollerActionTypeID In (4, 5) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'RolType45PositionForbidden'
		RETURN 1
	End

if	@campaignFinishDate < @tomorrow and @campaignTypeID <> 2 And @RightToGoBack <> 1 And @isTrafficManager = 0
	set @msgError = 'CampaignAlreadyFinished'
else if @campaignTypeID = 2 
	Begin
	Declare
		@extraChargeFirst tinyint,
		@extraChargeSecond tinyint,
		@extraChargeLast tinyint

	Select
		@extraChargeFirst = IsNull(extraChargeFirstRoller, 0),
		@extraChargeSecond = IsNull(extraChargeSecondRoller, 0),
		@extraChargeLast = IsNull(extraChargeLastRoller, 0)
	From 
		Pricelist p
		Inner Join Tariff t on p.pricelistID = t.pricelistID
		Inner Join TariffWindow tw on tw.tariffId = t.tariffID
	Where 
		tw.windowId = @windowID

	If @timeBonus < @issuesDuration + dbo.f_GetSponsorDuration(@rollerDuration, @positionId, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
		Begin
		set @msgError = 'TimeBonusExceed'
		return 1
		End
	End
if (@needByTypeVerify = 0 or @campaignTypeID in (1,2))
	select top 1 @msgError = 
		case
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And @deadLine is not null and tw.dayActual <= @deadLine And @isAdmin = 0 And @isTrafficManager = 0 then 'DeadLineViolation' 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And tw.dayOriginal < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
			when tw.isDisabled = 1 then 'DisabledInsertRoller'
			when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
			when (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
					or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
			when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
			when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
			else null 
		end 
	from [TariffWindow] tw
	where tw.[windowId] = @windowID 
else if @needByTypeVerify = 1 and @campaignTypeID = 3
	Begin
	-- Окна проверки. Постановка - окна тарифов модуля в день модуля. Смена позиции существующего выпуска (@moduleIssueID) -
	-- окна, где его строки стоят на самом деле (Issue.actualWindowID): трафик мог перенести часть строк в чужие окна, и
	-- проверка по слоту пропускала там занятую позицию (две «первых» в окне выхода), а окно слота, откуда строку унесли,
	-- проверяла зря. Счётчики этого выпуска ModuleIssueIUD к этому моменту уже снял в тех же окнах выхода (Р-16, Д-24)
	declare @windows table (windowID int primary key)
	if @moduleIssueID is null
		insert into @windows (windowID)
		select tw.windowID
		from [TariffWindow] tw
			Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
			Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID
		where
			mt.modulePriceListID = @modulePricelistID
			-- день модуля и пакета - по dayOriginal, как вставка выпусков (ModuleIssueIUD, PackModuleIssueID)
			-- и IsModuleExist; индекс IX_TariffWindow_TariffID_DayOriginal; время окна переносится только
			-- внутри дня, так что dayOriginal = dayActual
			And tw.dayOriginal = @issueDate
	else
		insert into @windows (windowID)
		select distinct i.actualWindowID
		from Issue i
		where i.moduleIssueID = @moduleIssueID

	-- окон у модуля за день несколько: сообщение - первой сработавшей проверки по порядку CASE, как в линейной ветке
	-- (окно одно), а не наибольший по алфавиту ключ среди окон (WindowOverflow одного окна перебивал
	-- FirstLastIssueError и закрытое окно соседнего); строки и TOP 1 прежние, окно без ошибки - последним
	select top 1 @msgError = e.msgError
	from @windows w
		Inner Join [TariffWindow] tw On tw.windowID = w.windowID
		Cross Apply (Select
		case 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And @deadLine is not null and tw.dayActual <= @deadLine And @isAdmin = 0 And @isTrafficManager = 0 then 'DeadLineViolation' 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And tw.dayOriginal < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
			when tw.isDisabled = 1 then 'DisabledInsertRoller'
			when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
			when (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
					or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
			when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
			when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
			else null 
		end As msgError) e
	order by
		case e.msgError
			when 'DeadLineViolation' then 1
			when 'IncorrectIssueDate' then 2
			when 'DisabledInsertRoller' then 3
			when 'DisabledInsertSimpleRoller' then 4
			when 'FirstLastIssueError' then 5
			when 'WindowMaxCapacityOverflow' then 6
			when 'WindowOverflow' then 7
			else 8 -- окно без ошибки - последним (NULL при ASC встал бы первым)
		end
	if @msgError Is Null And @positionId <> 0
	Begin
	Select @msgError = 'MaxCapacityModuleSetPositionError'
	From
		ModuleTariff mt
		Inner Join Tariff t On t.tariffID = mt.tariffID
	Where
		mt.modulePriceListID = @modulePriceListId
		And t.maxCapacity > 0 And t.maxCapacity < 4
	End
	End 
else if @needByTypeVerify = 1 and @campaignTypeID = 4
	Begin
	If (@actionName = 'AddItem' Or @isConfirmed = 1) And @isAdmin = 0 And @isTrafficManager = 0 And 
		Exists
		(
		Select 1 
		from 
			[PackModuleContent]  pmc
			INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on tw.massmediaID = mm.massmediaID
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayOriginal = @issueDate
			AND @issueDate <= mm.deadLine
		)
		Set @msgError = 'DeadLineViolation'

	If @msgError Is Null
		-- окон у пакета за день несколько: сообщение - по порядку проверок, как в модульной ветке (не по алфавиту)
		select top 1 @msgError = e.msgError
		from [PackModuleContent]  pmc
			INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
			INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on mm.massmediaID = tw.massmediaID
			Cross Apply (Select
			case 
				-- дата в прошлом - одна на все окна пакета: первой, как в линейной и модульной ветках
				when (@actionName = 'AddItem' Or @isConfirmed = 1) And @issueDate < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
				when tw.isDisabled = 1 then 'DisabledInsertRoller'
				when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
				when @positionId <> 0 and (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
						or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
						or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
				when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
				when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
				else null 
			end As msgError) e
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayOriginal = @issueDate
		order by
			case e.msgError
				when 'DeadLineViolation' then 1
				when 'IncorrectIssueDate' then 2
				when 'DisabledInsertRoller' then 3
				when 'DisabledInsertSimpleRoller' then 4
				when 'FirstLastIssueError' then 5
				when 'WindowMaxCapacityOverflow' then 6
				when 'WindowOverflow' then 7
				else 8 -- окно без ошибки - последним (NULL при ASC встал бы первым)
			end
	if @msgError Is Null And @positionId <> 0
		Begin
		Select @msgError = 'MaxCapacityPackModuleSetPositionError'
		From
			PackModuleContent pmc
			Inner Join ModuleTariff mt On mt.modulePriceListID = pmc.modulePriceListID
			Inner Join Tariff t On t.tariffID = mt.tariffID
		Where
			pmc.pricelistID = @packModulePriceListId
			And t.maxCapacity > 0 And t.maxCapacity < 4
		End
	End
if @msgError is not null 
	return 1

return 0
GO

CREATE OR ALTER PROCEDURE [dbo].[ModuleIssueIUD]
(
@moduleIssueID int = NULL,
@campaignID int = NULL,
@moduleID smallint = NULL,
@modulePricelistID smallint = NULL,
@issueDate datetime = NULL,
@rollerID int = NULL,
@rollerDuration smallint = NULL,
@ratio decimal(18,10) = 1,
@positionId smallint = 0,
@tariffPrice decimal(18,2) = NULL,
@isConfirmed bit = NULL,
@loggedUserId smallint,
@actionName varchar(32),
@grantorID SMALLINT = NULL
)
WITH EXECUTE AS OWNER
AS
Set Nocount On
DECLARE	
	@massmediaID smallint, 
	@windowId int,
	@IsAdmin bit,	
	@IsTrafficManager bit,	
	@RightForMinus bit, 
	@RightToGoBack bit,
	@campaignTypeID smallint,
	@finishDate datetime,
	@tariffTime datetime,
	@rollerIssueDate datetime,
	@issuePrice decimal(18,2),
	@extraChargeFirst tinyint,
	@extraChargeSecond tinyint,
	@extraChargeLast TINYINT,
	@actionID int,
	@DeadLine datetime,
	@campaignFinishDate datetime,
	@msgError varchar(64),@res smallint,
	@managerDiscount decimal(18,10),
	@campaignStartDate datetime
	
EXEC hlp_GetMainUserCredentials
	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT, @grantorID
	
SELECT 
	@massmediaID = c.massmediaID,
	@campaignTypeID	= c.campaignTypeID,
	@finishDate = c.finishDate,
	@actionID = [actionID],
	@deadline = m.deadline, 
	@campaignFinishDate = c.finishDate,
	@managerDiscount = c.managerDiscount,
	@campaignStartDate = case when coalesce(c.startDate, @issueDate) > @issueDate then @issueDate else coalesce(c.startDate, @issueDate) end,
	@campaignFinishDate = case when coalesce(c.finishDate, @issueDate) < @issueDate then @issueDate else coalesce(c.finishDate, @issueDate) end
FROM 
	Campaign c
	Inner Join Massmedia m On m.massmediaId = c.massmediaId
WHERE 
	c.campaignID = @campaignID	

Select
	@extraChargeFirst = IsNull(extraChargeFirstRoller, 0),
	@extraChargeSecond = IsNull(extraChargeSecondRoller, 0),
	@extraChargeLast = IsNull(extraChargeLastRoller, 0)
From 
	ModulePriceList
Where
	modulePriceListID = @modulePricelistID

-- Длительность ролика и цена модуля при добавлении - из базы, а не от клиента (как в IssueIUD)
if @actionName = 'AddItem'
begin
	select @rollerDuration = duration from Roller where rollerID = @rollerID
	select @tariffPrice = price from ModulePriceList where modulePriceListID = @modulePricelistID
end

if @actionName in ('AddItem', 'UpdateItem') 
	and dbo.[fn_IsAcceptRatioForUser](@loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0
begin 
	 raiserror('CannotChangeCampaignWithMaxDiscount', 16, 1)
	 return
end

IF @actionName in ('AddItem', 'UpdateItem') And @isConfirmed = 1
	And Exists (Select 1 From Roller where rollerID = @rollerID And advertTypeID Is Null) Begin
		RAISERROR('RollerWithoutAdvertType', 16, 1)
		RETURN 
	End

if (@actionName in ('DeleteItem', 'UpdateItem'))
BEGIN
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then timeInUseConfirmed - t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.durationU
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed - t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssuesU
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 0
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 0
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 0
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCountU,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCountU,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCountU
	From
		(select i.actualWindowID as windowID, 
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as duration, 
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durationU, 
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as countIssues,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as countIssuesU,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end, 0)) as lastCount,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end, 0)) as firstCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end, 0)) as secondCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end, 0)) as lastCountU
		 from 
			Issue i 
			Inner Join Roller r On r.rollerId = i.rollerId
		where i.moduleIssueID = @moduleIssueID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
END

IF @actionName IN ('AddItem') BEGIN
	-- 1. impossible to add issues with date less than today 
	If	@issueDate <= dbo.ToShortDate(getdate()) And @IsAdmin <> 1 And @IsTrafficManager <> 1 BEGIN
		RAISERROR('IncorrectIssueDate', 16, 1)
		RETURN
	END

	If @positionId <> 0 
		And Exists (
			Select 1 From ModuleTariff mt Inner Join Tariff t on mt.tariffID = t.tariffID 
			Where mt.modulePriceListID = @modulePricelistID And t.maxCapacity in (1, 2)
			) 
		Begin
			RAISERROR('ModuleSetPositionForbidden', 16, 1)
			RETURN
		End

		-- нельзя добавить несколько 'первых' роликов в окно в рамках одной акции, даже если 
	-- это макет. Такую акцию потом не активировать без ошибок
	If @positionId <> 0 And Exists (
		Select 1 
		From 
			ModuleIssue i Inner Join Campaign c On c.campaignID = i.campaignID
		Where
			i.issueDate = @issueDate
			And i.moduleID = @moduleID
			And i.positionId = @positionId
			And c.actionID = @actionID
		)
		Begin
			RAISERROR('PositionErrorForTheSameAction', 16, 1)
			RETURN 
		End
END

IF @actionName = 'AddItem' BEGIN	
	SELECT @issuePrice = dbo.fn_GetIssuePrice(
		@rollerDuration, @tariffPrice, 1, @positionId, @extraChargeFirst, 
		@extraChargeSecond, @extraChargeLast)

	INSERT INTO [ModuleIssue](campaignID, moduleID, modulePricelistID, issueDate, rollerID, ratio, positionId, isConfirmed, tariffPrice)
	VALUES(@campaignID, @moduleID, @modulePricelistID, @issueDate, @rollerID, @ratio, @positionId, @isConfirmed, @issuePrice)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @moduleIssueID = SCOPE_IDENTITY()

	declare @rolActionTypeID tinyint
	SELECT @rolActionTypeID = [rolActionTypeID] FROM [Roller] WHERE [rollerID] = @rollerID

	-- Нельзя смешивать политическую агитацию (тип 6) с другой рекламой в одной акции:
	-- отчётность перед избиркомом ведётся отдельно по каждому кандидату
	if @rolActionTypeID is not null
		and ((@rolActionTypeID = 6 and exists (
				select 1
				from Issue i
					inner join Campaign c on c.campaignID = i.campaignID
					inner join Roller r on r.rollerID = i.rollerID
				where c.actionID = @actionID
					and r.rolActionTypeID not in (6, 7, 44, 55)
			))
			or (@rolActionTypeID not in (6, 7, 44, 55) and exists (
				select 1
				from Issue i
					inner join Campaign c on c.campaignID = i.campaignID
					inner join Roller r on r.rollerID = i.rollerID
				where c.actionID = @actionID
					and r.rolActionTypeID = 6
			)))
	begin
		raiserror('AgitationMixError', 16, 1)
		return
	end

	exec @res = hlp_IssueVerify
		Null,
		@actionName,
		@massmediaID, --  smallint
		@DeadLine, --  datetime
		null, --  int
		@issueDate, --  datetime
		@rollerDuration, --  int
		@rightToGoBack, --  bit
		@isAdmin, --  bit
		@IsTrafficManager,
		@rightForMinus, --  bit
		@campaignFinishDate, --  datetime
		@campaignTypeID, --  tinyint
		@isConfirmed, --  bit
		@positionId, --  smallint
		NULL, --  int
		NULL, --  int
		NULL, --  int
		@rolActionTypeID, --  tinyint
		@msgError out,
		@modulePricelistID,
		1
	IF @res = 1 BEGIN
		RAISERROR(@msgError, 16, 1)
		RETURN 
	END

	declare @activationdate datetime 
	if @isConfirmed = 1
		set @activationdate = getdate()
	else 
		set @activationdate = null

	INSERT INTO [Issue](rollerID, actualWindowID, originalWindowId, campaignID, positionId, ratio, moduleIssueID, [packModuleIssueID], isConfirmed, [tariffPrice], grantorID, activationDate)
	select distinct @rollerID, tw.windowId, tw.windowId, @campaignID, @positionId, @ratio, @moduleIssueID, null, @isConfirmed, 0, @grantorID, @activationdate  
	from TariffWindow tw 
		Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
		Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID
	WHERE 
		mt.modulePriceListID = @modulePricelistID
		and tw.dayOriginal = @issueDate
	
	IF NOT EXISTS(SELECT * FROM [Issue] i WHERE i.moduleIssueID = @moduleIssueID)
	BEGIN
		DELETE FROM [ModuleIssue] WHERE moduleIssueID = @moduleIssueID
		SELECT -1 AS moduleIssueID
	END
	ELSE
		Exec [ModuleIssueRetrieve] @moduleIssueID = @moduleIssueID			
END
ELSE IF @actionName = 'DeleteItem' 
	BEGIN
	Select @issueDate = IssueDate, @isConfirmed = isConfirmed From ModuleIssue Where moduleIssueID = @moduleIssueID

	If	@issueDate < dbo.ToShortDate(getdate()+1) And @IsAdmin <> 1 And @IsTrafficManager <> 1 And @isConfirmed = 1 BEGIN
		RAISERROR('PastIssue', 16, 1)
		RETURN
	END	

	-- только админ может удалять выпуск активированной акции, если траффик-менеджер уже закрыл период
	if @IsConfirmed = 1 and @IsAdmin <> 1 And @IsTrafficManager <> 1 and @issueDate <= @deadLine
	begin
		raiserror('DeadLineViolationDelete', 16, 1)
		return
	end 

	IF @isConfirmed = 1
	BEGIN 
		insert into LogDeletedIssue (userId,rollerId,actionId,issueDate,massmediaID) 
		select @loggedUserId, i.rollerID, @actionID, tw.windowDateActual, tw.massmediaID  from Issue i inner join TariffWindow tw on tw.windowID = i.actualWindowID where i.moduleIssueID = @moduleIssueID
		
		if datediff(day,dbo.ToShortDate(getdate()),@issueDate) <= dbo.f_SysParamsDaysLog() 			
			exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	END 
	
	delete from Issue where moduleIssueID = @moduleIssueID
	DELETE FROM [ModuleIssue] WHERE moduleIssueID = @moduleIssueID

	END
else if @actionName = 'UpdateItem'
begin 
	if exists(select * from ModuleIssue where moduleIssueID = @moduleIssueID and @positionId <> positionId)
	begin 
		select @rollerDuration = r.duration,
			@rolActionTypeID = r.rolActionTypeID,
			@tariffPrice = mpl.price
		from ModuleIssue mi 
			inner join Roller r on mi.rollerID = r.rollerID
			inner join ModulePriceList mpl on mi.modulePriceListID = mpl.modulePriceListID
		where mi.moduleIssueID = @moduleIssueID
				
		exec @res = hlp_IssueVerify
			Null,
			@actionName,
			@massmediaID, --  smallint
			@DeadLine, --  datetime
			null, --  int
			@issueDate, --  datetime
			@rollerDuration, --  int
			@rightToGoBack, --  bit
			@isAdmin, --  bit
			@IsTrafficManager,
			@rightForMinus, --  bit
			@campaignFinishDate, --  datetime
			@campaignTypeID, --  tinyint
			@isConfirmed, --  bit
			@positionId, --  smallint
			NULL, --  int
			NULL, --  int
			NULL, --  int
			@rolActionTypeID, --  tinyint
			@msgError out,
			@modulePricelistID,
			1,
			@moduleIssueID = @moduleIssueID -- позиция проверяется в окнах выхода строк выпуска (Р-16)
		
		if @res = 1 
		begin
			RAISERROR(@msgError, 16, 1)
			RETURN 
		end
				
		SELECT @issuePrice = dbo.fn_GetIssuePrice(
			@rollerDuration, @tariffPrice, 1, @positionId, @extraChargeFirst, 
			@extraChargeSecond, @extraChargeLast)

		update ModuleIssue set tariffPrice = @issuePrice, positionId = @positionId where moduleIssueID = @moduleIssueID
		update Issue set positionId = @positionId where moduleIssueID = @moduleIssueID 
	end 
end 
if (@actionName in ('AddItem', 'UpdateItem'))
begin
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
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed + t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed + t1.countIssuesU
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 1
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 1
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 1
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed + firstCountU,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed + secondCountU,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed + lastCountU
	From
		(select i.actualWindowID as windowID, 
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as duration, 
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durationU, 
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as countIssues,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as countIssuesU,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end, 0)) as lastCount,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end, 0)) as firstCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end, 0)) as secondCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end, 0)) as lastCountU
		 from 
			Issue i 
			Inner Join Roller r On r.rollerId = i.rollerId
		where i.moduleIssueID = @moduleIssueID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
end
GO

IF EXISTS (SELECT * FROM sys.parameters WHERE object_id = OBJECT_ID('dbo.hlp_IssueVerify') AND name = '@moduleIssueID')
	AND OBJECT_DEFINITION(OBJECT_ID('dbo.ModuleIssueIUD')) LIKE '%@moduleIssueID = @moduleIssueID%'
	PRINT N'ГОТОВО: смена позиции выпуска модуля проверяется по окнам выхода (Р-16).';
ELSE
	RAISERROR(N'87: процедуры не обновлены.', 16, 1);
GO
