
CREATE  PROC [dbo].[ActionDeactivate]
(
@actionID int,
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare @IsAdmin bit, @isTrafficManager bit, @startDate datetime
DECLARE @firmID smallint, @userID smallint

set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @isTrafficManager = [dbo].[f_IsTrafficManager](@loggedUserID)

SELECT @firmID = a.firmID, @userID = a.userID, @startDate = a.startDate FROM ACTION a WHERE a.actionID = @actionID

-- только админ и трафик могут деактивировать акции, которые уже начались
if @startDate < [dbo].[ToShortDate](GETDATE()) and @IsAdmin = 0 and @isTrafficManager = 0
Begin
	raiserror('DeactivationErrorActionAlreadyStarted', 16, 1)
	return
End

EXEC [ActionIUD]
	@actionID = @actionID, --  int
	@firmID = @firmID, --  smallint
	@userID = @userID, --  smallint
	@isConfirmed = 0, --  bit
	@actionName = 'UpdateItem', --  varchar(32)
	@loggedUserID = @loggedUserID


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
					Then timeInUseUnconfirmed + t1.duration
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
					Then capacityInUseUnconfirmed + t1.countIssues
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
		firstPositionsUnconfirmed = firstPositionsUnconfirmed + firstCount,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed + secondCount,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed + lastCount
	from
		(select i.actualWindowID as windowID, 
			sum(r.duration) as duration, 
			count(i.issueID) as countIssues,
			sum(coalesce(case when i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.positionId = 10 then 1 else 0 end, 0)) as lastCount
		 from 
			Issue i 
			inner join Campaign c on i.campaignID = c.campaignID
			Inner Join Roller r On r.rollerId = i.rollerId
		where c.actionID = @actionID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

UPDATE Issue
	SET isConfirmed = 0, activationDate = null
FROM 
	Campaign c
WHERE
	c.actionID = @actionID
	AND c.campaignID = Issue.campaignID

UPDATE 
	ProgramIssue
SET 
	isConfirmed = 0
FROM 
	Campaign c
WHERE
	c.actionID = @actionID
	AND c.campaignID = ProgramIssue.campaignID

UPDATE 
	PackModuleIssue
SET 
	isConfirmed = 0
FROM 
	Campaign c
WHERE
	c.actionID = @actionID
	AND c.campaignID = PackModuleIssue.campaignID

UPDATE 
	ModuleIssue
SET 
	isConfirmed = 0
FROM 
	Campaign c
WHERE
	c.actionID = @actionID
	AND c.campaignID = ModuleIssue.campaignID


