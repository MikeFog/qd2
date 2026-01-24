
CREATE PROCEDURE [dbo].[GetUniqueMMsForPackModuleCampaign]
(
	@campaignID int,
	@isFact bit = 1
)
AS
begin
SET NOCOUNT on
	if @isFact = 1
		SELECT 
			mm.[massmediaID], mm.[name], tw.windowDateActual as date, i.[rollerID]
		FROM
			Issue i
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID] 
		WHERE
			i.campaignID = @campaignID
	else 
		SELECT 
			mm.[massmediaID], mm.[name], tw.windowDateOriginal as date, i.[rollerID]
		FROM
			Issue i
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID] 
		WHERE
			i.campaignID = @campaignID
END




