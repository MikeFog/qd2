CREATE  Procedure [dbo].[job_DeleteHistory]
(
	@date datetime
)
WITH EXECUTE AS OWNER
As
set	nocount ON

declare @actionId int
declare @balance table (
			firmId smallint not null,
			agencyId smallint not null,
			paymentTypeId smallint not null,
			summa money not null,
			managerId smallint not null,
			oldActionID int not null,
			newActionID int null
		)

declare @actions table (
			actionID int not null
		)


insert into @actions
select a.actionID
from [Action] a
where a.finishDate < @date and a.isConfirmed = 1 and a.isSpecial = 0
	and not exists(select * from Campaign c 
		inner join PaymentAction pa on c.actionID = pa.actionID
		inner join Payment p on p.paymentID = pa.paymentID 
		where c.actionID = a.actionID and (p.paymentTypeID <> c.paymentTypeID or p.agencyID <> c.agencyID or p.firmID <> a.firmID ))


insert into @balance (firmId, agencyId, paymentTypeId, summa, managerId, oldActionID)
select a.firmID, c.agencyID, c.paymentTypeID, 
	sum(case when c.campaignTypeID <> 4 then c.finalPrice * a.discount else c.finalPrice end),
	a.userID, a.actionID
from [Action] a 
	inner join Campaign c on a.actionID = c.actionID
	inner join @actions a0 on a.actionID = a0.actionID
group by a.firmID, c.agencyID, c.paymentTypeID, a.userID, a.actionID



declare cur_balance cursor local fast_forward
for
select b.firmID, b.agencyID, b.paymentTypeID, sum(b.summa), b.managerID from @balance b
group by b.firmID, b.agencyID, b.paymentTypeID, b.managerID


declare @firmID smallint, @agencyID smallint, @paymentTypeID smallint, @summa money, @userID smallint

open cur_balance

fetch next from cur_balance into @firmID, @agencyID, @paymentTypeID, @summa, @userID

while @@fetch_status = 0
begin

	insert into [Action] (firmID,startDate,finishDate,discount,userID,tariffPrice,priceSumByCampaigns,createDate,modDate,isSpecial,isConfirmed,totalPrice,isAlerted) 
	values(@firmID,@date,@date,1, @userID,@summa,@summa,@date,@date,1,1,@summa,1) 
	
	SET @actionID = SCOPE_IDENTITY()
	
	update @balance set newActionID = @actionId where 
		firmId = @firmID and agencyId = @agencyID and paymentTypeId = @paymentTypeID and managerId = @userID
	
	insert into Campaign (actionID,startDate,finishDate,discount,tariffPrice,finalPrice,paymentTypeID,massmediaID,campaignTypeID,issuesCount,issuesDuration,modTime,modUser,agencyID,timeBonus,programsCount,billNo,billDate,managerDiscount,contractNo) 
	values (@actionID, @date, @date, 1, @summa, @summa, @paymentTypeID, null, 1, 0, 0, @date,@userID,@agencyID,0,0,null,null,1,null)


	fetch next from cur_balance into @firmID, @agencyID, @paymentTypeID, @summa, @userID
end

close cur_balance
deallocate cur_balance

insert into PaymentAction (paymentId, actionID, summa)
select pa.paymentID, x.newActionID, sum(pa.summa)
from PaymentAction pa
	inner join(
		select distinct b0.oldActionID, b0.newActionID from @balance b0 
	) x on pa.actionID = x.oldActionID
	inner join Payment p on pa.paymentID = p.paymentID 
group by pa.paymentID, x.newActionID


delete from pa
from PaymentAction pa
	inner join @actions a on pa.actionID = a.actionID


DROP INDEX [IX_TariffWindow_WindowID_DayOriginal] ON [dbo].[TariffWindow] WITH ( ONLINE = OFF )
DROP INDEX [UIX_TariffWindow_Massmedia_Date] ON [dbo].[TariffWindow] WITH ( ONLINE = OFF )
DROP INDEX [UIX_TariffWindow_Tariff_Date] ON [dbo].[TariffWindow] WITH ( ONLINE = OFF )
DROP INDEX [IX_Issue_ModuleIssueId] ON [dbo].[Issue] WITH ( ONLINE = OFF )
DROP INDEX [IX_Issue] ON [dbo].[Issue] WITH ( ONLINE = OFF )
DROP INDEX [IX_Issue_PackModuleIssueId] ON [dbo].[Issue] WITH ( ONLINE = OFF )
		
delete from tl from TransferLog tl 
		inner join @actions a0 on tl.actionID = a0.actionID
		
delete from di from LogDeletedIssue di 
		inner join @actions a0 on di.actionID = a0.actionID

delete from i from Issue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
	inner join @actions a0 on a.actionID = a0.actionID
		
delete from i from ModuleIssue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
	inner join @actions a0 on a.actionID = a0.actionID
		
delete from i from PackModuleIssue i 
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
	inner join @actions a0 on a.actionID = a0.actionID

delete from c from [Campaign] c
	inner join @actions a0 on c.actionID = a0.actionID

delete from a from [Action] a
	inner join @actions a0 on a.actionID = a0.actionID

delete from tw 
from TariffWindow tw 
	where tw.dayActual < @date and
	not exists(select * from Issue i where tw.windowId = i.actualWindowID or tw.windowId = i.originalWindowID)
	
	
CREATE UNIQUE NONCLUSTERED INDEX [IX_TariffWindow_WindowID_DayOriginal] ON [dbo].[TariffWindow] 
(
	[windowId] ASC,
	[dayOriginal] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [UIX_TariffWindow_Massmedia_Date] ON [dbo].[TariffWindow] 
(
	[windowDateOriginal] ASC,
	[massmediaID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [UIX_TariffWindow_Tariff_Date] ON [dbo].[TariffWindow] 
(
	[tariffId] ASC,
	[windowDateOriginal] ASC,
	[massmediaID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_Issue_PackModuleIssueId] ON [dbo].[Issue] 
(
	[packModuleIssueID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE UNIQUE NONCLUSTERED INDEX [IX_Issue] ON [dbo].[Issue] 
(
	[issueID] ASC,
	[actualWindowID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

CREATE NONCLUSTERED INDEX [IX_Issue_ModuleIssueId] ON [dbo].[Issue] 
(
	[moduleIssueID] ASC
)WITH (PAD_INDEX  = OFF, STATISTICS_NORECOMPUTE  = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON) ON [PRIMARY]

exec job_DeleteEmptyActions


