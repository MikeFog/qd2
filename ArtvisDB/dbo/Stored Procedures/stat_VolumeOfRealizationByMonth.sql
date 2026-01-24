CREATE  Procedure [dbo].[stat_VolumeOfRealizationByMonth]
(
@StartDay DATETIME = null,
@FinishDay DATETIME = null,
@FirmID int = null, 
@ManagerID int = null,
@agencyID int = null,
@massmediaID smallint =NULL,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserId smallint 
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUserMassmedia(@loggedUserID, @massmediaID)

	declare @isRightToViewForeignActions bit,
			@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	If	@StartDay Is Null Or @FinishDay Is Null
	Begin
		Raiserror('FilterStartFinishDays', 16, 1)
		Return
	End

	Set	@StartDay = dbo.ToShortDate(@StartDay)
	Set	@FinishDay = dbo.ToShortDate(@FinishDay)

	declare @month tinyint, @year smallint, @finishYear smallint, @finishMonth tinyint 
	
	set @month = datepart(month, @StartDay)
	set @year = datepart(year, @StartDay)
	set @finishMonth = datepart(month, @FinishDay)
	set @finishYear = datepart(year, @FinishDay)

	create table #res (m tinyint, y smallint, massmediaID smallint, actionID int, managerID smallint, agencyID smallint, price money)
	
	declare @start datetime, @end datetime
	
	declare	@campaignID int, @actionID int, @mmID smallint, @campaignTypeID tinyint, @userID smallint, @campaignPrice money, @cStart datetime, @cEnd datetime, @aDiscount float, @cAgencyID int
	
	while @year < @finishYear or (@year = @finishYear and @month <= @finishMonth)
	begin 
		set @start = convert(datetime,'01.' + cast(@month as varchar) + '.' + cast(@year as varchar), 104)
		set @end = dateadd(day, -1, dateadd(month, 1, convert(datetime,'01.' + cast(@month as varchar) + '.' + cast(@year as varchar), 104)))

		if @@error <> 0
		begin 
			raiserror('InternalError', 16, 1)
			return
		end 

		if @StartDay > @start
			set @start = @StartDay
		
		if @FinishDay < @end
			set @end = @FinishDay

		declare cur_companies cursor local fast_forward
		for
		select c.campaignID, c.actionID, c.massmediaID, c.campaignTypeID, a.userID, c.agencyID, c.startDate, c.finishDate, a.discount, c.finalPrice
		from	
			Campaign c
			inner join [Action] a On c.ActionID = a.actionID
				and a.[isConfirmed] = 1 and a.isSpecial = 0
			inner join PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
			inner join @massmedias umm on c.massmediaID = umm.massmediaID
				and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
			inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
		where c.campaignTypeID <> 4 and c.StartDate <= @end and
			c.FinishDate >= @start and
			c.AgencyID = coalesce(@AgencyID, c.AgencyID) and
			a.firmID = coalesce(@FirmID, a.firmID) and
			a.userID = coalesce(@ManagerID, a.userID) and
			(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
			(@ShowBlack <> 0 or PaymentType.isHidden = 0) 
		union 
		select distinct	c.campaignID, c.actionID, m.massmediaID, c.campaignTypeID, a.userID, c.agencyID, c.startDate, c.finishDate, a.discount, c.finalPrice
		From	
			Campaign c
			INNER Join [Action] a On c.ActionID = a.actionID
				AND a.[isConfirmed] = 1 and a.isSpecial = 0
			INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
			INNER JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
			INNER JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
			INNER JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
			inner join @massmedias umm on m.massmediaID = umm.massmediaID
				and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
			inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
		where c.[campaignTypeID] = 4 and
				c.StartDate <= @end and
				c.FinishDate >= @start and
				c.AgencyID = coalesce(@AgencyID, c.AgencyID) and
				a.firmID = coalesce(@FirmID, a.firmID) and
				a.userID = coalesce(@ManagerID, a.userID) and
				(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
				(@ShowBlack <> 0 or PaymentType.isHidden = 0)
			
		open cur_companies	
		fetch next from cur_companies into @campaignID, @actionID, @mmID, @campaignTypeID, @userID, @cAgencyID, @cStart, @cEnd, @aDiscount, @campaignPrice
		
		while	@@fetch_status = 0
		begin 
			if (@campaignTypeID <> 4) and (@cStart between @start and @end) and (@cEnd between @start and @end)
				Set @campaignPrice = case when @campaignTypeID <> 4 then @campaignPrice * @aDiscount else @campaignPrice end 
			else 
				exec GetPriceByPeriod @campaignID, @campaignTypeID, @start, @end, @campaignPrice OUTPUT, @mmID
			
			if @campaignPrice > 0
			begin
				insert into #res (m,y,massmediaID,actionID,managerID,agencyID,price) 
				values (@month,@year,@mmID,@actionID,@userID,@cAgencyID,@campaignPrice ) 
			end
			
			fetch next from cur_companies into @campaignID, @actionID, @mmID, @campaignTypeID, @userID, @cAgencyID, @cStart, @cEnd, @aDiscount, @campaignPrice
		end 
		
		close cur_companies	
		deallocate cur_companies
	
		set @month = @month + 1
		if (@month > 12)
		begin
			set @month = 1
			set @year = @year + 1
		end 
	end 
	
	-------------------------------------------------------------------------
	declare	@sql nvarchar(max)
	set @sql = 'select row_number() over(order by #res.[y], #res.[m]) as RowNum,
				iMonthName.name + space(1) + cast(#res.[y] as varchar) + '' г.'' as [period],'
			
	if @IsGroupByMassmedia <> 0
		set @sql = @sql + 'vMassMedia.name as mmName, vMassMedia.groupName, '
	if @IsGroupByFirm <> 0
		set @sql = @sql + 'Firm.Name as firmName,'
	if @IsGroupByManager <> 0
		set @sql = @sql + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as manager,'
	if @IsGroupByAgency <> 0
		set @sql = @sql + N'Agency.Name as agencyName,'

	set @sql = @sql + ' sum(#res.price) as price from #res inner join [Action] on #res.actionID = [Action].actionID inner join iMonthName on #res.m = iMonthName.number '
	
	if @IsGroupByMassmedia <> 0
		set @sql = @sql + ' inner join vMassMedia on #res.massmediaID = vMassMedia.massmediaID '
	if @IsGroupByFirm <> 0
		set @sql = @sql + ' inner join Firm on [Action].firmID = Firm.firmID '
	if @IsGroupByManager <> 0
		set @sql = @sql + ' inner join [User] on [Action].userID = [User].userID '
	if @IsGroupByAgency <> 0
		set @sql = @sql + ' inner join Agency on #res.agencyID = Agency.agencyID '

	set @sql = @sql + ' group by #res.[y], #res.[m], iMonthName.name '

	if @IsGroupByMassmedia <> 0
		set @sql = @sql + ',vMassMedia.name, vMassMedia.groupName '
	if @IsGroupByFirm <> 0
		set @sql = @sql + ',Firm.name '
	if @IsGroupByManager <> 0
		set @sql = @sql + ',coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') '
	if @IsGroupByAgency <> 0
		set @sql = @sql + ',Agency.name '
	
	set @sql = @sql + ' order by #res.[y], #res.[m] '
	--select @sql
	EXECUTE sp_executesql @sql
	
	drop table #res

