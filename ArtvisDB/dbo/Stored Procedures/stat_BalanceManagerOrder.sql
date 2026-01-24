CREATE      PROCEDURE [dbo].[stat_BalanceManagerOrder]
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
[summa] int,
[userID] int,
[agencyID] int,
[firmID] int
)

If	@theDate Is Null Set @theDate = getdate()

set @theDate = dbo.ToShortDate(@theDate)

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

Insert 	Into @tmp1
Select	Sum(psoa.summa) as summa,
		soa.userID,
		pso.agencyID,
		pso.firmID
From	paymentStudioOrder pso
		inner join paymentStudioOrderAction psoa ON pso.paymentID = psoa.paymentID
		inner join StudioOrderAction soa ON soa.actionID = psoa.actionID
		inner join paymentType pt on pso.paymenttypeID = pt.paymentTypeID
		inner join [StudioOrder] so ON so.[actionID] = soa.[actionID] AND so.isComplete = 1
		inner join 
		(
			select distinct u.userID 
			from [User] u
				left join [GroupMember] gm on u.userID = gm.userID
				left join @ugroups ug on gm.groupID = ug.id
			where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
		) as x on soa.userID = x.userID
Where	pso.paymentDate <= @theDate And
		((pt.isHidden = 1 and @ShowBlack = 1)  or
		(pt.isHidden = 0 and @ShowWhite = 1))
Group 	by
		pso.agencyID,
		soa.userID,
		pso.firmID

-- calculate actions till defined date ------------------------
Insert	Into @tmp1(summa, userID, agencyID, firmID)
Select	Sum(-so.Price * so.Ratio), a.userID, agencyID, firmID
From	StudioOrder so
		inner join StudioOrderAction a On so.actionID = a.actionID
		inner join paymentType pt on so.paymenttypeID = pt.paymenttypeID
		inner join 
		(
			select distinct u.userID 
			from [User] u
				left join [GroupMember] gm on u.userID = gm.userID
				left join @ugroups ug on gm.groupID = ug.id
			where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
		) as x on a.userID = x.userID
Where	so.finishDate <= @theDate And
		((pt.isHidden = 1 and @ShowBlack = 1)  or
		(pt.isHidden = 0 and @ShowWhite = 1)) AND
		so.isComplete = 1
Group 	by
		agencyID,
		a.userID,
		a.firmID

Select	agencyID, sum(summa) as summa, userID
Into 	#tmp2
From	@tmp1
Group by agencyID, userID
--Having	sum(summa) < 0

Declare 	cur_H cursor local fast_forward for
Select	agency.agencyID, agency.Name, sum(summa) as summa
From	#tmp2 join Agency on #tmp2.agencyID = agency.agencyID
Group By agency.agencyID, agency.Name

Declare	@agencyID int, @name nvarchar(64), @summa money, @sqlText nvarchar(4000)

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
