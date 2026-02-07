
CREATE           PROC [dbo].[rpt_GenericBill]
(
@actionId int,
@agencyId smallint,
@beginDate datetime = null,
@endDate datetime = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare @invoiceTableText varchar(1024), @invoiceTableTextSponsor varchar(1024)
select @invoiceTableText = reportText from [dbo].[ReportPartText] where codeName='invoice1'
select @invoiceTableTextSponsor = reportText from [dbo].[ReportPartText] where codeName='InvoiceSponsor'

declare @isByMonth bit
select @isByMonth = case when @beginDate is not null and @endDate is not null then 1 else 0 end

Declare @billDate datetime
if (@isByMonth = 1)
	Select @billDate = @endDate
else
	Select @billDate = billDate
	From [Bill]
	Where	actionID = @actionId And agencyID = @agencyId

declare @res Table (
	[name] NVARCHAR(255),
	[price] [MONEY] NOT null,
	[taxPrice] [money] not null
)

declare cur_campaigns cursor local fast_forward
for 
select c.campaignID, c.campaignTypeID, c.startDate, c.finishDate
from Campaign c 
	Inner Join Paymenttype pt On pt.PaymenttypeId = c.PaymenttypeId
		and pt.IsHidden = 0
where 
	c.Actionid = @ActionId and
	c.AgencyId = @AgencyId and
	((@beginDate is null and @endDate is null) or 
	(c.startDate <= @endDate and c.finishDate >=  @beginDate))

declare @campaignID int, @campaignTypeID tinyint, @price money, @startDate datetime, @finishDate datetime,@taxPrice money
open cur_campaigns

fetch next from cur_campaigns into @campaignID, @campaignTypeID, @startDate, @finishDate

while @@fetch_status = 0
begin
	if (@isByMonth = 0)
	begin
		select @beginDate = @startDate, @endDate = @finishDate
	end 

	if (@campaignTypeID in (1,3))
	begin 
		exec GetPriceByPeriod
			@campaignID = @campaignID,
			@campaignTypeID = @campaignTypeID, 
			@startDate = @beginDate,
			@finishDate = @endDate,
			@price = @price OUT,
			@showBlack = 0,
			@withTax = 1,
			@taxPrice = @taxPrice out
		
		insert into @res 
		select replace(replace(@invoiceTableText, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), '{группа радиостанций}', 
			IsNull(mm.groupName, '{группа радиостанций}')) as name,
			@price as price, @taxPrice
		from Campaign c 
			inner join vMassmedia mm on c.massmediaID = mm.massmediaID
		where c.campaignID = @campaignID
	end 
	else if (@campaignTypeID in (2))
	begin 
		insert into @res
		Select	
			--'Реклама в программе ''' + p.[name] + '''' AS NAME,
			replace(replace(replace(@invoiceTableTextSponsor, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), 
			'{группа радиостанций}', IsNull(mm.groupName, '{группа радиостанций}')),  
			'{программа}', p.[name]) as name,
			Sum(i.[tariffPrice] * i.[ratio]) as price,
			sum(case when coalesce(at.divisor, 0) < 0.0000001 then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join SponsorProgram p on i.programID = p.sponsorProgramID 
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPricelist pl on st.priceListID = pl.pricelistID
			inner join vMassmedia mm on c.massmediaID = mm.massmediaID
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)), 112), 112) between at.startDate and at.finishDate
		Where		
			i.campaignID = @campaignID and 
			i.issueDate between DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @beginDate)) and dateadd(ss, -1, DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), dateadd(day, 1, @endDate))))
		GROUP BY 
			p.[name], mm.reportString, mm.groupName
	end 
	else if (@campaignTypeID in (4))
	begin 
		Exec GetPriceByPeriod 
			@campaignId, @campaignTypeID, @beginDate, @endDate, @price out, @withTax = 1, @taxPrice = @taxPrice out
		
		CREATE TABLE #tmp(massmediaID SMALLINT, price money, taxPrice money)--, tariffPrice MONEY)
		INSERT INTO #tmp
		SELECT 
			m.[massmediaID], sum(mpl.[price]),--, i.[tariffPrice]
			sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		FROM [PackModuleIssue] i 
			inner join dbo.Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (pt.IsHidden = 0)
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
		WHERE 
			i.campaignID = @campaignID
			and i.issueDate between @beginDate and @finishDate
		group by m.massmediaID

		declare @sumPrice MONEY
		SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1
		
		INSERT INTO @res 
		select replace(replace(@invoiceTableText, '{строка для счёта/договора}', IsNull(mm.reportString, '{строка для счёта/договора}')), '{группа радиостанций}', IsNull(mm.groupName, '{группа радиостанций}')) as name,
				(@price * sum(t1.price)/ @sumPrice) as price,
				(@taxPrice * sum(t1.price)/ @sumPrice) as taxPrice
		from #tmp as t1
			inner join vMassmedia mm on t1.massmediaID = mm.massmediaID
		group by t1.massmediaID, mm.groupName, mm.reportString
		
		drop table #tmp 
		
	end 		
	
	fetch next from cur_campaigns into @campaignID, @campaignTypeID, @startDate, @finishDate
end

close cur_campaigns
deallocate cur_campaigns

declare @res2 Table (
	[name] NVARCHAR(255),
	[quantity] int NOT null,
	[tax] [MONEY] NOT null,
	[price] [money] not null,
	dirPainting image
)

-- Result
Insert into @res2 ([name], [quantity], [tax], [price])
SELECT 
	[name],
	COUNT(*) AS [quantity],
	sum([taxPrice])	as tax,	
	SUM([price]) AS [price]
FROM 	   
	@res
GROUP BY 
	[name]

Update @res2 Set dirPainting = painting
From Agency Where agencyID = @agencyId

select * from @res2

