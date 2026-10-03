-- Локальное промо (типы роликов 8/9): запрет позиционирования. Развёртывание.
--
-- Выпуск с роликом локального промо (8 - со спонсором, 9 - без спонсора) нельзя
-- поставить первым/вторым/последним: место в блоке промо задаёт тип ролика при
-- выгрузке DJin, а позиция только заняла бы флаг окна и дала наценку.
--   - hlp_IssueVerify: PromoPositionForbidden при positionId <> 0 - добавление и
--     изменение выпуска (IssueIUD), модули (ModuleIssueIUD), пакеты (PackModuleIssueID),
--     перенос дня (CampaignTransferDay);
--   - IssueIUD: при изменении выпуска без @rollerID тип ролика берётся из выпуска
--     (раньше оставался пустым, и проверки по типу ролика не срабатывали);
--   - RollerSubstitute: выпуск с позицией на промо не заменяется, причина - в журнале
--     ошибок замены (iMessageToSubtitute).
--
-- Скрипт идемпотентен. Текст сообщения клиент qd2 подхватит после перезапуска
-- (сама проверка действует сразу).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i promo-position-forbidden-deploy.sql

SET NOCOUNT ON;
GO

-------------------------------------------------------------------------------
-- 1. Сообщения
-------------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'PromoPositionForbidden')
	INSERT INTO [dbo].[iMessage] (name, [message])
	VALUES ('PromoPositionForbidden',
		N'Для роликов локального промо позиционирование не применяется. Операция прервана.');

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessageToSubtitute] WHERE msgError = 'PromoPositionForbidden')
	INSERT INTO [dbo].[iMessageToSubtitute] (msgError, [message])
	VALUES ('PromoPositionForbidden',
		N'На ролик локального промо нельзя заменить выпуск с позиционированием');
GO

-------------------------------------------------------------------------------
-- 2. Процедуры (копии ArtvisDB/dbo/Stored Procedures)
-------------------------------------------------------------------------------
-- hlp_IssueVerify
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[hlp_IssueVerify]
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
		i.originalWindowID = @windowID
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
		i.originalWindowID = @windowID
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
		i.originalWindowID = @windowID
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

-- IssueIUD
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
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
	Select @windowID = originalWindowID From Issue Where issueID = @issueID

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
					inner join Issue i on tw.windowId = i.originalWindowID 
				where i.issueID = @issueID and tw.dayOriginal <= dbo.ToShortDate(getdate()))
begin
	raiserror('PastIssue', 16, 1)
	return
end

-- только админ может удалять выпуск активированной акции, если траффик-менеджер уже закрыл период
if @IsConfirmed = 1 and @actionName = 'DeleteItem' and @IsAdmin <> 1  And @IsTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.originalWindowID 
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
			i.originalWindowID = @windowID
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
		SELECT @actualDate = tw.windowDateOriginal FROM [Issue] i inner join TariffWindow tw on i.originalWindowID = tw.windowId WHERE i.[issueID] = @issueID
		EXEC [LogDeletedIssueInsert] @loggedUserId, @actionId, @rollerID, @actualDate, @massmediaID
		
		if exists(SELECT * FROM [Issue] i inner join TariffWindow tw on i.originalWindowID = tw.windowId 
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

-- RollerSubstitute
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
/*
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - error resolved
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - replace @moduleIssueID and @packModuleIssueID on @moduleID and @packModuleID
Modified: Denis Gladkikh (dgladkikh@fogsoft.ru) 14.10.2008 - some optimization + add substitude for only one issue
*/
CREATE OR ALTER PROCEDURE [dbo].[RollerSubstitute]
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
			i.originalWindowID = @windowID
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

-------------------------------------------------------------------------------
-- 3. Проверка
-------------------------------------------------------------------------------
SELECT name, [message] FROM [dbo].[iMessage] WHERE name = 'PromoPositionForbidden';
SELECT msgError, [message] FROM [dbo].[iMessageToSubtitute] WHERE msgError = 'PromoPositionForbidden';

-- Уже размещённые промо с позицией (запрет их не трогает - только новые правки)
SELECT r.rolActionTypeID, i.positionId, COUNT(*) AS issues
FROM [dbo].[Issue] i
	INNER JOIN [dbo].[Roller] r ON r.rollerID = i.rollerID
WHERE r.rolActionTypeID IN (8, 9) AND i.positionId <> 0
GROUP BY r.rolActionTypeID, i.positionId;
GO
