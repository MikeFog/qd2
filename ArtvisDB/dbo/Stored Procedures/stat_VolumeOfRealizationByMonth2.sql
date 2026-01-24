CREATE Procedure [dbo].[stat_VolumeOfRealizationByMonth2]
(
@StartDay DATETIME = NULL,
@FinishDay DATETIME = NULL,
@FirmID smallint = NULL, 
@headCompanyID smallint = NULL, 
@MassmediaID smallint = NULL, 
@PaymentTypeID smallint = NULL, --
@CampaignTypeID tinyint = NULL, --
@ManagerID smallint = NULL,
@AgencyID smallint = NULL, 
@massmediaGroupID int = NULL, --
@advertTypeID smallint = NULL, --
@IsGroupByMassmedia bit = 0,
@IsGroupByPaymentType bit = 0, --
@IsGroupByCampaignType bit = 0, --
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByHeadCompany bit = 0, 
@IsGroupByMassmediaGroupType bit = 0, --
@IsGroupByAdvertType bit = 0, --
@IsGroupByAdvertTypeTop bit = 0, --
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON

IF @IsGroupByHeadCompany = 1 SET @IsGroupByFirm = 0

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
			y smallint,
			m tinyint,
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
			INDEX i1 UNIQUE CLUSTERED (y, m, campaignID, massmediaID, advertTypeID)
			)

INSERT @Campaign
SELECT d.* 
FROM fn_statGetPriceByMonth(@startDate, @finishDate, @loggedUserID) d Inner Join Firm f On f.firmId = d.FirmId'
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

Set	@SQLString = @SQLString + N'Select	row_number() over(order by d.y, d.m) as RowNum,'
Set	@SQLString = @SQLString + N'MAX(iMonthName.name) + space(1) + cast(d.[y] as varchar) + '' г.'' as [period],'
If	@IsGroupByMassmedia <> 0
	Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup as mmName,  '
If	@IsGroupByFirm <> 0
	Set 	@SQLString = @SQLString + N'Firm.Name as "firmName",'
If	@IsGroupByHeadCompany <> 0  -- НОВАЯ ГРУППИРОВКА
	Set 	@SQLString = @SQLString + N'HeadCompany.Name as "head_company",'
If	@IsGroupByManager <> 0
	Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as manager,'
If	@IsGroupByAgency <> 0
	Set 	@SQLString = @SQLString + N'Agency.Name as "agencyName",'
If	@IsGroupByPaymentType <> 0
	Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
If	@IsGroupByCampaignType <> 0
	Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
If	@IsGroupByMassmediaGroupType <> 0
	Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
If	@IsGroupByAdvertType <> 0
	Set 	@SQLString = @SQLString + N'AdvertType.Name as "adverttype",'
If	@IsGroupByAdvertTypeTop <> 0
	Set 	@SQLString = @SQLString + N'at.Name as "topAdverttype",'
Set	@SQLString = @SQLString + N' IsNull(Sum(price), 0) as  price '
Set	@SQLString = @SQLString + N'FROM @Campaign d inner join iMonthName on d.m = iMonthName.number'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on d.massmediaGroupID = MassmediaGroup.massmediaGroupID '
If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on d.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on d.campaignTypeID = iCampaignType.CampaignTypeID'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on d.massmediaID = vMassMedia.massmediaID'
If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Firm on d.firmID = Firm.FirmID '
If	@IsGroupByHeadCompany <> 0 Set @SQLString = @SQLString + N' inner join Firm on d.firmID = Firm.FirmID inner join HeadCompany on Firm.headCompanyID = HeadCompany.headCompanyID '
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on d.userID = [User].UserID'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on d.AgencyID = Agency.AgencyID'
If	@IsGroupByAdvertType <> 0 OR @IsGroupByAdvertTypeTop <> 0 Set @SQLString = @SQLString + N' left join AdvertType on d.advertTypeID = AdvertType.AdvertTypeID'
If	@IsGroupByAdvertTypeTop <> 0 Set @SQLString = @SQLString + N' left join AdvertType at on AdvertType.parentID = at.AdvertTypeID'

Set 	@SQLString = @SQLString + N' Where price <> 0 '
Set 	@SQLString = @SQLString + N' Group by d.y, d.m'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N', MassmediaGroup.Name'
if	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N', Paymenttype.Name'
If	@IsGroupByCampaignType <> 0	Set	@SQLString = @SQLString + N', iCampaignType.Name'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N', vMassMedia.NameWithGroup'
If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N', Firm.Name'
If	@IsGroupByHeadCompany <> 0 Set @SQLString = @SQLString + N', HeadCompany.Name'
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N', [User].lastName, [User].firstName'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N', Agency.Name'
If	@IsGroupByAdvertType <> 0 Set @SQLString = @SQLString + N', AdvertType.Name'
If	@IsGroupByAdvertTypeTop <> 0 Set @SQLString = @SQLString + N', at.Name'

--print(@SQLString)
EXECUTE sp_executesql @SQLString,
	N'@startDate datetime, @finishDate datetime, @loggedUserID smallint, @FirmID smallint, @MassmediaID smallint, @PaymentTypeID smallint, @CampaignTypeID tinyint,
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


