-- Цена выпуска по длительности ролика и цене окна из базы, а не от клиента: развёртывание.
--
-- Клиент (qd2 и веб) присылал при добавлении выпуска длительность ролика и цену окна
-- (модуля, пакета) — то, что загрузилось в форме кампании при её открытии. Если ролик или
-- цену за это время поменяли, выпуск получал цену и проверку переполнения окна по старым
-- значениям, а занятость окна — по настоящей длительности. Теперь процедуры берут их сами:
--   - IssueIUD, AddItem: Roller.duration, TariffWindow.price; UpdateItem: сменить ролик
--     выпуска нельзя (InternalError) — цена и проверка окна там считались по старому ролику;
--   - RollerSubstitute: старая и новая длительность — из Roller;
--   - ModuleIssueIUD, PackModuleIssueID, AddItem: Roller.duration, цена ModulePriceList /
--     PackModulePriceList.
-- Параметры процедур не менялись, клиенты не трогаются. Скрипт идемпотентен.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i issue-price-from-db-deploy.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-------------------------------------------------------------------------------
-- IssueIUD
-------------------------------------------------------------------------------
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

-------------------------------------------------------------------------------
-- RollerSubstitute
-------------------------------------------------------------------------------
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
-- ModuleIssueIUD
-------------------------------------------------------------------------------
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
		select @loggedUserId, i.rollerID, @actionID, tw.windowDateOriginal, tw.massmediaID  from Issue i inner join TariffWindow tw on tw.windowID = i.originalWindowID where i.moduleIssueID = @moduleIssueID
		
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

-------------------------------------------------------------------------------
-- PackModuleIssueID
-------------------------------------------------------------------------------
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
			select @loggedUserId, i.rollerID, @actionID, tw.windowDateOriginal, tw.massmediaID  from Issue i inner join TariffWindow tw on i.originalWindowID = tw.windowID where i.packModuleIssueID = @packModuleIssueID

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
