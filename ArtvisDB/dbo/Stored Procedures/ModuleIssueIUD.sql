CREATE PROCEDURE [dbo].[ModuleIssueIUD]
(
@moduleIssueID int = NULL,
@campaignID int = NULL,
@moduleID smallint = NULL,
@modulePricelistID smallint = NULL,
@issueDate datetime = NULL,
@rollerID int = NULL,
@rollerDuration smallint = NULL,
@ratio float = 1,
@positionId smallint = 0,
@tariffPrice money = NULL,
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
	@issuePrice money,
	@extraChargeFirst tinyint,
	@extraChargeSecond tinyint,
	@extraChargeLast TINYINT,
	@actionID int,
	@DeadLine datetime,
	@campaignFinishDate datetime,
	@msgError varchar(64),@res smallint,
	@managerDiscount float,
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

IF @grantorID is not Null and @actionName in ('AddItem', 'UpdateItem')
BEGIN
	DECLARE @msg nvarchar(4000), @campaignTypeName nvarchar(200), @rollerName nvarchar(250)
	select @campaignTypeName = [name] from iCampaignType where campaignTypeID = @campaignTypeId
	select @rollerName = r.[name] from Roller r where r.rollerID = @rollerID
	SET @msg = 'Добавлен модуль ' +  Convert(varchar(10), @issueDate, 104) + ' ' + Convert(varchar(8), @issueDate, 108) 
		+ ', Ролик: ' + @rollerName
		+ ', Акция №' + RTRIM(@actionId) + ', Тип кампании: ' + @campaignTypeName

	select @msg = @msg + ', СМИ: ' + mm.name
		from Campaign c 
			inner join vMassmedia mm on c.massmediaID = mm.massmediaID
		where c.campaignID = @campaignId
	
	Exec ConfirmationHistoryID @confirmationTypeID = 2, @userID = @loggedUserId,
		@grantorID = @grantorID, @description = @msg, 
		@actionName = 'AddItem'
END
