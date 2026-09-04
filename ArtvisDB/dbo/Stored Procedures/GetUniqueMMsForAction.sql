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

	-- Раньше отдавала по строке на КАЖДЫЙ выпуск (Client\Classes\MediaPlan.cs:395,
	-- PrintActionInfo, до 1469 строк на сводный медиаплан по паре тысяч выпусков) —
	-- клиент группирует их в MediaPlanCampaignGroups.AddMassmedia(massmediaID, ...,
	-- rollerID, date) по КАЖДОМУ station в отдельности внутри своего agencyID.
	-- date/rollerID при этом клиентом фактически не используются:
	-- MediaPlanMassmedia.CompareTo(MediaPlanMassmedia) в MediaPlanCampaignGroups.cs
	-- захардкожен на `return 1` (реальное сравнение по rollers/days закомментировано),
	-- поэтому накопленные rollers/days ни на что не влияют, а на выход идёт только
	-- (massmediaID, name) — как раз то, что уникально по (massmediaID, agencyID).
	-- ROW_NUMBER берёт по одной представительной строке на пару (massmediaID,
	-- agencyID) вместо всех выпусков этой пары — тот же набор станций на выходе,
	-- на порядки меньше строк.
	;WITH mmRows AS
	(
		SELECT
			mm.[massmediaID],
			mm.[name],
			CASE WHEN @isFact = 1 THEN tw.windowDateActual ELSE tw.windowDateOriginal END as date,
			i.[rollerID],
			c.agencyID,
			ROW_NUMBER() OVER (PARTITION BY mm.[massmediaID], c.agencyID ORDER BY i.issueID) AS rn
		FROM Issue i
			INNER JOIN Campaign c ON c.[campaignID] = i.[campaignID]
			INNER JOIN TariffWindow tw ON tw.windowId = CASE WHEN @isFact = 1 THEN i.actualWindowID ELSE i.originalWindowID END
			INNER JOIN [vMassmedia] mm ON tw.[massmediaID] = mm.[massmediaID]
		WHERE c.actionID = ISNULL(@actionID, c.actionID)
			AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM @act))
	)
	-- WHERE c.actionID = ISNULL(@actionID, c.actionID) - тот же catch-all
	-- паттерн, что чинили в dbo.Firms / MediaPlanRetrieve_v2: один закешированный
	-- план обслуживал и вызов по одной акции, и сводный по набору.
	-- Замеры на ArtvisDev (ROW_NUMBER + RECOMPILE вместе, было / стало):
	--   одна акция 185531 (12572 выпуска)     1980 -> 205 мс
	--   одна акция 184174 (агит, 4374 окна)   1673 ->  85 мс
	--   сводный, 4 акции, isFact=1            1848 ->  49 мс
	--   сводный, 4 акции, isFact=0            1802 ->  45 мс
	-- Эквивалентность: 0 расхождений по множеству (massmediaID,name,agencyID)
	-- на 6 сценариях (одиночные/сводный/isFact 0-1/агитационная акция).
	SELECT [massmediaID], [name], [date], [rollerID], agencyID
	FROM mmRows
	WHERE rn = 1
	OPTION (RECOMPILE);

	Select distinct
		c.massmediaID, mm.name
	From Campaign c
		inner join vMassmedia mm on c.massmediaID = mm.massmediaID
	Where c.actionID = ISNULL(@actionID, c.actionID)
		AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM @act))
		and c.campaignTypeID = 2
END
