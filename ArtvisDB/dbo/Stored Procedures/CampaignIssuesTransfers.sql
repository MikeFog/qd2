
CREATE PROCEDURE [dbo].[CampaignIssuesTransfers]
(
@campaignID int 
)
AS
BEGIN
	SET NOCOUNT ON;

select 
	row_number() over(order by twA.windowDateActual) as RowNum, 
	r.[name], 
	dbo.fn_Int2Time(r.duration) as duration, 
	ip.description as position, 
	twA.windowDateActual,
	twO.windowDateOriginal
from 
	Issue i 
	inner join TariffWindow twO on i.originalWindowID = twO.windowId
	inner join TariffWindow twA on i.actualWindowID = twA.windowId
		and twA.windowDateActual <> twO.windowDateOriginal
	inner join Roller r on i.rollerID = r.rollerID
	inner join iIssuePosition ip on i.positionId = ip.positionId
where 
	i.campaignID = @campaignID
order by 
	twA.windowDateActual
	
END


