-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 24.11.2008
-- Description:	Объем реализации в секундах
-- =============================================
CREATE procedure [dbo].[Stat_VolumeOfRealizationSec] 
(
	@StartDay DATETIME = default,
	@FinishDay DATETIME = default,
	@FirmID int = default, 
	@MassmediaID int = default, 
	@PaymentTypeID int = default,
	@CampaignTypeID int = default,
	@ManagerID int = default,
	@AgencyID int = default,
	@IsGroupByPaymentType bit = 0,
	@IsGroupByCampaignType bit = 0,
	@IsGroupByMassmedia bit = 0,
	@IsGroupByFirm bit = 0,
	@IsGroupByManager bit = 0,
	@IsGroupByAgency bit = 0,
	@IsGroupByMassmediaGroupType bit = 0,
	@massmediaGroupID int = NULL,
	@ShowWhite bit = 1,
	@ShowBlack bit = 1,
	@loggedUserID smallint,
	@isShowNotActivated bit = 0
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;
	
	create table #massmedias (massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into #massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,
			@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	create table #ugroups (id int)
	insert into #ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	If	@StartDay Is Null Or @FinishDay Is Null
	Begin
		Raiserror('FilterStartFinishDays', 16, 1)
		Return
	end
	
	declare @sql nvarchar(max),	@IsStarted int
	
	Set	@sql = N'select row_number() over(order by coalesce(sum(r.duration), 0)) as RowNum,'
	
	If	@IsGroupByPaymentType <> 0
		Set 	@sql = @sql + N'pt.Name as "payment_type",'
	If	@IsGroupByCampaignType <> 0
		Set 	@sql = @sql + N'ct.Name as "campaign_type",'
	If	@IsGroupByMassmedia <> 0
		Set 	@sql = @sql + N'mm.Name as "massmedia", mm.groupName as "massmedia_group",'
	If	@IsGroupByMassmediaGroupType <> 0
		Set 	@sql = @sql + N'mmg.Name as "massmedia_group",'
	If	@IsGroupByFirm <> 0
		Set 	@sql = @sql + N'f.Name as "firm",'
	If	@IsGroupByManager <> 0
		Set 	@sql = @sql + N'coalesce(u.LastName, '''') + coalesce(space(1) + u.FirstName, '''') as "manager",'
	If	@IsGroupByAgency <> 0
		Set 	@sql = @sql + N'ag.Name as "agency",'


	Set 	@sql = @sql + 
			N'dbo.fn_Int2Time(sum(r.duration)) as duration 	
	 from Issue i 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
		inner join Roller r on i.rollerID = r.rollerID
		inner join Campaign c on i.campaignID = c.campaignID
		inner join [Action] a on a.actionID = c.actionID
		inner join PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		inner join #massmedias mmu on tw.massmediaID = mmu.massmediaID
		inner join vMassMedia mm on mmu.massmediaID = mm.massmediaID
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join #ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID '

	If	@IsGroupByMassmediaGroupType <> 0 Set @sql = @sql + N' inner join MassmediaGroup as mmg on mm.massmediaGroupID = mmg.massmediaGroupID '
	If	@IsGroupByCampaignType <> 0 Set @sql = @sql + N' inner join iCampaignType as ct on c.campaignTypeID = ct.CampaignTypeID'
	If	@IsGroupByFirm <> 0 Set @sql = @sql + N' inner join Firm as f on a.firmID = f.FirmID '
	If	@IsGroupByManager <> 0 Set @sql = @sql + N' inner join [User] as u on a.userID = u.UserID'
	If	@IsGroupByAgency <> 0 Set @sql = @sql + N' inner join Agency as ag on c.AgencyID = ag.AgencyID'

	set @sql = @sql + N' where'

	if @isShowNotActivated = 0
	begin 
		set @sql = @sql + N' i.isConfirmed = 1 and'
	end

	set @sql = @sql + N' tw.dayOriginal between @StartDay and @FinishDay and
		(@FirmID is null or a.firmID = @FirmID) and
		(@ManagerID is null or a.userID = @ManagerID) and
		(@CampaignTypeID is null or c.campaignTypeID = @CampaignTypeID) and
		(@AgencyID is null or c.AgencyID = @AgencyID) and
		(@PaymentTypeID is null or c.PaymentTypeID = @PaymentTypeID) and
		(@ShowWhite <> 0 or pt.isHidden <> 0) and  
		(@ShowBlack <> 0 or pt.isHidden = 0)  
		and (@MassmediaID is null or mmu.massmediaID = @MassmediaID)
		and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID) '

	If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByManager 
		+ @IsGroupByAgency + @IsGroupByMassmediaGroupType <> 0
	begin
		set	@IsStarted = 0
		Set 	@sql = @sql + N' Group by '
	
		if	@IsGroupByPaymentType <> 0 begin
			if	@IsStarted = 1 set @sql = @sql + N','	
			Set 	@sql = @sql + N'pt.Name'
			set	@IsStarted = 1
		end

		If	@IsGroupByCampaignType <> 0
			begin
			if	@IsStarted = 1 set @sql = @sql + N','	
			Set 	@sql = @sql + N'ct.Name'
			set	@IsStarted = 1
			end

		If	@IsGroupByMassmedia <> 0
			begin
			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set 	@sql = @sql + N'mm.Name, mm.groupName'
			set	@IsStarted = 1
			end

		If	@IsGroupByFirm <> 0
			begin
			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set 	@sql = @sql + N'f.Name'
			set	@IsStarted = 1
			end

		If	@IsGroupByManager <> 0
			begin

			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set 	@sql = @sql + N'coalesce(u.LastName, '''') + coalesce(space(1) + u.FirstName, '''')'
			set	@IsStarted = 1
			end

		If	@IsGroupByAgency <> 0
			begin
			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set 	@sql = @sql + N'ag.Name'
			set	@IsStarted = 1
			end
		
		If	@IsGroupByMassmediaGroupType <> 0
			begin
			if	@IsStarted = 1 set  @sql = @sql + N','	
			Set @sql = @sql + N'mmg.Name'
			set	@IsStarted = 1
			end

	end
	
	EXECUTE sp_executesql @sql,
		N'@StartDay DATETIME, @FinishDay DATETIME,@FirmID int, @MassmediaID int, @PaymentTypeID int,@CampaignTypeID int,@ManagerID int,@AgencyID int, @massmediaGroupID int,@ShowWhite bit,@ShowBlack bit,@loggedUserID smallint,@isRightToViewForeignActions bit,@isRightToViewGroupActions bit',
			@StartDay = @StartDay,
			@FinishDay = @FinishDay,
			@FirmID = @FirmID, 
			@MassmediaID = @MassmediaID, 
			@PaymentTypeID = @PaymentTypeID,
			@CampaignTypeID = @CampaignTypeID,
			@ManagerID = @ManagerID,
			@AgencyID = @AgencyID,
			@massmediaGroupID = @massmediaGroupID,
			@ShowWhite = @ShowWhite,
			@ShowBlack = @ShowBlack,
			@loggedUserID = @loggedUserID,
			@isRightToViewForeignActions = @isRightToViewForeignActions,
			@isRightToViewGroupActions = @isRightToViewGroupActions

	drop table [#massmedias]
end
