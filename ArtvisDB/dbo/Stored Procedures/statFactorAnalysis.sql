-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.05.2009
-- Description:	Modified to include headCompanyID filtering and grouping
-- =============================================
CREATE PROCEDURE [dbo].[statFactorAnalysis] 
(
	@StartDay DATETIME = default,
	@FinishDay DATETIME = default,
	@ComparedStartDay datetime = default,
	@FirmID int = default, 
	@headCompanyID int = default,
	@MassmediaID int = default, 
	@PaymentTypeID int = default,
	@CampaignTypeID int = default,
	@ManagerID int = default,
	@AgencyID int = default,
	@IsGroupByPaymentType bit = 0,
	@IsGroupByCampaignType bit = 0,
	@IsGroupByMassmedia bit = 0,
	@IsGroupByFirm bit = 0,
	@IsGroupByHeadCompany bit = 0,
	@IsGroupByManager bit = 0,
	@IsGroupByAgency bit = 0,
	@IsGroupByMassmediaGroupType bit = 0,
	@massmediaGroupID int = null,
	@ShowWhite bit = 1,
	@ShowBlack bit = 1,
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
BEGIN
	-- SET NOCOUNT ON added to prevent extra result sets from
	-- interfering with SELECT statements.
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

	If	@StartDay is null or @FinishDay is null or @ComparedStartDay is null
	Begin
		Raiserror('StatFactorAnalysisStartFinishDays', 16, 1)
		return
	end
	

	Set	@StartDay = dbo.ToShortDate(@StartDay)
	Set	@FinishDay = dbo.ToShortDate(@FinishDay)

	declare @ComparedFinishDay datetime
	
	set @ComparedFinishDay = dateadd(day, datediff(day, @StartDay, @FinishDay) , @ComparedStartDay)
			
	create table #res 
	(
		cPrice money,
		price money,
		agencyID smallint,
		firmID int,
		massmediaID smallint,
		paymentTypeID smallint,
		userID smallint,
		massmediaGroupID smallint,
		campaignTypeID tinyint,
		duration money,
		cDuration money,
		headCompanyID int
	)
	
	insert into #res 
	select 0 as comparedPrice,
		x.price as price,
		x.agencyID,
		x.firmID,
		x.massmediaID,
		x.paymentTypeID,
		x.userID,
		x.massmediaGroupID,
		x.campaignTypeID,
		x.duration as duration,
		0 as cDuration,
		f.headCompanyID
	from 
	(
		select c.campaignID, 
			case 
				when 
					c.startDate between @startDay and @finishDay and 
					c.finishDate between @startDay and @finishDay and 
					i.cWithCapacity = 0
				then c.finalPrice * a.discount
				else i.price
			end as price,
			c.agencyID,
			a.firmID,
			c.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select	c.campaignID, 
					sum(case when tw.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when tw.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when tw.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when tw.maxCapacity > 0 then 0 else r.duration end) as duration
				from	
					Issue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join TariffWindow tw on i.originalWindowID = tw.windowId
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
				where c.campaignTypeID = 1 and
					tw.dayOriginal between @StartDay and @FinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		where c.campaignTypeID = 1 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @FinishDay and c.FinishDate >= @StartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
		
		union all
		
		select c.campaignID, 
			case 
				when 
					c.startDate between @startDay and @finishDay and 
					c.finishDate between @startDay and @finishDay and 
					i.cWithCapacity = 0
				then c.finalPrice * a.discount
				else i.price
			end as price,
			c.agencyID,
			a.firmID,
			c.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select	c.campaignID, 
					sum(case when mpl.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when mpl.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when mpl.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when mpl.maxCapacity > 0 then 0 else r.duration end) as duration
				from	
					dbo.ModuleIssue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join 
					(
						select mt.modulePriceListID, max(t.maxCapacity) as maxCapacity
						from dbo.ModuleTariff mt 
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by mt.modulePriceListID 
					) mpl on i.modulePricelistID = mpl.modulePriceListID
				where c.campaignTypeID = 3 and
					i.issueDate between @StartDay and @FinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		where c.campaignTypeID = 3 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @FinishDay and c.FinishDate >= @StartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))

		union all			

		select c.campaignID, 
			(case 
				when 
					c.startDate between @startDay and @finishDay and 
					c.finishDate between @startDay and @finishDay and 
					i.cWithCapacity = 0
				then c.finalPrice 
				else i.price
			end) * (ii.price / iiSum.price) as price,
			c.agencyID,
			a.firmID,
			mm.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select c.campaignID, 
					sum(case when pmpl.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when pmpl.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when pmpl.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when pmpl.maxCapacity > 0 then 0 else r.duration end) as duration
				from dbo.PackModuleIssue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where c.campaignTypeID = 4 and
					i.issueDate between @startDay and @finishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
			inner join 
			(
				select c.campaignID, m.massmediaID, sum(mpl.price) as price
				from dbo.PackModuleIssue i 
					inner JOIN dbo.PackModuleContent pmc ON i.priceListID = pmc.pricelistID
					inner JOIN dbo.ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					inner JOIN dbo.Module m ON mpl.moduleID = m.moduleID
					inner JOIN dbo.Campaign c ON i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where i.issueDate between @startDay and @finishDay
				group by c.campaignID, m.massmediaID
			) ii on ii.campaignID = c.campaignID and mm.massmediaID = ii.massmediaID
			inner join 
			(
				select c.campaignID, sum(mpl.price) as price
				from dbo.PackModuleIssue i 
					inner JOIN dbo.PackModuleContent pmc ON i.priceListID = pmc.pricelistID
					inner JOIN dbo.ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					inner JOIN dbo.Module m ON mpl.moduleID = m.moduleID
					inner JOIN dbo.Campaign c ON i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where i.issueDate between @startDay and @finishDay
				group by c.campaignID
			) iiSum on iiSum.campaignID = c.campaignID
		where c.campaignTypeID = 4 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @FinishDay and c.FinishDate >= @StartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
	) x
	inner join (
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) xu on x.userID = xu.userID
	inner join dbo.Firm f on x.firmID = f.firmID
	where 
		x.AgencyID = IsNull(@AgencyID, x.AgencyID) and
		x.firmID = IsNull(@FirmID, x.firmID) and
		f.headCompanyID = IsNull(@headCompanyID, f.headCompanyID) and
		x.userID = IsNull(@ManagerID, x.userID) and
		x.PaymentTypeID = IsNull(@PaymentTypeID, x.PaymentTypeID) and
		x.campaignTypeID = IsNull(@CampaignTypeID, x.campaignTypeID) and
		(@ShowWhite <> 0 or x.isHidden <> 0) and  
		(@ShowBlack <> 0 or x.isHidden = 0) and
		(@MassmediaID is null or x.massmediaID = @MassmediaID) and
		(@massmediaGroupID is null or x.massmediaGroupId = @massmediaGroupID)
	
	union all
	
	select x.price as comparedPrice,
		0 as price,
		x.agencyID,
		x.firmID,
		x.massmediaID,
		x.paymentTypeID,
		x.userID,
		x.massmediaGroupID,
		x.campaignTypeID,
		0 as duration,
		x.duration as cDuration,
		f.headCompanyID
	from 
	(
		select c.campaignID, 
			case 
				when 
					c.startDate between @ComparedStartDay and @ComparedFinishDay and 
					c.finishDate between @ComparedStartDay and @ComparedFinishDay and 
					i.cWithCapacity = 0
				then c.finalPrice * a.discount
				else i.price
			end as price,
			c.agencyID,
			a.firmID,
			c.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select c.campaignID, 
					sum(case when tw.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when tw.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when tw.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when tw.maxCapacity > 0 then 0 else r.duration end) as duration
				from Issue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join TariffWindow tw on i.originalWindowID = tw.windowId
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
				where c.campaignTypeID = 1 and
					tw.dayOriginal between @ComparedStartDay and @ComparedFinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		where c.campaignTypeID = 1 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @ComparedFinishDay and c.FinishDate >= @ComparedStartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
		
		union all
		
		select c.campaignID, 
			case 
				when 
					c.startDate between @ComparedStartDay and @ComparedFinishDay and 
					c.finishDate between @ComparedStartDay and @ComparedFinishDay and 
					i.cWithCapacity = 0
				then c.finalPrice * a.discount
				else i.price
			end as price,
			c.agencyID,
			a.firmID,
			c.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select c.campaignID, 
					sum(case when mpl.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when mpl.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when mpl.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when mpl.maxCapacity > 0 then 0 else r.duration end) as duration
				from dbo.ModuleIssue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join 
					(
						select mt.modulePriceListID, max(t.maxCapacity) as maxCapacity
						from dbo.ModuleTariff mt 
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by mt.modulePriceListID 
					) mpl on i.modulePricelistID = mpl.modulePriceListID
				where c.campaignTypeID = 3 and
					i.issueDate between @ComparedStartDay and @ComparedFinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		where c.campaignTypeID = 3 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @ComparedFinishDay and c.FinishDate >= @ComparedStartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))

		union all			

		select c.campaignID, 
			(case 
				when 
					c.startDate between @ComparedStartDay and @ComparedFinishDay and 
					c.finishDate between @ComparedStartDay and @ComparedFinishDay and 
					i.cWithCapacity = 0
				then c.finalPrice 
				else i.price
			end) * (ii.price / iiSum.price) as price,
			c.agencyID,
			a.firmID,
			mm.massmediaID,
			c.paymentTypeID,
			a.userID,
			mm.massmediaGroupID,
			a.actionID,
			c.campaignTypeID,
			pt.isHidden,
			i.duration
		from dbo.Campaign c 
			inner join dbo.[Action] a on c.actionID = a.actionID
			inner join dbo.MassMedia mm on c.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID
			inner join 
			(
				select c.campaignID, 
					sum(case when pmpl.maxCapacity > 0 then 0 else i.[tariffPrice] * i.[ratio] end) as price, 
					sum(case when pmpl.maxCapacity > 0 then 1 else 0 end) as cWithCapacity, 
					sum(case when pmpl.maxCapacity > 0 then 0 else 1 end) as cWithoutCapacity, 
					sum(case when pmpl.maxCapacity > 0 then 0 else r.duration end) as duration
				from dbo.PackModuleIssue i
					inner JOIN Roller r on i.rollerID = r.rollerID
					inner join Campaign c on i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where c.campaignTypeID = 4 and
					i.issueDate between @ComparedStartDay and @ComparedFinishDay 
				group by c.campaignID 
			) i on i.campaignID = c.campaignID
			inner join dbo.PaymentType pt on c.paymentTypeID = pt.paymentTypeID
			inner join 
			(
				select c.campaignID, m.massmediaID, sum(mpl.price) as price
				from dbo.PackModuleIssue i 
					inner JOIN dbo.PackModuleContent pmc ON i.priceListID = pmc.pricelistID
					inner JOIN dbo.ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					inner JOIN dbo.Module m ON mpl.moduleID = m.moduleID
					inner JOIN dbo.Campaign c ON i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where i.issueDate between @ComparedStartDay and @ComparedFinishDay
				group by c.campaignID, m.massmediaID
			) ii on ii.campaignID = c.campaignID and mm.massmediaID = ii.massmediaID
			inner join 
			(
				select c.campaignID, sum(mpl.price) as price
				from dbo.PackModuleIssue i 
					inner JOIN dbo.PackModuleContent pmc ON i.priceListID = pmc.pricelistID
					inner JOIN dbo.ModulePriceList mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					inner JOIN dbo.Module m ON mpl.moduleID = m.moduleID
					inner JOIN dbo.Campaign c ON i.campaignID = c.campaignID
					inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
						and (@showBlack = 1 or pt.IsHidden = 0)
					inner join (
						select pmc.pricelistID, max(t.maxCapacity) as maxCapacity
						from dbo.PackModuleContent pmc
							inner join dbo.ModuleTariff mt on mt.modulePriceListID = pmc.modulePriceListID
							inner join dbo.Tariff t on mt.tariffID = t.tariffID 
						where t.maxCapacity = 0
						group by pmc.pricelistID
					) pmpl on i.pricelistID = pmpl.pricelistID
				where i.issueDate between @ComparedStartDay and @ComparedFinishDay
				group by c.campaignID
			) iiSum on iiSum.campaignID = c.campaignID
		where c.campaignTypeID = 4 and a.isSpecial = 0 and a.isConfirmed = 1
			and c.StartDate <= @ComparedFinishDay and c.FinishDate >= @ComparedStartDay
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
	) x
	inner join (
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) xu on x.userID = xu.userID
	inner join dbo.Firm f on x.firmID = f.firmID
	where 
		x.AgencyID = IsNull(@AgencyID, x.AgencyID) and
		x.firmID = IsNull(@FirmID, x.firmID) and
		f.headCompanyID = IsNull(@headCompanyID, f.headCompanyID) and
		x.userID = IsNull(@ManagerID, x.userID) and
		x.PaymentTypeID = IsNull(@PaymentTypeID, x.PaymentTypeID) and
		x.campaignTypeID = IsNull(@CampaignTypeID, x.campaignTypeID) and
		(@ShowWhite <> 0 or x.isHidden <> 0) and  
		(@ShowBlack <> 0 or x.isHidden = 0) and
		(@MassmediaID is null or x.massmediaID = @MassmediaID) and
		(@massmediaGroupID is null or x.massmediaGroupId = @massmediaGroupID)


	declare	@SQLString NVARCHAR(max),
				@IsStarted int
	
	set @SQLString = '
	select row_number() over(order by IsNull(Sum(price), 0)) as RowNum, '
	
	If	@IsGroupByPaymentType <> 0
		Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
	If	@IsGroupByCampaignType <> 0
		Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
	If	@IsGroupByMassmedia <> 0
		Set 	@SQLString = @SQLString + N'vMassMedia.Name as "massmedia", vMassmedia.GroupName as "massmedia_group",'
	If	@IsGroupByMassmediaGroupType <> 0
		Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
	If	@IsGroupByFirm <> 0
		Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
	If	@IsGroupByHeadCompany <> 0
		Set 	@SQLString = @SQLString + N'HeadCompany.Name as "head_company",'
	If	@IsGroupByManager <> 0
		Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as "manager",'
	If	@IsGroupByAgency <> 0
		Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
	
	set @SQLString = @SQLString + '
	
		case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end avgPrice,
		case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end avgCPrice,
		dbo.fn_Int2Time(cast(sum(r.duration) as int)) duration,
		dbo.fn_Int2Time(cast(sum(r.cduration) as int)) cduration,
		sum(r.price) as price,
		sum(r.cprice) as cprice,
		1.0/2.0 * (sum(r.duration) - sum(r.cDuration)) * (case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end + case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end) as q1,
		1.0/2.0 * (sum(r.duration) + sum(r.cDuration)) * (case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end - case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end) as q2,
		1.0/2.0 * (sum(r.duration) - sum(r.cDuration)) * (case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end + case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end)
		+ 1.0/2.0 * (sum(r.duration) + sum(r.cDuration)) * (case when sum(r.duration) = 0 then 0 else sum(r.price)/sum(r.duration) end - case when sum(r.cduration) = 0 then 0 else sum(r.cprice)/sum(r.cduration) end) as q
	from #res r '
	
	If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on r.massmediaGroupID = MassmediaGroup.massmediaGroupID '
	If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on r.PaymentTypeID = Paymenttype.PaymenttypeID'
	If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on r.campaignTypeID = iCampaignType.CampaignTypeID'
	If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on r.massmediaID = vMassMedia.massmediaID'
	If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Firm on r.firmID = Firm.FirmID '
	If	@IsGroupByHeadCompany <> 0 Set @SQLString = @SQLString + N' inner join HeadCompany on r.headCompanyID = HeadCompany.headCompanyID '
	If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on r.userID = [User].UserID'
	If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on r.AgencyID = Agency.AgencyID'
	
	If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + 
	@IsGroupByHeadCompany + @IsGroupByManager + 
	@IsGroupByAgency + @IsGroupByMassmediaGroupType <> 0
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
		Set 	@SQLString = @SQLString + N'vMassMedia.Name, vMassMedia.GroupName'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByHeadCompany <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'HeadCompany.Name'
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
	
	EXECUTE sp_executesql @SQLString
END
