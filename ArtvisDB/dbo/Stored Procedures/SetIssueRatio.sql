/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE     PROC [dbo].[SetIssueRatio]
(
@campaignID int, 
@campaignTypeID int,
@startDate datetime, 
@finishDate datetime,
@ratio float
)
As
Set Nocount On
DECLARE @issue TABLE(issueID int)
Set	@startDate = Convert(datetime, Convert(varchar(8), @startDate, 112), 112)
Set	@finishDate = Convert(datetime, Convert(varchar(8), @finishDate, 112), 112)

INSERT @issue
SELECT i.issueID
FROM Issue i
	JOIN TariffWindow tw ON	tw.windowId = i.originalWindowID
WHERE i.campaignId = @campaignID  
		and tw.dayOriginal between @startDate and @finishDate

Update	Issue WITH (ROWLOCK)
Set		ratio = @ratio
WHERE issueID IN (SELECT issueID FROM @issue)

If @campaignTypeID = 2
	Update	i
	Set		i.Ratio = @ratio
	from programIssue i inner join Campaign c on i.campaignId = @campaignID and c.campaignID = i.campaignID
		inner join SponsorTariff st on i.tariffID = st.tariffID
		inner join SponsorProgramPriceList pl on st.pricelistID = pl.pricelistID
	where i.issueDate between DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @startDate))
					and DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @finishDate))

If @campaignTypeID = 3
	Update	moduleIssue
	Set		ratio = @ratio
	Where	campaignId = @campaignID and 
			issueDate between @startDate and @finishDate
			
IF @campaignTypeID = 4
	Update	[PackModuleIssue]
	Set		[ratio] = @ratio
	Where	[campaignID] = @campaignID and 
			[issueDate] between @startDate and @finishDate


