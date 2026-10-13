-- Сетка кампании (десктоп CampaignForm / RollerIssuesGrid3, веб — вкладка «Рекламные окна»): третий и четвёртый
-- результат Grid (бирюзовые окна фирмы, номера роликов чужих кампаний фирмы) строятся одним проходом.
-- Разбор пика 13.10.2026 по логу прода (12:03–12:27, вызовы Grid до 4,1 с при медиане 213 мс): время уходило не на
-- процессор (макс. 56 мс из 3966 мс) и не на блокировки, а на физическое чтение — 812 страниц за вызов в третьем
-- запросе (в четвёртом, читающем те же страницы, ни разу больше 36 мс и 0 физических чтений). Оба запроса шли по всем
-- выпускам всех кампаний фирмы на станции за всё время (IX_Issue_campaignID_covering по campaignID) и на каждый
-- выпуск прыгали в кластерный индекс Issue и в TariffWindow — до 35 тыс. логических чтений за вызов.
-- Теперь: список кампаний фирмы на станции + список окон станции за период, выпуски между ними, из одной выборки
-- обе выдачи. Колонки, порядок и содержимое результатов те же (сверено на ArtvisDev: 1164 случая, 129 из них —
-- фирмы с 3000+ выпусков, расхождений нет). Логические чтения за вызов в среднем 16 тыс. → 1,9 тыс., максимум 108 тыс. → 4 тыс.;
-- на холодном кэше самая тяжёлая фирма 182 → 32 мс, случайных физических чтений 288 → 29. Мелкая фирма — на 4–8 мс медленнее
-- (две служебные таблицы).
--
-- Только процедура: клиент не нужен, откат — предыдущий текст Grid из 25_grid-actual-window.sql.
-- Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 41_grid-firm-windows-one-pass.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [dbo].[Grid]
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

-- Проверка: новая версия применена.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.Grid')) LIKE N'%INSERT INTO #firmIssues%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.Grid')) LIKE N'%ON i.actualWindowID = tw.windowId AND tw.massmediaID = @massmediaID%'
    PRINT N'ГОТОВО: Grid — бирюзовые окна и ролики фирмы одним проходом.';
ELSE
    RAISERROR(N'41: новая версия Grid не применена.', 16, 1);
GO
