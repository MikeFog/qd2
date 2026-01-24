-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: Unknown
-- Description:	Modified to include headCompanyID filtering and grouping
-- =============================================
CREATE PROCEDURE [dbo].[stat_PackModuleFinancy]
(
	@startDay datetime = null, 
	@finishDay datetime = null,
	@packModuleID int = null,
	@FirmID int = null,
	@headCompanyID int = null,
	@UserID int = null,
	@ShowBusyOnly bit = 1,
	@actionID int = null,
	@IsGroupByManager bit = 0,
	@IsGroupByFirm bit = 0,
	@IsGroupByHeadCompany bit = 0,
	@IsGroupByAction bit = 0,
	@IsGroupByDay bit = 0,
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
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

	if @startDay is not null
		set @startDay = dbo.ToShortDate(@startDay)
		
	if @finishDay is not null
		set @finishDay = dateadd(ss, -1, dateadd(day, 1, dbo.ToShortDate(@finishDay)))
		
	create table #res (packModuleID int, price money, actionID int, issueDate datetime, headCompanyID int)
	
	insert into [#res] (packModuleID, price, actionID, issueDate, headCompanyID)
	select 
		pmpl.packModuleID, 
		pmi.tariffPrice * pmi.ratio, 
		a.actionID, 
		pmi.issueDate,
		f.headCompanyID
	from 
		PackModuleIssue pmi  
		inner join Campaign c on pmi.campaignID = c.campaignID
		inner join [Action] a on c.actionID = a.actionID
		inner join Firm f on a.firmID = f.firmID
		inner join PackModulePriceList pmpl on pmpl.pricelistID = pmi.pricelistID
		inner join 
		(
			select distinct u.userID 
			from [User] u
				left join [GroupMember] gm on u.userID = gm.userID
				left join @ugroups ug on gm.groupID = ug.id
			where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
		) x on a.userID = x.userID
	where c.actionID = coalesce(@actionID, c.actionID)
		and a.firmID = coalesce(@firmID, a.firmID)
		and f.headCompanyID = coalesce(@headCompanyID, f.headCompanyID)
		and a.userID = coalesce(@userID, a.userID)
		and pmpl.packModuleID = coalesce(@packModuleID, pmpl.packModuleID)
		and (@startDay is null or @startDay <= pmi.issueDate)
		and (@finishDay is null or @finishDay >= pmi.issueDate)
		and pmi.isConfirmed = 1
		and not exists (
			select * 
			from PackModuleContent pmc 
				inner join Module m on pmc.moduleID = m.moduleID
				left join @massmedias ummm on m.massmediaID = ummm.massmediaID
			where pmpl.pricelistID = pmc.pricelistID 
				and (ummm.massmediaID is null or 
					 (a.userID = @loggedUserID and ummm.myMassmedia = 0) or
					 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0))
		)
	
	declare @fullPrice money
	
	select @fullPrice = sum(price) from #res 		
		
	declare @sql nvarchar(max)
	
	set @sql = 'select 
					row_number() over(order by pm.packModuleID, pm.name) as RowNum,
					pm.[name] as pmname, 
					pm.packModuleID,
					(sum(r.price) / @fullPrice) * 100 as percentage,
					sum(r.price) as price ' 
					
	if @IsGroupByAction = 1
		select @sql = @sql + ', r.actionID as actionID '
	if @IsGroupByFirm = 1
		select @sql = @sql + ', f.firmID, f.name as fname '
	if @IsGroupByHeadCompany = 1
		select @sql = @sql + ', hc.headCompanyID, hc.name as head_company '
	if @IsGroupByManager = 1
		select @sql = @sql + ', u.userID, isnull(u.lastname, '''') + space(1) + isnull(left(u.firstname, 1), '''') + ''.'' + isnull(left(u.secondname, 1), '''') as manager '
	if @IsGroupByDay = 1
		select @sql = @sql + ', r.issueDate as issueDate '
					
	select @sql = @sql + 'from [#res] r 
						inner join PackModule pm on r.packModuleID = pm.packModuleID
						inner join [Action] a on r.actionID = a.actionID
						inner join Firm f on a.firmID = f.firmID
						inner join [User] u on a.userID = u.userID '
	if @IsGroupByHeadCompany = 1
		select @sql = @sql + 'inner join HeadCompany hc on r.headCompanyID = hc.headCompanyID '
					
	select @sql = @sql + 'group by pm.packModuleID, pm.name '		
	
	if @IsGroupByAction = 1
		select @sql = @sql + ', r.actionID '
	if @IsGroupByFirm = 1
		select @sql = @sql + ', f.firmID, f.name '
	if @IsGroupByHeadCompany = 1
		select @sql = @sql + ', hc.headCompanyID, hc.name '
	if @IsGroupByManager = 1
		select @sql = @sql + ', u.userID, u.lastname, u.secondname, u.firstname '
	if @IsGroupByDay = 1
		select @sql = @sql + ', r.issueDate '
		
	execute sp_executesql @sql, N'@fullPrice money', @fullPrice
END
