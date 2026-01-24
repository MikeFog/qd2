CREATE    PROCEDURE [dbo].[ActionsForBalance]
(
@firmID smallint = NULL,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@userID smallint = null,
--@paymentTypesIDString varchar(1024) = null,
@agenciesIDString varchar(1024) = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;
	
	Select	@startOfInterval = Convert(datetime, Convert(varchar, @startOfInterval, 112), 112)
	Select	@endOfInterval = Convert(datetime, Convert(varchar, @endOfInterval, 112), 112)
			
	CREATE TABLE #Agency(agencyID smallint)

	-- Populate temporary tables with Agency and Payment types
	IF @agenciesIDString Is Null
		INSERT INTO #Agency 
		SELECT agencyID FROM Agency
	Else
		Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString 
		
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
		
	CREATE TABLE #tmp1(actionID int primary key,	summa MONEY NULL, tariffPrice MONEY null, [priceSumByCampaigns] money null)
	INSERT INTO #tmp1 ([actionID], [summa], [tariffPrice], [priceSumByCampaigns]) 
	SELECT distinct a.actionID, 0, 0, 0
	FROM 
		[Action] a
			Inner Join Campaign c ON c.actionId = a.actionId
			Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
			inner join [#Agency] ag on c.agencyID = ag.agencyID
			inner join 
			(
				select distinct am.agencyID, max(cast(mm.foreignMassmedia as tinyint)) as foreignMassmedia from AgencyMassmedia am 
					inner join @massmedias mm on am.massmediaID = mm.massmediaID
				group by am.agencyID
			) x on ag.agencyID = x.agencyID and (a.isSpecial = 0 or x.foreignMassmedia = 1) 
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			left join GroupMember gm on a.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		WHERE		
			(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(a.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and	
			a.finishDate >= Coalesce(@startOfInterval, a.finishDate) And
			a.startDate <= Coalesce(@endOfInterval, a.startDate) And
			a.userId = Coalesce(@userId, a.userId) And
			(pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) and
			a.[firmID] = COALESCE(@firmID, a.[firmID]) 
			AND (a.[isConfirmed] = 1)
		
	Declare cur_Companies Cursor local fast_forward
	For
	SELECT 	c.campaignID, c.campaignTypeID,
			c.startDate,
			a.[actionID],
			c.finalPrice,
			c.managerDiscount,
			c.discount,
			c.finishDate,
			ac.discount,
			c.tariffPrice
	From	campaign AS c join #tmp1 as a on c.actionID = a.actionID
			join paymenttype as pt on c.paymentTypeID = pt.paymenttypeID
			join agency as ag on ag.agencyID = c.agencyID
			inner join [Action] ac on ac.actionID = c.actionID
	Where	ag.agencyID IN (Select agencyID From #Agency)
			and (pt.isHidden = 0 or @isHideWhite = 0) And
				(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) 
				
	Declare	@campaignID int, @TypeID int, @StartDate DATETIME,
			@Price money, @Action int,
			@FinalPrice MONEY, @tariffPrice money, @managerDiscount float, @discount float, @finishDate datetime, @actiondiscount float
		
	Open	cur_Companies
	Fetch	Next from cur_Companies
	Into 	@campaignID, @TypeID, @StartDate, @Action, @FinalPrice, @managerDiscount, @discount, @finishDate, @actiondiscount, @tariffPrice

	While	@@fetch_status = 0
	Begin
		If	(@startOfInterval is null and @endOfInterval IS null) OR (@endOfInterval > @finishDate and @startOfInterval < @StartDate)
		begin 
			set  @Price = case when @TypeID <> 4 then @FinalPrice * @actiondiscount else @FinalPrice end 
		end 
		else
		begin 
			exec GetPriceByPeriod @campaignID = @campaignID, @campaignTypeID = @TypeID, @startDate = @startOfInterval, @finishDate = @endOfInterval, @price = @price OUTPUT, @tariffPrice = @tariffPrice output
		end
		
		UPDATE [#tmp1] SET summa = summa + ISNULL(@Price, 0), [tariffPrice] = [tariffPrice] + ISNULL(@tariffPrice, 0), [priceSumByCampaigns] = [priceSumByCampaigns] + ISNULL(@tariffPrice * @managerDiscount * @discount, 0)
		WHERE [actionID] = @Action

		Fetch	Next from cur_Companies
		Into 	@campaignID, @TypeID, @StartDate, @Action, @FinalPrice, @managerDiscount, @discount, @finishDate, @actiondiscount, @tariffPrice

	end

	close		cur_Companies
	Deallocate	cur_Companies
		
	SELECT 
		ac.[actionID],
		ac.[firmID],
		ac.[startDate],
		ac.[finishDate],
		ac.[discount],
		ac.[userID],
		a.tariffPrice AS [tariffPrice],
		a.[priceSumByCampaigns] AS [priceSumByCampaigns],
		ac.[createDate],
		ac.[modDate],
		ac.[isSpecial],
		ac.[isConfirmed],
		a.summa AS totalPrice,
		us.firstName + Space(1) + us.lastName as creator,
		'Акция №' + LTRIM(ac.[actionID]) as name,
		f.name as firmName
	FROM 
		#tmp1 a
		INNER JOIN [Action] ac ON a.actionID = ac.actionID
		INNER JOIN [User] us ON us.userID = ac.userID
		INNER JOIN [Firm] f ON f.firmID = ac.firmID
	ORDER BY
		ac.[actionID] DESC		
END
