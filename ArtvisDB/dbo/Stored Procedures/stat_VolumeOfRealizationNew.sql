CREATE Procedure [dbo].[stat_VolumeOfRealizationNew]
(
@StartDay DATETIME = default,
@FinishDay DATETIME = default,
@FirmID int = default, 
@MassmediaID int = default, 
@PaymentTypeID int = default,
@CampaignTypeID int = default,
@ManagerID int = default,
@AgencyID int = default,
@AdvertTypeID int = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByMassmediaGroupType bit = 0,
@IsGroupByAdvertType bit = 0,
@massmediaGroupID int = NULL,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON

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

If	@StartDay Is Null Or @FinishDay Is Null
	Begin
	Raiserror('FilterStartFinishDays', 16, 1)
	Return
	End

CREATE TABLE #tmp1
(
CompanyPrice MONEY,
MassmediaID SMALLINT,
PaymentTypeID SMALLINT,
ActionID INT,
campaignTypeID SMALLINT,
Manager_ID SMALLINT,
AgencyID SMALLINT,
massmediaGroupID int,
advertTypeID smallint,
issuePrice MONEY
)

Set	@StartDay = dbo.ToShortDate(@StartDay)
Set	@FinishDay = dbo.ToShortDate(@FinishDay)

-- select all companies, which has appropriated 
-- start and finish dates
Declare cur_companies Cursor Local fast_forward
For
select distinct  
	c.campaignID, c.ActionID, c.massmediaID, 
	c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
	--c.[startDate], max(coalesce(mm.roltypeID,0)),  max(coalesce(mm.massmediaGroupID,0)), 
	c.[startDate], mm.massmediaGroupID,  --max(coalesce(mm.massmediaGroupID,0)), 
	a.discount, c.finalPrice, c.finishDate, r.advertTypeID, i.ratio * i.tariffPrice
From	
	Campaign c
	INNER Join [Action] a On c.ActionID = a.actionID AND a.[isConfirmed] = 1
	INNER JOIN Issue i On i.campaignID = c.campaignID
	INNER Join Roller r On r.rollerID = i.rollerID
	INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
	left join MassMedia mm on c.massmediaID = mm.massmediaID	
	left JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
	left JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
	left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]	
	inner join @massmedias mmu on (mm.massmediaID = mmu.massmediaID 
						or m.massmediaID = mmu.massmediaID)
	inner join MassMedia mmfu on mmu.massmediaID = mmfu.massmediaID 	
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
Where	
	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
	c.StartDate <= @FinishDay and
	c.FinishDate >= @StartDay and
	c.AgencyID = IsNull(@AgencyID, c.AgencyID) and
	a.firmID = IsNull(@FirmID, a.firmID) and
	a.userID = IsNull(@ManagerID, a.userID) and
	c.PaymentTypeID = IsNull(@PaymentTypeID, c.PaymentTypeID) and
	c.campaignTypeID = IsNull(@CampaignTypeID, c.campaignTypeID) and
	(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
	(@ShowBlack <> 0 or PaymentType.isHidden = 0)  
	and (@MassmediaID is null or mmfu.massmediaID = @MassmediaID)
	and (@massmediaGroupID is null or mmfu.massmediaGroupId = @massmediaGroupID)
	and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
	and r.advertTypeID = IsNull(@AdvertTypeID, r.advertTypeID)
/*
group by 
	c.campaignID, c.ActionID, c.massmediaID, 
	c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
	c.[startDate], a.discount, c.finalPrice, c.finishDate,
	mm.roltypeID
*/

-- select all companies, which have Issues inside interval
Declare	
	@campaignID int, 
	@ActionID int, 
	@campaignPrice MONEY,
	@SummaVar money,
	@CompStartDate datetime,
	@actionDiscount float,
	@sumPrice money,
	@finalPrice money,
	@cfinishDate datetime,
	@campMassmediaGroupID int,
	@mmID smallint,
	@advTypeID smallint,
	@issuePrice money
	
declare @tmp table (massmediaID smallint, price money)

Open	cur_companies
Fetch	next from cur_companies into 
	@campaignID, @ActionID, @mmID, @PaymenttypeID, 
	@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, 
	@finalPrice, @cfinishDate, @advTypeID, @issuePrice

--Set	@FinishDay = Convert(datetime, Convert(varchar, @FinishDay, 112), 112) - 1
While	@@fetch_status = 0
begin
	if @FinishDay < @cfinishDate or @StartDay > @CompStartDate
		exec GetPriceByPeriod @campaignId, @CampaignTypeID, @StartDay, @FinishDay, @campaignPrice out
	else 
		set @campaignPrice = case when @CampaignTypeID = 4 then @finalPrice else @actionDiscount * @finalPrice end

	IF @CampaignTypeID = 4
	begin
		delete from @tmp
				
		insert into @tmp(massmediaID, price)
		select
			m.[massmediaID], sum(mpl.[price])
		from [PackModuleIssue] i 
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		where 
			i.campaignID = @campaignID	and
			i.issueDate between @StartDay and @FinishDay 
		group by m.massmediaID
			
		select @sumPrice = sum(t1.price) FROM @tmp AS t1
		
		insert into #tmp1 ([CompanyPrice],	[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],	[AgencyID], massmediaGroupID) 
		select @campaignPrice * sum(t1.price)/ @sumPrice,  t1.massmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID,@ManagerID, @AgencyID, mm.massmediaGroupID
		from @tmp as t1
			inner join MassMedia mm on t1.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
		where t1.price > 0  
			and (@MassmediaID is null or mm.massmediaID = @MassmediaID)
			and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID)
		group by t1.massmediaID, mm.massmediaGroupID
	END
	ELSE
	begin
		if	@campaignPrice > 0 
			Insert	Into #tmp1 ([CompanyPrice],[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],[AgencyID], massmediaGroupID, advertTypeID) 
			Values(@campaignPrice,  @mmID, @PaymenttypeID, @ActionID, @CampaignTypeID, @ManagerID, @AgencyID, @campMassmediaGroupID, @advTypeID)
	end
		
	fetch next from cur_companies into 
			@campaignID, @ActionID, @mmID, @PaymenttypeID,
			@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, 
			@finalPrice, @cfinishDate, @advTypeID, @issuePrice
End	

close cur_companies
deallocate cur_companies

Select	@SummaVar = IsNull(sum(CompanyPrice), 0) From	#tmp1

-- output ---------------------------------------------------------
Declare	@SQLString NVARCHAR(2500),
				@IsStarted int

/* Build the SQL string once.*/
Set	@SQLString = N'Select	row_number() over(order by IsNull(Sum(CompanyPrice), 0)) as RowNum,'
Set	@SQLString = @SQLString + N' IsNull(Sum(CompanyPrice), 0) as  sum1'

Set @SQLString = @SQLString + N',  '

If	@IsGroupByAdvertType <> 0
	Set 	@SQLString = @SQLString + N'AdvertType.Name as "advert_type",'
If	@IsGroupByPaymentType <> 0
	Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
If	@IsGroupByCampaignType <> 0
	Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
If	@IsGroupByMassmedia <> 0
	Set 	@SQLString = @SQLString + N'vMassMedia.Name as "massmedia", vMassMedia.groupName as "massmedia_group",'
If	@IsGroupByMassmediaGroupType <> 0
	Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
If	@IsGroupByFirm <> 0
	Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
If	@IsGroupByManager <> 0
	Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as "manager",'
If	@IsGroupByAgency <> 0
	Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByAdvertType +
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType /*+ @IsGroupByCommissionaire*/ = 0
	set		@SQLString = @SQLString + N'max(''Все'') as "all",'

Set 	@SQLString = @SQLString + 
		N'case @Summa
			when	0 then 0
			else	Cast((IsNull(Sum(CompanyPrice), 0) * 100.0 / @Summa) as decimal(12,2))
		End as "percent"	
From	#tmp1'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on #tmp1.massmediaGroupID = MassmediaGroup.massmediaGroupID '
If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on #tmp1.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on #tmp1.campaignTypeID = iCampaignType.CampaignTypeID'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on #tmp1.massmediaID = vMassMedia.massmediaID'
If	@IsGroupByAdvertType <> 0 Set @SQLString = @SQLString + N' inner join AdvertType on #tmp1.advertTypeID = AdvertType.advertTypeID'
If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Action on #tmp1.ActionID = Action.ActionID inner join Firm on Action.firmID = Firm.FirmID '
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on #tmp1.Manager_ID = [User].UserID'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on #tmp1.AgencyID = Agency.AgencyID'


Set 	@SQLString = @SQLString + N' Where CompanyPrice <> 0 '

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByAdvertType +
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType /*+ @IsGroupByCommissionaire*/ <> 0
	begin

	-- Group By part
	set	@IsStarted = 0
	Set 	@SQLString = @SQLString + N' Group by '

/*
	if	@IsGroupByCommissionaire <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Dic_Commissionaire.Description'
		set	@IsStarted = 1
	end
*/
	if	@IsGroupByPaymentType <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Paymenttype.Name'
		set	@IsStarted = 1
	end

	If	@IsGroupByCampaignType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'iCampaignType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByAdvertType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'AdvertType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmedia <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'vMassMedia.Name, vMassMedia.groupName'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin

		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''')'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end
	
	If	@IsGroupByMassmediaGroupType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set @SQLString = @SQLString + N'MassmediaGroup.Name'
		set	@IsStarted = 1
		end

	end

EXECUTE sp_executesql @SQLString,
	N'@Summa money',
	@Summa = @SummaVar		

Drop		table #tmp1
