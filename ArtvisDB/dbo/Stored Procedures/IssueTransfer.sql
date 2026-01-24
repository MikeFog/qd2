
CREATE                     PROC [dbo].[IssueTransfer]
(
@issueID int,
@campaignID int,
@newWindowId int,
@newDate datetime,
@newPosition smallint = null,
@loggedUserID smallint,
@massmediaID smallint,
@isConfirmed bit
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare 
	@isAdmin bit,	
	@IsTrafficManager bit,	
	@rightForMinus bit, 
	@rightToGoBack bit

-- Проверка на возможность размещения
if exists(select * 
		from DisabledWindow dw 
			inner join TariffWindow tw on tw.massmediaID = dw.massmediaID
				and tw.windowDateActual between dw.startDate And dw.finishDate
			where tw.windowID = @newWindowId)
begin
	raiserror('DisabledWindowTransfer',16,1)
	return
end 

declare @msgError varchar(50)
EXEC hlp_GetMainUserCredentials

	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT

select top 1 @msgError = 
	case
		when mm.deadLine is not null and tw.dayActual <= mm.deadLine and @isAdmin = 0 and @IsTrafficManager = 0 then 'DeadLineViolationTransfer' 
		when tw.isDisabled = 1 then 'DisabledInsertRoller'
		when r.rolActionTypeID = 1 and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
		when (( tw.isFirstPositionOccupied = 1 And @newPosition = -20) 
				or (tw.isSecondPositionOccupied = 1	And @newPosition = -10)
				or (tw.isLastPositionOccupied = 1	And @newPosition = 10)) then 'FirstLastIssueErrorTransfer'
		when @rightForMinus = 0 AND i.isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1)) < 0) then 'WindowMaxCapacityOverflowTransfer'
		when @rightForMinus = 0 AND i.isConfirmed = 1 and tw.[timeInUseConfirmed] + r.duration > tw.duration then 'WindowOverflowTransfer'
		else null 
	end 
from 
	Issue i 
	inner join Roller r on i.rollerID = r.rollerID
	inner join [TariffWindow] tw on tw.windowId = @newWindowId
	inner join MassMedia mm on tw.massmediaID = mm.massmediaID
where i.issueID = @issueID 
order by 1 desc

if @msgError is not null 
begin 
	raiserror(@msgError,16,1)
	return
end 

Declare 
	@oldDate datetime

Select 
	@oldDate = tw.windowDateActual
From 
	Issue i 
	inner join TariffWindow tw on i.actualWindowID = tw.windowID
Where 
	i.issueId = @issueId

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

UPDATE Issue
SET	actualWindowID = @newWindowId,
	positionId = @newPosition
WHERE issueID = @issueID

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

declare @actionID int
select  @actionID = c.actionID from Campaign c where c.campaignID = @campaignID

-- Insert record to transfer log journal
-- Write Log
INSERT INTO [TransferLog]([userID], [oldDate], [newDate], [actionID], [issueID])
Values(@loggedUserID, @oldDate, @newDate, @actionID, @issueID)


