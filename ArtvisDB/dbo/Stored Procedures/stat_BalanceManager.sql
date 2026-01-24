CREATE        PROCEDURE [dbo].[stat_BalanceManager]
(
	@theDate datetime = null,
	@ShowBlack bit = 1,
	@ShowWhite bit = 1,
	@loggedUserID smallint
) 
WITH EXECUTE AS OWNER
AS

set nocount on

-- calculate payments till defined date -----------------------
declare @tmp1 table
(
[summa] money,
[userID] int,
[agencyID] int,
[firmID] int
)

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

Insert 	Into @tmp1
Select	Sum(pa.summa) as summa, 
		a.userID,
		p.agencyID,
		p.firmID
From	payment p
		inner join paymentaction pa ON p.paymentID = pa.paymentID
		inner join [Action] a ON a.actionID = pa.actionID AND a.isConfirmed = 1
		inner join paymentType pt on p.paymentTypeID = pt.paymentTypeID
		left join 
				(
					select distinct am.agencyID from AgencyMassmedia am 
						inner join @massmedias mm on am.massmediaID = mm.massmediaID and mm.foreignMassmedia = 1
				) x on p.agencyID = x.agencyID
Where	(a.userID = @loggedUserID or x.agencyID is not null) and
		paymentDate <= coalesce(@theDate,paymentDate) And 
		((pt.isHidden = 1 and @ShowBlack = 1)  or 
		(pt.isHidden = 0 and @ShowWhite = 1))
Group 	by 
		p.agencyID,
		a.userID,
		p.firmID

-- calculate actions till defined date ------------------------
Declare 	cur_Companies cursor local fast_forward for
select distinct	c.campaignID, c.campaignTypeID, 
		c.startDate, a.userID,
		c.agencyID, c.finishDate, 
		c.finalPrice, a.firmID,a.discount
From	campaign  c
		inner join [Action] a on c.actionID = a.actionID AND a.isConfirmed = 1
		inner join 
		(
			select distinct am.agencyID, max(cast(mm.foreignMassmedia as tinyint)) as foreignMassmedia from AgencyMassmedia am 
				inner join @massmedias mm on am.massmediaID = mm.massmediaID
			group by am.agencyID
		) xx on c.agencyID = xx.agencyID and (a.isSpecial = 0 or xx.foreignMassmedia = 1) 
		inner join paymentType pt on c.paymentTypeID = pt.paymentTypeID
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
where	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(a.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and	
		c.startDate <= coalesce(@theDate,c.startDate) and
		((pt.isHidden = 1 and @ShowBlack = 1)  or 
		(pt.isHidden = 0 and @ShowWhite = 1))

Declare	@campaignID int, @TypeID int, @UserID int,
		@StartDay datetime, 
		@Price money, @AgencyID int, 
		@FinishDay datetime, @FinalPrice money, @firmID int, @actiondiscount float

Open	cur_Companies
Fetch	next from cur_Companies 
Into 	@campaignID, @TypeID, @StartDay, @UserID, @AgencyID, 
		@FinishDay, @FinalPrice, @firmID,@actiondiscount
	
While	@@fetch_status = 0
	Begin 
	
	If	coalesce(@theDate,@FinishDay) >= @FinishDay
		Set 	@Price = case when @TypeID <> 4 then @FinalPrice * @actiondiscount else @FinalPrice end 
	else
		Exec	GetPriceByPeriod @campaignID, @TypeID, @StartDay, @theDate, @Price output

	Insert	Into @tmp1(summa, userID, agencyID, firmID)
	Values	(-@Price, @UserID, @AgencyID, @firmID)

	Fetch	next from cur_Companies 
	Into 	@campaignID, @TypeID, @StartDay, @UserID, @AgencyID, 
			@FinishDay, @FinalPrice, @firmID,@actiondiscount
	End

close		cur_Companies	
Deallocate	cur_Companies

select t1.agencyID, sum(t1.summa) as summa, t1.userID
Into 	#tmp2
from 
	(Select	agencyID, sum(summa) as summa, userID, firmID
		From	@tmp1
		Group by agencyID, userID, firmID
		Having	sum(summa) < 0) as t1 
group by agencyID, userID
having sum(summa) < 0

Declare 	cur_H cursor local fast_forward for
Select	agency.agencyID, agency.Name, sum(summa) as summa
From	#tmp2 join Agency on #tmp2.agencyID = agency.agencyID
Group By agency.agencyID, agency.Name

Declare	@name nvarchar(64), @summa money, @sqlText nvarchar(4000)

create table #result 
(
	RowNum int,
	[$Итого] money default 0
)
insert #result(RowNum) select distinct UserID from #tmp2

Open	cur_H
while 1=1
begin
	Fetch	next from cur_H
	Into 	@AgencyID, @name, @summa
	if @@fetch_status <> 0	
		break

	set @sqlText = N'ALTER TABLE #result ADD [$' + @name + '] money default 0 with values;'
	exec sp_executeSQL @sqlText
	set @sqlText = N'UPDATE #result set [$' + @name + '] = summa, [$Итого] = [$Итого] + summa from #result join #tmp2 on #result.RowNum = #tmp2.UserID and #tmp2.agencyID =' + cast(@agencyID as varchar)
	exec sp_executeSQL @sqlText
	-- summary
	set @sqlText = N'UPDATE #result set [$' + @name + '] = @sum, [$Итого] = [$Итого] + @sum where RowNum = -1'
	exec sp_executeSQL @sqlText, N'@sum money', @sum = @summa
end

close		cur_H	
Deallocate	cur_H

select 	isnull([User].lastname, '') + space(1) + isnull(left([User].firstname, 1), '') + '.' + isnull(left([User].secondname, 1), '') as "Менеджер",
		#result.* 
from 	#result
		JOIN [User] ON [User].userID = #result.RowNum

drop table #tmp2
drop table #result
