CREATE Procedure [dbo].[stat_VolumeOfRealization2]
(
@StartDay DATETIME = NULL,
@FinishDay DATETIME = NULL,
@FirmID smallint = NULL, 
@headCompanyID smallint = NULL, 
@MassmediaID smallint = NULL, 
@PaymentTypeID smallint = NULL,
@CampaignTypeID tinyint = NULL,
@ManagerID smallint = NULL,
@AgencyID smallint = NULL,
@massmediaGroupID int = NULL,
@advertTypeID smallint = NULL,
@IsGroupByMassmedia bit = 0,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByHeadCompany bit = 0,  -- НОВЫЙ ПАРАМЕТР
@IsGroupByMassmediaGroupType bit = 0,
@IsGroupByAdvertType bit = 0,
@IsGroupByAdvertTypeTop bit = 0,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON

--IF @IsGroupByHeadCompany = 1 SET @IsGroupByFirm = 0

IF @StartDay IS NULL OR @FinishDay IS NULL
BEGIN
	RAISERROR('FilterStartFinishDays', 16, 1)
	RETURN
END

SET	@StartDay = dbo.ToShortDate(@StartDay)
SET	@FinishDay = dbo.ToShortDate(@FinishDay)


-- output ---------------------------------------------------------
DECLARE	@SQLString NVARCHAR(MAX), @IsStarted int

/* Build the SQL string once.*/
SET @SQLString = N'
DECLARE @Summa money
SET @Summa=0

DECLARE @Campaign TABLE (
			campaignID int, 
			advertTypeID smallint,
			actionID int, 
			massmediaID smallint, 
			paymentTypeID smallint, 
			campaignTypeID tinyint, 
			agencyID smallint,
			startDate datetime,
			finishDate datetime,
			finalPrice money,
			userID smallint,
			firmID smallint,
			discount float,
			massmediaGroupID int,
			price money,
			INDEX i1 UNIQUE CLUSTERED (campaignID, massmediaID, advertTypeID)
			)

INSERT @Campaign
SELECT d.* 
FROM fn_statGetPrice(@startDate, @finishDate, @loggedUserID) d Inner Join Firm f On f.firmId = d.FirmId'
If	@ShowWhite = 0 OR @ShowBlack = 0 Set @SQLString = @SQLString + N' inner join Paymenttype on d.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@advertTypeID IS NOT NULL Set @SQLString = @SQLString + N' left join AdvertType at on at.advertTypeID = d.advertTypeID'

Set @SQLString = @SQLString +
'
WHERE 1=1
'
If	@ShowWhite = 0 AND @ShowBlack <> 0 Set @SQLString = @SQLString + N' AND PaymentType.isHidden <> 0'
If	@ShowBlack = 0 AND @ShowWhite <> 0 Set @SQLString = @SQLString + N' AND PaymentType.isHidden = 0'
IF	@FirmID IS NOT NULL Set @SQLString = @SQLString + N' AND d.firmID = @FirmID'
IF	@headCompanyID IS NOT NULL Set @SQLString = @SQLString + N' AND f.headCompanyID = @headCompanyID'
IF	@MassmediaID IS NOT NULL Set @SQLString = @SQLString + N' AND d.massmediaID = @MassmediaID'
IF	@PaymentTypeID IS NOT NULL Set @SQLString = @SQLString + N' AND d.paymentTypeID = @PaymentTypeID'
IF	@CampaignTypeID IS NOT NULL Set @SQLString = @SQLString + N' AND d.campaignTypeID = @CampaignTypeID'
IF	@ManagerID IS NOT NULL Set @SQLString = @SQLString + N' AND d.userID = @ManagerID'
IF	@AgencyID IS NOT NULL Set @SQLString = @SQLString + N' AND d.agencyID = @AgencyID'
IF	@massmediaGroupID IS NOT NULL Set @SQLString = @SQLString + N' AND d.massmediaGroupID = @massmediaGroupID'
IF	@advertTypeID IS NOT NULL Set @SQLString = @SQLString + N' AND @advertTypeID IN(at.parentID,d.advertTypeID)'

Set @SQLString = @SQLString +
'
SELECT	@Summa = ISNULL(sum(price), 0) FROM @Campaign
'
If @IsGroupByFirm = 1 And @IsGroupByHeadCompany = 1 and 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia
	  + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType
	  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
	Begin
Set @SQLString = @SQLString +
'
	DECLARE @res TABLE (
		RowNum int, 
		sum1 money,
		sum4 money,
		firm varchar(256), 
		head_company varchar(256), 
		hc2 varchar(256), 
		[percent] decimal(12,2),
		row_style varchar(20),
		INDEX i1 UNIQUE CLUSTERED (RowNum)
	)

	Insert Into @res(RowNum, sum1, firm, head_company, [percent])
	'
	End
Set	@SQLString = @SQLString + N'Select	row_number() over(order by IsNull(Sum(price), 0)) as RowNum,'
Set	@SQLString = @SQLString + N' IsNull(Sum(price), 0) as  sum1'
Set @SQLString = @SQLString + N',  '

If	@IsGroupByPaymentType <> 0
	Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
If	@IsGroupByCampaignType <> 0
	Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
If	@IsGroupByMassmedia <> 0
	Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup as "massmedia", vMassMedia.massmediaID,'
If	@IsGroupByMassmediaGroupType <> 0
	Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
If	@IsGroupByFirm <> 0
	Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
If	@IsGroupByHeadCompany <> 0  -- НОВАЯ ГРУППИРОВКА
	Set 	@SQLString = @SQLString + N'HeadCompany.Name as "head_company",'
If	@IsGroupByManager <> 0
	Set 	@SQLString = @SQLString + N'[User].userName as "manager",'
If	@IsGroupByAgency <> 0
	Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
If	@IsGroupByAdvertType <> 0
	Set 	@SQLString = @SQLString + N'AdvertType.Name as "adverttype",'
If	@IsGroupByAdvertTypeTop <> 0
	Set 	@SQLString = @SQLString + N'at.Name as "topAdverttype",'

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia + @IsGroupByFirm
	  + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType + @IsGroupByHeadCompany
	  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
	set		@SQLString = @SQLString + N'max(''Все'') as "all",'

Set 	@SQLString = @SQLString + 
		N'case @Summa
			when	0 then 0
			else	Cast((IsNull(Sum(price), 0) * 100.0 / @Summa) as decimal(12,2))
		End as "percent"	
FROM @Campaign d'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on d.massmediaGroupID = MassmediaGroup.massmediaGroupID '
If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on d.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on d.campaignTypeID = iCampaignType.CampaignTypeID'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on d.massmediaID = vMassMedia.massmediaID'
If	@IsGroupByFirm <> 0 And @IsGroupByHeadCompany = 0  Set @SQLString = @SQLString + N' inner join Firm on d.firmID = Firm.FirmID '
If	@IsGroupByHeadCompany <> 0 Set @SQLString = @SQLString + N' inner join Firm on d.firmID = Firm.FirmID inner join HeadCompany on Firm.headCompanyID = HeadCompany.headCompanyID '  -- НОВЫЙ JOIN
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on d.userID = [User].UserID'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on d.AgencyID = Agency.AgencyID'
If	@IsGroupByAdvertType <> 0 OR @IsGroupByAdvertTypeTop <> 0 Set @SQLString = @SQLString + N' left join AdvertType on d.advertTypeID = AdvertType.AdvertTypeID'
If	@IsGroupByAdvertTypeTop <> 0 Set @SQLString = @SQLString + N' left join AdvertType at on AdvertType.parentID = at.AdvertTypeID'

Set 	@SQLString = @SQLString + N' Where price <> 0 '

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType
	  + @IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType 
	  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop + @IsGroupByHeadCompany <> 0  -- ДОБАВИЛИ В УСЛОВИЕ
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
		Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup, vMassMedia.massmediaID'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByHeadCompany <> 0  -- НОВАЯ ГРУППИРОВКА В GROUP BY
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'HeadCompany.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin

		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'[User].userName'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByAdvertType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'AdvertType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByAdvertTypeTop <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'at.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmediaGroupType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set @SQLString = @SQLString + N'MassmediaGroup.Name'
		set	@IsStarted = 1
		end

	end


If @IsGroupByFirm = 1 And @IsGroupByHeadCompany = 1 and 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia
	  + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType
	  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
	Begin
	Set @SQLString = @SQLString +
	'
	Declare @c int
	Select @c = count(*) from @res

	Insert Into @res(RowNum, sum4, head_company, [percent], row_style)
	Select row_number() over (order by head_company) + @c, Sum(sum1), Head_Company, SUM([percent]), ''bold'' From @res Group By head_company having COUNT(*) > 1;

	Update @res Set hc2 = head_company;

	WITH DuplicatesCTE AS (
    SELECT 
        head_company,
        COUNT(*) as count
    FROM @res
    GROUP BY head_company
    HAVING COUNT(*) > 1
	)
	UPDATE t
	SET t.head_company = NULL
	FROM @res t
	INNER JOIN DuplicatesCTE d ON t.head_company = d.head_company
	Where t.firm Is Not Null;

	Update @res Set sum4 = sum1 Where sum1 Is Not Null And head_company Is Not Null;

	Select * From @res Order By hc2, firm
	'
	
	End

print @SQLString

EXECUTE sp_executesql @SQLString,
	N'@startDate datetime, @finishDate datetime, @loggedUserID smallint, @FirmID smallint, @MassmediaID smallint, 
		@PaymentTypeID smallint, @CampaignTypeID tinyint,
		@ManagerID smallint, @AgencyID smallint, @massmediaGroupID int, @advertTypeID smallint, @headCompanyID smallint',
	@startDate = @StartDay, 
	@finishDate = @FinishDay, 
	@loggedUserID = @loggedUserID, 
	@FirmID = @FirmID,
	@MassmediaID = @MassmediaID,
	@PaymentTypeID = @PaymentTypeID,
	@CampaignTypeID = @CampaignTypeID,
	@ManagerID = @ManagerID,
	@AgencyID = @AgencyID,
	@massmediaGroupID = @massmediaGroupID,
	@advertTypeID = @advertTypeID,
	@headCompanyID = @headCompanyID

