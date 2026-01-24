CREATE PROC [dbo].[FirmWithActions1]
(
@firmId smallint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@paymentTypeId smallint = null,
@campaignTypeId tinyint = null,
@campaignFinishDate datetime = null,
@firmId2 smallint = null,
@userID smallint = null,
@changeStartOfInterval datetime = null,
@changeEndOfInterval datetime = null,
@massmediaId smallint = null,
@agencyID smallint = null,
@actionID int = null,
@issueDay datetime = null,
@issueDate datetime = null,
@rollerId int = null,
@isHideBlack bit = 0,
@isHideWhite bit = 0,
@isShowActivate BIT = 0,
@isShowNotActivate BIT = 0,
@withoutActionsSince datetime = null,
@showBlack bit = 1,
@showWhite bit = 1,
@moduleID smallint = null,
@packModuleID smallint = null,
@loggedUserID smallint = null,
@managerDiscount float = null,
@massmediaGroupID int = null,
@showDeleted bit = 0,
@headCompanyId smallint = null  -- Добавлен новый параметр
)
AS
SET NOCOUNT on

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	if @issueDay is not null or @rollerId is not null or @issueDate is not null or @moduleID is not null or @packModuleID is not null
	begin 
		declare @issues table (actionID int primary key)
		insert into @issues
		select distinct c.actionID 
		from 
			Issue i 
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
			left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
			left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
			left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
		where i.rollerID = coalesce(@rollerId, i.rollerID)
			and (@issueDate is null or ((datepart(hh, tw.windowDateOriginal) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateOriginal) = datepart(minute, @issueDate))) )
			and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
			and (@moduleID is null or mi.moduleID = @moduleID)
			and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
			and mm.massmediaGroupID = Coalesce(@massmediaGroupID, mm.massmediaGroupID)
			and tw.massmediaID = Coalesce(@massmediaId, tw.massmediaId)

		SELECT DISTINCT
			f.*, @userID  AS userID, @startOfInterval as startOfInterval, @endOfInterval as endOfInterval, 
			@actionID as actionID /*To filtered*/, @massmediaGroupID as massmediaGroupID, @showDeleted as showDeleted, @isShowActivate as isShowActivate, @isShowNotActivate as isShowNotActivate
		FROM 
			[Action] a
			inner join @issues i on i.actionID = a.actionID
			Inner Join Campaign c ON c.actionId = a.actionId
			Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
			INNER JOIN [Agency] ag ON c.[agencyID] = ag.[agencyID]
			INNER JOIN [vUser] us ON us.userID = a.userID
			INNER JOIN [Firm] f ON f.firmID = a.firmID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			left join GroupMember gm on us.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		WHERE	
			(us.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			((c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and
															 
			a.isSpecial = 0 and	
			a.finishDate >= Coalesce(@startOfInterval, a.finishDate) And
			a.startDate <= Coalesce(@endOfInterval, a.startDate) And
			c.paymentTypeId = Coalesce(@paymentTypeId, c.paymentTypeId) And
			c.campaignTypeId = Coalesce(@campaignTypeId, c.campaignTypeId) And
			c.finishDate = Coalesce(@campaignFinishDate, c.finishDate) And
			a.firmId = Coalesce(@firmId2, a.firmId) And
			a.userId = Coalesce(@userId, a.userId) And
			a.modDate >= Coalesce(@changeStartOfInterval, a.modDate) And
			a.modDate <= Coalesce(@changeEndOfInterval, a.modDate) And
			((c.[agencyID] IS NULL AND @agencyID IS NULL) OR c.agencyId = Coalesce(@agencyID, c.agencyId)) And
			a.actionId = Coalesce(@actionId, a.actionId) And
			(pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) and
			a.[actionID] = COALESCE(@actionID, a.[actionID]) AND
			a.[firmID] = COALESCE(@firmID, a.[firmID]) 
			AND ((a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) OR (a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) or (a.deleteDate is not null and @showDeleted = 1))
			and (@withoutActionsSince is null or not exists(select top 1 a1.actionID 
															from [Action] a1 
															where a.firmID = a1.firmID  
																and a1.finishDate >= @withoutActionsSince 
																and (@startOfInterval is null or a1.startDate < @startOfInterval)))
			and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			and f.headCompanyId = COALESCE(@headCompanyId, f.headCompanyId)  -- Добавлен фильтр по headCompanyId
		order by f.[name]
	end 
	else 
		SELECT
			f.*, 
			@userID  AS userID, @startOfInterval as startOfInterval, @endOfInterval as endOfInterval, 
			@actionID as actionID /*To filtered*/, @massmediaGroupID as massmediaGroupID, @showDeleted as showDeleted, 
			@isShowActivate as isShowActivate, @isShowNotActivate as isShowNotActivate
		FROM 
			[Firm] f
		WHERE EXISTS (
			SELECT 1 
			FROM [Action] a
				Inner Join Campaign c ON c.actionId = a.actionId
				Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
				LEFT JOIN (
					PackModuleIssue i 
					JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
					JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
					) ON c.campaignTypeID=4 AND i.campaignID = c.campaignID
			WHERE		
				a.firmID = f.firmID
				and a.userId = IsNull(@userId, a.userId) 
				and a.isSpecial = 0			
				and a.finishDate >= IsNull(@startOfInterval, a.finishDate)
				and a.startDate <= Coalesce(@endOfInterval, a.startDate) 
				AND (a.userID = @loggedUserID 
						or @isRightToViewForeignActions = 1 
						or (
							@isRightToViewGroupActions = 1 
							AND EXISTS (
								SELECT 1 
								FROM GroupMember gm 
									JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
								WHERE a.userID = gm.userID
								)
							)
						)
				and EXISTS (
						SELECT 1 
						FROM @massmedias umm 
						WHERE umm.massmediaID = CASE WHEN c.campaignTypeID=4 THEN m.massmediaID ELSE c.massmediaID END
								and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
						) 
				AND (@massmediaGroupID IS NULL 
						OR 
						EXISTS (
							SELECT 1
							FROM MassMedia mm
							WHERE mm.massmediaID = CASE WHEN c.campaignTypeID=4 THEN m.massmediaID ELSE c.massmediaID END
								AND mm.massmediaGroupID = @massmediaGroupID
							)
						)
				and	c.paymentTypeId = Coalesce(@paymentTypeId, c.paymentTypeId) 
				And	c.campaignTypeId = Coalesce(@campaignTypeId, c.campaignTypeId) 
				And	c.finishDate = Coalesce(@campaignFinishDate, c.finishDate) 
				And	a.firmId = Coalesce(@firmId2, a.firmId) 
				--and a.modDate BETWEEN COALESCE(@changeStartOfInterval,'1900-01-01') AND COALESCE(@changeEndOfInterval, '2200-12-31')
				and a.modDate BETWEEN COALESCE(@changeStartOfInterval,[dbo].[GetMinDate]()) AND COALESCE(@changeEndOfInterval, [dbo].[GetMaxDate]())
				and (
						@massmediaId IS NULL
						OR 
						c.campaignTypeID=4 AND m.massmediaID=@massmediaId
						OR
						c.massmediaID=@massmediaId
						)
				and ((c.[agencyID] IS NULL AND @agencyID IS NULL) OR c.agencyId = Coalesce(@agencyID, c.agencyId))
				And	a.actionId = Coalesce(@actionId, a.actionId) 
				And	(pt.isHidden = 0 or @isHideWhite = 0) 
				And	(pt.isHidden = 1 or @isHideBlack = 0) 
				and	(
						(pt.IsHidden = 1 and @showBlack = 1)  
						or
						(pt.IsHidden = 0 and @showWhite = 1)
					) 
				and	a.[actionID] = COALESCE(@actionID, a.[actionID]) 
				AND	a.[firmID] = COALESCE(@firmID, a.[firmID]) 
				AND (
						(a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) 
						OR 
						(a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) 
						or 
						(a.deleteDate is not null and @showDeleted = 1)
					)
				and (
					@withoutActionsSince is null 
					or 
					not exists(
						select 1 
						from [Action] a1 
						where a.firmID = a1.firmID  
							and a1.finishDate >= @withoutActionsSince 
							and (@startOfInterval is null or a1.startDate < @startOfInterval)
						)
					)
				and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			)
		AND f.headCompanyId = COALESCE(@headCompanyId, f.headCompanyId)  -- Добавлен фильтр по headCompanyId
		order by f.[name]
