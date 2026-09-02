-- =============================================================================
-- Сводный медиаплан «График размещения по нескольким акциям» — процедуры.
--
-- Собран из канонических файлов (при их изменении — пересобрать):
--   ArtvisDB\dbo\Stored Procedures\MultiActionMediaPlanActions.sql   (новая)
--   ArtvisDB\dbo\Stored Procedures\GetUniqueMMsForAction.sql         (изменена)
--   ArtvisDB\dbo\Stored Procedures\MediaPlanRetrieve_v2.sql          (изменена)
--
-- Изменения обратно совместимы: добавлен параметр @actionIDString, старые
-- вызовы (@actionId / @campaignId) не затронуты.
--
-- Запуск (флаг -I обязателен — QUOTED_IDENTIFIER ON):
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i ArtvisDB\Scripts\multi-action-media-plan-procs-deploy.sql
--
-- После процедур — метаданные: ArtvisDB\Scripts\multi-action-media-plan-seed.sql
-- =============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

-- -----------------------------------------------------------------------------
-- 1. MultiActionMediaPlanActions — проверка введённых номеров акций.
--    Возвращает строку на каждую существующую (deleteDate IS NULL) акцию
--    из списка + её фирму-заказчика. Форма ввода сверяет число строк с
--    числом введённых номеров.
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[MultiActionMediaPlanActions]
(
    @actionIDString varchar(8000)
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.actionID,
        a.firmID,
        LTRIM(RTRIM(ISNULL(f.prefix, N'') + N' ' + f.[name])) AS firmName
    FROM fn_CreateTableFromString(@actionIDString) s
        INNER JOIN [dbo].[Action] a ON a.actionID = CAST(s.[ID] AS int) AND a.deleteDate IS NULL
        INNER JOIN [dbo].[Firm] f ON f.firmID = a.firmID
    ORDER BY a.actionID;
END
GO

-- -----------------------------------------------------------------------------
-- 2. GetUniqueMMsForAction — карта агентство/радиостанция для раскладки листов.
--    Добавлен @actionIDString; @actionID стал необязательным.
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[GetUniqueMMsForAction]
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
GO

-- -----------------------------------------------------------------------------
-- 3. MediaPlanRetrieve_v2 — данные медиаплана. Добавлен @actionIDString:
--    #act через fn_CreateTableFromString, фильтр рядом с существующим
--    ISNULL(@actionID, ...) в обеих ветках #issue и в блоке 5 (ProgramIssues).
-- -----------------------------------------------------------------------------
CREATE OR ALTER PROCEDURE [dbo].[MediaPlanRetrieve_v2]
(
    @campaignId int = null,
    @campaignTypeId tinyint = null,
    @isFact bit = 1,
    @massmediaIDString VARCHAR(8000) = null,
    @year smallint = null,
    @month tinyint = null,
    @actionId int = null,
    @startDate datetime = null,
    @finishDate datetime = null,
    @onlyRollers bit = 0,
    @rollerIDString VARCHAR(8000) = null,
    @agencyId int = null,                         -- опциональный фильтр по агентству
    @actionIDString VARCHAR(8000) = null          -- набор акций для сводного медиаплана
)
AS
BEGIN
    SET NOCOUNT ON;

    --------------------------------------------------------------------
    -- #mm, #rr
    --------------------------------------------------------------------
    CREATE TABLE #mm (massmediaID int NOT NULL PRIMARY KEY);
    CREATE TABLE #rr (rollerID int NOT NULL PRIMARY KEY);
    CREATE TABLE #act (actionID int NOT NULL PRIMARY KEY);

    IF @actionIDString IS NOT NULL
        INSERT INTO #act(actionID)
        SELECT DISTINCT CAST([ID] AS int)
        FROM fn_CreateTableFromString(@actionIDString);

    IF @massmediaIDString IS NOT NULL
        INSERT INTO #mm(massmediaID)
        SELECT CAST([ID] AS int)
        FROM fn_CreateTableFromString(@massmediaIDString);
    ELSE
        INSERT INTO #mm(massmediaID)
        SELECT [massmediaID]
        FROM [MassMedia];

    IF @rollerIDString IS NOT NULL
        INSERT INTO #rr(rollerID)
        SELECT CAST([ID] AS int)
        FROM fn_CreateTableFromString(@rollerIDString);

    --------------------------------------------------------------------
    -- Месячный диапазон (считаем 1 раз)
    --------------------------------------------------------------------
    DECLARE @monthStart datetime = NULL;
    DECLARE @monthEndExcl datetime = NULL;

    IF @year IS NOT NULL AND @month IS NOT NULL
    BEGIN
        SET @monthStart = CONVERT(datetime, '01.' + CAST(@month AS varchar(2)) + '.' + CAST(@year AS varchar(4)), 104);
        SET @monthEndExcl = DATEADD(day, 1, dbo.fn_LastDateOfMonth(@monthStart));
    END

    --------------------------------------------------------------------
    -- #issue вместо @issue
    --------------------------------------------------------------------
    CREATE TABLE #issue
    (
        issueID INT NOT NULL,
        rollerId int NULL,
        issueDate datetime NOT NULL,
        comment nvarchar(32) NULL,
        positionID SMALLINT NULL,
        price decimal(18,2) NULL,
        broadcast datetime NULL,
        mmID smallint NOT NULL,
        timeString varchar(5) NULL,
        shiftedDate datetime NULL,
        radioDate datetime NULL
    );

    CREATE CLUSTERED INDEX CX_issue_issueDate ON #issue(issueDate);
    CREATE NONCLUSTERED INDEX IX_issue_roller ON #issue(rollerId);
    CREATE NONCLUSTERED INDEX IX_issue_mmID ON #issue(mmID);

    --------------------------------------------------------------------
    -- Заполнение #issue
    --------------------------------------------------------------------
    IF @isFact = 1
    BEGIN
        INSERT INTO #issue (issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID)
        SELECT
            i.issueID,
            i.rollerId,
            tw.windowDateActual,
            MAX(COALESCE(t.comment, N'')),
            i.positionId,
            tw.price,
            pl1.broadcastStart,
            tw.massmediaID
        FROM Issue i WITH (NOLOCK)
        INNER JOIN TariffWindow tw ON i.actualWindowID = tw.windowId
        INNER JOIN #mm mmm ON tw.massmediaID = mmm.massmediaID
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
        LEFT  JOIN Tariff t ON t.tariffId = tw.tariffId
        LEFT  JOIN #rr rr ON i.rollerID = rr.rollerID
        OUTER APPLY
        (
            SELECT TOP (1) pl.broadcastStart
            FROM dbo.Pricelist pl
            WHERE pl.massmediaID = tw.massmediaID
              AND pl.startDate <= tw.dayActual
              AND pl.finishDate >= tw.dayActual
            ORDER BY pl.startDate DESC
        ) pl1
        WHERE
            i.campaignId = ISNULL(@campaignId, i.campaignID)
            AND c.actionID = ISNULL(@actionID, c.actionID)
            AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM #act))
            AND c.agencyID = ISNULL(@agencyId, c.agencyID)        -- фильтр по агентству
            AND (@rollerIDString IS NULL OR rr.rollerID IS NOT NULL)
            AND (@monthStart IS NULL OR (tw.dayActual >= @monthStart AND tw.dayActual < @monthEndExcl))
            AND ((@startDate IS NULL AND @finishDate IS NULL) OR (tw.dayActual BETWEEN @startDate AND @finishDate))
        GROUP BY
            i.issueID, i.rollerId, tw.windowDateActual, tw.price, i.positionId, tw.massmediaID, pl1.broadcastStart;
    END
    ELSE
    BEGIN
        INSERT INTO #issue (issueID, rollerId, issueDate, comment, positionID, price, broadcast, mmID)
        SELECT
            i.issueID,
            i.rollerId,
            tw.windowDateOriginal,
            MAX(COALESCE(t.comment, N'')),
            i.positionId,
            tw.price,
            pl1.broadcastStart,
            tw.massmediaID
        FROM Issue i WITH (NOLOCK)
        INNER JOIN TariffWindow tw ON i.originalWindowID = tw.windowId
        INNER JOIN #mm mmm ON tw.massmediaID = mmm.massmediaID
        INNER JOIN Campaign c ON i.campaignID = c.campaignID
        LEFT  JOIN Tariff t ON t.tariffId = tw.tariffId
        LEFT  JOIN #rr rr ON i.rollerID = rr.rollerID
        OUTER APPLY
        (
            SELECT TOP (1) pl.broadcastStart
            FROM dbo.Pricelist pl
            WHERE pl.massmediaID = tw.massmediaID
              AND pl.startDate <= tw.dayOriginal
              AND pl.finishDate >= tw.dayOriginal
            ORDER BY pl.startDate DESC
        ) pl1
        WHERE
            i.campaignId = ISNULL(@campaignId, i.campaignID)
            AND c.actionID = ISNULL(@actionID, c.actionID)
            AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM #act))
            AND c.agencyID = ISNULL(@agencyId, c.agencyID)        -- фильтр по агентству
            AND (@rollerIDString IS NULL OR rr.rollerID IS NOT NULL)
            AND (@monthStart IS NULL OR (tw.dayOriginal >= @monthStart AND tw.dayOriginal < @monthEndExcl))
            AND ((@startDate IS NULL AND @finishDate IS NULL) OR (tw.dayOriginal BETWEEN @startDate AND @finishDate))
        GROUP BY
            i.issueID, i.rollerId, tw.windowDateOriginal, tw.price, i.positionId, tw.massmediaID, pl1.broadcastStart;
    END

    --------------------------------------------------------------------
    -- @massmediaCount считаем по фактической выборке
    --------------------------------------------------------------------
    DECLARE @massmediaCount int = NULL;
    SELECT @massmediaCount = COUNT(DISTINCT mmID) FROM #issue;
    IF @massmediaCount IS NULL OR @massmediaCount = 0 SET @massmediaCount = 1;

    --------------------------------------------------------------------
    -- Вычисляем timeString / shiftedDate / radioDate (один раз)
    --------------------------------------------------------------------
    UPDATE i
    SET
        shiftedDate = DATEADD(minute,
                        - (DATEPART(hour, i.broadcast) * 60 + DATEPART(minute, i.broadcast)),
                        i.issueDate),
        radioDate =
            CONVERT(datetime,
                CONVERT(varchar(8),
                    DATEADD(minute,
                        - (DATEPART(hour, i.broadcast) * 60 + DATEPART(minute, i.broadcast)),
                        i.issueDate
                    ),
                112), 112),
        timeString =
        (
            RIGHT('0' + CAST(
                (DATEPART(hour, i.issueDate) +
                    CASE WHEN (DATEPART(hour, i.issueDate) * 60 + DATEPART(minute, i.issueDate))
                              < (DATEPART(hour, i.broadcast) * 60 + DATEPART(minute, i.broadcast))
                         THEN 24 ELSE 0 END
                ) AS varchar(3)), 2)
            + ':'
            + RIGHT('0' + CAST(DATEPART(minute, i.issueDate) AS varchar(2)), 2)
        )
    FROM #issue i;

    --------------------------------------------------------------------
    -- 1) Ролики
    --------------------------------------------------------------------
    SELECT
        r.rollerId,
        r.[name],
        r.advertTypeName,
        r.duration,
        COUNT(*) AS quantity
    FROM #issue i
    INNER JOIN vRoller r ON i.rollerId = r.rollerId
    GROUP BY r.[name], r.advertTypeName, r.duration, r.rollerId;

    IF @onlyRollers = 1
        RETURN;

    --------------------------------------------------------------------
    -- 2) Тайм-слоты
    --------------------------------------------------------------------
    IF @campaignTypeId IS NOT NULL AND @campaignTypeId = 1
    BEGIN
        SELECT
            i.timeString AS [time],
            MAX(i.comment) AS comment,
            i.price,
            SUM(r.duration) / @massmediaCount AS totalDuration
        FROM #issue i
        INNER JOIN Roller r ON i.rollerId = r.rollerId
        GROUP BY i.price, i.timeString
        ORDER BY i.timeString;
    END
    ELSE
    BEGIN
        SELECT
            i.timeString AS [time],
            MAX(i.comment) AS comment,
            SUM(r.duration) / @massmediaCount AS totalDuration
        FROM #issue i
        INNER JOIN Roller r ON i.rollerId = r.rollerId
        GROUP BY i.timeString
        ORDER BY i.timeString;
    END

    --------------------------------------------------------------------
    -- 3) Детализация
    --------------------------------------------------------------------
    IF @campaignTypeId IS NULL OR @campaignTypeId = 4
    BEGIN
        SELECT
            i.rollerId,
            i.radioDate AS issueDate,
            i.timeString AS [time],
            i.positionID
        FROM #issue i
        WHERE i.mmID = (SELECT TOP 1 massmediaID FROM #mm ORDER BY massmediaID)
        ORDER BY i.radioDate, i.timeString;
    END
    ELSE IF @campaignTypeId IS NULL OR @campaignTypeId = 1
    BEGIN
        SELECT
            i.rollerId,
            i.radioDate AS issueDate,
            i.timeString AS [time],
            i.price,
            i.positionID
        FROM #issue i
        ORDER BY i.radioDate, i.timeString;
    END
    ELSE
    BEGIN
        SELECT
            i.rollerId,
            i.radioDate AS issueDate,
            i.timeString AS [time],
            i.positionID
        FROM #issue i
        ORDER BY i.radioDate, i.timeString;
    END

    --------------------------------------------------------------------
    -- 4) Счётчики по дням
    --------------------------------------------------------------------
    IF @monthStart IS NOT NULL
    BEGIN
        ;WITH d AS
        (
            SELECT 1 AS [day]
            UNION ALL SELECT [day] + 1
            FROM d
            WHERE [day] < DAY(dbo.fn_LastDateOfMonth(@monthStart))
        ),
        c AS
        (
            SELECT
                DAY(shiftedDate) AS [day],
                COUNT(*) AS cnt
            FROM #issue
            GROUP BY DAY(shiftedDate)
        )
        SELECT ISNULL(c.cnt, 0) AS c
        FROM d
        LEFT JOIN c ON c.[day] = d.[day]
        ORDER BY d.[day]
        OPTION (MAXRECURSION 100);
    END
    ELSE
    BEGIN
        SELECT
            COUNT(i.rollerId) AS [COUNT]
        FROM #issue i
        GROUP BY i.radioDate
        ORDER BY i.radioDate;
    END

    --------------------------------------------------------------------
    -- 5) ProgramIssues
    --------------------------------------------------------------------
    IF (@campaignTypeId IS NOT NULL AND @campaignTypeId = 2) OR (@actionId IS NOT NULL) OR (@actionIDString IS NOT NULL)
    BEGIN
        SELECT
            sp.[name],
            pri.issueDate
        FROM ProgramIssue pri
        INNER JOIN SponsorProgram sp ON pri.programID = sp.sponsorProgramID
        INNER JOIN Campaign c ON pri.campaignID = c.campaignID
        INNER JOIN #mm mm ON c.massmediaID = mm.massmediaID
        WHERE pri.campaignId = COALESCE(@campaignID, pri.campaignID)
          AND c.actionID = COALESCE(@actionID, c.actionID)
          AND (@actionIDString IS NULL OR c.actionID IN (SELECT actionID FROM #act))
          AND c.agencyID = COALESCE(@agencyId, c.agencyID);      -- фильтр по агентству
    END
END
GO

PRINT 'Сводный медиаплан: процедуры обновлены (MultiActionMediaPlanActions, GetUniqueMMsForAction, MediaPlanRetrieve_v2).';
GO
