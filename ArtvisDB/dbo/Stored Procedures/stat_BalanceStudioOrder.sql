CREATE        PROC [dbo].[stat_BalanceStudioOrder]
(
@theDate datetime = null,
@firmID int = default,
@agencyNameID int = default,
@paymentTypeID int = default,
@isHideBlack int = 0,
@isHideWhite int = 0,
@userID int = default,
@IsGroupByAgency int = 0,
@agenciesIDString varchar(1024) = null,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
As
SET NOCOUNT ON
If	@theDate Is Null Begin
	Raiserror('DateIsNotSelected', 16, 1)
	Return
	End

-- @theDate will be included
Select	@theDate = dbo.ToShortDate(@theDate) + 1

-- calculate payments till defined date -----------------------
CREATE TABLE #Agency(agencyID smallint)

Declare	@tmp1 Table	(
	[summa] int,
	[firmId] int,
	[agency] varchar(32)
	)

-- Populate temporary tables with Agency and Payment types
IF @agenciesIDString Is Null
	INSERT INTO #Agency 
	SELECT agencyID FROM Agency
Else
	Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString 

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

If	@userID Is Null
	Insert 	Into @tmp1
	Select	
		Sum(pso.summa) as summa,
		pso.firmId,
		a.name
	From		
		PaymentStudioOrder pso
		inner join paymentType pt on pso.paymenttypeId = pt.paymentTypeID
		inner join Agency a on a.agencyID = pso.agencyID
		inner join #Agency ag on a.[agencyID] = ag.[agencyID]
	Where	
		pso.paymentDate < DATEADD(DAY, 1, @theDate) and
		a.agencyID = IsNull(@agencyNameID, a.agencyID) and
		pt.paymenttypeId = IsNull(@paymentTypeID, pt.paymenttypeId) and
		pso.firmId = IsNull(@firmID, pso.firmId) and
		((pt.isHidden = 1 and @isHideBlack = 0)  or
		(pt.isHidden = 0 and @isHideWhite = 0)) and
		((pt.IsHidden = 1 and @showBlack = 1)  or
		(pt.IsHidden = 0 and @showWhite = 1)) 
	Group by 
		pso.firmId, a.name
Else
	Insert 	Into @tmp1
	Select	
		Sum(psoa.summa) as summa,
		pso.firmId,
		a.name
	From	
		PaymentStudioOrder pso
		inner join paymentType pt on pso.paymenttypeId = pt.paymentTypeID
		inner join PaymentStudioOrderAction psoa on pso.paymentID = psoa.paymentID
		inner join Agency a on a.agencyID = pso.agencyID
		inner join StudioOrderAction soa on soa.actionID = psoa.actionID
		inner join #Agency ag on a.[agencyID] = ag.[agencyID]
	Where	
		pso.paymentDate < DATEADD(DAY, 1, @theDate)  and
		a.agencyID = IsNull(@agencyNameID, a.agencyID) and
		pt.paymenttypeId = IsNull(@paymentTypeID, pt.paymenttypeId) and
		pso.firmId = IsNull(@firmID, pso.firmId) and
		((pt.isHidden = 1 and @isHideBlack = 0)  or
		(pt.isHidden = 0 and @isHideWhite = 0)) and
		((pt.IsHidden = 1 and @showBlack = 1)  or
		(pt.IsHidden = 0 and @showWhite = 1)) and
		soa.userID = @userID
	Group By 
		pso.firmId, a.name

-- Actions
Insert 	Into @tmp1
Select	
	Sum(-so.finalPrice),
	soa.firmId,
	a.name
From	
	StudioOrder so
	join StudioOrderAction soa on so.actionID = soa.actionID
	join paymentType pt on so.paymenttypeId = pt.paymentTypeID
	join Agency a on a.AgencyID = so.agencyID
	inner join #Agency ag on a.[agencyID] = ag.[agencyID]
	inner join 
	(
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) as x on soa.userID = x.userID
Where	
	so.finishDate <= @theDate and
	soa.firmId = IsNull(@firmID, soa.firmId) and
	so.agencyID = IsNull(@agencyNameID, so.agencyID) and
	so.paymenttypeId = IsNull(@paymentTypeID, so.paymenttypeId) and
	((pt.isHidden = 1 and @isHideBlack = 0)  or
	(pt.isHidden = 0 and @isHideWhite = 0)) and
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1)) and
	soa.userID = IsNull(@userID, soa.userID)
	AND so.isComplete = 1
Group By 
	soa.firmId, a.name

Declare	@agencyName varchar(32)

		Select 
				Firm.firmID,
				Firm.name,
				case
					when sum(summa) > 0 then Cast(sum(summa) as varchar)
					else 0
				end as summaPositive,
				case
					when sum(summa) < 0 then Cast(sum(summa) as varchar)
					else 0
				end as summaNegative
		From	@tmp1 t Join Firm On t.firmId = Firm.firmID
		Group by Firm.name, Firm.firmID
		Having 	sum(summa) <> 0
