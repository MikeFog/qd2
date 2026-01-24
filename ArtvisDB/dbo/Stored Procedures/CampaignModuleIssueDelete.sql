CREATE PROCEDURE [dbo].[CampaignModuleIssueDelete]
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
select @loggedUserID, @actionID, i.rollerID, tw.windowDateOriginal, tw.massmediaID 
from @issues it 
	inner join Issue i on it.issueID = i.moduleIssueID 
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
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
