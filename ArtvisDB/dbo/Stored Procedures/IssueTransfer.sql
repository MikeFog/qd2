
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

-- Р-12 (09.10.2026): окно-приёмник — той же станции, что и окно, где выпуск стоит сейчас (у пакетной
-- кампании Campaign.massmediaID пуст: пакет на нескольких станциях). Интерфейс переносит только внутри
-- сетки одной станции; здесь — страховка от ошибки вызывающего кода.
if not exists (select 1
		from Issue i
			inner join TariffWindow twCur on twCur.windowId = i.actualWindowID
			inner join TariffWindow tw on tw.windowId = @newWindowId and tw.massmediaID = twCur.massmediaID
		where i.issueID = @issueID)
begin
	raiserror('TransferOtherMassmedia', 16, 1)
	return
end

-- Р-12: в окне один «Локальное СМИ»/«Локальное СМИ (агитация)», один «Федеральное СМИ»/«Федеральное
-- СМИ (агитация)» и одна «Отбивка политической агитации» — как при постановке (hlp_IssueVerify),
-- по окну выхода. Раньше перенос трафиком это не проверял.
declare @transferRolType tinyint, @transferTypeError varchar(50)
select @transferRolType = r.rolActionTypeID
from Issue i inner join Roller r on r.rollerID = i.rollerID
where i.issueID = @issueID

if @transferRolType in (4, 44, 5, 55, 7) and exists (select 1
		from Issue i2
			inner join Roller r2 on r2.rollerID = i2.rollerID
		where i2.actualWindowID = @newWindowId
			and i2.issueID <> @issueID
			and ((@transferRolType in (4, 44) and r2.rolActionTypeID in (4, 44))
				or (@transferRolType in (5, 55) and r2.rolActionTypeID in (5, 55))
				or (@transferRolType = 7 and r2.rolActionTypeID = 7)))
begin
	set @transferTypeError = case when @transferRolType in (4, 44) then 'RolType4AlreadyExistInWindow'
		when @transferRolType in (5, 55) then 'RolType5AlreadyExistInWindow'
		else 'RolType7AlreadyExistInWindow' end
	raiserror(@transferTypeError, 16, 1)
	return
end

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
		when r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
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

-- Политическая агитация: запоминаем окно и тип ролика до переноса, чтобы после
-- переноса снять обвязку со старого окна и создать в новом (см. хвост процедуры)
Declare @agitOldWindowID int, @agitRolActionTypeID tinyint, @agitIsConfirmed bit
Select
	@agitOldWindowID = i.actualWindowID,
	@agitRolActionTypeID = r.rolActionTypeID,
	@agitIsConfirmed = i.isConfirmed
From Issue i Inner Join Roller r On r.rollerID = i.rollerID
Where i.issueID = @issueID

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

-- Политическая агитация: перенос подтверждённого ролика типа 6 переносит и обвязку -
-- снимаем её со старого окна (если там больше нет агитации) и создаём в новом
If @agitRolActionTypeID = 6 And @agitIsConfirmed = 1
Begin
	Exec AgitationFraming
		@actionName = 'CleanupWindow',
		@windowID = @agitOldWindowID,
		@loggedUserID = @loggedUserID

	Exec AgitationFraming
		@actionName = 'InsertForWindow',
		@windowID = @newWindowId,
		@loggedUserID = @loggedUserID
End


