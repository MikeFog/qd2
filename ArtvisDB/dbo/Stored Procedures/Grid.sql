
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
    SET @b = DATEADD(day, -1, dbo.ToShortDate(@finishDate));
    
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
        moduleID int, 
        rollerID int,
        INDEX IX_res_moduleID (moduleID) INCLUDE (rollerDuration, windowDateOriginal, timeString, [weekday], campaignID, positionId, originalWindowID, rollerID),
        INDEX IX_res_weekday (weekday)
    );
    
    -- Основной запрос с использованием OPTION (RECOMPILE) для адаптивного плана
    INSERT INTO #res
    SELECT 
        r.duration,
        tw.windowDateOriginal,
        CONVERT(varchar(5), tw.windowDateOriginal, 108),
        DATEPART(dw, tw.dayOriginal),
        i.campaignID,
        i.positionId,
        i.originalWindowID,
        mi.moduleID,
        i.rollerID
    FROM dbo.Issue i  -- Убрали NOLOCK
        INNER JOIN dbo.TariffWindow tw ON i.originalWindowID = tw.windowId
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
    
    -- Третий запрос только если нужен firmId
    IF @campaignID <> -1 AND @firmId IS NOT NULL
    BEGIN
        SELECT DISTINCT tw.windowId
        FROM dbo.Issue i
            INNER JOIN dbo.TariffWindow tw ON i.originalWindowID = tw.windowId
            INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID 
            INNER JOIN dbo.Action a ON a.actionID = c.actionID
        WHERE
            c.massmediaID = @massmediaID 
            AND tw.dayOriginal BETWEEN @a AND @b
            AND a.firmID = @firmId
            AND (i.isConfirmed = 1 OR @showUnconfirmed = 1)
            AND a.deleteDate IS NULL;
    END
    
    DROP TABLE #res;
END
