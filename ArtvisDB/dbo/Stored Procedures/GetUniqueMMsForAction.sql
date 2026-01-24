
CREATE PROCEDURE [dbo].[GetUniqueMMsForAction]
(
	@actionID int,
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
			INNER JOIN Campaign c ON c.[campaignID] = i.[campaignID]
			INNER JOIN [Action] a ON c.actionID = a.actionID
			INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID] 
		WHERE
			c.actionID = @actionID
	else 
		SELECT 
			mm.[massmediaID], mm.[name], tw.windowDateOriginal as date, i.[rollerID]
		FROM
			Issue i
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			INNER JOIN Campaign c ON c.[campaignID] = i.[campaignID]
			INNER JOIN [Action] a ON c.actionID = a.actionID
			INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID] 
		WHERE
			c.actionID = @actionID
			
	Select distinct 
		c.massmediaID, mm.name
	From Campaign c
		inner join vMassmedia mm on c.massmediaID = mm.massmediaID
	Where c.actionID = @actionID and c.campaignTypeID = 2
END

