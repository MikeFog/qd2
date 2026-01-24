CREATE       Procedure [dbo].[stat_VolumeOfRealizationForRollers]
(
@StartDay datetime = default,
@FinishDay datetime = default,
@FirmID int = default, 
@AgencyID int = default,
@StudioID int = default, 
@PaymentTypeID int = default,
@ManagerID int = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByStudio bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT ON
If	@StartDay Is Null Or @FinishDay Is Null
	Begin
	Raiserror('FilterStartFinishDays', 16, 1)
	Return
	End

Set	@StartDay = Convert(datetime, Convert(varchar, @StartDay, 112), 112)
Set	@FinishDay = Convert(datetime, Convert(varchar, @FinishDay, 112), 112) + 1

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignSOActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupSOActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

Select	so.Price * so.Ratio As Summa,
		a.FirmID as FirmId,
		so.PAYMENTTYPEID,
		so.StudioId,
		so.AgencyId,
		a.UserId
Into		#tmp1
From	[StudioOrder] so
		inner join StudioOrderAction a On a.actionId = so.ActionId
		inner join RolStyle rs On rs.rolStyleID = so.rolStyleID
		inner join PaymentType pt On pt.PaymentTypeId = so.PAYMENTTYPEID
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
WHERE so.createDate >= @StartDay And
		so.createDate < @FinishDay And
		so.AGENCYID = IsNull(@AgencyID, so.AgencyID) and
		a.FirmID = IsNull(@FirmID, a.FirmID) and
		so.PaymentTypeID = IsNull(@PaymentTypeID, so.PaymentTypeID) and
		a.UserID = IsNull(@ManagerID, a.UserId) And
		(@ShowWhite <> 0 or pt.IsHidden <> 0) And  
		(@ShowBlack <> 0 or pt.IsHidden = 0) And
		(so.IsComplete = 1)

Declare	@SummaVar money
Select	@SummaVar = IsNull(sum(Summa), 0) From #tmp1

Declare	@SQLString NVARCHAR(4000), @IsStarted int
Set	@IsStarted = 0

Set	@SQLString = N'Select	row_number() over(order by IsNull(Sum(Summa), 0)) as RowNum, '
Set	@SQLString = @SQLString + N' IsNull(Sum(Summa), 0) as  sum1, '
If	@IsGroupByFirm <> 0			Set 	@SQLString = @SQLString + N'Firm.Name as firm,'
If	@IsGroupByPaymentType <> 0	Set 	@SQLString = @SQLString + N'PaymentType.name as "payment_type",'
If	@IsGroupByStudio <> 0			Set 	@SQLString = @SQLString + N'vStudio.Name as studio,'
If	@IsGroupByManager <> 0		Set 	@SQLString = @SQLString + N'[User].LastName + space(1) + [User].FirstName as manager,'
If	@IsGroupByAgency <> 0 		Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'

Set 	@SQLString = @SQLString + 
		N'case @Summa
			when	0 then 0
			else	Cast((IsNull(Sum(Summa), 0) * 100.0 / @Summa) as decimal(12,2))
		End as "percent"	
From	#tmp1 '

If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N'Join Paymenttype On Paymenttype.PaymentTypeId = #tmp1.PaymentTypeId '
If	@IsGroupByStudio <> 0 Set @SQLString = @SQLString + N'Join vStudio On vStudio.studioId =  #tmp1.StudioId '
If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N'Join Firm On Firm.firmId = #tmp1.FirmId '
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N'Join [User] On [User].userId = #tmp1.UserId '
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N'Join Agency On Agency.agencyId = #tmp1.AgencyId '

If	0 + @IsGroupByPaymentType + @IsGroupByStudio + @IsGroupByFirm + @IsGroupByManager + @IsGroupByAgency <> 0
	Begin
	-- Group By part
	Set	@IsStarted = 0
	Set @SQLString = @SQLString + N' Group by '

	If	@IsGroupByStudio <> 0
		begin
		If	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set	@SQLString = @SQLString + N'vStudio.Name'
		Set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	if	@IsGroupByPaymentType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Paymenttype.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'[User].LastName + space(1) + [User].FirstName'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end
	End

--set @SQLString = @SQLString + N' Select * from #Tmp2'

EXECUTE sp_executesql @SQLString,
	N'@Summa int',
	@Summa = @SummaVar
