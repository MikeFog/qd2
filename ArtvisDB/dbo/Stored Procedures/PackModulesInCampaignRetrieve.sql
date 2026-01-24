CREATE PROC [dbo].[PackModulesInCampaignRetrieve]
(
@campaignId INT,
@packModuleID INT = NULL
)
AS
SET NOCOUNT ON

SELECT DISTINCT
	m.[packModuleID],
	m.[name],
	@campaignId AS campaignId,
	a.deleteDate
FROM 
	[PackModuleIssue] i
	INNER JOIN [PackModulePriceList] pl ON i.[pricelistID] = pl.[priceListID]
	INNER JOIN [PackModule] m ON pl.[packModuleID] = m.[packModuleID]
	Inner Join Campaign c On c.campaignID = i.campaignID
	Inner Join Action a on c.actionID = a.actionID
WHERE 
	i.[campaignID] = @campaignId
	AND m.packModuleID = COALESCE(@packModuleID, m.packModuleID)
ORDER BY
	m.[name]

