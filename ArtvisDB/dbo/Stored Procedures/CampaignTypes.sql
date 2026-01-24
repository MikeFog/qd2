
CREATE PROC [dbo].[CampaignTypes]
AS
SET NOCOUNT ON
SELECT 
	ct.*
FROM 
	[iCampaignType] ct
ORDER BY 
	ct.[name]

