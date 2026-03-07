CREATE   PROCEDURE [dbo].[stat_Balance]
(
@theDate datetime = null,
@FirmID int = default,
@HeadCompanyID int = default,
@AgencyID int = default,
@PaymentTypeID int = default,
@ShowBlack bit = 1,
@ShowWhite bit = 1,
@ManagerID int = default,
@EmptyFirmsOnly bit = 0,
@IsGroupByAgency bit = 0,
@agenciesIDString varchar(1024) = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As
set	nocount ON

create	table #tmp1
(
[summa] 	decimal(18,2),
[firmID] 	int,
[agencyID] 	int
)

-- calculate payments till defined date -----------------------
CREATE TABLE #Agency(agencyID smallint primary key)

-- Populate temporary tables with Agency and Payment types
IF @agenciesIDString Is Null
	INSERT INTO #Agency 
	SELECT agencyID FROM Agency
Else
	Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString 

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

If @EmptyFirmsOnly = 1
	Insert 	Into #tmp1
	Select	ISNULL(Sum(p.summa), 0) as summa,
			p.firmID,
			p.agencyID
	From	payment p
			inner join Firm f on p.firmID = f.firmID
			inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
			inner join [#Agency] ag on p.agencyID = ag.agencyID
			left join [Action] a on a.firmID = p.firmID and a.[isConfirmed] = 1
	Where	a.actionID is null
			and (@theDate IS NULL OR p.paymentDate <= @theDate) and
			p.agencyID = IsNull(@AgencyID, p.agencyID) and
			p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
			p.firmID = IsNull(@FirmID, p.firmID) and
			f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
			((pt.IsHidden = 1 and @ShowBlack = 1)  or
			(pt.IsHidden = 0 and @ShowWhite = 1)) and
			(pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0)
	Group by p.firmID, p.agencyID
Else
	Begin
	If	@ManagerID is null 
	begin
		if @isRightToViewForeignActions = 0 and @isRightToViewGroupActions = 1
		begin 
			Insert 	Into #tmp1
			Select	ISNULL(Sum(pa.summa), 0) as summa,
					p.firmID,
					p.agencyID
			From	payment p
					inner join Firm f on f.firmID = p.firmID
					inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
					inner join paymentAction pa on p.paymentID = pa.paymentID
					inner join [Action] a on a.actionID = pa.actionID
					inner join [#Agency] ag  on p.agencyID = ag.agencyID
					inner join (
						select distinct u.userID 
						from [User] u
							left join [GroupMember] gm on u.userID = gm.userID
							left join @ugroups ug on gm.groupID = ug.id
						where 
							u.userID = @loggedUserID 
							or @isRightToViewForeignActions = 1 
							or (@isRightToViewGroupActions = 1 and ug.id is not null)
					) as xu on a.userID = xu.userID
			Where (a.userID = @loggedUserID 
				   OR EXISTS (
					   SELECT 1 
					   FROM campaign c 
					   INNER JOIN @massmedias mm ON c.massmediaID = mm.massmediaID 
												 AND mm.foreignMassmedia = 1
					   WHERE c.actionID = a.actionID
							 AND c.campaignTypeID <> 4
				   )
				   OR EXISTS (
					   SELECT 1 
					   FROM campaign c 
					   INNER JOIN PackModuleIssue pmi ON c.campaignID = pmi.campaignID
					   INNER JOIN PackModuleContent pmc ON pmi.pricelistID = pmc.pricelistID
					   INNER JOIN Module m ON pmc.moduleID = m.moduleID
					   INNER JOIN @massmedias mm ON m.massmediaID = mm.massmediaID 
												 AND mm.foreignMassmedia = 1
					   WHERE c.actionID = a.actionID
							 AND c.campaignTypeID = 4
				   )) and
					(@theDate IS NULL OR p.paymentDate <= @theDate) and
					p.agencyID = IsNull(@AgencyID, p.agencyID) and
					p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
					p.firmID = IsNull(@FirmID, p.firmID) and
					f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
					((pt.IsHidden = 1 and @ShowBlack = 1)  or
					(pt.IsHidden = 0 and @ShowWhite = 1)) 
					AND (pt.isHidden = 0 or @isHideWhite = 0) And
					(pt.isHidden = 1 or @isHideBlack = 0) 
			Group by p.firmID, p.agencyID
		end
		else 
		begin 
			if @isRightToViewForeignActions = 1
				-- Пользователь с полными правами: берем ВСЕ платежи
				Insert 	Into #tmp1
				Select	ISNULL(Sum(p.summa), 0) as summa,
						p.firmID,
						p.agencyID
				From	payment p
						inner join Firm f on f.firmID = p.firmID
						inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
						inner join [#Agency] ag on p.agencyID = ag.agencyID
				Where	(@theDate IS NULL OR p.paymentDate <= @theDate) and
						p.agencyID = IsNull(@AgencyID, p.agencyID) and
						p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
						p.firmID = IsNull(@FirmID, p.firmID) and
						f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
						((pt.IsHidden = 1 and @ShowBlack = 1)  or
						(pt.IsHidden = 0 and @ShowWhite = 1))
						AND (pt.isHidden = 0 or @isHideWhite = 0) And
							(pt.isHidden = 1 or @isHideBlack = 0) 
				Group by p.firmID, p.agencyID
			else
				-- Пользователь БЕЗ полных прав: берем только СВОИ платежи через Action
				Insert 	Into #tmp1
				Select	ISNULL(Sum(pa.summa), 0) as summa,
						p.firmID,
						p.agencyID
				From	payment p
						inner join Firm f On f.firmID = p.firmID
						inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
						inner join paymentAction pa on p.paymentID = pa.paymentID
						inner join [Action] a on a.actionID = pa.actionID
						inner join [#Agency] ag  on p.agencyID = ag.agencyID
				Where	(a.userID = @loggedUserID) and
						(@theDate IS NULL OR p.paymentDate <= @theDate) and
						p.agencyID = IsNull(@AgencyID, p.agencyID) and
						p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
						p.firmID = IsNull(@FirmID, p.firmID) and
						f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
						((pt.IsHidden = 1 and @ShowBlack = 1)  or
						(pt.IsHidden = 0 and @ShowWhite = 1)) 
						AND (pt.isHidden = 0 or @isHideWhite = 0) And
							(pt.isHidden = 1 or @isHideBlack = 0) 
				Group by p.firmID, p.agencyID
		end 
	end 
	else 
	begin 
		Insert 	Into #tmp1
		Select	ISNULL(Sum(pa.summa), 0) as summa,
				p.firmID,
				p.agencyID
		From	payment p
				inner join Firm f on p.firmID = f.firmID
				inner join paymenttype pt on p.paymentTypeID = pt.paymenttypeID
				inner join paymentAction pa on p.paymentID = pa.paymentID
				inner join [Action] a on a.actionID = pa.actionID
				inner join [#Agency] ag  on p.agencyID = ag.agencyID
				inner join (
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as xu on a.userID = xu.userID
		Where	(@theDate IS NULL OR p.paymentDate <= @theDate) and
				p.agencyID = IsNull(@AgencyID, p.agencyID) and
				p.paymentTypeID = IsNull(@PaymentTypeID, p.paymentTypeID) and
				p.firmID = IsNull(@FirmID, p.firmID) and
				f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
				((pt.IsHidden = 1 and @ShowBlack = 1)  or
				(pt.IsHidden = 0 and @ShowWhite = 1)) and
				a.userID = coalesce(@ManagerID, a.userID)
				AND (pt.isHidden = 0 or @isHideWhite = 0) And
					(pt.isHidden = 1 or @isHideBlack = 0) 
		Group by p.firmID, p.agencyID
	end 

	-- calculate actions till defined date ------------------------
	Declare cur_Companies Cursor local fast_forward
	For
	select distinct	c.campaignID, c.campaignTypeID,
		c.startDate, a.firmID,
		c.agencyID,
		c.finishDate,
		c.finalPrice,
		a.discount
	From	campaign c
			inner join [Action] a on c.actionID = a.actionID
			inner join Firm f on a.firmID = f.firmID
			inner join paymenttype pt on c.paymentTypeID = pt.paymenttypeID
			inner join [#Agency] ag on c.agencyID = ag.agencyID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			left join GroupMember gm on a.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
	Where
			(a.userID = @loggedUserID 
			 or @isRightToViewForeignActions = 1 
			 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(a.isSpecial = 1 
			 or (c.campaignTypeID <> 4 
			     and umm.massmediaID is not null 
			     and ((a.userID = @loggedUserID and umm.myMassmedia = 1) 
			          or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1)))
			 or (c.campaignTypeID = 4 
			     and not exists(select * 
							from PackModuleIssue pmi 
								inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
								inner join Module m on pmc.moduleID = m.moduleID
								left join @massmedias ummm on m.massmediaID = ummm.massmediaID
							where pmi.campaignID = c.campaignID 
							  and (ummm.massmediaID is null 
							       or (a.userID = @loggedUserID and ummm.myMassmedia = 0)
							       or (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0))))) and		
			(@theDate is NULL or (c.startDate <= @theDate)) and
			a.firmID = IsNull(@FirmID, a.firmID) and
			f.headCompanyID = IsNull(@HeadCompanyID, f.headCompanyID) and
			c.agencyID = IsNull(@AgencyID, c.agencyID) and
			c.paymentTypeID = IsNull(@PaymentTypeID, c.paymentTypeID) and
			((pt.IsHidden = 1 and @ShowBlack = 1) or (pt.IsHidden = 0 and @ShowWhite = 1)) and
			a.userID = IsNull(@ManagerID, a.userID) and
			a.[isConfirmed] = 1 and
			(pt.isHidden = 0 or @isHideWhite = 0) and
			(pt.isHidden = 1 or @isHideBlack = 0)

	Declare	@campaignID int, @TypeID int,
			@StartDay datetime,
			@Price decimal(18,2), @Agency int,
			@FinishDay datetime, @FinalPrice decimal(18,2), @actiondiscount decimal(9,4)

	Open	cur_Companies

	Fetch	Next from cur_Companies
	Into 	@campaignID, @TypeID, @StartDay, @FirmID, @Agency, @FinishDay, @FinalPrice, @actiondiscount

	While	@@fetch_status = 0
		Begin
		If	@theDate IS NULL OR @theDate > @FinishDay 
			Set 	@Price = case when @TypeID <> 4 then @FinalPrice * @actiondiscount else @FinalPrice end 
		else
			EXEC GetPriceByPeriod @campaignID, @TypeID, @StartDay, @theDate, @Price output

		Insert	Into #tmp1(summa, firmID, agencyID)
		Values	(-@Price, @FirmID, @Agency)

		Fetch	Next from cur_Companies
		Into 	@campaignID, @TypeID, @StartDay, @FirmID, @Agency, @FinishDay, @FinalPrice, @actiondiscount

		End

	close		cur_Companies
	Deallocate	cur_Companies

	End

If	@IsGroupByAgency = 0
	Begin
		Select	
			firm.[firmID] AS firmID,
			firm.Name as name,
			hc.name as headCompanyName,
			case
				when sum(summa) > 0 then sum(summa)
				else 0
			end as summaPositive,
			case
				when sum(summa) < 0 then sum(summa)
				else 0
			end as summaNegative
		From	
			#tmp1 Join firm On #tmp1.firmID = firm.firmID
			inner join HeadCompany hc on hc.headCompanyID = firm.headCompanyID
		Group by firm.Name, firm.firmID, hc.name
		Having 	abs(sum(summa)) >= 0.005

	End
Else
	Begin

	Select	agencyID, firmID, sum(summa) as summa
	into		#tmp2
	From	#tmp1
	Group By agencyID, firmID
	
	drop table #tmp1

	-- ALTER  table with Agency ID ---------------------------
	Declare	@SQLString NVARCHAR(2500), @Desc NVARCHAR(64), @Where NVARCHAR(4000), @Select NVARCHAR(4000), @summa decimal(18,2)

	declare	cur_agency cursor local fast_forward for 
	Select	agency.agencyID, agency.Name, sum(summa) as summa
	From	#tmp2 join Agency on #tmp2.agencyID = agency.agencyID
	Group By agency.agencyID, agency.Name

	create table #rc (
		[RowNum] [int],
		[$Итого] decimal(18,2) default 0
	)
	insert 	#rc(RowNum, [$Итого])
	select	firmID, sum(summa)
	from	#tmp2
	group	by firmID

	declare @sql nvarchar(max), @addwhere bit
	set @sql = 'select 	firm.Name as "Фирма", hc.name as "Группа компаний",
						#rc.* 
				from 	#rc
						JOIN Firm ON Firm.firmID = #rc.RowNum
						Inner Join HeadCompany hc on hc.headCompanyID = firm.headCompanyID'
	set @addwhere = 0
	
	open	cur_agency
	while 1=1
	begin
		fetch next from cur_agency into @AgencyID, @Desc, @Summa
		if @@fetch_status <> 0	
			break

		if @addwhere = 0
		 set @sql = @sql + ' where (0 '
		 
		set @addwhere = 1
		
		set @sql = @sql + ' + abs([$' + @Desc + '])'

		set @SQLString = 'ALTER TABLE #rc ADD [$' + @Desc + '] [decimal(18,2)] default 0 with values;'
		exec sp_executeSQL @SQLString
		set @SQLString = N'UPDATE #rc set [$' + @Desc + '] = summa from #rc join #tmp2 on #rc.RowNum = #tmp2.firmID and #tmp2.agencyID = @a'
		exec sp_executeSQL @SQLString, N'@a int', @a = @agencyID
		-- summary
		set @SQLString = N'UPDATE #rc set [$' + @desc + '] = @sum, [$Итого] = [$Итого] + @sum where RowNum = -1'
		exec sp_executeSQL @SQLString, N'@sum decimal(18,2)', @sum = @summa
	end

	close		cur_agency
	deallocate	cur_agency

	if @addwhere = 1
		set @sql = @sql + ') >= 0.005 '

	set @sql = @sql + ' order by firm.name '

	exec sp_executeSQL @sql
	 
	drop table #rc
	drop table #tmp2
	end