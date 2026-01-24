CREATE PROCEDURE [dbo].[stat_PackModuleLoading]
(
@startDay datetime = null, 
@finishDay datetime = null,
@packModuleID int = null,
@FirmID int = null,
@UserID int = null,
@ShowBusyOnly bit = 1,
@actionID int = null,
@loggedUserID smallint,
@headCompanyId smallint = null
)
AS
BEGIN
	SET NOCOUNT ON;
	
	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,
			@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	SET DATEFIRST 1

	if @StartDay is not null
		set @StartDay = dbo.ToShortDate(@StartDay)
		
	if @FinishDay is not null
		set @FinishDay = dateadd(ss, -1, dateadd(day, 1, dbo.ToShortDate(@FinishDay)))

	if @UserID is not null 
		set	@ShowBusyOnly = 1
		
	if @ShowBusyOnly = 0
	begin 
		if @StartDay is null or @FinishDay is null 
		select @StartDay = case when @StartDay is null then dbo.ToShortDate(min(pl.startDate)) else @StartDay end,
			@finishDay = case when @finishDay is null then dbo.ToShortDate(max(pl.finishDate)) else @finishDay end
		from PackModulePriceList pl 
		
		declare @date datetime, @dateweek tinyint
		set @date = @StartDay
		
		declare @res table(issueDate datetime, packModulePriceListID int)
		
		while (@date <= @FinishDay)
		begin 
			set @dateweek = datepart(dw, @date)
		
			insert into @res (issueDate,packModulePriceListID) 
			select distinct @date, pmpl.pricelistID
			from 
				ModuleTariff mt
				inner join Tariff t on mt.tariffID = t.tariffID
				inner join TariffWindow tw on t.tariffID = tw.tariffId
				inner join ModulePriceList mpl on mt.modulePriceListID = mpl.modulePriceListID
				inner join Pricelist pl on mpl.priceListID = pl.pricelistID
				inner join PackModuleContent pmc on pmc.modulePriceListID = mpl.modulePriceListID
				inner join PackModulePriceList pmpl on pmc.pricelistID = pmpl.priceListID
			where pmpl.startDate <= @FinishDay and pmpl.finishDate >= @StartDay
			 and (t.[time] < pl.broadcastStart and ((t.monday = 1 and @dateweek = 7)
					or (t.tuesday = 1 and @dateweek = 1)
					or (t.wednesday = 1 and @dateweek = 2)
					or (t.thursday = 1 and @dateweek = 3)
					or (t.friday = 1 and @dateweek = 4)
					or (t.saturday = 1 and @dateweek = 5)
					or (t.sunday = 1 and @dateweek = 6))
				 or (t.[time] > pl.broadcastStart and ((t.monday = 1 and @dateweek = 1)
					or (t.tuesday = 1 and @dateweek = 2)
					or (t.wednesday = 1 and @dateweek = 3)
					or (t.thursday = 1 and @dateweek = 4)
					or (t.friday = 1 and @dateweek = 5)
					or (t.saturday = 1 and @dateweek = 6)
					or (t.sunday = 1 and @dateweek = 7))))
							
			set @date = dateadd(day, 1, @date)
		end 
		
		select 
			row_number() over(order by pmi.issueDate) as RowNum,
			pm.[name] as pmname, 
			r.issueDate as issueDate, 
			pmi.tariffPrice * pmi.ratio as price,
			f.name as fname,
			hc.name as headCompanyName,
			case when u.userID is null then '' else isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') end as manager,
			a.actionID 
		from @res r
			inner join PackModulePriceList pmpl on r.packModulePriceListID = pmpl.priceListID
			inner join PackModule pm on pmpl.packModuleID = pm.packModuleID
			left join PackModuleIssue pmi on r.issueDate = pmi.issueDate and pmi.pricelistID = pmpl.priceListID
			left join Campaign c on pmi.campaignID = c.campaignID
			left join [Action] a on c.actionID = a.actionID
			left join [User] u on a.userID = u.userID
			left join Firm f on a.firmID = f.firmID
			left join HeadCompany hc on hc.headCompanyID = f.headCompanyID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
		where (c.actionID is null or c.actionID = coalesce(@actionID, c.actionID))
			and (a.firmID is null or a.firmID = coalesce(@firmID, a.firmID))
			and (hc.headCompanyID is null or hc.headCompanyID = coalesce(@headCompanyID, hc.headCompanyID))
			and (a.userID is null or a.userID = coalesce(@userID, a.userID))
			and pm.packModuleID = coalesce(@packModuleID, pm.packModuleID)
			and (@StartDay is null or @StartDay <= r.issueDate)
			and (@finishDay is null or @FinishDay >= r.issueDate)
			and (pmi.isConfirmed is null or pmi.isConfirmed = 1)
			and not exists(select * 
						from PackModuleContent pmc1
							inner join Module m1 on pmc1.moduleID = m1.moduleID
							left join @massmedias ummm on m1.massmediaID = ummm.massmediaID
						where  pmpl.pricelistID = pmc1.pricelistID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0)))
		order by pmi.issueDate
	end 
	else 
	begin
		select 
			row_number() over(order by pmi.issueDate) as RowNum,
			pm.[name] as pmname, 
			pmi.issueDate as issueDate, 
			pmi.tariffPrice * pmi.ratio as price,
			f.name as fname,
			hc.name as headCompanyName,
			isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') as manager,
			a.actionID as actionID
		from 
			PackModuleIssue pmi  
			inner join Campaign c on pmi.campaignID = c.campaignID
			inner join [Action] a on c.actionID = a.actionID
			inner join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
			inner join PackModule pm on pmpl.packModuleID = pm.packModuleID
			inner join [User] u on a.userID = u.userID
			inner join Firm f on a.firmID = f.firmID
			inner join HeadCompany hc on hc.headCompanyID = f.headCompanyID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
		where c.actionID = coalesce(@actionID, c.actionID)
			and a.firmID = coalesce(@firmID, a.firmID)
			and hc.headCompanyID = coalesce(@headCompanyID, hc.headCompanyID)
			and a.userID = coalesce(@userID, a.userID)
			and pm.packModuleID = coalesce(@packModuleID, pm.packModuleID)
			and (@StartDay is null or @StartDay <= pmi.issueDate)
			and (@finishDay is null or @FinishDay >= pmi.issueDate)
			and pmi.isConfirmed = 1
			and not exists(select * 
						from PackModuleContent pmc1
							inner join Module m1 on pmc1.moduleID = m1.moduleID
							left join @massmedias ummm on m1.massmediaID = ummm.massmediaID
						where  pmpl.pricelistID = pmc1.pricelistID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0)))
		order by pmi.issueDate
	end
END
