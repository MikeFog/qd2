/*
Modified by Denis Gladkikh (dgladkikh@fogsoft.ru) 19.09.2008 - Bad logic
*/
CREATE      PROCEDURE [dbo].[stat_BalanceAgency]
(
	@theDate datetime = null,
	@PaymentTypeID int = default,
	@ShowBlack bit = 1,
	@ShowWhite bit = 1,
	@loggedUserID smallint
) 
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
-- calculate payments till defined date -----------------------
create	table #tmp1
(
[summa] money,
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

Insert 	Into #tmp1
Select	sum(p.summa) as summa, 
		p.agencyID,
		p.firmID
From	payment p
		inner join paymentType pt on p.paymentTypeID = pt.paymentTypeID
		inner join 
			(
				select distinct am.agencyID from AgencyMassmedia am 
					inner join @massmedias mm on am.massmediaID = mm.massmediaID and mm.foreignMassmedia = 1
			) x on p.agencyID = x.agencyID
Where	(@theDate IS NULL OR p.paymentDate <= @theDate) and
		p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
		((pt.isHidden = 1 and @ShowBlack = 1)  or 
		(pt.isHidden = 0 and @ShowWhite = 1))
group by p.agencyID, p.firmID

-- calculate actions till defined date ------------------------
declare 		cur_Companies cursor local fast_forward for
select distinct		c.campaignID, c.campaignTypeID, 
			c.startDate, a.userid, 
			c.agencyID, c.paymentTypeID, 
			c.finishDate, c.FINALPRICE,
			a.firmID, a.discount
from		campaign c
			inner join [Action] a  on c.actionID = a.actionID
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
where		(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(a.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and	
			c.startDate <= @theDate and
			c.paymentTypeID = IsNull(@PaymentTypeID, c.paymentTypeID) and
			((pt.isHidden = 1 and @ShowBlack = 1)  or 
			(pt.isHidden = 0 and @ShowWhite = 1)) AND a.[isConfirmed] = 1

declare	@campaignID int, @TypeID int, @UserID int,
		@StartDay datetime, 
		@Price money, @AgencyID int, 
		@FinishDay datetime, @FinalPrice money, @firmID int, @actionDiscount float

open	cur_Companies
fetch	next from cur_Companies 
into 	@campaignID, @TypeID, @StartDay, @UserID, @AgencyID, @PaymentTypeID, 
			@FinishDay, @FinalPrice, @firmID, @actionDiscount
	
while	@@fetch_status = 0
	begin 
	
	If	@theDate IS NULL OR @theDate > @FinishDay
		Set 	@Price = case when @TypeID <> 4 then @FinalPrice * @actiondiscount else @FinalPrice end 
	else
		EXEC GetPriceByPeriod @campaignID, @TypeID, @StartDay, @theDate, @Price output

	Insert	Into #tmp1(summa, agencyID, firmID)
	Values	(-@Price, @AgencyID, @firmID)

	fetch	next from cur_Companies 
	into 	@campaignID, @TypeID, @StartDay, @UserID, @AgencyID, @PaymentTypeID, 
				@FinishDay, @FinalPrice, @firmID, @actionDiscount
	end
	
close cur_Companies
deallocate	cur_Companies

select		agencyID, sum(summa) as sum_plus, cast(0 as money) as sum_minus
into 		#tmp2
from		#tmp1
group by 	agencyID, firmID
having		sum(summa) > 0

insert into 	#tmp2
select		agencyID, cast(0 as money) as sum_plus, sum(summa) as sum_minus
from		#tmp1
group by 	agencyID, firmID
having		sum(summa) < 0

select		row_number() over(order by agency.name) as RowNum,
			agency.name as 'Агенство',
			sum(sum_plus) as '$Сумма (+)',
			sum(sum_minus) as '$Сумма (-)'
from		#tmp2 JOIN agency ON #tmp2.agencyID = agency.agencyID
Group 	by 	agency.name
order by 	agency.name

drop table #tmp1
drop table #tmp2
