

CREATE    PROC [dbo].[ReportPartTexts]
(
@reportPartTextID int = NULL
)
as
set nocount on

select 
	rpt.reportPartTextID, rpt.reportText, rpt.codeName, rt.name as reportName, rpt.[description]
from 
	[dbo].[ReportPartText] rpt 
	INNER JOIN [dbo].[ReportType] rt ON rpt.reportTypeID = rt.reportTypeID
WHERE 
	reportPartTextID = COALESCE(@reportPartTextID, reportPartTextID)
Order by 
	rt.name,
	rpt.description



