CREATE PROCEDURE [dbo].[stat_ModuleLoading]
(
@startDay datetime = null, 
@finishDay datetime = null,
@moduleID int = null,
@FirmID int = null,
@UserID int = null,
--@ShowBusyOnly bit = 1,
@actionID int = null,
@massmediaID int = null,
@loggedUserID smallint = null,
@headCompanyID smallint = NULL
)
AS
BEGIN
	SET NOCOUNT ON;
	SET DATEFIRST 1

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

	if @StartDay is not null
		set @StartDay = dbo.ToShortDate(@StartDay)
		
	if @FinishDay is not null
		set @FinishDay = dateadd(ss, -1, dateadd(day, 1, dbo.ToShortDate(@FinishDay)))
/*
	if @UserID is not null 
		set	@ShowBusyOnly = 1
*/
/*
	if @ShowBusyOnly = 0
	begin 
		if @StartDay is null or @FinishDay is null 
		select @StartDay = case when @StartDay is null then dbo.ToShortDate(min(pl.startDate)) else @StartDay end,
			@finishDay = case when @finishDay is null then dbo.ToShortDate(max(pl.finishDate)) else @finishDay end
		from ModulePriceList pl 
		
		declare @date datetime, @dateweek tinyint
		set @date = @StartDay
		
		declare @res table(issueDate datetime, moduleID int)
		
		while (@date <= @FinishDay)
		begin 
			set @dateweek = datepart(dw, @date)
		
			insert into @res (issueDate,moduleID) 
			select distinct @date, mpl.moduleID
			from 
				ModuleTariff mt
				inner join Tariff t on mt.tariffID = t.tariffID
				inner join TariffWindow tw on t.tariffID = tw.tariffId
				inner join ModulePriceList mpl on mt.modulePriceListID = mpl.modulePriceListID
				inner join Module m on mpl.moduleID = m.moduleID
				inner join Pricelist pl on mpl.priceListID = pl.pricelistID
				inner join @massmedias umm on pl.massmediaID = umm.massmediaID
			where m.massmediaID = coalesce(@massmediaID, m.massmediaID) 
			 and mpl.startDate <= @FinishDay and mpl.finishDate >= @StartDay
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
			row_number() over(order by mi.issueDate) as RowNum,
			mm.name as mmname,
			m.[name] as mname, 
			r.issueDate as issueDate, 
			mi.tariffPrice * mi.ratio as price,
			f.name as fname,
			case when u.userID is null then '' else isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') end as manager,
			a.actionID 
		from @res r
			inner join Module m on r.moduleID = m.moduleID
			left join ModuleIssue mi on r.issueDate = mi.issueDate and r.moduleID = mi.moduleID
			left join Campaign c on mi.campaignID = c.campaignID
			left join [Action] a on c.actionID = a.actionID
			left join [User] u on a.userID = u.userID
			left join Firm f on a.firmID = f.firmID
			inner join vMassmedia mm on m.massmediaID = mm.massmediaID
			inner join @massmedias umm on mm.massmediaID = umm.massmediaID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
		where 
			m.massmediaID = coalesce(@massmediaID, m.massmediaID) 
			and m.moduleID = coalesce(@moduleID, r.moduleID)
			and (c.actionID is null or c.actionID = coalesce(@actionID, c.actionID))
			and (a.firmID is null or a.firmID = coalesce(@firmID, a.firmID))
			and (a.userID is null or a.userID = coalesce(@userID, a.userID))
			and (@StartDay is null or @StartDay <= r.issueDate)
			and (@finishDay is null or @FinishDay >= r.issueDate)
			and (mi.isConfirmed is null or mi.isConfirmed = 1)
			and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
		order by mi.issueDate
	end 
	else 
	begin

	*/
		select 
			row_number() over(order by mi.issueDate) as RowNum,
			mm.name as mmname,
			m.[name] as mname, 
			mi.issueDate as issueDate, 
			mi.tariffPrice * mi.ratio as price,
			f.name as fname,
			hc.name as headCompanyName,
			isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') as manager,
			a.actionID as actionID
		from 
			ModuleIssue mi  
			inner join Campaign c on mi.campaignID = c.campaignID
			inner join [Action] a on c.actionID = a.actionID
			inner join Module m on mi.moduleID = m.moduleID
			inner join [User] u on a.userID = u.userID
			inner join Firm f on a.firmID = f.firmID
			inner join HeadCompany hc on hc.headCompanyID = f.headCompanyID
			inner join vMassmedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias umm on mm.massmediaID = umm.massmediaID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
		where c.massmediaID = coalesce(@massmediaID, c.massmediaID) 
			and c.actionID = coalesce(@actionID, c.actionID)
			and a.firmID = coalesce(@firmID, a.firmID)
			and hc.headCompanyID = coalesce(@headCompanyID, hc.headCompanyID)
			and a.userID = coalesce(@userID, a.userID)
			and mi.moduleID = coalesce(@moduleID, mi.moduleID)
			and (@StartDay is null or @StartDay <= mi.issueDate)
			and (@finishDay is null or @FinishDay >= mi.issueDate)
			and mi.isConfirmed = 1
			and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
		order by mi.issueDate
	--end
END
