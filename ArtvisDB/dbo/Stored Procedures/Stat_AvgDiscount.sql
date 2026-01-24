-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 21.11.2008
-- Description:	Информация о скидках
-- =============================================
CREATE procedure [dbo].[Stat_AvgDiscount] 
(
@StartDay DATETIME = default,
@FinishDay DATETIME = default,
@FirmID int = default, 
@HeadCompanyID int = default, 
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
@IsGroupByActionID bit = 0,
@massmediaGroupID int = NULL,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@Currency int = 1,
@loggedUserID smallint,
@actionID int = NULL
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

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

	CREATE TABLE #res
	(
		campaignTariffPrice money,
		campaignPrice money,
		campaignVolumeDiscount float,
		campaignManagerDiscount float,
		campaignPackDiscount float,
		MassmediaID SMALLINT,
		PaymentTypeID SMALLINT,
		ActionID INT,
		campaignTypeID SMALLINT,
		Manager_ID SMALLINT,
		AgencyID SMALLINT,
		AdvertType_ID smallint,
		massmediaGroupID int
	)

	Set	@StartDay = dbo.ToShortDate(@StartDay)
	Set	@FinishDay = dbo.ToShortDate(@FinishDay)

	Declare cur_companies Cursor Local fast_forward
	For
	select  distinct  c.campaignID, c.ActionID, c.massmediaID, 
			c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
			c.[startDate], mm.massmediaGroupID, a.discount, c.finalPrice, c.finishDate, c.managerDiscount, c.discount, c.tariffPrice
	From	
		Campaign c
		INNER Join [Action] a On c.ActionID = a.actionID AND a.[isConfirmed] = 1
		inner join Firm f on f.firmID = a.firmID
		inner join 
		(
			select distinct am.agencyID, max(cast(mm.foreignMassmedia as tinyint)) as foreignMassmedia from AgencyMassmedia am 
				inner join @massmedias mm on am.massmediaID = mm.massmediaID
			group by am.agencyID
		) xx on c.agencyID = xx.agencyID and (a.isSpecial = 0 or xx.foreignMassmedia = 1) 
		INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
		left join MassMedia mm on c.massmediaID = mm.massmediaID
		
		left JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
		left JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
		left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
		
		inner join @massmedias mmu on (mm.massmediaID = mmu.massmediaID 
							or m.massmediaID = mmu.massmediaID)
		inner join MassMedia mmfu on mmu.massmediaID = mmfu.massmediaID 
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
	Where	c.StartDate <= @FinishDay and
			c.FinishDate >= @StartDay and
			c.AgencyID = IsNull(@AgencyID, c.AgencyID) and
			a.firmID = IsNull(@FirmID, a.firmID) and
			f.headCompanyID = IsNull(@headCompanyID, f.headCompanyID) and
			a.userID = IsNull(@ManagerID, a.userID) and
			c.PaymentTypeID = IsNull(@PaymentTypeID, c.PaymentTypeID) and
			c.campaignTypeID = IsNull(@CampaignTypeID, c.campaignTypeID) and
			(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
			(@ShowBlack <> 0 or PaymentType.isHidden = 0)  
			and (@MassmediaID is null or mmfu.massmediaID = @MassmediaID)
			and (@massmediaGroupID is null or mmfu.massmediaGroupId = @massmediaGroupID)
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
			and (@actionID is null or a.actionID = @actionID)

	Declare	@campaignID int, 
		@campaignPrice MONEY,
		@SummaVar money,
		@CompStartDate datetime,
		@actionDiscount float,
		@sumPrice money,
		@finalPrice money,
		@cfinishDate datetime,
		@managerDiscount float,
		@volumeDiscount float,
		@campaignTariffPrice money

	Open	cur_companies
	Fetch	next from cur_companies into 
		@campaignID, @ActionID, @MassmediaID, @PaymenttypeID, 
		@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @massmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate,@managerDiscount,@volumeDiscount,@campaignTariffPrice

	While	@@fetch_status = 0
	begin
		if @FinishDay < @cfinishDate or @StartDay > @CompStartDate
			exec GetPriceByPeriod @campaignId, @CampaignTypeID, @StartDay, @FinishDay, @campaignPrice out, null,@campaignTariffPrice out
		else 
			set @campaignPrice = case when @CampaignTypeID = 4 then @finalPrice else @actionDiscount * @finalPrice end

		IF @CampaignTypeID = 4
		begin
			declare @tmp table (massmediaID smallint, price money, tariffPrice money)
					
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
			
			insert into #res (campaignVolumeDiscount,campaignManagerDiscount,campaignPackDiscount,[campaignPrice],campaignTariffPrice, [MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],	[AgencyID],[AdvertType_ID],	massmediaGroupID) 
			select 1,@managerDiscount, 1,@campaignPrice * sum(t1.price)/ @sumPrice, @campaignTariffPrice * sum(t1.price)/ @sumPrice, t1.massmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID,@ManagerID, @AgencyID, 0, mm.massmediaGroupID
			from @tmp as t1
				inner join MassMedia mm on t1.massmediaID = mm.massmediaID
				inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
			where t1.price > 0  
				and (@MassmediaID is null or mm.massmediaID = @MassmediaID)
				and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID)
				and ((@ManagerID = @loggedUserID and mmu.myMassmedia = 1) or (@ManagerID <> @loggedUserID and mmu.foreignMassmedia = 1))
			group by t1.massmediaID, mm.massmediaGroupID
		END
		ELSE
		begin
			if	@campaignPrice > 0 
				Insert	Into #res (campaignVolumeDiscount,campaignManagerDiscount,campaignPackDiscount,[campaignPrice],campaignTariffPrice, [MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],[AgencyID],[AdvertType_ID], massmediaGroupID) 
				Values( @volumeDiscount,@managerDiscount, @actionDiscount,@campaignPrice, @campaignTariffPrice, @MassmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID, @ManagerID, @AgencyID, 0, @massmediaGroupID)
		end
			
		fetch next from cur_companies into 
				@campaignID, @ActionID, @MassmediaID, @PaymenttypeID,
				@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @massmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate,@managerDiscount,@volumeDiscount,@campaignTariffPrice
	End	

	close cur_companies
	deallocate cur_companies

	-- output ---------------------------------------------------------
	Declare	@SQLString NVARCHAR(2500),
					@IsStarted int

	/* Build the SQL string once.*/
	Set	@SQLString = N'Select	row_number() over(order by coalesce(sum(r.campaignTariffPrice), 0)) as RowNum,'

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
	If	@IsGroupByActionID <> 0
		Set 	@SQLString = @SQLString + N'''Акция №'' + cast(r.actionID as varchar) as "actionID",'

	Set	@SQLString = @SQLString + N' coalesce(sum(r.campaignTariffPrice), 0) as tariffPrice
		, coalesce(avg(r.campaignVolumeDiscount), 0) as volumeDiscount
		, cast(coalesce(sum(r.campaignTariffPrice - r.campaignTariffPrice * r.campaignVolumeDiscount), 0) as money) as volumeDicountPrice
		, coalesce(avg(r.campaignPackDiscount),0) as packDiscount
		, cast(coalesce(sum(r.campaignTariffPrice * r.campaignVolumeDiscount * (1 - r.campaignPackDiscount)) , 0) as money) as discountPrice
		, coalesce(avg(r.campaignManagerDiscount), 0) as managerDiscount
		, cast(coalesce(sum(r.campaignTariffPrice * r.campaignVolumeDiscount * r.campaignPackDiscount * (1 - r.campaignManagerDiscount)), 0) as money) as managerDicountPrice
		, coalesce(sum(r.campaignPrice), 0) as price	
	from #res as r '

	If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on r.massmediaGroupID = MassmediaGroup.massmediaGroupID '
	If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on r.PaymentTypeID = Paymenttype.PaymenttypeID'
	If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on r.campaignTypeID = iCampaignType.CampaignTypeID'
	If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on r.massmediaID = vMassMedia.massmediaID'
	If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Action on r.ActionID = Action.ActionID inner join Firm on Action.firmID = Firm.FirmID '
	If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on r.Manager_ID = [User].UserID'
	If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on r.AgencyID = Agency.AgencyID'

	If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
		@IsGroupByMassmedia + @IsGroupByFirm + 
		@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType + @IsGroupByActionID <> 0
		begin

		-- Group By part
		set	@IsStarted = 0
		Set 	@SQLString = @SQLString + N' Group by '

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
		
		If	@IsGroupByActionID <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set @SQLString = @SQLString + N'r.actionID'
			set	@IsStarted = 1
			end

		end

	EXECUTE sp_executesql @SQLString

	Drop table #res
end
