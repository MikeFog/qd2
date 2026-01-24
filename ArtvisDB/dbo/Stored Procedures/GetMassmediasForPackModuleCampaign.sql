
CREATE PROCEDURE [dbo].[GetMassmediasForPackModuleCampaign]
(
	@campaignID INT
)
AS
BEGIN
	SET NOCOUNT ON;
	SELECT DISTINCT
		mm.massmediaID AS packmodulemassmediaID,
		mm.[name] AS massmediaName
	FROM 
		[Campaign] cm
		INNER JOIN [PackModuleIssue] pmi ON pmi.campaignID = cm.campaignID
		INNER JOIN [PackModuleContent] pmc ON pmc.pricelistID = pmi.pricelistID
		INNER JOIN Module m ON m.moduleID = pmc.moduleID
		INNER JOIN vMassMedia mm ON mm.massmediaID = m.massmediaID
	where cm.[campaignTypeID] = 4 and 
		cm.campaignID = @campaignID
END

