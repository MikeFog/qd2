CREATE PROCEDURE [dbo].[CampaignsIssueDelete]
(
	@campaignID INT,
	@rollerID INT = NULL,
	@actionName VARCHAR(32),
	@issueDate DATETIME = NULL,
	@loggedUserId SMALLINT,
	@massmediaID smallint
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	if @actionName <> 'DeleteItem'
		return 
	
	declare @issues table (issueID int primary key)
	declare 
		@actionID int,
		@IsConfirmed bit,
		@deadLine datetime,
		@IsAdmin bit,
		@IsTrafficManager bit
	Set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
	Set @IsTrafficManager = dbo.f_IsTrafficManager(@loggedUserID)

	select @deadLine = deadLine from MassMedia where massmediaID = @massmediaID
	
	select @actionID = c.actionID, @IsConfirmed = a.isConfirmed
	from Campaign c inner join Action a on a.actionID = c.actionID
	where c.campaignID = @campaignID

	insert into @issues 
	select i.issueID
	from Issue i
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	where i.campaignID = @campaignID 
		and tw.massmediaID = @massmediaID 
		and i.rollerID = coalesce(@rollerID, i.rollerID)
		and (@issueDate is null or tw.dayOriginal = Convert(datetime, Convert(varchar(8), @issueDate, 112), 112))

	if @IsConfirmed = 1 and @IsAdmin  = 0 And @IsTrafficManager = 0
		and exists(select * from @issues it 
					inner join Issue i on it.issueID = i.issueID 
					inner join TariffWindow tw on i.originalWindowID = tw.windowId
							and tw.dayOriginal <= dbo.ToShortDate(getdate()))
	begin 
		raiserror('PastIssue', 16, 1)
		return
	end

	if @IsConfirmed = 1 and @IsAdmin  = 0 And @IsTrafficManager = 0
		and exists(select * from @issues it 
					inner join Issue i on it.issueID = i.issueID 
					inner join TariffWindow tw on i.originalWindowID = tw.windowId
							and tw.dayOriginal <= dbo.ToShortDate(@deadLine))
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
			inner join Issue i on i0.issueID = i.issueID
			Inner Join Roller r On r.rollerId = i.rollerId
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
		
	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, @actionID, i.rollerID, tw.windowDateOriginal, tw.massmediaID 
	from @issues it 
		inner join Issue i on it.issueID = i.issueID 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	where i.isConfirmed = 1
	
	if exists(select *
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
		where i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 
	
	declare @pissues table(issueID int primary key)
	declare @missues table(issueID int primary key)
	
	insert into @pissues 	
	select pmi.packModuleIssueID 
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join PackModuleIssue pmi on pmi.packModuleIssueID = i.packModuleIssueID 
			inner join Issue ii on pmi.packModuleIssueID = ii.packModuleIssueID 
		group by pmi.packModuleIssueID 
		having count(distinct i.issueID) = count(distinct ii.issueID)
										
	insert into @missues 
	select pmi.moduleIssueID 
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join ModuleIssue pmi on pmi.moduleIssueID = i.moduleIssueID 
			inner join Issue ii on pmi.moduleIssueID = ii.moduleIssueID 
		group by pmi.moduleIssueID 
		having count(distinct i.issueID) = count(distinct ii.issueID)

	delete from i from @issues it inner join Issue i on it.issueID = i.issueID
	delete from i from @missues it inner join ModuleIssue i on it.issueID = i.moduleIssueID
	delete from i from @pissues it inner join PackModuleIssue i on it.issueID = i.packModuleIssueID
end
