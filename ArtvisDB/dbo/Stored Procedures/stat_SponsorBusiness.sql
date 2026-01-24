/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE    Procedure [dbo].[stat_SponsorBusiness] (
@StartDay datetime = null, 
@FinishDay datetime = null,
@ProgramID int = null,
@FirmID int = null,
@UserID int = null,
@ShowBusyOnly bit = 1,
@actionID int = null,
@massmediaID int = null ,
@loggedUserID smallint,
@headCompanyID smallint = NULL
)
As
set nocount on

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
		from SponsorProgramPricelist pl 

	declare @date datetime, @dateweek tinyint
	set @date = @StartDay
	
	declare @res table(issueDate datetime, price money, programID int, tariffID int)
	
	while (@date <= @FinishDay)
	begin 
		set @dateweek = datepart(dw, @date)
	
		insert into @res (issueDate,price,programID,tariffID) 
		select @date + st.[time], st.price, pl.sponsorProgramID, st.tariffID
		from 
			SponsorTariff st
			inner join SponsorProgramPricelist pl on st.pricelistID = pl.pricelistID
			inner join SponsorProgram sp on sp.sponsorProgramID = pl.sponsorProgramID
			inner join @massmedias umm on sp.massmediaID = umm.massmediaID
		where sp.massmediaID = coalesce(@massmediaID, sp.massmediaID) 
		 and pl.startDate <= @FinishDay and pl.finishDate >= @StartDay
		 and ((st.time >= pl.broadcastStart 
				and ((st.monday = 1 and @dateweek = 1)
					or (st.tuesday = 1 and @dateweek = 2)
					or (st.wednesday = 1 and @dateweek = 3)
					or (st.thursday = 1 and @dateweek = 4)
					or (st.friday = 1 and @dateweek = 5)
					or (st.saturday = 1 and @dateweek = 6)
					or (st.sunday = 1 and @dateweek = 7)))
			or (st.time < pl.broadcastStart 
				and ((st.monday = 1 and @dateweek = 7)
					or (st.tuesday = 1 and @dateweek = 1)
					or (st.wednesday = 1 and @dateweek = 2)
					or (st.thursday = 1 and @dateweek = 3)
					or (st.friday = 1 and @dateweek = 4)
					or (st.saturday = 1 and @dateweek = 5)
					or (st.sunday = 1 and @dateweek = 6))))
		
		set @date = dateadd(day, 1, @date)
	end 
	
	select 
		row_number() over(order by i.issueDate) as RowNum,
		mm.name as mmname,
		mm.groupName,
		sp.[name] as programName, 
		r.issueDate as issueDate, 
		r.price as price,
		f.name as firmName,
		hc.name as headCompanyName,
		case when u.userID is null then '' else isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') end as manager,
		a.actionID 
	from @res r
		inner join SponsorProgram sp on r.programID = sp.sponsorProgramID
		left join ProgramIssue i on r.tariffID = i.tariffID 
			and i.programID = r.programID
			and i.issueDate = r.issueDate
		left join SponsorTariff st on i.tariffID = st.tariffID
		left join SponsorProgramPricelist pl on pl.sponsorProgramID = sp.sponsorProgramID and r.issueDate between pl.startDate and pl.finishDate
		left join Campaign c on i.campaignID = c.campaignID
		left join [Action] a on c.actionID = a.actionID
		left join [User] u on a.userID = u.userID
		left join Firm f on a.firmID = f.firmID
		LEFT JOIN HeadCompany hc on hc.headCompanyID = f.headCompanyID
		inner join vMassmedia mm on sp.massmediaID = mm.massmediaID
		inner join @massmedias umm on mm.massmediaID = umm.massmediaID
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
	where sp.massmediaID = coalesce(@massmediaID, sp.massmediaID) 
		and (c.actionID is null or c.actionID = coalesce(@actionID, c.actionID))
		and (a.firmID is null or a.firmID = coalesce(@firmID, a.firmID))
		and (f.headCompanyID is null or f.headCompanyID = coalesce(@headCompanyID, f.headCompanyID))
		and (a.userID is null or a.userID = coalesce(@userID, a.userID))
		and r.programID = coalesce(@programID, r.programID)
		and (@StartDay is null or DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @StartDay)) <= r.issueDate)
		and (@finishDay is null or DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @FinishDay)) >= r.issueDate)
		and (i.isConfirmed is null or i.isConfirmed = 1)
		and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
	order by r.issueDate
end 
else 
begin
	select 
		row_number() over(order by i.issueDate) as RowNum,
		mm.name as mmname,
		mm.groupName,
		sp.[name] as programName, 
		i.issueDate as issueDate, 
		st.price as price,
		f.name as firmName,
		hc.name as headCompanyName,
		isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') as manager,
		a.actionID
	from 
		ProgramIssue i 
		inner join SponsorTariff st on i.tariffID = st.tariffID
		inner join SponsorProgramPricelist pl on st.pricelistID = pl.pricelistID
		inner join Campaign c on i.campaignID = c.campaignID
		inner join [Action] a on c.actionID = a.actionID
		inner join SponsorProgram sp on i.programID = sp.sponsorProgramID
		inner join [User] u on a.userID = u.userID
		inner join Firm f on a.firmID = f.firmID
		inner join HeadCompany hc on hc.headCompanyID = f.headCompanyID
		inner join vMassmedia mm on sp.massmediaID = mm.massmediaID
		inner join @massmedias umm on mm.massmediaID = umm.massmediaID
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
	where sp.massmediaID = coalesce(@massmediaID, sp.massmediaID) 
		and c.actionID = coalesce(@actionID, c.actionID)
		and a.firmID = coalesce(@firmID, a.firmID)
		and f.headCompanyID = coalesce(@headCompanyID, f.headCompanyID)
		and a.userID = coalesce(@userID, a.userID)
		and i.programID = coalesce(@programID, i.programID)
		and (@StartDay is null or DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @StartDay)) <= i.issueDate)
		and (@finishDay is null or DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @FinishDay)) > i.issueDate)
		and i.isConfirmed = 1
		and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
	order by i.issueDate
END
