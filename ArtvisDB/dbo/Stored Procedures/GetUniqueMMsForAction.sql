CREATE PROCEDURE [dbo].[GetUniqueMMsForAction]
(
	@actionID int,
	@isFact bit = 1
)
AS
BEGIN
SET NOCOUNT ON
	SELECT 
		mm.[massmediaID],
		mm.[name],
		CASE WHEN @isFact = 1 THEN tw.windowDateActual ELSE tw.windowDateOriginal END as date,
		i.[rollerID],
		c.agencyID
	FROM Issue i
		INNER JOIN Campaign c ON c.[campaignID] = i.[campaignID]
		INNER JOIN TariffWindow tw ON tw.windowId = CASE WHEN @isFact = 1 THEN i.actualWindowID ELSE i.originalWindowID END
		INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID]
	WHERE c.actionID = @actionID
	Select distinct 
		c.massmediaID, mm.name
	From Campaign c
		inner join vMassmedia mm on c.massmediaID = mm.massmediaID
	Where c.actionID = @actionID and c.campaignTypeID = 2
END