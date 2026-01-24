CREATE PROC [dbo].[CampaignModulesRetrieve]
(
@campaignId INT,
@moduleID INT = NULL 
)
AS
SET NOCOUNT ON

SELECT DISTINCT 
	m.[name], m.[moduleID], @campaignId AS campaignId, a.deleteDate
FROM 
	[ModuleIssue] i
	INNER JOIN [Module] m ON i.[moduleID] = m.[moduleID]
	Inner Join Campaign c On c.campaignID = i.campaignID
	Inner Join Action a On a.actionID = c.actionID
WHERE 
	i.[campaignID] = @campaignId
	AND m.[moduleID] = ISNULL(@moduleID, m.[moduleID])
ORDER BY
	m.[name]

