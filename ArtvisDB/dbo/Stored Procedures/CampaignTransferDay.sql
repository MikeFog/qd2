CREATE       PROC [dbo].[CampaignTransferDay]
(
@campaignID int,
@massmediaID SMALLINT = null,
@oldDate datetime,
@newDate datetime,
@loggedUserId smallint,
@rollerId smallint = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

IF @oldDate = @newDate
	return

Declare	
	@issueID int, @issueTimeString datetime,  
	@rollerDuration int,
	@RightToGoBack bit, @isAdmin bit, @rightForMinus bit, @isTrafficManager bit,
	@campaignFinishDate datetime, @campaignTypeID tinyint,
	@isConfirmed bit, @timeBonus int, @issuesDuration int,
	@res smallint, @msgError varchar(64), @positionId smallint,
	@startDate datetime,
	@windowId int,
	@deadLine DATETIME,
	@modulePriceListID INT,
	@moduleID INT,
	@priceListID INT,
	@packModulePriceListID INT,
	@packModuleID INT,
	@rolActionTypeID TINYINT,
	@issueDateOriginal DATETIME,
	@actionID int,
	@tomorrow datetime

SELECT 
	@campaignTypeID	= c.campaignTypeID,
	@campaignFinishDate = c.finishDate,
	@timeBonus = c.timeBonus,
	@issuesDuration = c.issuesDuration,
	--@deadLine = m.deadLine,
	@actionID = a.[actionID],
	@isConfirmed = a.isConfirmed
FROM 
	Campaign c
	INNER JOIN Action a ON c.actionID = a.actionID
	--LEFT Join MassMedia m On m.massmediaId = c.massmediaID
WHERE 
	c.campaignID = @campaignID

Select
	@deadLine = max(mm.deadLine)
From
	Issue i
	Inner Join TariffWindow tw on i.originalWindowID = tw.windowId
	Inner Join MassMedia mm on mm.massmediaID = tw.massmediaID
Where 
	i.campaignID = @campaignID

Exec hlp_GetMainUserCredentials
	@loggedUserId = @loggedUserId,
	@rightToGoBack = @rightToGoBack out,
	@isAdmin = @isAdmin out,
	@isTrafficManager = @isTrafficManager out,
	@rightForMinus = @rightForMinus OUT

SELECT 
	@oldDate = dbo.ToShortDate(@oldDate),
	@newDate = dbo.ToShortDate(@newDate),
	@tomorrow = dateadd(day, 1, Convert(datetime, Convert(varchar(8),getdate(), 112), 112))

if @isAdmin = 0 And @isTrafficManager = 0 And @isConfirmed = 1 And (@oldDate < @tomorrow Or @oldDate<= IsNull(@deadLine, Convert(datetime, '19000101',112)))
begin
	raiserror('TransferErrorFromThePast', 16, 1)
	return 
end

Declare	curIssues cursor local fast_forward
For	
Select	
	i.issueID,
	Convert(varchar, tw.windowDateOriginal, 108) as issueTimeString,
	i.positionId,
	r.duration,
	mpl.[priceListID],
	mpl.[moduleID],
	t.[pricelistID],
	pmi.[pricelistID],
	pmpl.[packModuleID],
	r.[rolActionTypeID]
From	
	Issue i
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	INNER JOIN [Roller] r ON i.[rollerID] = r.[rollerID]
	LEFT JOIN dbo.[ModuleIssue] mi ON mi.moduleIssueID = i.[moduleIssueID]
	LEFT JOIN dbo.[ModulePriceList] mpl ON mpl.[modulePriceListID] = mi.[modulePriceListID]
	LEFT JOIN [Tariff] t ON t.[tariffID] = tw.[tariffId]
	LEFT JOIN [PackModuleIssue] pmi ON pmi.[packModuleIssueID] = i.[packModuleIssueID]
	LEFT JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
Where	
	i.campaignID = @campaignID and
	i.rollerId = Coalesce(@rollerId, i.rollerId) and
	tw.dayOriginal = @oldDate

Open	curIssues
Fetch Next from curIssues 
Into @issueID, @issueTimeString, @positionId, @rollerDuration, @modulePriceListID, @moduleID, @priceListID, @packModulePriceListID, 
	@packModuleID, @rolActionTypeID

WHILE @@fetch_status = 0 
	BEGIN
	SET @issueDateOriginal = @newDate + @issueTimeString
	
	IF @campaignTypeID = 1 OR @campaignTypeID = 2
		Begin
		Select @windowId = tw.windowId From TariffWindow tw 
			INNER JOIN [Tariff] t ON tw.[tariffId] = t.[tariffID] AND t.[pricelistID] = @priceListID
		Where tw.windowDateOriginal = @issueDateOriginal
		End
	ELSE IF @campaignTypeID = 3
		Select @windowId = tw.windowId From TariffWindow tw 
			Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
			Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID And mpl.pricelistId = @modulePriceListID AND mpl.moduleId = @moduleId
		Where tw.windowDateOriginal = @issueDateOriginal
	ELSE IF @campaignTypeID = 4
		Begin
		Select 
			@windowId = tw.windowId, @deadLine = mm.[deadLine] 			
		From 
			TariffWindow tw 
			INNER JOIN [Tariff] t ON tw.[tariffId] = t.[tariffID] AND t.[pricelistID] = @priceListID
			Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
			Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID  
			INNER JOIN [PackModuleContent] pmc ON mpl.modulePriceListID = pmc.modulePriceListID AND pmc.[pricelistID] = @packModulePriceListID
			INNER JOIN [PackModulePriceList] pmpl ON pmc.[pricelistID] = pmpl.[priceListID] AND pmpl.[packModuleID] = @packModuleID
			INNER JOIN [Pricelist] pl ON t.[pricelistID] = pl.[pricelistID]
			INNER JOIN [MassMedia] mm ON mm.[massmediaID] = pl.[massmediaID]
		Where 
			tw.windowDateOriginal = @issueDateOriginal

		End
	
	If Not @windowId Is Null begin
		set @issuesDuration = @issuesDuration - @rollerDuration

		Exec @res = hlp_IssueVerify	
			@issueID, 
			'AddItem',
			@massmediaID,  
			@deadLine,
			@windowId,
			@issueDateOriginal, 
			@rollerDuration,
			@rightToGoBack,	
			@isAdmin, 
			@isTrafficManager, 
			@rightForMinus, 
			@campaignFinishDate,
			1,  -- так как проверяем конкретный выпуск, то всё будем проверять по правилам линейной кампании
			@isConfirmed, 
			@positionId, 
			@timeBonus,
			@issuesDuration, 
			NULL,
			@rolActionTypeID,
			@msgError out

		IF @res = 1 BEGIN
			RAISERROR(@msgError, 16, 1)
			close curIssues
			deallocate curIssues
			RETURN 
		END

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

		Update Issue Set actualWindowId = @windowId, [originalWindowID] = @windowId	Where issueID = @issueID
		
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
	ELSE BEGIN
		raiserror('TransferError', 16, 1)
		close curIssues
		deallocate curIssues
		return
	END
	
	Fetch Next from curIssues 
	Into @issueID, @issueTimeString, @positionId, @rollerDuration, @modulePriceListID, @moduleID, @priceListID, @packModulePriceListID, @packModuleID, @rolActionTypeID
end

IF @campaignTypeID = 3
	UPDATE [ModuleIssue] SET [issueDate] = @newDate WHERE [campaignID] = @campaignID AND [issueDate] = @oldDate
		
IF @campaignTypeID = 4
	UPDATE [PackModuleIssue] SET [issueDate] = @newDate WHERE [campaignID] = @campaignID AND [issueDate] = @oldDate

close curIssues
deallocate curIssues
