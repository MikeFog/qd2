	CREATE PROC [dbo].[ActionRestore]
	(
	@actionID int
	)
	AS

	Update [Action] Set deleteDate = NULL WHERE actionID = @actionID
	
	Update 
		TariffWindow
	Set
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0	Then timeInUseUnconfirmed + t1.durationU
				Else timeInUseUnconfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When [maxCapacity] > 0 Then capacityInUseUnconfirmed + t1.countIssuesU
				Else capacityInUseUnconfirmed
			end,
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
			inner join Campaign c on i.campaignID = c.campaignID
		where c.actionID = @actionID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID