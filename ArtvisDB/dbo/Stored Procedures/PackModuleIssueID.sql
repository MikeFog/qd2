CREATE PROC [dbo].[PackModuleIssueID]
(
@packModuleIssueID INT = NULL,
@pricelistId SMALLINT = NULL,
@issueDate DATETIME = NULL,
@rollerId INT = NULL,
@campaignId INT = NULL,
@loggedUserId SMALLINT = NULL,
@rollerDuration SMALLINT = NULL,
@tariffPrice MONEY = NULL,
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
	@issuePrice MONEY,
	@windowId INT,
	@windowDate DATETIME,
	@actionID INT,
	@isConfirmed BIT ,
	@msgError varchar(64),@res smallint,
	@managerDiscount float,
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
IF @grantorID is not Null and @actionName in ('AddItem', 'UpdateItem')
BEGIN
	DECLARE @msg nvarchar(4000), @campaignTypeName nvarchar(200), @rollerName nvarchar(250)
	select @campaignTypeName = [name] from iCampaignType where campaignTypeID = 4
	select @rollerName = r.[name] from Roller r where r.rollerID = @rollerID
	SET @msg = 'Добавлен пакетный модуль ' +  Convert(varchar(10), @issueDate, 104) + ' ' + Convert(varchar(8), @issueDate, 108) 
		+ ', Ролик: ' + @rollerName
		+ ', Акция №' + RTRIM(@actionId) + ', Тип кампании: ' + @campaignTypeName

	Exec ConfirmationHistoryID @confirmationTypeID = 2, @userID = @loggedUserId,
		@grantorID = @grantorID, @description = @msg, 
		@actionName = 'AddItem'
END
