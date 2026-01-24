CREATE PROCEDURE [dbo].[job_DeleteEmptyActions]
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	declare @isTransactionStart bit 
	select @isTransactionStart = 0

	if @@trancount = 0
	begin 
		begin tran 
		select @isTransactionStart = 1
	end 
	
	delete from [Action] where startDate is null and finishDate is null

	delete from a 
		from [Action] a 
			left join Campaign c on a.actionID = c.actionID
		where a.isSpecial = 0 and c.campaignID is null 
	
	exec DeleteHistory 		
	
	-- Update TariffWindows
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then coalesce(t1.duration, 0)
				Else 0
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then coalesce(t1.countIssues, 0)
				Else 0
			End,
		isFirstPositionOccupied = 
			Case 
				When coalesce(firstCount, 0) > 0 Then 1
				Else 0
			End,
		isSecondPositionOccupied = 
			Case 
				When coalesce(secondCount, 0) > 0 Then 1
				Else 0
			End,
		isLastPositionOccupied = 
			Case 
				When coalesce(lastCount, 0) > 0 Then 1
				Else 0
			End
	from
		TariffWindow tw inner join 
			(select i.actualWindowID as windowID, 
				sum(r.duration) as duration, 
				count(i.issueID) as countIssues,
				sum(coalesce(case when i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
				sum(coalesce(case when i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
				sum(coalesce(case when i.positionId = 10 then 1 else 0 end, 0)) as lastCount
			 from 
				Issue i 
				inner join Roller r on i.rollerID = r.rollerID
			where i.isConfirmed = 1
			group by i.actualWindowID ) as t1 on tw.windowId = t1.windowID
	where  tw.isFirstPositionOccupied <> coalesce(t1.firstCount, 0)
			or tw.isSecondPositionOccupied <> coalesce(t1.secondCount, 0)
			or tw.isLastPositionOccupied <> coalesce(t1.lastCount, 0)
			or (([maxCapacity] > 0 and tw.capacityInUseConfirmed <> coalesce(t1.countIssues, 0) )
			or ([maxCapacity] = 0 and tw.timeInUseConfirmed <> coalesce(t1.duration, 0)) )
		
		
	Update 
		TariffWindow
	Set
		timeInUseUnConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then coalesce(t1.duration, 0)
				Else 0
			End,
		capacityInUseUnConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then coalesce(t1.countIssues, 0)
				Else 0
			End,
		firstPositionsUnconfirmed = coalesce(firstCount, 0),
		secondPositionsUnconfirmed = coalesce(secondCount, 0),
		lastPositionsUnconfirmed = coalesce(lastCount, 0)
	from
		TariffWindow tw left join 
			(select i.actualWindowID as windowID, 
				sum(r.duration) as duration, 
				count(i.issueID) as countIssues,
				sum(coalesce(case when i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
				sum(coalesce(case when i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
				sum(coalesce(case when i.positionId = 10 then 1 else 0 end, 0)) as lastCount
			 from 
				Issue i 
				inner join Roller r on i.rollerID = r.rollerID
			where i.isConfirmed = 0
			group by i.actualWindowID ) as t1 on tw.windowId = t1.windowID
	where  tw.firstPositionsUnconfirmed <> coalesce(t1.firstCount, 0)
			or tw.secondPositionsUnconfirmed <> coalesce(t1.secondCount, 0)
			or tw.lastPositionsUnconfirmed <> coalesce(t1.lastCount, 0)
			or (([maxCapacity] > 0 and tw.capacityInUseUnConfirmed <> coalesce(t1.countIssues, 0))
				or ([maxCapacity] = 0 and tw.timeInUseUnConfirmed <> coalesce(t1.duration, 0)) )
			
	if @@trancount > 0
	begin 
		if @@error = 0 
		begin 
			if @isTransactionStart = 1	commit tran 
		end 
		else rollback tran 
	end 
	else 
		raiserror('TransactionError', 16, 1)
END
