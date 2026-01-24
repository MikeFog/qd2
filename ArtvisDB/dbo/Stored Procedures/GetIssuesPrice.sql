/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE           Procedure [dbo].[GetIssuesPrice]
(
@campaignID int, 
@campaignTypeID int,								
@startDate datetime, 
@finishDate DATETIME,
@price money = 0 out
)
as
SET NOCOUNT on
SET @startDate = dbo.ToShortDate(@startDate)
SET @finishDate = dbo.ToShortDate(@finishDate)
	
If @campaignTypeID = 1	Begin
	Select	
		@price = Sum(i.[tariffPrice])
	From		
		Issue i
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	Where	
		i.campaignID = @campaignID	and
		tw.dayOriginal between @startDate and @finishDate 
End

Else If @campaignTypeID = 2 Begin
	Select	
		@price = Sum(i.[tariffPrice])
	From		
		ProgramIssue i 
		inner join SponsorTariff st on i.tariffID = st.tariffID
		inner join SponsorProgramPriceList pl on pl.priceListID = st.priceListID
	Where	
		i.campaignID = @campaignID	and
		i.issueDate between DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @startDate)) 
			and DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), dateadd(day, 1, @finishDate))) 
End				

Else If @campaignTypeID = 3 Begin
	Select	
		@price = Sum(i.[tariffPrice])
	From		
		ModuleIssue i 
	Where	
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate
End				

Else If @campaignTypeID = 4 Begin
	Select	
		@price = Sum(i.[tariffPrice])
	From		
		[PackModuleIssue] i 
	Where	
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate
End				





















