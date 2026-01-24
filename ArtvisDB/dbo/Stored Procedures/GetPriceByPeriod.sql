/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE                    PROC [dbo].[GetPriceByPeriod]
(
@campaignID int, 
@campaignTypeID int,
@startDate datetime, 
@finishDate datetime, 
@price money OUT,
@massmediaID INT = NULL,
@tariffPrice MONEY = NULL out,
@showBlack bit = 1,
@taxPrice money = null out,
@withTax bit = 0,
@rollerIDString VARCHAR(8000) = null
)
As
SET NOCOUNT ON

if EXISTS(SELECT * 
	FROM [Campaign] c 
		INNER JOIN [Action] a ON c.[actionID] = a.[actionID] 
		inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			and (@showBlack = 1 or pt.IsHidden = 0)
		WHERE c.[campaignID] = @campaignID AND a.isSpecial = 1)
	BEGIN
	SELECT @price = price, @tariffPrice = tariffPrice 
	FROM [Campaign] WHERE [campaignID] = @campaignID
		
	RETURN
	end
	
IF (@startDate IS NULL AND @finishDate IS NULL)
begin 
	SELECT @price = 0
	return 
end 

SET @startDate = dbo.ToShortDate(@startDate)
SET @finishDate = dbo.ToShortDate(@finishDate) 
Set	@price = 0

If	@campaignTypeID = 1 
	begin
	
	if @withTax = 0
		Select	@price = sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice])
		From	
			Issue i
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where	
			i.campaignID = @campaignID	and
			tw.dayOriginal between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
	else 
		Select	@price = sum(i.[tariffPrice] * i.[ratio]), 
			@tariffPrice = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From	
			Issue i
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and tw.dayOriginal between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where	
			i.campaignID = @campaignID	and
			tw.dayOriginal between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)

	End
Else If	@campaignTypeID = 2	
	begin
	
	if @withTax = 0
		Select	
			@price = isnull(Sum(i.[tariffPrice] * i.[ratio]), 0), @tariffPrice = isnull(SUM(i.[tariffPrice]), 0)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
		Where		
			i.campaignID = @campaignID and 
			Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)), 112), 112) between @startDate and @finishDate
	else 
		Select	
			@price = isnull(Sum(i.[tariffPrice] * i.[ratio]), 0), @tariffPrice = isnull(SUM(i.[tariffPrice]), 0),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)), 112), 112) between at.startDate and at.finishDate
		Where		
			i.campaignID = @campaignID and 
			Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)), 112), 112) between @startDate and @finishDate
	End
Else If	@campaignTypeID = 3 begin
	if @withTax = 0
		SELECT @price = 	Sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice])
		From		
			ModuleIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where		
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
	else 
		SELECT @price = 	Sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ModuleIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where		
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
end
			
ELSE IF @campaignTypeID = 4
BEGIN

	DECLARE @packModulePrice MONEY 
	DECLARE @tariffPricePM MONEY

	if @withTax = 0
		SELECT 
			@packModulePrice = SUM(i.[tariffPrice] * i.[ratio]), @tariffPricePM = SUM(i.[tariffPrice])
		FROM [PackModuleIssue] i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
	else  
		SELECT 
			@packModulePrice = SUM(i.[tariffPrice] * i.[ratio]), @tariffPricePM = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when coalesce(at.divisor, 0) < 0.0000001 then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		FROM [PackModuleIssue] i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
		
	IF @massmediaID IS NULL
		BEGIN
			SET	@price = @packModulePrice
			SET @tariffPrice = @tariffPricePM
			RETURN
		END

	CREATE TABLE #tmp(massmediaID SMALLINT, price MONEY)
	INSERT INTO #tmp
	SELECT 
		m.[massmediaID], sum(mpl.[price])
	FROM [PackModuleIssue] i 
		INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
		INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
		INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
		inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			and (@showBlack = 1 or pt.IsHidden = 0)
		left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
	WHERE 
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate  and
		(@rollerIDString is null or rr.ID is not null)
	group by m.massmediaID

	declare @sumPrice money
	SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1

	DECLARE @sumPriceMM money
	SELECT @sumPriceMM = sum(t1.price) FROM [#tmp] AS t1 WHERE t1.[massmediaID] = @massmediaID

	SET @price = @packModulePrice * @sumPriceMM / @sumPrice
	SET @tariffPrice = @tariffPricePM * @sumPriceMM / @sumPrice
	drop table #tmp
END
