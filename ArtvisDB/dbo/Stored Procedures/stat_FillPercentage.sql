CREATE   Procedure [dbo].[stat_FillPercentage]
(
@StartDay datetime = null, 
@FinishDay datetime = null,
@StartTime datetime = NULL,
@FinishTime datetime = NULL,
@MassMediaID smallint = default,
@massmediaGroupID int = default,
@PaymentTypeID smallint = default,
@campaignTypeID tinyint = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByDay bit = 0,
@IncludeEmptyBlocks bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As
SET NOCOUNT on

declare @sql nvarchar(4000)
declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
declare @available TABLE (mmid smallint, fulltime int, primary key (mmid))
declare @availableByDays TABLE (mmid smallint, date datetime, fulltime int, primary key (mmid, date))

create table #issues(mmid smallint, campaigntypeid tinyint, paymenttypeid tinyint, date datetime, duration int, fulltime int, primary key (mmid, date, campaigntypeid, paymenttypeid))

if (@StartDay is not null)
	set @StartDay = dbo.ToShortDate(@StartDay)

if (@FinishDay is not null)
	set @FinishDay = dbo.ToShortDate(@FinishDay)

insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

IF @IsGroupByDay=0
BEGIN
	insert into @available 
	select tw.massmediaID as mmid, 
		sum(tw.duration) as fulltime
	from TariffWindow tw
		inner join MassMedia mm On mm.massmediaID = tw.massmediaID
	where tw.tariffId is not null and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID) 
		and tw.dayActual BETWEEN COALESCE(@StartDay,'1900-01-01') AND COALESCE(@FinishDay, '2200-12-31')
		and CAST(tw.windowDateActual AS time) BETWEEN COALESCE(CAST(@StartTime AS time),'00:00:00.0000000') AND COALESCE(CAST(@FinishTime AS time), '23:59:59.9999999')
		and tw.massmediaID IN (
			select massmediaID from @massmedias
			)
		AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
		and (mm.massmediaGroupID = coalesce(@massmediaGroupID, mm.massmediaGroupID) or (mm.massmediaGroupID is null and @massmediaGroupID is null))
	group by tw.massmediaID

	insert into #issues (mmid, campaigntypeid, paymenttypeid, date, duration, fulltime) 
	select tw.massmediaID as mmid, c.campaignTypeID as campaigntypeid, c.paymentTypeID as paymenttypeid
		, tw.dayActual as date
		, sum((r.duration / (case when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0 then tw.capacityInUseConfirmed else 1 end))) as duration
		, MAX(r1.fulltime)
	from 
		TariffWindow tw
		inner join Issue i on i.actualWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID
		inner join [Action] a on c.actionID = a.actionID
		inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID = 1 
		inner join @available r1 ON r1.mmid = tw.massmediaID
	where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)	
		and i.isConfirmed = 1 
		and tw.dayActual BETWEEN COALESCE(@StartDay,'1900-01-01') AND COALESCE(@FinishDay, '2200-12-31')
		and CAST(tw.windowDateActual AS time) BETWEEN COALESCE(CAST(@StartTime AS time),'00:00:00.0000000') AND COALESCE(CAST(@FinishTime AS time), '23:59:59.9999999')
		and EXISTS (
			select 1 
			from @massmedias umm
			where umm.massmediaID = tw.massmediaID
				and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
			)
		AND (a.userID = @loggedUserID 
				OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1 
				OR (
					dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1 
					AND EXISTS (
						SELECT 1 
						FROM GroupMember gm 
							JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
						WHERE a.userID = gm.userID
						)
					)
				)
	group by tw.massmediaID, tw.dayActual, c.campaignTypeID, c.paymentTypeID
END
ELSE
BEGIN
	insert into @availableByDays 
	select tw.massmediaID as mmid, 
		tw.dayActual, 
		sum(tw.duration) as fulltime
	from TariffWindow tw
	where tw.tariffId is not null and tw.massmediaID = coalesce(@massmediaID, tw.massmediaID) 
		and tw.dayActual BETWEEN COALESCE(@StartDay,'1900-01-01') AND COALESCE(@FinishDay, '2200-12-31')
		and CAST(tw.windowDateActual AS time) BETWEEN COALESCE(CAST(@StartTime AS time),'00:00:00.0000000') AND COALESCE(CAST(@FinishTime AS time), '23:59:59.9999999')
		and tw.massmediaID IN (
			select massmediaID from @massmedias
			)
		AND (@IncludeEmptyBlocks = 1 OR tw.timeInUseConfirmed > 0)
	group by tw.massmediaID,tw.dayActual

	insert into #issues (mmid, campaigntypeid, paymenttypeid, date, duration, fulltime) 
	select tw.massmediaID as mmid, c.campaignTypeID as campaigntypeid, c.paymentTypeID as paymenttypeid
		, tw.dayActual as date
		, sum((r.duration / (case when tw.maxCapacity > 0 and tw.capacityInUseConfirmed > 0 then tw.capacityInUseConfirmed else 1 end))) as duration
		, MAX(r1.fulltime)
	from 
		TariffWindow tw
		inner join Issue i on i.actualWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID
		inner join [Action] a on c.actionID = a.actionID
		inner join Roller r on i.rollerID = r.rollerID and r.rolActionTypeID = 1 
		inner join @availableByDays r1 ON r1.mmid = tw.massmediaID and r1.date = tw.dayActual
	where tw.massmediaID = coalesce(@massmediaID, tw.massmediaID)	
		and i.isConfirmed = 1 
		and tw.dayActual BETWEEN COALESCE(@StartDay,'1900-01-01') AND COALESCE(@FinishDay, '2200-12-31')
		and CAST(tw.windowDateActual AS time) BETWEEN COALESCE(CAST(@StartTime AS time),'00:00:00.0000000') AND COALESCE(CAST(@FinishTime AS time), '23:59:59.9999999')
		and EXISTS (
			select 1 
			from @massmedias umm
			where umm.massmediaID = tw.massmediaID
				and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
			)
		AND (a.userID = @loggedUserID 
				OR dbo.fn_IsRightToViewForeignActions(@loggedUserID) = 1 
				OR (
					dbo.fn_IsRightToViewGroupActions(@loggedUserID) = 1 
					AND EXISTS (
						SELECT 1 
						FROM GroupMember gm 
							JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
						WHERE a.userID = gm.userID
						)
					)
				)
	group by tw.massmediaID, tw.dayActual, c.campaignTypeID, c.paymentTypeID
END


set @sql = 'select row_number() over(order by ' 

if (@IsGroupByDay = 1)
	set @sql = @sql + ' i.date, '

set @sql = @sql + ' mm.name) as RowNum, '

if (@IsGroupByDay = 1)
	set @sql = @sql + 'i.date as [date], '

set @sql = @sql + '	mm.[name] as [massmedia], mm.groupName, '
	
if (@IsGroupByPaymentType = 1)
	set @sql = @sql + 'pt.[name] as [paymentType], '
if (@IsGroupByCampaignType = 1)
	set @sql = @sql + 'ct.[name] as [campaignType], '
	
set @sql = @sql + 'dbo.fn_Int2Time(sum(i.duration)) as [realTime], 
		cast(sum(cast(i.duration as float)) / max(cast(i.fulltime as float)) * 100 as decimal(5,2))  as [fill]
	from #issues i 
		inner join vMassmedia mm on i.mmid = mm.massmediaID '

if (@IsGroupByPaymentType = 1)
	set @sql = @sql + ' inner join PaymentType pt on i.paymenttypeid = pt.paymentTypeID '
if (@IsGroupByCampaignType = 1)
	set @sql = @sql + ' inner join iCampaignType ct on i.campaigntypeid = ct.campaignTypeID '

set @sql = @sql + ' WHERE 1 = 1 ' 

if (@PaymentTypeID IS NOT NULL)
	set @sql = @sql + ' AND i.paymentTypeID = ' + CAST(@PaymentTypeID AS varchar)

if (@campaignTypeID IS NOT NULL)
	set @sql = @sql + ' AND i.campaignTypeID = ' + CAST(@campaignTypeID AS varchar)
	
set @sql = @sql + ' group by ' 

if (@IsGroupByDay = 1)
	set @sql = @sql + ' i.date, '
	
set @sql = @sql + ' mm.massmediaID, mm.[name], mm.groupName '	
	
if (@IsGroupByPaymentType = 1)
	set @sql = @sql + ' , pt.paymentTypeID, pt.[name] '
if (@IsGroupByCampaignType = 1)
	set @sql = @sql + ' , ct.campaignTypeID, ct.[name] '

set @sql = @sql + ' order by ' 

if (@IsGroupByDay = 1)
	set @sql = @sql + ' i.date, '
	
set @sql = @sql + ' mm.name '	

exec sp_executeSQL @sql 

drop table [#issues]
