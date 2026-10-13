CREATE PROC [dbo].[Grid]
(
    @massmediaID smallint,
    @startDate datetime,
    @finishDate datetime,
    @showUnconfirmed bit,
    @campaignID int = -1,
    @position smallint = 0,
    @moduleID int = null
)
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFIRST 1;
    
    DECLARE @a datetime, @b datetime, @firmId int;
    
    SET @a = dbo.ToShortDate(@startDate);
    SET @b = dbo.ToShortDate(@finishDate);
    --SET @b = DATEADD(day, -1, dbo.ToShortDate(@finishDate));
    
    -- Получаем firmId один раз в начале (если нужен)
    IF @campaignID <> -1
    BEGIN
        SELECT @firmId = a.firmId 
        FROM dbo.Campaign c 
        INNER JOIN dbo.Action a ON a.actionID = c.actionID 
        WHERE c.campaignID = @campaignID;
    END
    
    -- Используем временную таблицу вместо табличной переменной
    CREATE TABLE #res (
        rollerDuration int, 
        windowDateOriginal datetime, 
        timeString varchar(10), 
        [weekday] tinyint, 
        campaignID int, 
        positionId smallint, 
        originalWindowID int, 
        actualWindowID int,
        moduleID int, 
        rollerID int,
        INDEX IX_res_moduleID (moduleID) INCLUDE (rollerDuration, windowDateOriginal, timeString, [weekday], campaignID, positionId, originalWindowID, actualWindowID, rollerID),
        INDEX IX_res_weekday (weekday)
    );
    
    -- Основной запрос с использованием OPTION (RECOMPILE) для адаптивного плана
    -- Окно выпуска — фактическое (actualWindowID: куда выпуск перенёс трафик), правило 07.10.2026,
    -- docs/tasks/window-actual-switch.md. originalWindowID оставлен в выдаче для старого клиента на переходный период.
    INSERT INTO #res
    SELECT 
        r.duration,
        tw.windowDateOriginal,
        CONVERT(varchar(5), tw.windowDateOriginal, 108),
        DATEPART(dw, tw.dayOriginal),
        i.campaignID,
        i.positionId,
        i.originalWindowID,
        i.actualWindowID,
        mi.moduleID,
        i.rollerID
    FROM dbo.Issue i  -- Убрали NOLOCK
        INNER JOIN dbo.TariffWindow tw ON i.actualWindowID = tw.windowId AND tw.massmediaID = @massmediaID
        INNER JOIN dbo.Roller r ON i.rollerID = r.rollerID
        INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID 
        LEFT JOIN dbo.ModuleIssue mi ON i.moduleIssueID = mi.moduleIssueID
    WHERE
        c.massmediaID = @massmediaID 
        AND tw.dayOriginal BETWEEN @a AND @b
        AND (@campaignID = -1 OR i.campaignID = @campaignID)
        AND (i.isConfirmed = 1 OR @showUnconfirmed = 1 OR i.campaignID = @campaignID)
    OPTION (RECOMPILE);
    
    -- Результаты
    SELECT * 
    FROM #res 
    WHERE @moduleID IS NULL OR moduleID = @moduleID;
    
    SELECT [weekday], COUNT(*) AS [count]
    FROM #res
    GROUP BY [weekday];
    
    -- Третий и четвёртый результат только если нужен firmId. Оба строятся из одной выборки
    -- выпусков фирмы на станции за период (раньше — два одинаковых запроса). Выборка идёт от
    -- маленьких списков (кампании фирмы на станции, окна станции за период): оптимизатор
    -- сам выбирает, с какого начать, и не обходит всю историю фирмы с переходами в кластерный
    -- индекс Issue и TariffWindow на каждый выпуск (на проде это давало до 4 с чтения с диска).
    IF @campaignID <> -1 AND @firmId IS NOT NULL
    BEGIN
        CREATE TABLE #firmCampaigns (campaignID int PRIMARY KEY);
        INSERT INTO #firmCampaigns
        SELECT c.campaignID
        FROM dbo.Campaign c
            INNER JOIN dbo.Action a ON a.actionID = c.actionID
        WHERE
            a.firmID = @firmId
            AND c.massmediaID = @massmediaID
            AND a.deleteDate IS NULL;

        CREATE TABLE #periodWindows (windowId int PRIMARY KEY);
        INSERT INTO #periodWindows
        SELECT tw.windowId
        FROM dbo.TariffWindow tw
        WHERE
            tw.massmediaID = @massmediaID
            AND tw.dayOriginal BETWEEN @a AND @b;

        CREATE TABLE #firmIssues (windowId int, rollerID int, positionId smallint, campaignID int);
        INSERT INTO #firmIssues
        SELECT w.windowId, i.rollerID, i.positionId, i.campaignID
        FROM #periodWindows w
            INNER JOIN dbo.Issue i ON i.actualWindowID = w.windowId
            INNER JOIN #firmCampaigns fc ON fc.campaignID = i.campaignID
        WHERE i.isConfirmed = 1 OR @showUnconfirmed = 1;

        -- Третий: окна, где у фирмы есть выпуски (бирюзовые ячейки), включая свою кампанию.
        SELECT DISTINCT windowId
        FROM #firmIssues;

        -- Четвёртый: ролики чужих кампаний той же фирмы по окнам — для номеров
        -- роликов в бирюзовых ячейках (RollerIssuesGrid3.GetRollerNumbersText). Свои
        -- кампании исключены: их ролики грид берёт из первого запроса и держит актуальными
        -- при ручном добавлении/удалении.
        SELECT DISTINCT windowId, rollerID, positionId
        FROM #firmIssues
        WHERE campaignID <> @campaignID;
    END
    
    DROP TABLE #res;
END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Grid] TO PUBLIC
    AS [dbo];

