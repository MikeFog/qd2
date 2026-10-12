-- «Локальное СМИ» (4) и «Федеральное СМИ» (5) больше не позиционируются (пожелание заказчика 11.10.2026), как уже
-- «Агитационные материалы» (6), отбивки агитации (7, 44, 55) и локальное промо (8, 9): место в блоке им задаёт тип ролика.
-- 1. hlp_IssueVerify: ролик 4/5 с позицией (первый/второй/последний) — отказ RolType45PositionForbidden. Работает везде,
--    где выпуск проверяется: добавление и изменение выпуска («Сделать первым…»), шаблоны, модули, пакеты, перенос дня.
-- 2. RollerSubstitute: выпуск с позицией не заменяется на ролик 4/5 — строка в «Незамененные ролики»
--    (iMessageToSubtitute), остальные выпуски заменяются.
-- 3. Сообщения iMessage / iMessageToSubtitute (+ испанский перевод, где есть многоязычность веба).
-- Уже стоящие выпуски не трогаются: на ArtvisDev и Tumen будущих выпусков 4/5 с позицией нет (последние — 01.01.2026).
--
-- Ставить после 26 и 34 (несёт их правки обеих процедур) — без них скрипт ничего не меняет. Клиент не нужен;
-- после наката перезапустить qd2 и веб (новое сообщение в iMessage читается при старте). Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 37_smi-position-forbidden.sql

SET NOCOUNT ON;
GO
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) NOT LIKE N'%i.actualWindowID = @windowID%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) NOT LIKE N'%PromoPositionForbidden%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitute')) NOT LIKE N'%d.windowID = i.actualWindowID%'
BEGIN
    RAISERROR(N'37: сначала Deploy/26 и 34. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- Сообщения (+ испанский перевод, где есть многоязычность веба)
DECLARE @msg NVARCHAR(4000) = N'Для роликов с типом "Локальное СМИ" и "Федеральное СМИ" позиционирование не применяется. Операция прервана.';
DECLARE @es  NVARCHAR(4000) = N'El posicionamiento no se aplica a los spots de tipo "Medio local" y "Medio federal". Operación cancelada.';
DECLARE @sub NVARCHAR(4000) = N'На ролик с типом "Локальное СМИ" или "Федеральное СМИ" нельзя заменить выпуск с позиционированием';

IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'RolType45PositionForbidden')
    UPDATE dbo.iMessage SET [message] = @msg WHERE name = 'RolType45PositionForbidden';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('RolType45PositionForbidden', @msg);

IF EXISTS (SELECT 1 FROM dbo.iMessageToSubtitute WHERE msgError = 'RolType45PositionForbidden')
    UPDATE dbo.iMessageToSubtitute SET [message] = @sub WHERE msgError = 'RolType45PositionForbidden';
ELSE
    INSERT INTO dbo.iMessageToSubtitute (msgError, [message]) VALUES ('RolType45PositionForbidden', @sub);

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg AS [source], @es AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
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
@packModulePriceListId int = null
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
																	INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID] and tw.dayActual = @issueDate
																	inner join DisabledWindow dw on tw.windowDateActual between dw.startDate And dw.finishDate and tw.massmediaID = dw.massmediaID
																	where pmc.pricelistID = @packModulePriceListId ) ) )
begin
	set @msgError = 'DisabledWindowInsert'
	return 1
end


--set @msgError = @rollerActionTypeID
--RETURN 1

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
		Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
		Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID
	where 
		mt.modulePriceListID = @modulePricelistID
		And tw.dayActual = @issueDate
	order by 1 desc
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
			AND tw.dayActual = @issueDate
			AND @issueDate <= mm.deadLine
		)
		Set @msgError = 'DeadLineViolation'

	If @msgError Is Null
		select top 1 @msgError = 
			case 
				when tw.isDisabled = 1 then 'DisabledInsertRoller'
				when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
				when @positionId <> 0 and (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
						or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
						or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
				when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
				when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
				when (@actionName = 'AddItem' Or @isConfirmed = 1) And @issueDate < @tomorrow And @RightToGoBack <> 1And @isTrafficManager = 0 then 'IncorrectIssueDate'
				else null 
			end 
		from [PackModuleContent]  pmc
			INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
			INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on tw.massmediaID = tw.massmediaID
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayActual = @issueDate
		order by 1 desc
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

	-- «Локальное СМИ» (4) и «Федеральное СМИ» (5) не позиционируются: выпуск с позицией на них не меняем
	If @newRollerActionTypeID In (4, 5) And IsNull(@position, 0) <> 0
	begin
		select @msgError = 'RolType45PositionForbidden'
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

-- Проверка: новые версии применены, сообщения есть.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) LIKE N'%RolType45PositionForbidden%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitute')) LIKE N'%RolType45PositionForbidden%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitute')) LIKE N'%d.windowID = i.actualWindowID%'
   AND EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'RolType45PositionForbidden')
   AND EXISTS (SELECT 1 FROM dbo.iMessageToSubtitute WHERE msgError = 'RolType45PositionForbidden')
    PRINT N'ГОТОВО: «Локальное СМИ» и «Федеральное СМИ» не позиционируются (2 процедуры + сообщения).';
ELSE
    RAISERROR(N'37: новая версия применена не ко всем процедурам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
