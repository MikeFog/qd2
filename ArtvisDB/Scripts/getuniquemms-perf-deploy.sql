/*
    ПРОД-ДЕПЛОЙ: dbo.GetUniqueMMsForAction — DISTINCT-по-станции + OPTION (RECOMPILE).
    Ветка hotfix/getuniquemms-perf.

    ПОВОД
      Побочная находка при разборе перф-инцидента медиаплана 31.08-04.09.2026
      (см. hotfix/mediaplan-v2-perf). После N+1-фикса в MediaPlan.PrintFooter
      следующим по времени в логе сводного медиаплана осталась
      GetUniqueMMsForAction: 1 вызов, 1,8 с, rows=1469 (по числу выпусков акций).

    ЧТО БЫЛО
      Процедура отдавала ПО СТРОКЕ НА КАЖДЫЙ ВЫПУСК акции(й) — до 1469 строк на
      сводный медиаплан. Client\Classes\MediaPlan.cs:395 (PrintActionInfo)
      скармливает каждую строку в MediaPlanCampaignGroups.AddMassmedia(massmediaID,
      name, rollerID, date), который группирует по agencyID и извлекает
      уникальные станции через GetUniqueMassmedias().

      date/rollerID клиентом фактически не используются: MediaPlanMassmedia.
      CompareTo(MediaPlanMassmedia) в MediaPlanCampaignGroups.cs захардкожен на
      `return 1` (настоящее сравнение по накопленным rollers/days закомментировано),
      поэтому вся эта агрегация ни на что не влияет — на выход идёт только
      (massmediaID, name), уникальные по паре (massmediaID, agencyID). Единственный
      вызыватель процедуры в коде - Client\Classes\MediaPlan.cs:395.

    ЧТО СТАЛО
      1) ROW_NUMBER() OVER (PARTITION BY massmediaID, agencyID ORDER BY issueID) = 1
         вместо всех строк по (massmediaID, agencyID) - одна представительная
         строка на пару вместо всех выпусков. Тот же набор станций на выходе,
         на порядки меньше строк.
      2) OPTION (RECOMPILE) на первый SELECT: WHERE c.actionID = ISNULL(@actionID,
         c.actionID) - тот же catch-all паттерн, что чинили в dbo.Firms и
         MediaPlanRetrieve_v2 (один закешированный план на одиночный вызов и на
         сводный по набору акций).
      Второй SELECT (SELECT DISTINCT ... campaignTypeID = 2) не тронут - он и так
      маленький и уже DISTINCT.

    ЗАМЕРЫ (ArtvisDev, было -> стало, оба фикса вместе)
      одна акция 185531 (12572 выпуска)     1980 -> 205 мс
      одна акция 184174 (агит, 4374 окна)   1673 ->  85 мс
      одна акция 173666                     1679 -> 174 мс
      одна акция 176888, isFact=0           1725 -> 103 мс
      сводный, 4 акции, isFact=1            1848 ->  49 мс
      сводный, 4 акции, isFact=0            1802 ->  45 мс

    ЭКВИВАЛЕНТНОСТЬ
      6 сценариев (см. замеры выше), сравнение по множеству (massmediaID, name,
      agencyID) старой и новой формы из идентичного состояния БД: 0 расхождений.
      См. ArtvisDB/Scripts/getuniquemms-perf-check.sql - готовит *_old копию,
      прогоняет то же сравнение, можно повторить на проде перед деплоем.

    QUOTED_IDENTIFIER ON
      Процедура ничего не пишет, но деплоится с QI ON / ANSI_NULLS ON для
      консистентности с остальными процедурами в этой серии фиксов.

    ИДЕМПОТЕНТНОСТЬ  повторный запуск перезаливает то же тело.
    ОТКАТ  git show master:"ArtvisDB/dbo/Stored Procedures/GetUniqueMMsForAction.sql"
      (взять версию ДО этой ветки, ALTER, те же два батча SET.../GO).

    ЗАПУСК
      sqlcmd -S <прод-сервер> -d <прод-БД> -E -b -I -i getuniquemms-perf-deploy.sql
      либо открыть в SSMS на нужной БД и выполнить целиком.
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO

/* ── Преполёт ─────────────────────────────────────────────────────────── */
IF OBJECT_ID('dbo.GetUniqueMMsForAction') IS NULL
   OR OBJECT_ID('dbo.Issue') IS NULL
   OR OBJECT_ID('dbo.vMassmedia') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: не найдены dbo.GetUniqueMMsForAction / Issue / vMassmedia. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
PRINT 'БД     : ' + DB_NAME();
PRINT 'Сервер : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
PRINT 'GetUniqueMMsForAction до: ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.GetUniqueMMsForAction')) LIKE '%ROW_NUMBER%'
                                          THEN 'уже с DISTINCT-по-станции' ELSE 'старая (строка на каждый выпуск)' END;
GO

/* ── ALTER dbo.GetUniqueMMsForAction ─────────────────────────────────────── */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
ALTER PROCEDURE [dbo].[GetUniqueMMsForAction]
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
GO

/* ── Проверка ─────────────────────────────────────────────────────────── */
SET NOEXEC OFF;
GO
SELECT
    [процедура]           = o.name,
    [quoted_identifier]   = m.uses_quoted_identifier,   -- ожидается 1
    [ansi_nulls]          = m.uses_ansi_nulls,          -- ожидается 1
    [есть_ROW_NUMBER]     = CASE WHEN m.definition LIKE '%ROW_NUMBER%' THEN 1 ELSE 0 END,      -- ожидается 1
    [есть_RECOMPILE]      = CASE WHEN m.definition LIKE '%OPTION (RECOMPILE)%' THEN 1 ELSE 0 END, -- ожидается 1
    [изменена]            = o.modify_date
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name = 'GetUniqueMMsForAction';
GO
PRINT 'Готово. Ожидается: quoted_identifier=1, ansi_nulls=1, есть_ROW_NUMBER=1, есть_RECOMPILE=1.';
