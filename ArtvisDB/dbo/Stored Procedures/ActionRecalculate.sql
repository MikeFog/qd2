CREATE PROC [dbo].[ActionRecalculate]
(
@actionID INT,
@needShow BIT = 1,
@loggedUserID int = null,
@todayDate datetime = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
Declare	
	@discountValue float, 	
	@discountValueID int,	
	@tariffPrice money,
	@campaignID int, 
	@massmediaID smallint,
	@campaignTypeID tinyint, 
	@startDate datetime,
	@finishDate datetime,
	@price money,
	@finalPrice money,
	@theDate datetime,
	@newDiscount float,
	@estimatedPrice money,
	@managerDiscountCampaign float,
	@fixedPrice money,
	@issuesPrice money,
	@ratio float,
	@campaignDiscount float,
	@timeBonus int,
	@programsCount int
	
Declare	cur_companies0 Cursor local fast_forward
FOR 
SELECT [campaignID], [campaignTypeID], massmediaID, tariffPrice FROM [Campaign] WHERE [actionID] = @actionID

Open	cur_companies0
Fetch	next from cur_companies0 
Into	@campaignID, @campaignTypeID, @massmediaID, @tariffPrice

declare @issueDuration int, @issueCount int, @campaignCount int

WHILE	@@fetch_status = 0 begin
	select @timeBonus = 0, @programsCount = 0

	IF @campaignTypeID = 1
		SELECT @issueDuration = SUM(r.[duration]), @tariffPrice = SUM(i.[tariffPrice]), 
			@issueCount = COUNT(*), @finishDate = MAX(tw.dayOriginal),
			@startDate = MIN(tw.dayOriginal)
					FROM [Issue] i 
						inner join TariffWindow tw on i.originalWindowID = tw.windowId
						inner join Roller r on i.rollerID = r.rollerID
					WHERE i.[campaignID] = @campaignID 
	ELSE IF @campaignTypeID = 2
	begin
		SELECT 
			@tariffPrice = SUM(i.[tariffPrice]), 
			@finishDate = MAX(DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)) ),
			@startDate = MIN(DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)) ),
			@timeBonus = sum(pl.bonus),
			@programsCount = count(*)
		FROM 
			[ProgramIssue] i 
			inner join Campaign c on c.campaignID = i.campaignID
			inner join SponsorTariff st on i.tariffID = st.tariffID
			INNER JOIN [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
		WHERE 
			i.[campaignID] = @campaignID
					
		SELECT 
			@issueDuration = SUM(dbo.f_GetSponsorDuration(r.[duration], i.positionId, pl.extraChargeFirstRoller, pl.extraChargeSecondRoller, pl.extraChargeLastRoller)), 
			@issueCount = COUNT(*), 
			@startDate = CASE WHEN MIN(tw.dayOriginal) IS NULL OR @startDate < MIN(tw.dayOriginal) THEN @startDate ELSE MIN(tw.dayOriginal) end,
			@finishDate = CASE WHEN MAX(tw.dayOriginal) IS NULL OR @finishDate > MAX(tw.dayOriginal) THEN @finishDate ELSE MAX(tw.dayOriginal) END
		FROM 
			[Issue] i 
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			Inner Join Tariff t on tw.tariffId = t.tariffID
			Inner Join Pricelist pl On pl.pricelistID = t.pricelistID
			inner join Roller r on i.rollerID = r.rollerID
			inner join MassMedia mm on tw.massmediaID = mm.massmediaID
		WHERE 
			i.[campaignID] = @campaignID
	END
	ELSE IF @campaignTypeID = 3
	begin
		SELECT 
			@issueDuration = SUM(r.[duration]), @issueCount = COUNT(*) 
		FROM 
			[Issue] i 
			inner join Roller r on i.rollerID = r.rollerID 
		WHERE 
		i.[campaignID] = @campaignID 
	
		SELECT @tariffPrice = SUM(i.[tariffPrice]), 
			@finishDate = MAX(i.[issueDate]),
			@startDate = MIN(i.[issueDate])
		FROM [ModuleIssue] i 
		WHERE i.[campaignID] = @campaignID
	END
	ELSE IF @campaignTypeID = 4
	begin
		SELECT @issueDuration = SUM(r.[duration]), @issueCount = COUNT(*) 
					FROM [Issue] i inner join Roller r on i.rollerID = r.rollerID WHERE i.[campaignID] = @campaignID
		
		SELECT @tariffPrice = SUM(i.[tariffPrice]), 
			@finishDate = MAX(i.[issueDate]),
			@startDate = MIN(i.[issueDate])
					FROM [PackModuleIssue] i WHERE i.[campaignID] = @campaignID
	END
	
	select @startDate = dbo.ToShortDate(@startDate), @finishDate = dbo.ToShortDate(@finishDate)
	
	exec hlp_CompanyDiscountCalculate @massMediaID, @campaignTypeID, @startDate, @tariffPrice, @campaignDiscount output

	UPDATE [Campaign] SET [tariffPrice] = ISNULL(@tariffPrice, 0), 
		[issuesDuration] = ISNULL(@issueDuration, 0), [issuesCount] = ISNULL(@issueCount, 0),
		[startDate] = @startDate, [finishDate] = @finishDate, discount = @campaignDiscount, 
		timeBonus = isnull(@timeBonus, 0), programsCount = isnull(@programsCount, 0)
		WHERE [campaignID] = @campaignID
	
	Fetch	next from cur_companies0 
	Into	@campaignID, @campaignTypeID, @massmediaID, @tariffPrice
end

close cur_companies0
deallocate cur_companies0

SELECT	
	@tariffPrice = IsNull(Sum(tariffPrice), 0),
	@startDate = dbo.ToShortDate(Min(startDate)),
	@finishDate = dbo.ToShortDate(Max(finishDate)),
	@campaignCount = COUNT(*)
From		
	Campaign
Where	
	actionID = @actionID

declare @duration int, @dayX datetime

-- если в акции всего 1 кампания, то нет смысла вычислять пакетную скидку
If @campaignCount > 1
	exec	hlp_ActionDiscountCalculate	
		@actionID,
		@startDate,
		@discountValue output
else
	Set @discountValue = 1

Update 	
	[Action]
Set 		
	tariffPrice = @tariffPrice,
	discount = @discountValue,
	startDate = @startDate,
	finishDate = @finishDate,
	modDate = getdate()
Where 	
	actionId = @actionID

Declare	cur_companies Cursor local fast_forward
For
Select	
	campaignID, 
	massmediaID,
	campaignTypeID, 
	startDate, 
	finishDate, 
	price, 
	managerDiscount,	
	finalPrice
From
	Campaign
Where	
	actionID = @actionID

Open	cur_companies
Fetch	next from cur_companies 
Into	@campaignID, @massmediaID, @campaignTypeID, @startDate, @finishDate, 
	@price, @managerDiscountCampaign, @finalPrice

If @todayDate Is Null
	SET @dayX = Convert(datetime, Convert(varchar(6), getdate(), 112) + '01', 112)
Else
	SET @dayX = Convert(datetime, Convert(varchar(6), @todayDate, 112) + '01', 112)
--Set	@theDate = DATEADD(DAY, 1, @dayX)
Set	@theDate = DATEADD(DAY, 0, @dayX)
Set	@dayX = DATEADD(DAY, -1, @dayX)

WHILE	@@fetch_status = 0 BEGIN
	-- Campaign should have a price ...
	IF @campaignTypeID <> 4
		Set	@estimatedPrice = @managerDiscountCampaign * @price * @discountValue
	ELSE 
		Set	@estimatedPrice = @managerDiscountCampaign * @price 

	If @startDate < @theDate
		Exec GetPriceByPeriod @campaignID, @campaignTypeID, @startDate, @dayX, @fixedPrice out
	Else Begin
		Set @fixedPrice = 0
	End

	SET @finishDate = DATEADD(DAY, 1, @finishDate)

	Exec GetIssuesPrice @campaignID, @campaignTypeID, @theDate, @finishDate, @issuesPrice out

	IF @issuesPrice IS NOT NULL AND @issuesPrice > 0 
	begin
		Set	@ratio = (CAST(@estimatedPrice AS FLOAT) - CAST(@fixedPrice AS FLOAT)) / CAST(@issuesPrice AS FLOAT)

		if @ratio < 0
		BEGIN
			close cur_companies
			deallocate cur_companies

			Raiserror('CantChangeDiscount2', 16, 1)
			Return
		END

		exec SetIssueRatio @campaignID, @campaignTypeID, @theDate, @finishDate, @ratio
	end
	
	Update	
		Campaign
	Set			
		finalPrice = @managerDiscountCampaign * @price,
		modTime = getdate(),
		modUser = isnull(@loggedUserID, modUser)
	Where	
		campaignID = @campaignID
	
	Fetch	next from cur_companies 
	Into	@campaignID, @massmediaID, @campaignTypeID, @startDate, @finishDate, 
		@price, @managerDiscountCampaign, @finalPrice

end

close cur_companies
deallocate cur_companies

Declare @priceSumByCampaigns MONEY, @sumPackModules MONEY, @sumOther MONEY 
SELECT @sumPackModules = 0, @sumOther = 0, @priceSumByCampaigns = 0

SELECT 
	@sumPackModules = 
	CASE WHEN c.[campaignTypeID] = 4 
		THEN (@sumPackModules + c.[finalPrice] )
		ELSE @sumPackModules
	END,
	@sumOther = 
	CASE WHEN c.[campaignTypeID] = 4 
		THEN @sumOther
		ELSE (@sumOther + c.[finalPrice] )
	END,
	@priceSumByCampaigns = @priceSumByCampaigns + c.[finalPrice]
FROM [Campaign] c WHERE c.[actionID] = @actionID

Update [Action]
Set 	priceSumByCampaigns = IsNull(@priceSumByCampaigns, 0),
	[totalPrice] = ISNULL((CAST([discount] AS MONEY)*@sumOther) + @sumPackModules,0)
Where actionID = @actionID

IF @needShow = 1
	EXEC Actions1 @actionID = @actionID
