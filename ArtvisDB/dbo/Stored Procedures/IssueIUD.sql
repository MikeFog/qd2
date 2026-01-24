CREATE	PROCEDURE [dbo].[IssueIUD]
(
@issueID int = NULL OUT,
@rollerID int = NULL,
@rollerDuration smallint = NULL,
@windowID int = NULL,
@tariffWindowPrice money = NULL,
@campaignID int = NULL,
@issueDate datetime = NULL,
@positionId float = 0,
@ratio float = 1,
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
	@issuePrice money,
	@deadLine datetime,
	@res smallint,
	@msgError varchar(64),
	@rolActionTypeID TINYINT,
	@actionID int,
	@managerDiscount float,
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

if @actionName in ('AddItem', 'UpdateItem') 
	and dbo.[fn_IsAcceptRatioForUser](@loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0
begin 
	 raiserror('CannotChangeCampaignWithMaxDiscount', 16, 1)
	 return
end

if @actionName in ('AddItem', 'UpdateItem') 
	SELECT @rolActionTypeID = [rolActionTypeID] FROM [Roller] WHERE [rollerID] = @rollerID

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
END
ELSE IF @actionName = 'DeleteItem' BEGIN
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
	
	DECLARE @tariffPrice MONEY
	
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

IF @grantorID is not Null and @actionName in ('AddItem', 'UpdateItem')
BEGIN
	DECLARE @msg nvarchar(4000), @campaignTypeName nvarchar(200), @rollerName nvarchar(250)
	select @campaignTypeName = [name] from iCampaignType where campaignTypeID = @campaignTypeId
	select @rollerName = r.[name] from Roller r where r.rollerID = @rollerID
	SET @msg = 'Добавлен выпуск ' +  Convert(varchar(10), @issueDate, 104) + ' ' + Convert(varchar(8), @issueDate, 108) 
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
