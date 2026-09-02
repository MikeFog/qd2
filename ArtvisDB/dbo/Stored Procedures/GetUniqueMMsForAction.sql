CREATE PROCEDURE [dbo].[GetUniqueMMsForAction]
(
	@actionID int = null,
	@isFact bit = 1,
	@actionIDString varchar(8000) = null   -- набор акций для сводного медиаплана
)
AS
BEGIN
SET NOCOUNT ON

	DECLARE @act TABLE (actionID int NOT NULL PRIMARY KEY);
	IF @actionIDString IS NOT NULL
		INSERT INTO @act(actionID)
		SELECT DISTINCT CAST([ID] AS int)
		FROM fn_CreateTableFromString(@actionIDString);

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
	WHERE c.actionID = ISNULL(@actionID, c.actionID)
		AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM @act))
	Select distinct
		c.massmediaID, mm.name
	From Campaign c
		inner join vMassmedia mm on c.massmediaID = mm.massmediaID
	Where c.actionID = ISNULL(@actionID, c.actionID)
		AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM @act))
		and c.campaignTypeID = 2
END
