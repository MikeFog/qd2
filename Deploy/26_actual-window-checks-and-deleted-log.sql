-- Фактическое окно выпуска, шаг 3 (docs/tasks/window-actual-switch.md, §5.5 п. 1: остаток этапа 1 и А-3).
-- Окно выпуска — то, где он реально выходит (Issue.actualWindowID), а не то, куда его изначально поставили.
-- С удалением broadcastStart эти места не пересекаются. Параметры и выдача процедур не меняются.
--
-- 1. Один ролик «Локальное СМИ»/«Локальное СМИ (агитация)», «Федеральное СМИ»/«Федеральное СМИ (агитация)»,
--    «Отбивка политической агитации» в окне — hlp_IssueVerify (постановка), RollerSubstitute:152 (замена ролика);
--    одна позиция в окне в пределах акции — IssueIUD:256. Раньше проверялось окно, куда ролик поставили.
-- 2. IssueIUD UpdateItem («Сделать первым…» и т. п.) без @windowID брал исходное окно и возвращал туда выпуск,
--    перенесённый трафиком (:73 → :376). Теперь берёт окно выхода.
-- 3. Активация, «Не использовать окна, где есть ролики данной фирмы» — ActionActivate:306.
-- 4. «Использовать только для модулей» у тарифа — TariffIUD:224.
-- 5. Журнал «Удаленные рекламные выпуски» — время выхода (windowDateActual) окна, где выпуск выходил:
--    IssueIUD, CampaignsIssueDelete, CampaignIUD, ActionIUD, CampaignModuleIssueDelete, CampaignPackDayDelete,
--    ModuleIssueIUD, PackModuleIssueID. Старые записи не пересчитываются.
-- 6. «Предметы рекламы» в «Размещение комбо-модулями» — ComboModuleFreeTimeRetrieve.
--
-- Идемпотентен. Клиент не нужен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 26_actual-window-checks-and-deleted-log.sql

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

CREATE OR ALTER PROCEDURE [dbo].[IssueIUD]
(
@issueID int = NULL OUT,
@rollerID int = NULL,
@rollerDuration smallint = NULL,
@windowID int = NULL,
@tariffWindowPrice decimal(18,2) = NULL,
@campaignID int = NULL,
@issueDate datetime = NULL,
@positionId int = 0,
@ratio decimal(18,10) = 1,
@moduleIssueID int = NULL,
@packModuleIssueID int = NULL,
@isConfirmed bit = NULL,
@loggedUserId smallint,
@massmediaID SMALLINT = NULL,
@actionName varchar(32),
@grantorID SMALLINT = NULL
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
DECLARE	
	@isAdmin bit,	
	@IsTrafficManager bit,	
	@rightForMinus bit, 
	@rightToGoBack bit,
	@campaignTypeID smallint,
	@finishDate datetime,
	@startDate datetime,
	@totalRollersDuration int,
	@timeBonus int,
	@issuesDuration int,
	@extraChargeFirst tinyint,
	@extraChargeSecond tinyint,
	@extraChargeLast tinyint,
	@issuePrice decimal(18,2),
	@deadLine datetime,
	@res smallint,
	@msgError varchar(64),
	@rolActionTypeID TINYINT,
	@actionID int,
	@managerDiscount decimal(18,10),
	@campaignStartDate datetime,
	@campaignFinishDate datetime

If @campaignID Is Null
	Select @campaignID = campaignID From Issue Where issueID = @issueID

EXEC hlp_GetMainUserCredentials
	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT, @grantorID 

SELECT 
	@actionID = c.[actionID],
	@massmediaID = ISNULL(@massmediaID, c.massmediaID),
	@campaignTypeID	= c.campaignTypeID,
	@finishDate = c.finishDate,
	@timeBonus = c.timeBonus,
	@issuesDuration = c.issuesDuration,
	@deadLine = m.deadLine,
	@isConfirmed = a.[isConfirmed],
	@managerDiscount = c.managerDiscount,
	@campaignStartDate = case when coalesce(c.startDate, @issueDate) > @issueDate then @issueDate else coalesce(c.startDate, @issueDate) end,
	@campaignFinishDate = case when coalesce(c.finishDate, @issueDate) < @issueDate then @issueDate else coalesce(c.finishDate, @issueDate) end
FROM 
	Campaign c
	LEFT Join Massmedia m On m.massmediaId = c.massmediaId
	INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
WHERE 
	c.campaignID = @campaignID

If @windowID Is Null and @issueID is not null
	Select @windowID = actualWindowID From Issue Where issueID = @issueID

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

-- Only admin is allowed to delete issue which is in the past already
if @IsConfirmed = 1 and @actionName = 'DeleteItem' and @IsAdmin <> 1 And @IsTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
				where i.issueID = @issueID and tw.dayOriginal <= dbo.ToShortDate(getdate()))
begin
	raiserror('PastIssue', 16, 1)
	return
end

-- только админ может удалять выпуск активированной акции, если траффик-менеджер уже закрыл период
if @IsConfirmed = 1 and @actionName = 'DeleteItem' and @IsAdmin <> 1  And @IsTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
				where i.issueID = @issueID and tw.dayOriginal <= dbo.ToShortDate(@deadLine))
begin
	raiserror('DeadLineViolationDelete', 16, 1)
	return
end 

--select @loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate
if @actionName in ('AddItem', 'UpdateItem') 
	and dbo.[fn_IsAcceptRatioForUser](@loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0
begin 
	 raiserror('CannotChangeCampaignWithMaxDiscount', 16, 1)
	 return
end

-- Длительность ролика и цена окна при добавлении - из базы, а не от клиента: в открытой
-- давно форме кампании они могли устареть, и цена с проверкой окна разошлись бы с
-- занятостью окна, которую процедура всё равно считает по Roller.duration.
if @actionName = 'AddItem'
begin
	SELECT @rollerDuration = [duration] FROM [Roller] WHERE [rollerID] = @rollerID
	SELECT @tariffWindowPrice = [price] FROM [TariffWindow] WHERE [windowId] = @windowID
end

-- Ролик выпуска здесь не меняется: цена и проверка окна в ветке UpdateItem считаются по
-- ролику выпуска. Замена ролика - RollerSubstitute.
if @actionName = 'UpdateItem' and @rollerID is not null
	and not exists (select 1 from [Issue] where [issueID] = @issueID and [rollerID] = @rollerID)
begin
	raiserror('InternalError', 16, 1)
	return
end

if @actionName in ('AddItem', 'UpdateItem')
	SELECT @rolActionTypeID = [rolActionTypeID] FROM [Roller] WHERE [rollerID] = @rollerID

-- Нельзя смешивать политическую агитацию (тип 6) с другой рекламой в одной акции:
-- отчётность перед избиркомом ведётся отдельно по каждому кандидату
if @actionName in ('AddItem', 'UpdateItem') and @rolActionTypeID is not null
	and ((@rolActionTypeID = 6 and exists (
			select 1
			from Issue i
				inner join Campaign c on c.campaignID = i.campaignID
				inner join Roller r on r.rollerID = i.rollerID
			where c.actionID = @actionID
				and r.rolActionTypeID not in (6, 7, 44, 55)
				and i.issueID != IsNull(@issueID, -1)
		))
		or (@rolActionTypeID not in (6, 7, 44, 55) and exists (
			select 1
			from Issue i
				inner join Campaign c on c.campaignID = i.campaignID
				inner join Roller r on r.rollerID = i.rollerID
			where c.actionID = @actionID
				and r.rolActionTypeID = 6
				and i.issueID != IsNull(@issueID, -1)
		)))
begin
	raiserror('AgitationMixError', 16, 1)
	return
end

if (@actionName in ('DeleteItem', 'UpdateItem'))
BEGIN
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When i.isConfirmed = 1 AND [maxCapacity] = 0 
					Then timeInUseConfirmed - r.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When i.isConfirmed = 0 AND [maxCapacity] = 0
					Then timeInUseUnconfirmed - r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When (i.isConfirmed = 1 AND [maxCapacity] > 0) 
					Then capacityInUseConfirmed - 1
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When (i.isConfirmed = 0 AND [maxCapacity] > 0)
					Then capacityInUseUnconfirmed - 1
				Else capacityInUseUnconfirmed
			End,
		isFirstPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = -20 Then 0
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = -10 Then 0
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = 10 Then 0
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = 
			Case  
				When i.isConfirmed = 0 And i.positionId = -20 Then firstPositionsUnconfirmed - 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed = 
			Case 
				When i.isConfirmed = 0 And i.positionId = -10 Then secondPositionsUnconfirmed - 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed = 
			Case	
				When i.isConfirmed = 0 And i.positionId = 10 Then lastPositionsUnconfirmed - 1
				Else	lastPositionsUnconfirmed
			End
	From
		Issue i 
		Inner Join Roller r On r.rollerId = i.rollerId
	Where
		TariffWindow.windowId = i.actualWindowId
		and i.issueID = @issueID
END

IF @actionName = 'AddItem' BEGIN
	If Exists (Select 1 From Roller where rollerID = @rollerID And advertTypeID Is Null And @isConfirmed = 1) Begin
		RAISERROR('RollerWithoutAdvertType', 16, 1)
		RETURN 
	End

	--select 1, @issueDate

	Exec @res = hlp_IssueVerify	
		Null,
		@actionName, @massmediaID, @deadLine, @windowID, @issueDate, @rollerDuration,
		@rightToGoBack,	@isAdmin, @IsTrafficManager, @rightForMinus, @finishDate,
		@campaignTypeID, @isConfirmed, @positionId, @timeBonus,
		@issuesDuration, NULL, @rolActionTypeID, @msgError out
	IF @res = 1 BEGIN
		RAISERROR(@msgError, 16, 1)
		RETURN 
	END

	-- нельзя добавить несколько 'первых' роликов в окно в рамках одной акции, даже если 
	-- это макет. Такую акцию потом не активировать без ошибок
	If @positionId <> 0 And Exists (
		Select 1 
		From 
			Issue i Inner Join Campaign c On c.campaignID = i.campaignID
		Where
			i.actualWindowID = @windowID
			And i.positionId = @positionId
			And c.actionID = @actionID
		)
		Begin
			RAISERROR('PositionErrorForTheSameAction', 16, 1)
			RETURN 
		End
	
	SELECT @issuePrice = dbo.fn_GetIssuePrice(
		@rollerDuration, @tariffWindowPrice, 1, @positionId, @extraChargeFirst, 
		@extraChargeSecond, @extraChargeLast)

	declare @activationdate datetime 
	if @isConfirmed = 1
		set @activationdate = getdate()
	else 
		set @activationdate = null

	-- Add issue
	INSERT INTO [Issue](rollerID, actualWindowID, originalWindowId, campaignID, positionId, ratio, moduleIssueID, [packModuleIssueID], isConfirmed, [tariffPrice], grantorID, activationDate)
	VALUES(@rollerID, @windowID, @windowID, @campaignID, @positionId, @ratio, @moduleIssueID, @packModuleIssueID, @isConfirmed, @issuePrice, @grantorID, @activationDate)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return
	end

	SET @issueID = SCOPE_IDENTITY()

	-- Политическая агитация: размещение в уже активированной акции создаёт сразу
	-- подтверждённый выпуск минуя ActionActivate - обвязку добавляем здесь.
	-- В черновике обвязка не нужна: её создаст активация.
	IF @rolActionTypeID = 6 AND @isConfirmed = 1
		EXEC AgitationFraming
			@actionName = 'InsertForWindow',
			@windowID = @windowID,
			@loggedUserID = @loggedUserId
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	-- Политическая агитация: до удаления запоминаем тип ролика и окно, чтобы после
	-- удаления последнего типа 6 снять авто-обвязку (44/7/55) служебной акции
	DECLARE @delRolActionTypeID tinyint, @delActualWindowID int
	SELECT @delRolActionTypeID = r.rolActionTypeID, @delActualWindowID = i.actualWindowID
	FROM Issue i INNER JOIN Roller r ON r.rollerID = i.rollerID
	WHERE i.issueID = @issueID

	IF (@isConfirmed = 1)
	BEGIN
		If @rollerID Is Null
			Select @rollerID = rollerID From Issue Where issueID = @issueID

		DECLARE @actualDate DATETIME
		SELECT @actualDate = tw.windowDateActual FROM [Issue] i inner join TariffWindow tw on i.actualWindowID = tw.windowId WHERE i.[issueID] = @issueID
		EXEC [LogDeletedIssueInsert] @loggedUserId, @actionId, @rollerID, @actualDate, @massmediaID
		
		if exists(SELECT * FROM [Issue] i inner join TariffWindow tw on i.actualWindowID = tw.windowId 
		and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog() 
		WHERE i.[issueID] = @issueID) 
		begin 
			exec SayAdminThatIssuesDelete @loggedUserID, @actionID
		end 
	END 
	
	DELETE FROM [Issue] WHERE IssueID = @IssueID

	IF @delRolActionTypeID = 6
		EXEC AgitationFraming
			@actionName = 'CleanupWindow',
			@windowID = @delActualWindowID,
			@loggedUserID = @loggedUserId
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	DECLARE @oldPositionID INT 
	SELECT 
		@windowID = CASE WHEN @windowID IS NULL THEN i.[actualWindowID] ELSE @windowID END,
		@rollerID = CASE WHEN @rollerID IS NULL THEN i.rollerID ELSE @rollerID END,
		@campaignID = CASE WHEN @campaignID IS NULL THEN i.campaignID ELSE @campaignID END,
		@positionId = CASE WHEN @positionId IS NULL THEN i.[positionId] ELSE @positionId END,
		@ratio = CASE WHEN @ratio IS NULL THEN i.ratio ELSE @ratio END,
		@oldPositionID = i.[positionId],
		@tariffWindowPrice = tw.[price],
		@rollerDuration = r.[duration],
		@ratio = i.[ratio],
		@issuePrice = i.[tariffPrice],
		@issuesDuration = @issuesDuration -
			case 
				when @campaignTypeID = 2 then dbo.f_GetSponsorDuration(r.[duration], i.[positionId], @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
					else @rollerDuration 
			end
	FROM [Issue] i 
		inner join Roller r on i.rollerID = r.rollerID
		INNER JOIN [TariffWindow] tw ON i.[actualWindowID] = tw.[windowId]
	WHERE 
		i.[issueID] = @issueID	

	-- @rollerID мог прийти пустым и быть взят из выпуска выше: тип ролика - по нему
	SELECT @rolActionTypeID = [rolActionTypeID] FROM [Roller] WHERE [rollerID] = @rollerID
	
	Exec @res = hlp_IssueVerify	
		@issueID,
		@actionName, @massmediaID, @deadLine, @windowID, @issueDate, @rollerDuration,
		@rightToGoBack,	@isAdmin, @IsTrafficManager, @rightForMinus, @finishDate,
		@campaignTypeID, @isConfirmed, @positionId, @timeBonus,
		@issuesDuration, NULL, @rolActionTypeID, @msgError out
	IF @res = 1 BEGIN
		RAISERROR(@msgError, 16, 1)
		RETURN 
	end
	
	DECLARE @tariffPrice decimal(18,2)
	
	IF @oldPositionID != @positionId
		SELECT @issuePrice = dbo.fn_GetIssuePrice(@rollerDuration, @tariffWindowPrice, 1, @positionId, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)

	UPDATE	
		[Issue]
	SET	
		rollerID = @rollerID, 
		actualWindowID = @windowID, 
		positionId = @positionId, 
		ratio = @ratio,
		[tariffPrice] = @issuePrice
	WHERE		
		IssueID = @IssueID
end

if (@actionName in ('AddItem', 'UpdateItem'))
BEGIN
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When i.isConfirmed = 1 AND [maxCapacity] = 0 
					Then timeInUseConfirmed + r.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When i.isConfirmed = 0 AND [maxCapacity] = 0 
					Then timeInUseUnconfirmed + r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When (i.isConfirmed = 1 AND [maxCapacity] > 0) 
					Then capacityInUseConfirmed + 1
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When (i.isConfirmed = 0 AND [maxCapacity] > 0)
					Then capacityInUseUnconfirmed + 1
				Else capacityInUseUnconfirmed
			End,
		isFirstPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = -20 Then 1
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = -10 Then 1
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = 10 Then 1
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = 
			Case  
				When i.isConfirmed = 0 And i.positionId = -20 Then firstPositionsUnconfirmed + 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed = 
			Case 
				When i.isConfirmed = 0 And i.positionId = -10 Then secondPositionsUnconfirmed + 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed = 
			Case	
				When i.isConfirmed = 0 And i.positionId = 10 Then lastPositionsUnconfirmed + 1
				Else	lastPositionsUnconfirmed
			End
	From
		Issue i
		Inner Join Roller r On r.rollerId = i.rollerId
	Where
		TariffWindow.windowId = i.actualWindowId
		and i.issueID = @issueID
END
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
			create table #days (windowID int, issueDate datetime)
			insert into [#days] (windowID,issueDate) 
			select @originalWindowID, tw.dayOriginal from TariffWindow tw where tw.windowId = @originalWindowID
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
	pl.extraChargeFirstRoller, 
	pl.extraChargeSecondRoller, 
	pl.extraChargeLastRoller,
	mm.deadline,
	tw.price,
	tw.dayOriginal
from
	Issue i
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	inner join Tariff t on t.tariffID = tw.tariffId
	inner join Pricelist pl on pl.pricelistID = t.pricelistID
	inner join Campaign c on i.campaignID = c.campaignID
	inner join MassMedia mm on tw.massmediaID = mm.massmediaID
	left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
	left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
	left join PackModulePriceList pmpl on pmi.priceListID = pmpl.priceListID
	inner join #days d on tw.dayOriginal = d.issueDate and (@campaignTypeID not in (1,2) or d.windowID = i.originalWindowID)
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

select row_number() over(order by tw.windowDateOriginal) as RowNum, tw.windowDateOriginal, msg.message
from @issues i
	inner join iMessageToSubtitute msg on i.msgError = msg.msgError
	inner join Issue ii on i.issueID = ii.issueID
	inner join TariffWindow tw on ii.originalWindowID = tw.windowId
where i.msgError is not null
order by tw.windowDateOriginal
GO

CREATE OR ALTER PROC [dbo].[ActionActivate]
(
@actionID int,
@loggedUserID smallint,
@isTestActivate bit,
@tryTransferFailedIssues bit = 0,
@allowDifferentWindowPrice bit = 0,
@transferAttemptCount int = 0,
@avoidFirmRollerWindows bit = 1
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON	

DECLARE @isAdmin bit, @rightForMinus bit, @rightToGoBack bit, @IsTrafficManager bit
DECLARE @blockActivation bit = 0;
	
EXEC hlp_GetMainUserCredentials
	@loggedUserId = @loggedUserID, @rightToGoBack = @rightToGoBack out, @isAdmin = @isAdmin out, @rightForMinus = @rightForMinus out,  @IsTrafficManager = @IsTrafficManager out

declare @issue table (
	name nvarchar(64) not null,
	advertTypeName nvarchar(256) null,
	statusDescription nvarchar(64) not null,
	duration int not null,
	positionID int not null,
	issueDate datetime not null,
	issueID int primary key,
	rollerID int not null,
	moduleIssueID int,
	packModuleIssueID int,
	campaignID int not null,
	campaignTypeID tinyint not null,
	massmediaID int not null, 
	windowId int not null,
	oldWindowId int not null,
	oldIssueDate datetime not null,
	oldWindowPrice decimal(18, 2) not null,
	grantorID smallint null,
	isTransferred bit not null default 0,
	transferStatus nvarchar(64) null
)

declare @fatalErrors table (name nvarchar(64) not null)
declare @plannedTransfers table (
	issueID int primary key,
	oldWindowID int not null,
	newWindowID int not null,
	oldIssueDate datetime not null,
	newIssueDate datetime not null
)

declare @tomorrow datetime
select @tomorrow = dateadd(day, 1, CONVERT(date, getdate()))

declare @firmID smallint
select @firmID = firmID from [Action] where actionID = @actionID

DECLARE cur_issues CURSOR FOR
Select i.issueId From Issue i inner join Campaign c on c.campaignID = i.campaignID  where c.actionID = @actionID and i.isConfirmed = 0
declare @issueId int

OPEN cur_issues
FETCH NEXT FROM cur_issues INTO @issueId

WHILE @@FETCH_STATUS = 0
BEGIN
	insert into @issue([name], advertTypeName, statusDescription,duration,positionID,issueDate,issueID,rollerID,moduleIssueID,packModuleIssueID,campaignID,campaignTypeID,massmediaID, windowId, oldWindowId, oldIssueDate, oldWindowPrice, grantorID)
	select 
		r.[name],
		r.advertTypeName,
		case 
			when c.finishDate < @tomorrow and c.campaignTypeID <> 2 and @rightToGoBack <> 1 then 'CampaignAlreadyFinished' 
			when (tw.isDisabled = 1) then 'DisabledInsertRoller' 
			when (r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0) then 'DisabledInsertSimpleRoller' 
			when ((tw.isFirstPositionOccupied = 1 And i.positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And i.positionId = -10)
					or (tw.isLastPositionOccupied = 1	And i.positionId = 10)) then 'FirstLastIssueError' 
			when (tw.dayActual < @tomorrow) and @rightToGoBack <> 1 And @IsTrafficManager = 0 then 'IncorrectIssueDate' 
			when (@rightForMinus = 0 
				and (tw.[timeInUseConfirmed] + r.duration + (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId) > tw.duration) 
				and (i.grantorID is null or (dbo.fn_IsRightForMinus(i.grantorID) = 0)))
				then 'WindowOverflow' 
			when (@rightForMinus = 0 and (tw.[maxCapacity] > 0 
				AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1 + (Select count(*) From @issue it2 Where it2.windowId = tw.windowId))) < 0) 
				and (i.grantorID is null or (dbo.fn_IsRightForMinus(i.grantorID) = 0))) 
				then 'WindowMaxCapacityOverflow' 
			when r.advertTypeID Is Null then 'RollerWithoutActionType' 
			when tw.dayOriginal <= mm.deadLine And @isAdmin = 0 And @IsTrafficManager = 0  then 'DeadLineViolation'
			else 'OK'
		end,
		r.duration, i.positionId, tw.windowDateActual, i.issueID, i.rollerID, i.moduleIssueID, i.packModuleIssueID, i.campaignID, c.campaignTypeID, mm.massmediaID, tw.windowId, tw.windowId, tw.windowDateActual, tw.price, i.grantorID
	from Issue i 
		inner join vRoller r on i.rollerID = r.rollerID
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Campaign c on c.campaignID = i.campaignID 
		inner join MassMedia mm on mm.massmediaID = tw.massmediaID
	where i.issueID = @issueId

	FETCH NEXT FROM cur_issues INTO @issueId
END

CLOSE cur_issues
DEALLOCATE cur_issues

declare @programmissue table (
	[name] nvarchar(64) not null,
	statusDescription nvarchar(64) not null,
	duration int not null,
	issueID int primary key,
	campaignID int not null,
	issueDate datetime not null,
	advertTypeName nvarchar(256)
)

insert into @programmissue (
	[name],
	statusDescription,
	duration,
	issueDate,
	issueID,
	campaignID,
	advertTypeName
) 
select 
	COALESCE(NULLIF(LTRIM(RTRIM(st.comment)), ''), sp.[name]),
case 
    when i.advertTypeID Is Null then 'RollerWithoutActionType'
    when (i.issueDate < @tomorrow) and @rightToGoBack <> 1 and @IsTrafficManager = 0 then 'IncorrectIssueDate'
    when i2.issueID is null then 'OK' 
    else 'AlreadySponsered' 
end,
	st.duration,
	i.issueDate,
	i.issueID,
	i.campaignID,
	adv.name
from ProgramIssue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join SponsorProgram sp on i.programID = sp.sponsorProgramID
	inner join SponsorTariff st on i.tariffID = st.tariffID
	left join AdvertType adv on adv.advertTypeID = i.advertTypeID
	left join ProgramIssue i2 on i.issueID <> i2.issueID and i.programID = i2.programID 
		and i.tariffID = i2.tariffID and i.issueDate = i2.issueDate and i2.isConfirmed = 1 
where c.actionID = @actionID and i.isConfirmed = 0

update i2 set i2.statusDescription = 'PartOfModule' 
from @issue i INNER JOIN @issue i2 ON i.moduleIssueID = i2.moduleIssueID
	where i.statusDescription <> 'OK' and i2.statusDescription like 'OK' 

update i2 set i2.statusDescription = 'PartOfPackModule' 
from @issue i INNER JOIN @issue i2 ON i.packModuleIssueID = i2.packModuleIssueID
	where i.statusDescription <> 'OK' and i2.statusDescription like 'OK' 

if exists(
		select * from 
		(
			select c.campaignID, SUM(dbo.f_GetSponsorDuration(r.[duration], i.positionId, pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller)) as sumDuration 
			from [Issue] i 
				inner join @issue i2 on i.issueID = i2.issueID and i2.statusDescription like 'OK'
				inner join Campaign c on i.campaignID = c.campaignID
				inner join TariffWindow tw on i.originalWindowID = tw.windowId
				Inner Join Tariff t on tw.tariffId = t.tariffID
				Inner Join Pricelist pl On pl.pricelistID = t.pricelistID
				inner join Roller r on i.rollerID = r.rollerID
				inner join MassMedia mm on tw.massmediaID = mm.massmediaID
			where 
				c.actionID = @actionID and c.campaignTypeID = 2
			group by 
				c.campaignID
		) as x
		left join (
			select c.campaignID, sum(pl.bonus) as sumBonus
			from [ProgramIssue] i 
				inner join @programmissue i2 on i.issueID = i2.issueID and i2.statusDescription like 'OK'
				inner join Campaign c on c.campaignID = i.campaignID
				inner join SponsorTariff st on i.tariffID = st.tariffID
				inner join [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
			where c.actionID = @actionID and c.campaignTypeID = 2
			group by c.campaignID
		) as y on x.campaignID = y.campaignID 
		where y.sumBonus is null or y.sumBonus < x.sumDuration 
	)
begin 
    UPDATE it
      SET it.statusDescription = 'TimeBonusExceed'
    FROM @issue it
    INNER JOIN Campaign c ON c.campaignID = it.campaignID
    WHERE c.actionID = @actionID
      AND c.campaignTypeID = 2
      AND it.statusDescription = 'OK';

    SET @blockActivation = 1;
	Insert Into @fatalErrors(name) values('CannotPerfomActivationSponsor')
end

-- Политическая агитация: у станций всех активируемых роликов типа 6 должны быть
-- заполнены три ролика обвязки в карточке, иначе обвязку не из чего создать
if exists (
	select 1
	from @issue it
		inner join Roller r on r.rollerID = it.rollerID
		inner join MassMedia mm on mm.massmediaID = it.massmediaID
	where r.rolActionTypeID = 6
		and (mm.agitationLocalRollerID is null
			or mm.agitationAnnounceRollerID is null
			or mm.agitationFederalRollerID is null))
begin
	update it set it.statusDescription = 'AgitationStationRollersNotSet'
	from @issue it
		inner join Roller r on r.rollerID = it.rollerID
		inner join MassMedia mm on mm.massmediaID = it.massmediaID
	where r.rolActionTypeID = 6
		and (mm.agitationLocalRollerID is null
			or mm.agitationAnnounceRollerID is null
			or mm.agitationFederalRollerID is null)

	SET @blockActivation = 1;
	Insert Into @fatalErrors(name) values('AgitationStationRollersNotSet')
end

IF @isTestActivate = 0
	AND @blockActivation = 0
	AND @tryTransferFailedIssues = 1
	AND @transferAttemptCount > 0
BEGIN
	DECLARE
		@transferIssueID int,
		@transferSourceWindowID int,
		@transferSourceDate datetime,
		@transferSourcePrice decimal(18, 2),
		@transferPositionID int,
		@transferDuration int,
		@transferGrantorID smallint,
		@candidateWindowID int,
		@candidateIssueDate datetime

	DECLARE cur_transfer CURSOR LOCAL FOR
		SELECT issueID, oldWindowId, oldIssueDate, oldWindowPrice, positionID, duration, grantorID
		FROM @issue
		WHERE statusDescription <> 'OK'
			AND campaignTypeID = 1
			AND moduleIssueID IS NULL
			AND packModuleIssueID IS NULL

	OPEN cur_transfer
	FETCH NEXT FROM cur_transfer INTO @transferIssueID, @transferSourceWindowID, @transferSourceDate,
		@transferSourcePrice, @transferPositionID, @transferDuration, @transferGrantorID

	WHILE @@FETCH_STATUS = 0
	BEGIN
		SET @candidateWindowID = NULL
		SET @candidateIssueDate = NULL

		;WITH rankedCandidates AS
		(
			SELECT
				tw.windowId,
				tw.windowDateActual,
				attemptNo = ROW_NUMBER() OVER (ORDER BY tw.windowDateActual DESC)
			FROM TariffWindow tw
			WHERE tw.windowDateActual < @transferSourceDate
				AND tw.massmediaID = (SELECT massmediaID FROM @issue WHERE issueID = @transferIssueID)
				AND tw.dayActual = (SELECT dayActual FROM TariffWindow WHERE windowId = @transferSourceWindowID)
				AND tw.windowId <> @transferSourceWindowID
				AND tw.isDisabled = 0
				AND NOT EXISTS (SELECT 1 FROM Tariff t WHERE t.tariffID = tw.tariffId AND t.isForModuleOnly = 1)

			UNION ALL

			SELECT
				tw.windowId,
				tw.windowDateActual,
				attemptNo = ROW_NUMBER() OVER (ORDER BY tw.windowDateActual ASC)
			FROM TariffWindow tw
			WHERE tw.windowDateActual > @transferSourceDate
				AND tw.massmediaID = (SELECT massmediaID FROM @issue WHERE issueID = @transferIssueID)
				AND tw.dayActual = (SELECT dayActual FROM TariffWindow WHERE windowId = @transferSourceWindowID)
				AND tw.windowId <> @transferSourceWindowID
				AND tw.isDisabled = 0
				AND NOT EXISTS (SELECT 1 FROM Tariff t WHERE t.tariffID = tw.tariffId AND t.isForModuleOnly = 1)
		),
		validCandidates AS
		(
			SELECT
				rc.windowId,
				rc.windowDateActual,
				rc.attemptNo,
				freeTime = tw.duration - tw.timeInUseConfirmed
					- (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK')
			FROM rankedCandidates rc
				INNER JOIN TariffWindow tw ON tw.windowId = rc.windowId
				INNER JOIN Issue i ON i.issueID = @transferIssueID
				INNER JOIN Roller r ON r.rollerID = i.rollerID
				INNER JOIN Campaign c ON c.campaignID = i.campaignID
				INNER JOIN MassMedia mm ON mm.massmediaID = tw.massmediaID
			WHERE rc.attemptNo <= @transferAttemptCount
				AND (@allowDifferentWindowPrice = 1 OR tw.price = @transferSourcePrice)
				AND (@avoidFirmRollerWindows = 0
					OR (NOT EXISTS (
							SELECT 1
							FROM Issue fi
								INNER JOIN Campaign fc ON fc.campaignID = fi.campaignID
								INNER JOIN [Action] fa ON fa.actionID = fc.actionID
							WHERE fi.actualWindowID = tw.windowId
								AND fa.firmID = @firmID
								AND fa.deleteDate IS NULL
								AND fi.isConfirmed = 1)
						AND NOT EXISTS (
							SELECT 1 FROM @issue it2
							WHERE it2.windowId = tw.windowId
								AND it2.statusDescription = 'OK')))
				AND NOT (c.finishDate < @tomorrow and c.campaignTypeID <> 2 and @rightToGoBack <> 1)
				AND NOT (r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0)
				AND NOT ((tw.isFirstPositionOccupied = 1 And @transferPositionID = -20)
					or (tw.isSecondPositionOccupied = 1 And @transferPositionID = -10)
					or (tw.isLastPositionOccupied = 1 And @transferPositionID = 10))
				AND NOT EXISTS (
					SELECT 1
					FROM @issue it2
					WHERE it2.windowId = tw.windowId
						AND it2.statusDescription = 'OK'
						AND it2.positionID = @transferPositionID
						AND @transferPositionID IN (-20, -10, 10)
				)
				AND NOT ((tw.dayActual < @tomorrow) and @rightToGoBack <> 1 And @IsTrafficManager = 0)
				AND NOT (@rightForMinus = 0
					and (tw.[timeInUseConfirmed] + @transferDuration
						+ (Select IsNull(sum(it2.duration), 0) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK') > tw.duration)
					and (@transferGrantorID is null or (dbo.fn_IsRightForMinus(@transferGrantorID) = 0)))
				AND NOT (@rightForMinus = 0
					and (tw.[maxCapacity] > 0
						AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1
							+ (Select count(*) From @issue it2 Where it2.windowId = tw.windowId And it2.statusDescription = 'OK'))) < 0)
					and (@transferGrantorID is null or (dbo.fn_IsRightForMinus(@transferGrantorID) = 0)))
				AND r.advertTypeID Is Not Null
				AND NOT (tw.dayOriginal <= mm.deadLine And @isAdmin = 0 And @IsTrafficManager = 0)
		)
		SELECT TOP 1
			@candidateWindowID = windowId,
			@candidateIssueDate = windowDateActual
		FROM validCandidates
		ORDER BY attemptNo, freeTime DESC, windowDateActual

		IF @candidateWindowID IS NOT NULL
		BEGIN
			UPDATE @issue
			SET
				statusDescription = 'OK',
				windowId = @candidateWindowID,
				issueDate = @candidateIssueDate,
				isTransferred = 1,
				transferStatus = 'Transferred'
			WHERE issueID = @transferIssueID

			INSERT INTO @plannedTransfers(issueID, oldWindowID, newWindowID, oldIssueDate, newIssueDate)
			VALUES(@transferIssueID, @transferSourceWindowID, @candidateWindowID, @transferSourceDate, @candidateIssueDate)
		END

		FETCH NEXT FROM cur_transfer INTO @transferIssueID, @transferSourceWindowID, @transferSourceDate,
			@transferSourcePrice, @transferPositionID, @transferDuration, @transferGrantorID
	END

	CLOSE cur_transfer
	DEALLOCATE cur_transfer
END

SELECT 
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription, 
	i2.[name], 
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
FROM
	@ISSUE i2
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
WHERE i2.statusDescription like 'OK'
	and i2.isTransferred = 0
union all
select 
	'pi_' + cast(i.issueID as  varchar) as issueID,
	am.message as statusDescription, 
	i.[name], 
	i.advertTypeName,
	dbo.fn_Int2Time(i.duration) as duration,
	null as issuePosition,
	i.issueDate,
	m.name as radiostationName,
	m.groupName
from @programmissue i
	Inner Join Campaign c on c.campaignID = i.campaignID
	Inner Join vMassmedia m On m.massmediaID = c.massmediaID
	LEFT JOIN iMessageToActivate am ON i.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i.statusDescription like 'OK'
ORDER BY 
	issueDate 

select 
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription,	
	i2.[name],
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
from
	@ISSUE i2
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i2.statusDescription <> 'OK'
union all
select 
	'pi_' + cast(i.issueID as  varchar),
	am.message as statusDescription, 
	i.[name], 
	i.advertTypeName,
	dbo.fn_Int2Time(i.duration) as duration,
	null as issuePosition,
	i.issueDate as issueDate,
	m.name as radiostationName,
	m.groupName
from 
	@programmissue i
	Inner Join Campaign c on c.campaignID = i.campaignID
	Inner Join vMassmedia m On m.massmediaID = c.massmediaID
	LEFT JOIN iMessageToActivate am ON i.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
where i.statusDescription <> 'OK'
order by  
	issueDate 

Select am.message as errorMessage from @fatalErrors fe LEFT JOIN iMessageToActivate am ON fe.name COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT

SELECT
	'i_' + cast(i2.issueID as varchar) as issueID,
	am.message as statusDescription,
	i2.[name],
	i2.advertTypeName,
	dbo.fn_Int2Time(i2.duration) as duration,
	ip.[description] as issuePosition,
	i2.oldIssueDate,
	i2.issueDate,
	m.name as radiostationName,
	m.groupName
FROM
	@ISSUE i2
	INNER JOIN iIssuePosition ip ON ip.positionID = i2.positionID
	Inner Join vMassmedia m On m.massmediaID = i2.massmediaID
	LEFT JOIN iMessageToActivate am ON i2.statusDescription COLLATE DATABASE_DEFAULT = am.name COLLATE DATABASE_DEFAULT
WHERE i2.statusDescription like 'OK'
	and i2.isTransferred = 1
ORDER BY
	i2.issueDate

IF @isTestActivate = 0 AND @blockActivation = 0
begin
--	INSERT INTO [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
--	select @loggedUserID, @actionID, i.rollerID, it.issueDate, tw.massmediaID 
--	from @issue it inner join Issue i on it.issueID = i.issueID inner join TariffWindow tw on i.originalWindowID = tw.windowId where it.statusDescription <> 'OK'
/*
	delete from i from @issue it inner join Issue i on it.issueID = i.issueID where it.statusDescription <> 'OK'
	delete from mi from @issue it inner join ModuleIssue mi on it.moduleIssueID = mi.moduleIssueID where it.statusDescription <> 'OK'
	delete from pmi from @issue it inner join PackModuleIssue pmi on it.packModuleIssueID = pmi.packModuleIssueID where it.statusDescription <> 'OK'
	delete from i from @programmissue it inner join ProgramIssue i on it.issueID = i.issueID where it.statusDescription <> 'OK'
*/

	Update
		TariffWindow
	Set
		timeInUseUnconfirmed =
			Case
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseUnconfirmed =
			Case
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - 1
				Else capacityInUseUnconfirmed
			End,
		firstPositionsUnconfirmed =
			Case
				When i.positionId = -20 Then firstPositionsUnconfirmed - 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed =
			Case
				When i.positionId = -10 Then secondPositionsUnconfirmed - 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed =
			Case
				When i.positionId = 10 Then lastPositionsUnconfirmed - 1
				Else lastPositionsUnconfirmed
			End
	From
		@plannedTransfers pt
		Inner Join Issue i On i.issueID = pt.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
	Where
		TariffWindow.windowId = pt.oldWindowID

	Update i
	Set
		originalWindowID = pt.newWindowID,
		actualWindowID = pt.newWindowID,
		tariffPrice = dbo.fn_GetIssuePrice(
			r.duration,
			twNew.price,
			1,
			i.positionId,
			IsNull(pl.extraChargeFirstRoller, 0),
			IsNull(pl.extraChargeSecondRoller, 0),
			IsNull(pl.extraChargeLastRoller, 0))
	From
		Issue i
		Inner Join @plannedTransfers pt On pt.issueID = i.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
		Inner Join TariffWindow twNew On twNew.windowId = pt.newWindowID
		Left Join Tariff t On t.tariffID = twNew.tariffId
		Left Join Pricelist pl On pl.pricelistID = t.pricelistID

	Update
		TariffWindow
	Set
		timeInUseUnconfirmed =
			Case
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed + r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseUnconfirmed =
			Case
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed + 1
				Else capacityInUseUnconfirmed
			End,
		firstPositionsUnconfirmed =
			Case
				When i.positionId = -20 Then firstPositionsUnconfirmed + 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed =
			Case
				When i.positionId = -10 Then secondPositionsUnconfirmed + 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed =
			Case
				When i.positionId = 10 Then lastPositionsUnconfirmed + 1
				Else lastPositionsUnconfirmed
			End
	From
		@plannedTransfers pt
		Inner Join Issue i On i.issueID = pt.issueID
		Inner Join Roller r On r.rollerID = i.rollerID
	Where
		TariffWindow.windowId = pt.newWindowID

	INSERT INTO [TransferLog]([userID], [oldDate], [newDate], [actionID], [issueID])
	SELECT @loggedUserID, oldIssueDate, newIssueDate, @actionID, issueID
	FROM @plannedTransfers

-- 1. Удаление обычных выпусков (Issue)
	DECLARE @delIssueID int;
	DECLARE cur_del_issue CURSOR LOCAL FOR
		SELECT issueID FROM @issue WHERE statusDescription <> 'OK';
	
	OPEN cur_del_issue;
	FETCH NEXT FROM cur_del_issue INTO @delIssueID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[IssueIUD]
			@issueID = @delIssueID,
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_issue INTO @delIssueID;
	END
	CLOSE cur_del_issue;
	DEALLOCATE cur_del_issue;


	-- 2. Удаление ProgramIssue (через соответствующую процедуру)
	DECLARE @delProgIssueID int;
	DECLARE cur_del_prog CURSOR LOCAL FOR
		SELECT issueID FROM @programmissue WHERE statusDescription <> 'OK';
	
	OPEN cur_del_prog;
	FETCH NEXT FROM cur_del_prog INTO @delProgIssueID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[ProgramIssueIUD] -- Укажи правильное имя процедуры!
			@issueID = @delProgIssueID,
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_prog INTO @delProgIssueID;
	END
	CLOSE cur_del_prog;
	DEALLOCATE cur_del_prog;


	-- 3. Удаление ModuleIssue (уникальные ID, чтобы не дергать процу дважды для одного модуля)
	DECLARE @delModuleID int;
	DECLARE cur_del_mod CURSOR LOCAL FOR
		SELECT DISTINCT moduleIssueID FROM @issue WHERE statusDescription <> 'OK' AND moduleIssueID IS NOT NULL;
	
	OPEN cur_del_mod;
	FETCH NEXT FROM cur_del_mod INTO @delModuleID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[ModuleIssueIUD] -- Укажи правильное имя процедуры!
			@moduleIssueID = @delModuleID, -- или @issueID, в зависимости от того, как проца принимает параметр
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_mod INTO @delModuleID;
	END
	CLOSE cur_del_mod;
	DEALLOCATE cur_del_mod;


	-- 4. Удаление PackModuleIssue 
	DECLARE @delPackID int;
	DECLARE cur_del_pack CURSOR LOCAL FOR
		SELECT DISTINCT packModuleIssueID FROM @issue WHERE statusDescription <> 'OK' AND packModuleIssueID IS NOT NULL;
	
	OPEN cur_del_pack;
	FETCH NEXT FROM cur_del_pack INTO @delPackID;
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC [dbo].[PackModuleIssueID] -- Укажи правильное имя процедуры!
			@packModuleIssueID = @delPackID, -- параметр твоей процы
			@actionName = 'DeleteItem',
			@loggedUserId = @loggedUserID;

		FETCH NEXT FROM cur_del_pack INTO @delPackID;
	END
	CLOSE cur_del_pack;
	DEALLOCATE cur_del_pack;

	update i set isConfirmed = 1, activationDate = getdate()
	from Issue i inner join @Issue it on i.issueID = it.issueID and it.statusDescription = 'OK' 

	update i set i.isConfirmed = 1 from ProgramIssue i inner join Campaign c on i.campaignID = c.campaignID inner join @programmissue ii on i.issueID = ii.issueID where c.actionID = @actionID 

	update mi set mi.isConfirmed = 1 from ModuleIssue mi 
		inner join @Issue i on mi.moduleIssueID = i.moduleIssueID and i.statusDescription = 'OK' 
		
	update pmi set pmi.isConfirmed = 1 from PackModuleIssue pmi 
		inner join @Issue i on pmi.packModuleIssueID = i.packModuleIssueID and i.statusDescription = 'OK'  
		
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
					Then timeInUseUnconfirmed - t1.duration
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
					Then capacityInUseUnconfirmed - t1.countIssues
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
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCount,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCount,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCount
	from
		(select i.actualWindowID as windowID, 
			sum(r.duration) as duration, 
			count(i.issueID) as countIssues,
			sum(coalesce(case when i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.positionId = 10 then 1 else 0 end, 0)) as lastCount
		 from 
			@issue i0 
			inner join Issue i on i0.issueID = i.issueID
			Inner Join Roller r On r.rollerId = i.rollerId
		where i0.statusDescription = 'OK'
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	-- Политическая агитация: авто-вставка обвязки (44/7/55) в окна с подтверждённым
	-- типом 6; выпуски создаются в служебной акции, минус по времени окна допустим
	if exists (
		select 1 from @issue it
			inner join Roller r on r.rollerID = it.rollerID
		where it.statusDescription = 'OK' and r.rolActionTypeID = 6)
	begin
		exec AgitationFraming
			@actionName = 'InsertForAction',
			@actionID = @actionID,
			@loggedUserID = @loggedUserID
	end

	UPDATE [Action] Set isConfirmed = 1 WHERE actionID = @actionID

	exec ActionRecalculate
		@actionID = @actionID
END
GO

CREATE OR ALTER PROCEDURE [dbo].[TariffIUD]
(
@tariffID int = NULL,
@pricelistID smallint = NULL,
@time smalldatetime = NULL,
@monday tinyint = NULL,
@tuesday tinyint = NULL,
@wednesday tinyint = NULL,
@thursday tinyint = NULL,
@friday tinyint = NULL,
@saturday tinyint = NULL,
@sunday tinyint = NULL,
@price decimal(18,2) = 0,
@duration smallint = NULL,
@duration_total smallint = NULL,
@comment nvarchar(32) = NULL,
@isForModuleOnly bit = 0,
@actionName varchar(32),
@suffix NVARCHAR(16) = NULL,
@needExt BIT = 0,
@maxCapacity SMALLINT,
@needInJingle bit = 1,
@needOutJingle bit = 1,
@isUnionEnable bit = 0,
@tariffUnionID int = NULL,
@blockTypeId smallint = NULL,
@notEarly bit = 0,
@notLater bit = 0,
@openBlock bit = 0,
@openPhonogram bit = 0
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
-- Продолжительность не может быть больше полной; нулевая полная продолжительность означает «не задана»
IF @actionName In ('AddItem', 'UpdateItem', 'Clone') And @duration_total > 0 And @duration > @duration_total
	BEGIN
		RAISERROR('DurationExceedsTotal', 16, 1)
		RETURN
	END

-- Check, may be tariff with such attributes has been already created
IF @actionName In ('AddItem', 'UpdateItem', 'Clone') And
	Exists(
		Select	* From Tariff
		Where	tariffID <> IsNull(@tariffID, 0) and
		pricelistId = @pricelistID and
		time = @time and 
		(
		(monday = @monday and @monday = 1) or
		(tuesday = @tuesday and @tuesday = 1) or
		(wednesday = @Wednesday and @wednesday = 1) or
		(thursday = @thursday and @thursday = 1) or
		(friday = @friday and @friday = 1) or
		(saturday = @saturday and @saturday = 1) or
		(sunday = @sunday and @sunday = 1) 
		)
	)
	BEGIN
		RAISERROR('TariffAlreadyExists', 16, 1)
		RETURN
	END

-- Проверка "на чужой территории": на той же станции в это же время уже есть спонсорский тариф (SponsorTariff живёт в другой таблице/цепочке pricelistID)
IF @actionName In ('AddItem', 'UpdateItem', 'Clone') And
	Exists(
		Select	1
		From	SponsorTariff st
		Inner Join SponsorProgramPricelist spl On spl.pricelistID = st.pricelistID
		Inner Join SponsorProgram sp On sp.sponsorProgramID = spl.sponsorProgramID
		Inner Join Pricelist p On p.pricelistID = @pricelistID
		Where	sp.massmediaID = p.massmediaID and
		spl.startDate <= p.finishDate and spl.finishDate >= p.startDate and
		st.time = @time and
		(
		(st.monday = 1 and @monday = 1) or
		(st.tuesday = 1 and @tuesday = 1) or
		(st.wednesday = 1 and @wednesday = 1) or
		(st.thursday = 1 and @thursday = 1) or
		(st.friday = 1 and @friday = 1) or
		(st.saturday = 1 and @saturday = 1) or
		(st.sunday = 1 and @sunday = 1)
		)
	)
	BEGIN
		RAISERROR('TariffConflictsWithSponsorTariff', 16, 1)
		RETURN
	END

-- проверим, что новый тариф не "разрывает" цепочку. Ищем тариф, который "перед" этим в какой либо из дней, и который начало цепочки 
IF @actionName In ('AddItem', 'UpdateItem', 'Clone')
	Begin
		Declare @time2 smalldatetime, @broadcastStart smalldatetime, @hour int
		Select @broadcastStart = broadcastStart From [dbo].[Pricelist] Where [pricelistID] = @pricelistID
		Set @hour = DATEPART(hour, @time)
		Set @time2 = dbo.fn_GetTariffTimesWithBroadcast(@time, @broadcastStart)

		-- Это для случая, если создаётся или изменяется тариф, который не является частью цепочки. Тут мы проверяем, что он не "влез" между 2-мя тарифами, объединёнными в цепочку
		IF @tariffUnionID Is Null And Exists (
			SELECT 1 
			FROM 
				TariffUnion
			WHERE
				[tariffID] IN (
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @monday = 1 And ((monday = 1 And @hour >= DATEPART(hour, time)) Or (sunday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @tuesday = 1 And ((tuesday = 1 And @hour >= DATEPART(hour, time)) Or (monday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @wednesday = 1 And ((wednesday = 1 And @hour >= DATEPART(hour, time)) Or (tuesday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @thursday = 1 And ((thursday = 1 And @hour >= DATEPART(hour, time)) Or (wednesday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @friday = 1 And ((friday = 1 And @hour >= DATEPART(hour, time)) Or (thursday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @saturday = 1 And ((saturday = 1 And @hour >= DATEPART(hour, time)) Or (friday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @sunday = 1 And ((sunday = 1 And @hour >= DATEPART(hour, time)) Or (saturday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				)
				AND [tariffUnionID] <> ISNULL(@tariffID, 0)
			)
		BEGIN
			RAISERROR('TariffChainDamage', 16, 1)
			RETURN
		END
	END 
	
/*
if @actionName in ('AddItem', 'UpdateItem', 'Clone') and @isUnionEnable = 1 
	and @tariffUnionID is not null 
	and exists(select * from Tariff t where t.tariffID = @tariffUnionID and t.[time] < @time)
begin 
	RAISERROR('TariffUnionError', 16, 1)
	RETURN
end 
*/
	
IF (@actionName IN ('AddItem', 'Clone')) BEGIN
	INSERT INTO [Tariff](pricelistID, time, monday, tuesday, wednesday, thursday, friday, saturday, sunday, 
		price, duration, comment, isForModuleOnly, suffix, needExt, [maxCapacity], [needInJingle], [needOutJingle], 
		[blockTypeID], [notEarly], [notLater], [openBlock], [openPhonogram], duration_total)
	VALUES(@pricelistID, @time, @monday, @tuesday, @wednesday, @thursday, @friday, @saturday, @sunday, 
		@price, @duration, @comment, @isForModuleOnly, @suffix, @needExt, @maxCapacity, @needInJingle, @needOutJingle,
		@blockTypeID, @notEarly, @notLater, @openBlock, @openPhonogram, @duration_total)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @tariffID = SCOPE_IDENTITY()
	
	if @isUnionEnable = 1 and @tariffUnionID is not null 
	begin 
		if exists(select * from TariffUnion tu where tu.tariffUnionID = @tariffUnionID)
		begin 
			RAISERROR('TariffUnionErrorAlreadyUsed', 16, 1)
			RETURN
		end 
		
		insert into TariffUnion (tariffID, tariffUnionID) 
		values (@tariffID, @tariffUnionID) 
	end 
	
	EXEC Tariffs @TariffID = @TariffID
	END
ELSE IF @actionName = 'DeleteItem'
begin 
	delete from TariffUnion where tariffID = @tariffID or tariffUnionID = @tariffID
	DELETE FROM [Tariff] WHERE TariffID = @TariffID
end 
ELSE IF @actionName = 'UpdateItem' BEGIN
	-- Check if Tariff already in use and attributes can't be changed
	Declare	@oldIsForModuleOnly bit,
		@oldTime datetime, 
		@oldPrice decimal(18,2),
		@oldMonday bit,
		@oldTuesday bit,
		@oldWednesday bit,
		@oldThursday bit,
		@oldFriday bit,
		@oldSaturday bit,
		@oldSunday BIT,
		@oldMaxCap SMALLINT,
		@oldDuration int,
		@oldDurationTotal int

	SELECT 
		@oldIsForModuleOnly = isForModuleOnly,
		@oldTime = time, 
		@oldPrice = price,
		@oldMonday = monday,
		@oldTuesday = tuesday,
		@oldWednesday = wednesday,
		@oldThursday = thursday,
		@oldFriday = friday,
		@oldSaturday = saturday,
		@oldSunday = sunday,
		@oldMaxCap = maxCapacity,
		@oldDuration = duration,
		@oldDurationTotal = duration_total
	FROM
		[Tariff]
	WHERE		
		TariffID = @TariffID		
	
	IF (@oldIsForModuleOnly <> @IsForModuleOnly And @IsForModuleOnly = 1)
		And EXISTS(
			SELECT * 
			FROM issue i Inner Join TariffWindow tw On i.actualWindowID = tw.windowId
			WHERE tw.tariffID = @tariffID And i.moduleIssueID Is Null
		)
		BEGIN
		RAISERROR('TariffForModuleOnlyError', 16, 1)
		RETURN
	END

	if ((@oldMaxCap = 0 and @maxCapacity > 0) or (@oldMaxCap > 0 and @maxCapacity = 0))
		and exists(select * from ModuleTariff mt where mt.tariffID = @tariffID)
	begin 
		RAISERROR('TariffForModuleCannotChangeType', 16, 1)
		RETURN
	end 

	IF (@oldMaxCap <> @maxCapacity 
		or @oldTime <> @time Or @oldPrice <> @price Or @oldMonday <> @monday Or
		@oldTuesday <> @tuesday	Or @oldWednesday <> @wednesday Or
		@oldThursday <> @thursday Or @oldFriday <> @friday Or 
		@oldSaturday <> @saturday Or @oldSunday <> @sunday Or
		@oldDuration <> @duration
		) And
		EXISTS(SELECT * FROM tariffWindow WHERE tariffID = @tariffID)
		Begin
			RAISERROR('TariffInUse', 16, 1)
			RETURN	
		End

	-- новая полная уходит в окна тарифа (ниже), а у окна продолжительность может быть своя, больше тарифной
	IF @duration_total > 0 And @duration_total <> @oldDurationTotal
		And EXISTS(SELECT * FROM TariffWindow WHERE tariffId = @tariffID And duration_total = @oldDurationTotal And duration > @duration_total)
		BEGIN
			RAISERROR('DurationExceedsTotal', 16, 1)
			RETURN
		END

	UPDATE	
		[Tariff]
	SET			
		[time] = ISNULL(@time, TIME), 
		monday = ISNULL(@monday, [monday]), 
		tuesday = ISNULL(@tuesday, tuesday),
		wednesday = ISNULL(@wednesday, wednesday),
		thursday = ISNULL(@thursday, thursday),
		friday = ISNULL(@friday, friday),
		saturday = ISNULL(@saturday, saturday),
		sunday = ISNULL(@sunday, sunday),
		price = ISNULL(@price, price),
		duration = ISNULL(@duration, duration),
		duration_total = ISNULL(@duration_total, duration),
		comment = ISNULL(@comment, comment),
		isForModuleOnly = ISNULL(@isForModuleOnly,isForModuleOnly),
		suffix = ISNULL(@suffix,suffix),
		needExt = ISNULL(@needExt,needExt),
		[maxCapacity] = ISNULL(@maxCapacity,maxCapacity),
		needInJingle = ISNULL(@needInJingle, needInJingle),
		needOutJingle = ISNULL(@needOutJingle, needOutJingle),
		[blockTypeID] = @blockTypeID,
		[notEarly] = @notEarly,
		[notLater] = @notLater,
		[openBlock] = @openBlock,
		[openPhonogram] = @openPhonogram
	WHERE		
		TariffID = @TariffID
	
	if exists(select * from TariffUnion tu where tu.tariffUnionID = @tariffUnionID and tu.tariffID <> @tariffID)
		begin 
			RAISERROR('TariffUnionErrorAlreadyUsed', 16, 1)
			RETURN
		end 

	-- если обновилась информация о тарифе, который входит в существующую цепочку, надо проверить, что с цепочкой всё в порядке
	declare @startID int, @secondID int, @nextID int
	Select @startID = tariffID, @secondID = tariffUnionID From TariffUnion Where tariffID = @TariffID Or tariffUnionID = @TariffID
	If @startId Is Not Null
	Begin
		Set @nextID = dbo.fn_FindTariffIDForChain(@startID, @pricelistID)
		If IsNull(@nextID, 0) != @secondID
		begin 
			RAISERROR('TariffChainWrongUpdate', 16, 1)
			RETURN
		end 
	End
		
	delete from TariffUnion where tariffID = @tariffID
	
	if @isUnionEnable = 1 and @tariffUnionID is not null 
		insert into TariffUnion (tariffID, tariffUnionID) 
		values (@tariffID, @tariffUnionID) 

	-- если поменяласть полная продолжительность, надо обновить TariffWindow
	Update TariffWindow set duration_total = @duration_total Where tariffId = @tariffID and duration_total = @oldDurationTotal

	EXEC Tariffs @TariffID = @TariffID
	END
GO

CREATE OR ALTER PROCEDURE [dbo].[CampaignsIssueDelete]
(
	@campaignID INT,
	@rollerID INT = NULL,
	@actionName VARCHAR(32),
	@issueDate DATETIME = NULL,
	@loggedUserId SMALLINT,
	@massmediaID smallint
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	if @actionName <> 'DeleteItem'
		return 
	
	declare @issues table (issueID int primary key)
	declare 
		@actionID int,
		@IsConfirmed bit,
		@deadLine datetime,
		@IsAdmin bit,
		@IsTrafficManager bit
	Set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
	Set @IsTrafficManager = dbo.f_IsTrafficManager(@loggedUserID)

	select @deadLine = deadLine from MassMedia where massmediaID = @massmediaID
	
	select @actionID = c.actionID, @IsConfirmed = a.isConfirmed
	from Campaign c inner join Action a on a.actionID = c.actionID
	where c.campaignID = @campaignID

	insert into @issues 
	select i.issueID
	from Issue i
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	where i.campaignID = @campaignID 
		and tw.massmediaID = @massmediaID 
		and i.rollerID = coalesce(@rollerID, i.rollerID)
		and (@issueDate is null or tw.dayOriginal = Convert(datetime, Convert(varchar(8), @issueDate, 112), 112))

	if @IsConfirmed = 1 and @IsAdmin  = 0 And @IsTrafficManager = 0
		and exists(select * from @issues it 
					inner join Issue i on it.issueID = i.issueID 
					inner join TariffWindow tw on i.actualWindowID = tw.windowId
							and tw.dayOriginal <= dbo.ToShortDate(getdate()))
	begin 
		raiserror('PastIssue', 16, 1)
		return
	end

	if @IsConfirmed = 1 and @IsAdmin  = 0 And @IsTrafficManager = 0
		and exists(select * from @issues it 
					inner join Issue i on it.issueID = i.issueID 
					inner join TariffWindow tw on i.actualWindowID = tw.windowId
							and tw.dayOriginal <= dbo.ToShortDate(@deadLine))
	begin 
		raiserror('DeadLineViolationDelete', 16, 1)
		return
	end

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
	from
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
			@issues i0 
			inner join Issue i on i0.issueID = i.issueID
			Inner Join Roller r On r.rollerId = i.rollerId
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
		
	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, @actionID, i.rollerID, tw.windowDateActual, tw.massmediaID 
	from @issues it 
		inner join Issue i on it.issueID = i.issueID 
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
	where i.isConfirmed = 1
	
	if exists(select *
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
		where i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 
	
	declare @pissues table(issueID int primary key)
	declare @missues table(issueID int primary key)
	
	insert into @pissues 	
	select pmi.packModuleIssueID 
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join PackModuleIssue pmi on pmi.packModuleIssueID = i.packModuleIssueID 
			inner join Issue ii on pmi.packModuleIssueID = ii.packModuleIssueID 
		group by pmi.packModuleIssueID 
		having count(distinct i.issueID) = count(distinct ii.issueID)
										
	insert into @missues 
	select pmi.moduleIssueID 
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join ModuleIssue pmi on pmi.moduleIssueID = i.moduleIssueID 
			inner join Issue ii on pmi.moduleIssueID = ii.moduleIssueID 
		group by pmi.moduleIssueID 
		having count(distinct i.issueID) = count(distinct ii.issueID)

	-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
	-- окна с подтверждённой агитацией запоминаем до удаления и снимаем обвязку после
	declare @agitWindows table (windowID int primary key)
	insert into @agitWindows (windowID)
	select distinct i.actualWindowID
	from @issues it
		inner join Issue i on it.issueID = i.issueID
		inner join Roller r on r.rollerID = i.rollerID
	where r.rolActionTypeID = 6 and i.isConfirmed = 1

	delete from i from @issues it inner join Issue i on it.issueID = i.issueID
	delete from i from @missues it inner join ModuleIssue i on it.issueID = i.moduleIssueID
	delete from i from @pissues it inner join PackModuleIssue i on it.issueID = i.packModuleIssueID

	declare @agitWindowID int
	declare cur_agit_del cursor local for select windowID from @agitWindows

	open cur_agit_del
	fetch next from cur_agit_del into @agitWindowID
	while @@FETCH_STATUS = 0
	begin
		exec AgitationFraming
			@actionName = 'CleanupWindow',
			@windowID = @agitWindowID,
			@loggedUserID = @loggedUserId

		fetch next from cur_agit_del into @agitWindowID
	end
	close cur_agit_del
	deallocate cur_agit_del
end
GO

CREATE OR ALTER PROCEDURE [dbo].[CampaignIUD]
(
@campaignID int OUT,
@actionID int = NULL,
@campaignTypeID tinyint = NULL,
@massmediaID smallint = NULL,
@paymentTypeID smallint = NULL,
@agencyID smallint = NULL,
@loggedUserId smallint,
@actionName varchar(32),
@needShow bit = 1,
@managerDiscount decimal(18, 10) = 1
)
WITH EXECUTE AS OWNER
as
set nocount on

declare @IsAdmin bit, @IsTraffic bit
set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @IsTraffic = dbo.f_IsTrafficManager(@loggedUserID)

-- Only admin is allowed to delete issue which is in the past already
if @actionName = 'DeleteItem' and @IsAdmin <> 1 and @IsTraffic <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
				where i.campaignID = @campaignID and tw.dayOriginal <= dbo.ToShortDate(getdate()))
begin
	raiserror('PastIssue', 16, 1)
	return
end 

if @actionID is null and @campaignID is not null
	select @actionID = c.actionID from Campaign c where c.campaignID = @campaignID

IF @actionName = 'AddItem' begin
	if @agencyID is null 
	begin 
		raiserror('CannotAddCampaignWithoutAgency', 16, 1)
		return
	end

	if @IsAdmin = 0 and @IsTraffic = 0 and exists(select * 
				from [Action] a 
					inner join Campaign c on a.actionID = c.actionID
				where a.actionID = @actionID and a.finishDate < getdate() and a.isConfirmed = 1)
	begin 
		raiserror('CannotAddCampaignInConfirmedFinishedAction', 16, 1)
		return 
	end 

	select @managerDiscount = [dbo].[fn_GetMaxUserDiscount](@loggedUserId, GETDATE(), [dbo].[GetMaxDate]())

	INSERT INTO [Campaign](actionID, campaignTypeID, massmediaID, paymentTypeID, agencyID, modUser, managerDiscount)
	VALUES(@actionID, @campaignTypeID, @massmediaID, @paymentTypeID, @agencyID, @loggedUserId, @managerDiscount)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @campaignID = SCOPE_IDENTITY()

	if @needShow = 1
		EXEC Campaigns @CampaignID = @CampaignID, @actionID = @actionID
END
ELSE IF @actionName = 'DeleteItem' begin
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
		where i.campaignID = @campaignID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, @actionID, i.rollerID, tw.windowDateActual, tw.massmediaID 
	from Issue i 
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
	where i.campaignID = @CampaignID and i.isConfirmed = 1

	if exists(select *
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
		where i.campaignID = @CampaignID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 

	-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
	-- окна с агитацией запоминаем до удаления и снимаем обвязку после
	DECLARE @agitWindows table (windowID int primary key)
	INSERT INTO @agitWindows (windowID)
	SELECT DISTINCT i.actualWindowID
	FROM Issue i INNER JOIN Roller r ON r.rollerID = i.rollerID
	WHERE i.campaignID = @CampaignID AND r.rolActionTypeID = 6 AND i.isConfirmed = 1

	DELETE Issue WHERE CampaignID = @CampaignID
	DELETE FROM [Campaign] WHERE CampaignID = @CampaignID

	DECLARE @agitWindowID int
	DECLARE cur_agit_camp CURSOR LOCAL FOR SELECT windowID FROM @agitWindows

	OPEN cur_agit_camp
	FETCH NEXT FROM cur_agit_camp INTO @agitWindowID
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC AgitationFraming
			@actionName = 'CleanupWindow',
			@windowID = @agitWindowID,
			@loggedUserID = @loggedUserID

		FETCH NEXT FROM cur_agit_camp INTO @agitWindowID
	END
	CLOSE cur_agit_camp
	DEALLOCATE cur_agit_camp
END
ELSE IF @actionName = 'UpdateItem' BEGIN

    -- Read current values before update
    DECLARE @currentAgencyID     SMALLINT;
    DECLARE @currentPaymentTypeID SMALLINT;

    SELECT
        @currentAgencyID      = agencyID,
        @currentPaymentTypeID = paymentTypeID
    FROM dbo.Campaign
    WHERE CampaignID = @CampaignID;

    -- Check agencyID change: block if a linked payment belongs to the old agency
    IF @currentAgencyID <> @agencyID
    BEGIN
        DECLARE @conflictingPaymentID INT;

        SELECT TOP 1 @conflictingPaymentID = pa.paymentID
        FROM dbo.Campaign c
        INNER JOIN dbo.PaymentAction pa ON pa.actionID  = c.actionID
        INNER JOIN dbo.Payment p        ON p.paymentID  = pa.paymentID
        WHERE c.CampaignID = @CampaignID
          AND p.agencyID   = @currentAgencyID
          AND p.isEnabled  = 1;

        IF @conflictingPaymentID IS NOT NULL
            RAISERROR('CannotChangeAgency_PaymentExists', 16, 1);
    END

    -- Check paymentTypeID change: block if a linked payment has the old paymentTypeID
    IF @currentPaymentTypeID <> @paymentTypeID
    BEGIN
        DECLARE @conflictingPaymentID2 INT;

        SELECT TOP 1 @conflictingPaymentID2 = pa.paymentID
        FROM dbo.Campaign c
        INNER JOIN dbo.PaymentAction pa ON pa.actionID      = c.actionID
        INNER JOIN dbo.Payment p        ON p.paymentID      = pa.paymentID
        WHERE c.CampaignID        = @CampaignID
          AND p.paymentTypeID     = @currentPaymentTypeID
          AND p.isEnabled         = 1;

        IF @conflictingPaymentID2 IS NOT NULL
            RAISERROR('CannotChangePaymentType_PaymentExists', 16, 1);
    END

    UPDATE dbo.Campaign
    SET
        paymentTypeID = @paymentTypeID,
        agencyID      = @agencyID,
        modUser       = @loggedUserId,
        actionID      = @actionID
    WHERE CampaignID = @CampaignID;

    IF @needShow = 1
        EXEC Campaigns @CampaignID = @CampaignID, @actionID = @actionID;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[ActionIUD]
(
@actionID int = NULL,
@firmID smallint = NULL,
@userID smallint = NULL,
@newCreatorID smallint = NULL,
@isConfirmed bit = NULL,
@actionName varchar(32),
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on

declare @IsAdmin bit, @isTrafficManager bit

set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @isTrafficManager = [dbo].[f_IsTrafficManager](@loggedUserID)

-- Only admin is allowed to delete ACTIVATED issue which is in the past already
if @actionName = 'DeleteItem' and @IsAdmin <> 1 and @isTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
					inner join Campaign c on i.campaignID = c.campaignID
				where c.actionID = @actionID and tw.dayOriginal <= dbo.ToShortDate(getdate()) and i.isConfirmed = 1)
begin
	raiserror('PastIssue', 16, 1)
	return
end 

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Action](firmID, userID, isConfirmed)
	VALUES(@firmID, @userID, @isConfirmed)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @actionID = SCOPE_IDENTITY()

	EXEC Actions1 @actionID = @actionID
END
ELSE IF @actionName = 'DeleteItem' 
	begin
	-- если акция уже находится в журнале удалённых акций, то удалять по настоящему
	If Exists (Select 1 From [Action] WHERE actionID = @actionID And Not deleteDate Is Null)
		Begin

		-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
		-- окна с агитацией запоминаем до удаления и снимаем обвязку после
		Declare @agitWindows table (windowID int primary key)
		Insert Into @agitWindows (windowID)
		Select Distinct i.actualWindowID
		From Issue i
			Inner Join Campaign c On c.campaignID = i.campaignID
			Inner Join Roller r On r.rollerID = i.rollerID
		Where c.actionID = @actionID And r.rolActionTypeID = 6 And i.isConfirmed = 1

		DELETE Issue FROM Campaign c
		WHERE c.campaignID = Issue.campaignID AND c.actionID = @actionID

		DELETE FROM [Action] WHERE actionID = @actionID

		Declare @agitWindowID int
		Declare cur_agit_act Cursor Local For Select windowID From @agitWindows

		Open cur_agit_act
		Fetch Next From cur_agit_act Into @agitWindowID
		While @@FETCH_STATUS = 0
		Begin
			Exec AgitationFraming
				@actionName = 'CleanupWindow',
				@windowID = @agitWindowID,
				@loggedUserID = @loggedUserID

			Fetch Next From cur_agit_act Into @agitWindowID
		End
		Close cur_agit_act
		Deallocate cur_agit_act

		Return

		End
	
	Select @isConfirmed = IsConfirmed From Action Where actionID = @actionID
	If @isConfirmed = 1	Begin
		Exec [ActionDeactivate] @actionID = @actionID, @loggedUserID = @loggedUserID
	End

	Update [Action] Set deleteDate = GETDATE() WHERE actionID = @actionID

	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 Then timeInUseConfirmed - t1.duration
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
			inner join Campaign c on i.campaignID = c.campaignID
		where c.actionID = @actionID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, c.actionID, i.rollerID, tw.windowDateActual, tw.massmediaID 
	from Issue i 
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID 
	where c.actionID = @actionID and i.isConfirmed = 1

	if exists(select *
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID 
		where c.actionID = @actionID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 
END
ELSE IF @actionName = 'UpdateItem' 
	BEGIN
	DECLARE @oldFirmId INT, @oldIsConfirmed BIT 
	SELECT @oldFirmId = a.[firmID], @oldIsConfirmed = a.isConfirmed 
	FROM [Action] a 
	WHERE a.[actionID] = @actionID
	
	IF (@oldFirmId <> @firmID)
		DELETE FROM [PaymentAction] WHERE [actionID] = @actionID

	IF (@oldIsConfirmed = 1 
		AND @isConfirmed = 0 AND EXISTS(SELECT * FROM dbo.[PaymentAction] pa WHERE pa.actionID = @actionID))
	BEGIN
		RAISERROR('ActionWithPaymentsCannotDeactivate',16,1)
		RETURN
	END

	UPDATE	
		[Action]
	SET			
		firmID = @firmID,
		userID = IsNull(@newCreatorID, @userID),
		isConfirmed = @isConfirmed
	WHERE		
		actionID = @actionID

	EXEC Actions1 @actionID = @actionID
END
GO

CREATE OR ALTER PROCEDURE [dbo].[CampaignModuleIssueDelete]
(
@issueDate datetime = null,
@campaignID INT,
@moduleId INT = NULL,
@loggedUserId INT,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
begin
set nocount on
if @actionName <> 'DeleteItem'
	return

declare @issues table (issueID int primary key)
declare 
	@actionID int, 
	@isConfirmed bit,
	@IsAdmin bit,	
	@IsTrafficManager bit,	
	@RightForMinus bit, 
	@RightToGoBack bit,
	@DeadLine datetime

EXEC hlp_GetMainUserCredentials
	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT
	
select 
	@actionID = c.actionID, 
	@isConfirmed = a.isConfirmed, 
	@DeadLine =  mm.deadLine
from 
	Campaign c 
	Inner Join Action a On a.actionID = c.actionID 
	Inner Join MassMedia mm On mm.massmediaID = c.massmediaID
where 
	campaignID = @campaignID
	
insert into @issues 
select mi.moduleIssueID
	from [ModuleIssue] mi 
	where mi.[campaignID] = @campaignId 
		AND mi.[issueDate] = isnull(dbo.ToShortDate(@issueDate), mi.[issueDate])
		AND mi.[moduleID] = isnull(@moduleId, mi.moduleID)
			
If	@IsAdmin <> 1 And @IsTrafficManager <> 1 And @isConfirmed = 1 
	and exists(select * from @issues it 
			inner join [ModuleIssue] i on it.issueID = i.moduleIssueID 
					and i.issueDate <= dbo.ToShortDate(getdate()))
BEGIN
	RAISERROR('PastIssue', 16, 1)
	RETURN
END	

if @IsConfirmed = 1 and @IsAdmin <> 1 And @IsTrafficManager <> 1 and @issueDate <= @deadLine
begin
	raiserror('DeadLineViolationDelete', 16, 1)
	return
end 
	
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
from
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
		@issues i0 
		inner join Issue i on i0.issueID = i.moduleIssueID
		Inner Join Roller r On r.rollerId = i.rollerId
	group by i.actualWindowID ) as t1
Where
	TariffWindow.windowId = t1.windowID	
		
insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
select @loggedUserID, @actionID, i.rollerID, tw.windowDateActual, tw.massmediaID 
from @issues it 
	inner join Issue i on it.issueID = i.moduleIssueID 
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
where i.isConfirmed = 1
	
if exists(select *
	from @issues it 
		inner join Issue i on it.issueID = i.moduleIssueID 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	where i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
begin 
	exec SayAdminThatIssuesDelete @loggedUserID, @actionID
end 

	
delete from i from @issues it inner join Issue i on it.issueID = i.moduleIssueID
delete from mi from @issues it inner join ModuleIssue mi on it.issueID = mi.moduleIssueID

END
GO

CREATE OR ALTER PROCEDURE [dbo].[CampaignPackDayDelete]
(
	@campaignId INT ,
	@issueDate DATETIME = null,
	@loggedUserID SMALLINT = NULL,
	@packModuleId SMALLINT = NULL,
	@actionName varchar(32)	
)
WITH EXECUTE AS OWNER
AS
begin
set nocount on
if @actionName <> 'DeleteItem'
	return
declare @issues table (issueID int primary key)

DECLARE	
	@isAdmin bit,	
	@IsTrafficManager bit,	
	@rightForMinus bit, 
	@rightToGoBack bit,
	@actionID int,
	@IsConfirmed bit

EXEC hlp_GetMainUserCredentials	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT, null 
select @actionID = a.actionID, @IsConfirmed = a.isConfirmed from Campaign c Inner Join Action a On a.actionID = c.actionID where campaignID = @campaignID
	
insert into @issues 
select 
	pmi.packModuleIssueID
from 
	PackModuleIssue pmi 
	INNER JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
	INNER JOIN [PackModule] pm ON pmpl.[packModuleID] = pm.[packModuleID]
where 
	pmi.[campaignID] = @campaignId 
	AND pmi.[issueDate] = isnull(dbo.ToShortDate(@issueDate), pmi.[issueDate])
	and pm.[packModuleID] = isnull(@packModuleId, pm.[packModuleID])
			
if @IsConfirmed = 1 and @IsAdmin <> 1 And @IsTrafficManager <> 1 And @rightForMinus = 0
	and exists(select * from @issues it 
				inner join PackModuleIssue i on it.issueID = i.packModuleIssueID 
						and i.issueDate <= dbo.ToShortDate(getdate()))
	begin 
		raiserror('PastIssue', 16, 1)
		return
	end

-- только админ может удалять выпуск активированной акции, если траффик-менеджер уже закрыл период
if @IsConfirmed = 1 and @IsAdmin <> 1  And @IsTrafficManager <> 1
	and exists(select * 
				From @issues it 
				inner join PackModuleIssue i on it.issueID = i.packModuleIssueID 
				inner join PackModuleContent pmc on pmc.pricelistID = i.pricelistID
				inner join Module m On m.moduleID = pmc.moduleID
				inner join MassMedia mm on m.massmediaID = mm.massmediaID
				where i.issueDate <= dbo.ToShortDate(mm.deadLine))
begin
	raiserror('DeadLineViolationDelete', 16, 1)
	return
end 
		
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
from
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
		@issues i0 
		inner join Issue i on i0.issueID = i.packModuleIssueID
		Inner Join Roller r On r.rollerId = i.rollerId
	group by i.actualWindowID ) as t1
Where
	TariffWindow.windowId = t1.windowID		
		
insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
select @loggedUserID, @actionID, i.rollerID, tw.windowDateActual, tw.massmediaID 
from @issues it 
	inner join Issue i on it.issueID = i.packModuleIssueID 
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
where i.isConfirmed = 1
	
if exists(select *
	from @issues it 
		inner join Issue i on it.issueID = i.packModuleIssueID 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
where i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
begin 
	exec SayAdminThatIssuesDelete @loggedUserID, @actionID
end 
	
delete from i from @issues it inner join Issue i on it.issueID = i.packModuleIssueID
delete from pmi from @issues it inner join PackModuleIssue pmi on it.issueID = pmi.packModuleIssueID

END
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
			1
		
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

CREATE OR ALTER PROC [dbo].[PackModuleIssueID]
(
@packModuleIssueID INT = NULL,
@pricelistId SMALLINT = NULL,
@issueDate DATETIME = NULL,
@rollerId INT = NULL,
@campaignId INT = NULL,
@loggedUserId SMALLINT = NULL,
@rollerDuration SMALLINT = NULL,
@tariffPrice decimal(18,2) = NULL,
@positionId SMALLINT = 0,
@actionName VARCHAR(32),
@grantorID SMALLINT = NULL
)
WITH EXECUTE AS OWNER
AS

SET NOCOUNT ON

DECLARE	
	@massmediaID smallint, 
	@isAdmin bit,	
	@IsTrafficManager bit,
	@rightForMinus bit, 
	@rightToGoBack BIT,
	@finishDate DATETIME,
	@issuePrice decimal(18,2),
	@windowId INT,
	@windowDate DATETIME,
	@actionID INT,
	@isConfirmed BIT ,
	@msgError varchar(64),@res smallint,
	@managerDiscount decimal(18,10),
	@campaignStartDate datetime,
	@campaignFinishDate datetime
	
SELECT 
	@finishDate = c.finishDate,
	@actionID = c.[actionID],
	@isConfirmed = a.[isConfirmed],
	@managerDiscount = c.managerDiscount,
	@campaignStartDate = case when coalesce(c.startDate, @issueDate) > @issueDate then @issueDate else coalesce(c.startDate, @issueDate) end,
	@campaignFinishDate = case when coalesce(c.finishDate, @issueDate) < @issueDate then @issueDate else coalesce(c.finishDate, @issueDate) end
FROM 
	Campaign c 
	INNER JOIN [Action] a ON c.[actionID] = a.[actionID]
WHERE 
	c.campaignID = @campaignID	

-- Длительность ролика и цена пакета при добавлении - из базы, а не от клиента (как в IssueIUD)
if @actionName = 'AddItem'
begin
	select @rollerDuration = duration from Roller where rollerID = @rollerId
	select @tariffPrice = price from PackModulePriceList where pricelistID = @pricelistId
end

if @actionName in ('AddItem', 'UpdateItem') 
	and dbo.[fn_IsAcceptRatioForUser](@loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0
begin 
	 raiserror('CannotChangeCampaignWithMaxDiscount', 16, 1)
	 return
end

IF @actionName in ('AddItem', 'UpdateItem') And @isConfirmed = 1
	And Exists (Select 1 From Roller where rollerID = @rollerID And advertTypeID Is Null) 
	Begin
		RAISERROR('RollerWithoutAdvertType', 16, 1)
		RETURN 
	End

EXEC hlp_GetMainUserCredentials
	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT, @grantorID = @grantorID

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
		where i.packModuleIssueID = @packModuleIssueID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
END

IF @actionName = 'AddItem' 
	BEGIN
	-- нельзя добавить несколько 'первых' роликов в окно в рамках одной акции, даже если 
	-- это макет. Такую акцию потом не активировать без ошибок
	If @positionId <> 0 And Exists (
		Select 1 
		From 
			PackModuleIssue i Inner Join Campaign c On c.campaignID = i.campaignID
		Where
			i.issueDate = @issueDate
			And i.[pricelistID] = @pricelistId
			And i.positionId = @positionId
			And c.actionID = @actionID
		)
		Begin
			RAISERROR('PositionErrorForTheSameAction', 16, 1)
			RETURN 
		End

	DECLARE @extraChargeFirst SMALLINT, @extraChargeSecond SMALLINT, @extraChargeLast SMALLINT
	SELECT 
		@extraChargeFirst = extraChargeFirstRoller,
		@extraChargeSecond = extraChargeSecondRoller,
		@extraChargeLast = extraChargeLastRoller
	FROM [PackModulePriceList]
	WHERE pricelistId = @pricelistId
	
	SELECT @issuePrice = dbo.fn_GetIssuePrice(@rollerDuration, @tariffPrice, 1, @positionId, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
		
	INSERT INTO [PackModuleIssue] ([campaignID], [pricelistID],	[issueDate], [rollerID], [positionId], tariffPrice, [isConfirmed]) 
	VALUES (@campaignID, @pricelistID, @issueDate, @rollerID, @positionId, @issuePrice, @isConfirmed) 
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @packModuleIssueID = SCOPE_IDENTITY()
	
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
		NULL, --  smallint
		NULL, --  datetime
		NULL, --  int
		@issueDate, --  datetime
		@rollerDuration, --  int
		@rightToGoBack, --  bit
		@isAdmin, --  bit
		@IsTrafficManager,
		@rightForMinus, --  bit
		@finishDate, --  datetime
		4, --  tinyint
		@isConfirmed, --  bit
		@positionId, --  smallint
		NULL, --  int
		NULL, --  int
		NULL, --  int
		@rolActionTypeID, --  tinyint
		@msgError output,
		NULL, --  int
		1, --  bit
		@pricelistID	--  int
		
	IF @res = 1 BEGIN
		RAISERROR(@msgError, 16, 1)
		RETURN 
	end
	
	declare @activationdate datetime 
	if @isConfirmed = 1
		set @activationdate = getdate()
	else 
		set @activationdate = null
		
	INSERT INTO [Issue](rollerID, actualWindowID, originalWindowId, campaignID, positionId, ratio, moduleIssueID, [packModuleIssueID], isConfirmed, [tariffPrice], grantorID, activationDate)
	select distinct @rollerID, tw.windowId, tw.windowId, @campaignID, @positionId, 1, null, @packModuleIssueID, @isConfirmed, 0, @grantorID, @activationdate  
	FROM 
		[PackModuleContent]  pmc
		INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
		INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
		inner join Tariff t on tw.tariffID  = t.tariffID 
		inner join Pricelist pl on pl.pricelistID = t.priceListID
	WHERE 
		pmc.[pricelistID] = @pricelistId
		AND tw.dayOriginal = @issueDate
	
	IF NOT EXISTS(SELECT * FROM [Issue] i WHERE i.packModuleIssueID = @packModuleIssueID)
	BEGIN
		DELETE FROM [PackModuleIssue] WHERE packModuleIssueID = @packModuleIssueID
		SELECT -1 AS packModuleIssueID
	END
	ELSE
		Exec [PackModuleIssueRetrieve] @packModuleIssueID = @packModuleIssueID			   	
	END
ELSE IF @actionName = 'DeleteItem'
	begin
		Select @issueDate = IssueDate From PackModuleIssue Where packModuleIssueID = @packModuleIssueID

		If	@isConfirmed = 1 And @issueDate < dbo.ToShortDate(getdate()+1) And @IsAdmin <> 1 And @IsTrafficManager <> 1 BEGIN
			RAISERROR('PastIssue', 16, 1)
			RETURN
		END	
	
		-- только админ может удалять выпуск активированной акции, если траффик-менеджер уже закрыл период
		if @IsConfirmed = 1 and @IsAdmin <> 1 And @IsTrafficManager <> 1 
			and Exists (
				Select 1 
				From 
					Issue i 
					Inner join TariffWindow tw On i.originalWindowID = tw.windowId
					Inner Join MassMedia m On m.massmediaID = tw.massmediaID  Where m.deadLine >= @issueDate And i.packModuleIssueID = @packModuleIssueID
					)
		begin
			raiserror('DeadLineViolationDelete', 16, 1)
			return
		end 

		IF @isConfirmed = 1
		begin 
			insert into LogDeletedIssue (userId,rollerId,actionId,issueDate,massmediaID) 
			select @loggedUserId, i.rollerID, @actionID, tw.windowDateActual, tw.massmediaID  from Issue i inner join TariffWindow tw on i.actualWindowID = tw.windowID where i.packModuleIssueID = @packModuleIssueID

			if datediff(day,dbo.ToShortDate(getdate()),@issueDate) <= dbo.f_SysParamsDaysLog() 			
				exec SayAdminThatIssuesDelete @loggedUserID, @actionID
		end 
	
		delete from Issue where packModuleIssueID = @packModuleIssueID
		delete from [PackModuleIssue] where packModuleIssueID = @packModuleIssueID
	end
else if @actionName = 'UpdateItem'
begin 
	select @rollerDuration = r.duration,
		@rolActionTypeID = r.rolActionTypeID,
		@tariffPrice = pl.price,
		@extraChargeFirst = pl.extraChargeFirstRoller,
		@extraChargeSecond = pl.extraChargeSecondRoller,
		@extraChargeLast = pl.extraChargeLastRoller
	from PackModuleIssue pmi 
		inner join Roller r on pmi.rollerID = r.rollerID
		inner join PackModulePriceList pl on pmi.pricelistID = pl.priceListID
	where pmi.packModuleIssueID = @packModuleIssueID

	exec @res = hlp_IssueVerify
		Null,
		@actionName,
		NULL, --  smallint
		NULL, --  datetime
		NULL, --  int
		@issueDate, --  datetime
		@rollerDuration, --  int
		@rightToGoBack, --  bit
		@isAdmin, --  bit
		@IsTrafficManager,
		@rightForMinus, --  bit
		@finishDate, --  datetime
		4, --  tinyint
		@isConfirmed, --  bit
		@positionId, --  smallint
		NULL, --  int
		NULL, --  int
		NULL, --  int
		@rolActionTypeID, --  tinyint
		@msgError output,
		NULL, --  int
		1, --  bit
		@pricelistID --  int
	IF @res = 1 BEGIN
		RAISERROR(@msgError, 16, 1)
		RETURN 
	end

	SELECT @issuePrice = dbo.fn_GetIssuePrice(
			@rollerDuration, @tariffPrice, 1, @positionId, @extraChargeFirst, 
			@extraChargeSecond, @extraChargeLast)

	update PackModuleIssue set tariffPrice = @issuePrice, positionId = @positionId where packModuleIssueID = @packModuleIssueID
	update Issue set positionId = @positionId where packModuleIssueID = @packModuleIssueID

	Exec [PackModuleIssueRetrieve] @packModuleIssueID = @packModuleIssueID		
end 
if (@actionName in ('AddItem', 'UpdateItem'))
BEGIN
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
		where i.packModuleIssueID = @packModuleIssueID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
end
GO

-- Остаток по модулям за период - данные ячеек грида размещения комбо-модулями.
--
-- Набор модулей задаётся одним из двух способов:
--   @comboModuleID - состав комбо-модуля (размещение по мастеру);
--   @actionID      - модули, уже размещённые в акции (редактирование готовой акции
--                    из карточки; связь кампании с модулем существует только через
--                    выпуски, так же её выводит CampaignModulesRetrieve).
--
-- Одна строка на (модуль, день). Ячейка показывает самое заполненное окно модуля:
-- именно оно ограничивает возможность поставить ролик во весь модуль сразу.
--
-- positionFree   - выбранная позиция (первый/второй/последний ролик) свободна во
--                  ВСЕХ окнах модуля. При включённом учёте макетов смотрим и на
--                  неподтверждённые позиции;
-- advertTypeFree - выбранный предмет рекламы (наличие/отсутствие) выполняется во
--                  ВСЕХ окнах модуля. Критерий совпадения ролика с предметом
--                  рекламы - тот же, что в TariffWindowWithAdvertTypeRetrieve
--                  (advertTypeID ролика или его родитель);
-- Оба флага грид рисует жирным - как RollerIssuesGrid3.MarkCell для отдельного
-- окна, только применённым ко всем окнам модуля сразу;
-- freeTime       - минимальный остаток времени по окнам модуля, сек;
-- freeCapacity   - минимальный остаток по количеству среди окон со штучным
--                  ограничением, maxCapacity - вместимость того самого окна.
--                  Для модуля без штучных окон обе колонки NULL.
--
-- Время считается по всем окнам, в том числе штучным: hlp_IssueVerify проверяет
-- переполнение по времени независимо от maxCapacity, так что ограничение
-- реально для любого окна.
--
-- Строка возвращается только для дней, когда модуль есть целиком: окно есть на
-- каждый тариф прайс-листа модуля (та же проверка, что в IsModuleExist) и ни
-- одно из окон не отключено. В остальные дни модуля нет - ячейка пустая.
--
-- День берётся по dayOriginal - так же, как в IsModuleExist и ModuleIssueIUD,
-- то есть по исходной сетке окон, а не по перенесённой.
CREATE OR ALTER PROC [dbo].[ComboModuleFreeTimeRetrieve]
(
@comboModuleID smallint = NULL,
@actionID int = NULL,
@startDate datetime,
@finishDate datetime,
@showUnconfirmed bit = 0,
@positionId smallint = 0,
@advertTypeId smallint = NULL,
@advertTypePresence tinyint = 0   -- 0 - не фильтровать, 5 - есть, 10 - нет (AdvertTypePresences)
)
AS
SET NOCOUNT ON

DECLARE @modules TABLE (moduleID SMALLINT PRIMARY KEY)

IF @comboModuleID IS NOT NULL
	INSERT INTO @modules (moduleID)
	SELECT cmc.moduleID FROM [ComboModuleContent] cmc WHERE cmc.comboModuleID = @comboModuleID
ELSE
	INSERT INTO @modules (moduleID)
	SELECT DISTINCT mi.moduleID
	FROM [ModuleIssue] mi
		INNER JOIN [Campaign] c ON c.campaignID = mi.campaignID
	WHERE c.actionID = @actionID

;WITH windows AS
(
	SELECT
		mpl.moduleID,
		mpl.modulePriceListID,
		mpl.price,
		tw.dayOriginal AS issueDate,
		mt.tariffID,
		tw.isDisabled,
		tw.maxCapacity,
		tw.duration - tw.timeInUseConfirmed
			- CASE WHEN @showUnconfirmed = 1 THEN tw.timeInUseUnconfirmed ELSE 0 END AS timeLeft,
		CASE WHEN tw.maxCapacity > 0
			THEN tw.maxCapacity - tw.capacityInUseConfirmed
				- CASE WHEN @showUnconfirmed = 1 THEN tw.capacityInUseUnconfirmed ELSE 0 END
			END AS capacityLeft,
		CASE
			WHEN @positionId = -20 THEN   -- первый в блоке
				CASE WHEN tw.isFirstPositionOccupied = 0
					AND (@showUnconfirmed = 0 OR tw.firstPositionsUnconfirmed = 0) THEN 1 ELSE 0 END
			WHEN @positionId = -10 THEN   -- второй в блоке
				CASE WHEN tw.isSecondPositionOccupied = 0
					AND (@showUnconfirmed = 0 OR tw.secondPositionsUnconfirmed = 0) THEN 1 ELSE 0 END
			WHEN @positionId = 10 THEN    -- последний в блоке
				CASE WHEN tw.isLastPositionOccupied = 0
					AND (@showUnconfirmed = 0 OR tw.lastPositionsUnconfirmed = 0) THEN 1 ELSE 0 END
			ELSE 1
		END AS positionFree,
		CASE
			WHEN @advertTypePresence = 0 THEN 1
			WHEN @advertTypePresence = 5 THEN   -- есть предмет рекламы
				CASE WHEN EXISTS(
					SELECT 1 FROM [Issue] i
						INNER JOIN [Roller] r ON r.rollerID = i.rollerID
						INNER JOIN [Campaign] c ON c.campaignID = i.campaignID
						INNER JOIN [Action] a ON a.actionID = c.actionID
						LEFT JOIN [AdvertType] adt ON adt.advertTypeID = r.advertTypeID
					WHERE i.actualWindowID = tw.windowId
						AND a.deleteDate IS NULL   -- выпуски удалённых акций (журнал удалённых) не в счёт
						AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
						AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
					) THEN 1 ELSE 0 END
			WHEN @advertTypePresence = 10 THEN   -- нет предмета рекламы
				CASE WHEN NOT EXISTS(
					SELECT 1 FROM [Issue] i
						INNER JOIN [Roller] r ON r.rollerID = i.rollerID
						INNER JOIN [Campaign] c ON c.campaignID = i.campaignID
						INNER JOIN [Action] a ON a.actionID = c.actionID
						LEFT JOIN [AdvertType] adt ON adt.advertTypeID = r.advertTypeID
					WHERE i.actualWindowID = tw.windowId
						AND a.deleteDate IS NULL   -- выпуски удалённых акций (журнал удалённых) не в счёт
						AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
						AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
					) THEN 1 ELSE 0 END
			ELSE 1
		END AS advertTypeFree
	FROM
		@modules m
		INNER JOIN [ModulePriceList] mpl ON mpl.moduleID = m.moduleID
		INNER JOIN [ModuleTariff] mt ON mt.modulePriceListID = mpl.modulePriceListID
		INNER JOIN [TariffWindow] tw ON tw.tariffId = mt.tariffID
			AND tw.dayOriginal BETWEEN @startDate AND @finishDate
			AND tw.dayOriginal BETWEEN mpl.startDate AND mpl.finishDate
),
days AS
(
	SELECT
		w.moduleID,
		w.modulePriceListID,
		w.price,
		w.issueDate,
		MIN(w.timeLeft) AS freeTime,
		MIN(w.capacityLeft) AS freeCapacity,
		MIN(w.positionFree) AS positionFree,
		MIN(w.advertTypeFree) AS advertTypeFree
	FROM
		windows w
	GROUP BY
		w.moduleID,
		w.modulePriceListID,
		w.price,
		w.issueDate
	HAVING
		COUNT(DISTINCT w.tariffID) =
			(SELECT COUNT(*) FROM [ModuleTariff] mtAll
				WHERE mtAll.modulePriceListID = w.modulePriceListID)
		AND SUM(CASE WHEN w.isDisabled = 1 THEN 1 ELSE 0 END) = 0
)
SELECT
	d.moduleID,
	d.modulePriceListID,
	d.price,
	d.issueDate,
	d.freeTime,
	d.freeCapacity,
	d.positionFree,
	d.advertTypeFree,
	(SELECT MIN(w.maxCapacity)
		FROM windows w
		WHERE w.moduleID = d.moduleID
			AND w.issueDate = d.issueDate
			AND w.capacityLeft = d.freeCapacity) AS maxCapacity
FROM
	days d
ORDER BY
	d.moduleID,
	d.issueDate
GO

-- Проверка: новые версии применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) LIKE N'%i.actualWindowID = @windowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) NOT LIKE N'%i.originalWindowID = @windowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.IssueIUD')) LIKE N'%Select @windowID = actualWindowID From Issue%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.IssueIUD')) LIKE N'%tw.windowDateActual FROM %Issue% i inner join TariffWindow tw on i.actualWindowID = tw.windowId%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RollerSubstitute')) NOT LIKE N'%i.originalWindowID = @windowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionActivate')) LIKE N'%WHERE fi.actualWindowID = tw.windowId%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffIUD')) NOT LIKE N'%i.originalWindowID = tw.windowId%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsIssueDelete')) LIKE N'%tw.windowDateActual, tw.massmediaID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignIUD')) LIKE N'%tw.windowDateActual, tw.massmediaID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionIUD')) LIKE N'%tw.windowDateActual, tw.massmediaID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignModuleIssueDelete')) LIKE N'%tw.windowDateActual, tw.massmediaID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignPackDayDelete')) LIKE N'%tw.windowDateActual, tw.massmediaID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ModuleIssueIUD')) LIKE N'%tw.windowDateActual, tw.massmediaID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.PackModuleIssueID')) LIKE N'%tw.windowDateActual, tw.massmediaID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ComboModuleFreeTimeRetrieve')) NOT LIKE N'%i.originalWindowID = tw.windowId%'
    PRINT N'ГОТОВО: проверки окна, «Сделать первым», активация, тариф для модулей, журнал удалённых и комбо-модули — по окну выхода (13 процедур).';
ELSE
    RAISERROR(N'26: новая версия применена не ко всем процедурам.', 16, 1);
GO
