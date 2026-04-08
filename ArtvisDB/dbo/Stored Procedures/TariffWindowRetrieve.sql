
CREATE PROC [dbo].[TariffWindowRetrieve]
(
    @pricelistId int = null,
    @broadcastStart datetime = null,
    @startDate datetime = null,
    @finishDate datetime = null,
    @moduleId int = null,
    @windowId int = null,
    @actualDate datetime = NULL,
    @windowDateActual DATETIME = NULL,
    @windowDateOriginal DATETIME = NULL,
    @excludeSpecialWindows BIT = 0,
    @excludeModuleTariffs BIT = 0,
    @massmediaID INT = NULL,
    @showTrafficWindows BIT = 0,
    @showDisabledWindows bit = 1
)
AS
BEGIN
    SET NOCOUNT ON;
    -- Предотвращаем дедлоки при чтении
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    DECLARE @broadcasrStartHour tinyint;
    IF @broadcastStart IS NOT NULL 
        SET @broadcasrStartHour = DATEPART(hh, @broadcastStart);
    ELSE
        SET @broadcasrStartHour = 0;

    -- Используем временную таблицу вместо @tmpWindow для корректной статистики
    CREATE TABLE #tmpWindow (windowId int PRIMARY KEY);

    -------------------------------------------------------------------------
    -- 1. Наполнение списка ID окон
    -------------------------------------------------------------------------
    
    -- Точечный поиск по датам
    IF @windowDateActual IS NOT NULL AND @windowDateOriginal IS NOT NULL AND @massmediaID IS NOT NULL 
    BEGIN
        INSERT INTO #tmpWindow (windowId)
        SELECT windowID 
        FROM [TariffWindow]
        WHERE [windowDateActual] = @windowDateActual 
          AND [windowDateOriginal] = @windowDateOriginal
          AND massmediaID = @massmediaID;
    END
    -- Поиск по конкретному ID
    ELSE IF @windowId IS NOT NULL
    BEGIN
        INSERT INTO #tmpWindow (windowId) VALUES (@windowId);
    END
    -- Поиск по вхождению времени в длительность окна
    Else If @actualDate Is Not Null
    BEGIN
        INSERT INTO #tmpWindow (windowId)
        SELECT TOP 1 windowId
        FROM TariffWindow
        WHERE massmediaID = @massmediaID 
          AND windowDateActual <= @actualDate -- Это SARGable условие (быстрый поиск по индексу)
          AND DATEADD(s, duration, windowDateActual) >= @actualDate -- Проверка только для одной строки
        ORDER BY windowDateActual DESC; -- Берем самое близкое к моменту
    END
    -- Основной поиск: Модуль НЕ указан
    ELSE IF @moduleId IS NULL
    BEGIN
        INSERT INTO #tmpWindow (windowId)
        SELECT tw.windowId
        FROM TariffWindow tw
        INNER JOIN Tariff t ON t.tariffId = tw.tariffId
        WHERE (@excludeModuleTariffs = 0 OR t.[isForModuleOnly] = 0)
          AND (@pricelistId IS NULL OR t.pricelistId = @pricelistId)
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)
          AND (tw.isDisabled = 0 OR @showDisabledWindows = 1)

        UNION ALL

        SELECT DISTINCT tw.windowId
        FROM TariffWindow tw
        INNER JOIN [Pricelist] pl ON pl.[massmediaID] = tw.massmediaID 
            AND tw.dayOriginal BETWEEN pl.startDate AND pl.finishDate
        WHERE tw.tariffID IS NULL 
          AND @showTrafficWindows = 1 
          AND @excludeSpecialWindows = 0 
          AND (@pricelistId IS NULL OR pl.pricelistId = @pricelistId)
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)
          AND (tw.isDisabled = 0 OR @showDisabledWindows = 1);
    END
    -- Основной поиск: Модуль указан
    ELSE
    BEGIN
        INSERT INTO #tmpWindow (windowId)
        SELECT tw.windowId
        FROM TariffWindow tw
        INNER JOIN ModuleTariff mt ON mt.tariffId = tw.tariffId
        INNER JOIN ModulePriceList mpl ON mpl.modulePriceListID = mt.modulePriceListID
        WHERE mpl.moduleId = @moduleId
          AND mpl.pricelistId = @pricelistId
          AND mpl.startDate <= @finishDate AND mpl.finishDate >= @startDate
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)

        UNION ALL

        SELECT DISTINCT tw.windowId
        FROM TariffWindow tw
        INNER JOIN [Pricelist] pl ON pl.[massmediaID] = tw.massmediaID
            AND tw.dayOriginal BETWEEN pl.startDate AND pl.finishDate
        INNER JOIN [Module] m ON tw.massmediaID = m.[massmediaID] AND m.moduleId = @moduleId
        INNER JOIN ModulePriceList mpl ON mpl.priceListID = pl.priceListID
        WHERE tw.tariffID IS NULL 
          AND @showTrafficWindows = 1  
          AND @excludeSpecialWindows = 0 
          AND (@pricelistId IS NULL OR pl.pricelistId = @pricelistId)
          AND mpl.startDate <= @finishDate AND mpl.finishDate >= @startDate
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate);
    END

    -------------------------------------------------------------------------
    -- 2. Формирование финальных наборов данных
    -------------------------------------------------------------------------

    IF @excludeSpecialWindows = 0    
    BEGIN
        -- Формируем временный набор (без бесполезного ORDER BY внутри SELECT INTO)
        SELECT
            tw.*,
            'Рекламное окно ' + CONVERT(varchar(10), windowDateOriginal, 104) + ' ' + 
                CONVERT(varchar(5), windowDateOriginal, 108)
                + CASE WHEN windowDateOriginal != windowDateActual 
                       THEN ' (' + CONVERT(varchar(10), windowDateOriginal, 104) + ' ' + CONVERT(varchar(5), windowDateActual, 108) + ')' 
                       ELSE '' END AS [name],
            DATEPART(hh, windowDateOriginal) AS [hour],
            DATEPART(mi, windowDateOriginal) AS [min],
            dayOriginal AS windowDateBroadcast,
            dayActual  AS windowDateActualBroadcast
        INTO #final1 
        FROM #tmpWindow ttw
        INNER JOIN TariffWindow tw ON ttw.windowId = tw.windowId;

        -- Первый результат (сетка часов)
        SELECT DISTINCT    
            [hour],
            [min],
            price,
            CASE WHEN [hour] >= @broadcasrStartHour THEN 0 ELSE 1 END AS flag
        FROM #final1 
        ORDER BY flag, [hour], [min];

        -- Второй результат (список окон)
        SELECT f.*, 
            CASE WHEN tu.tariffID IS NULL THEN 0 ELSE 1 END AS IsTariffUnited 
        FROM #final1 f
        LEFT JOIN TariffUnion tu ON (f.tariffId = tu.tariffID OR f.tariffId = tu.tariffUnionID)
        ORDER BY f.windowDateOriginal DESC;
        
        DROP TABLE #final1;
    END
    ELSE    
    BEGIN
        SELECT
            tw.*,
            'Рекламное окно ' + CONVERT(varchar(10), windowDateOriginal, 104) + ' ' + 
                CONVERT(varchar(5), windowDateOriginal, 108) AS [name],
            DATEPART(hh, windowDateOriginal) AS [hour],
            DATEPART(mi, windowDateOriginal) AS [min],
            dayOriginal AS windowDateBroadcast,
            dayActual AS windowDateActualBroadcast
        INTO #final2           
        FROM #tmpWindow ttw
        INNER JOIN TariffWindow tw ON ttw.windowId = tw.windowId;
        
        -- Первый результат
        SELECT DISTINCT    
            [hour],
            [min],
            price,
            CASE WHEN [hour] >= @broadcasrStartHour THEN 0 ELSE 1 END AS flag
        FROM #final2 
        ORDER BY flag, [hour], [min];
    
        -- Второй результат
        SELECT * FROM #final2 ORDER BY windowDateOriginal DESC;

        DROP TABLE #final2;
    END

    DROP TABLE #tmpWindow;
END