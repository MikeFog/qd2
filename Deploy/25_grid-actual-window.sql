-- Сетка размещения кампании (десктоп CampaignForm / RollerIssuesGrid3, веб — вкладка «Рекламные окна»)
-- по окну выхода выпуска (Issue.actualWindowID), а не по окну, куда его изначально поставили (originalWindowID).
-- Правило 07.10.2026, docs/tasks/window-actual-switch.md (пакет 2). После переноса трафиком синяя отметка стояла
-- в окне, где выпуск уже не выходит, а список выпусков окна показывал его в другом окне.
--
-- 1. Grid — синяя отметка, номера роликов, счётчик дня, бирюзовые окна фирмы. В первой выборке добавлена
--    колонка actualWindowID; originalWindowID оставлен для старого клиента.
-- 2. TariffWindowWithAdvertTypeRetrieve — подсветка «Предметы рекламы» в той же сетке (Д и В).
-- 3. ModuleIssueRetrieve — список выпусков модулей кампании в окне (модульная сетка, Д).
-- Пример на ArtvisDev: выпуск 41707758 (кампания 411526, 14.10.2026) — синий в 09:45, а не в 08:45.
--
-- Идемпотентен. Порядок с клиентом любой, ничего не падает. Новый клиент без скрипта работает по-старому
-- (исходное окно). Старый клиент со скриптом показывает смешанно: синяя отметка — по исходному окну, а счётчик
-- дня, бирюзовые окна и подсветка «Предметы рекламы» — по окну выхода. Поэтому клиент лучше обновить тем же вечером.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 25_grid-actual-window.sql

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
    
    -- Третий запрос только если нужен firmId
    IF @campaignID <> -1 AND @firmId IS NOT NULL
    BEGIN
        SELECT DISTINCT tw.windowId
        FROM dbo.Issue i
            INNER JOIN dbo.TariffWindow tw ON i.actualWindowID = tw.windowId
            INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID 
            INNER JOIN dbo.Action a ON a.actionID = c.actionID
        WHERE
            c.massmediaID = @massmediaID 
            AND tw.dayOriginal BETWEEN @a AND @b
            AND a.firmID = @firmId
            AND (i.isConfirmed = 1 OR @showUnconfirmed = 1)
            AND a.deleteDate IS NULL;

        -- Четвёртый запрос: ролики чужих кампаний той же фирмы по окнам — для номеров
        -- роликов в бирюзовых ячейках (RollerIssuesGrid3.GetRollerNumbersText). Свои
        -- кампании исключены: их ролики грид берёт из первого запроса и держит актуальными
        -- при ручном добавлении/удалении. Фильтры те же, что у третьего запроса.
        SELECT DISTINCT tw.windowId, i.rollerID, i.positionId
        FROM dbo.Issue i
            INNER JOIN dbo.TariffWindow tw ON i.actualWindowID = tw.windowId
            INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
            INNER JOIN dbo.Action a ON a.actionID = c.actionID
        WHERE
            c.massmediaID = @massmediaID
            AND tw.dayOriginal BETWEEN @a AND @b
            AND a.firmID = @firmId
            AND i.campaignID <> @campaignID
            AND (i.isConfirmed = 1 OR @showUnconfirmed = 1)
            AND a.deleteDate IS NULL;
    END
    
    DROP TABLE #res;
END
GO

CREATE OR ALTER PROC [dbo].[TariffWindowWithAdvertTypeRetrieve]
(
    @advertTypeId smallint,
    @pricelistId smallint = null,
    @startDate datetime = null,
    @finishDate datetime = null,
    @windowId int = null,
    @showUnconfirmed bit = 1
)
AS
BEGIN
    SET NOCOUNT ON;
/*
Оптимизация: 2025-01
Причина:
- Процедура выполнялась ~1400 ms при rows=1

Решение:
- Переписан JOIN Issue -> EXISTS
- Добавлен индекс:
  IX_Issue_OriginalWindowID_isConfirmed_rollerID
  (originalWindowID, isConfirmed, rollerID)

Результат:
- Время выполнения ~50 ms

  С 07.10.2026 выпуск ищется в окне выхода (i.actualWindowID; правило docs/tasks/window-actual-switch.md),
  индекс IX_Issue_ActualWindowID_isConfirmed_COVERING (actualWindowID, isConfirmed) INCLUDE (campaignID, rollerID).

ВАЖНО:
- Удаление/изменение индекса приведёт к резкой деградации производительности
*/


    -- Частый кейс: ищем по конкретному окну
    IF @windowId IS NOT NULL
    BEGIN
        SELECT tw.windowId
        FROM TariffWindow tw
        JOIN Tariff t ON t.tariffId = tw.tariffId
        WHERE tw.windowId = @windowId
          AND (@pricelistId IS NULL OR t.pricelistId = @pricelistId)
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)
          AND EXISTS
          (
              SELECT 1
              FROM Issue i
              JOIN Roller r ON r.rollerID = i.rollerID
              LEFT JOIN AdvertType adt ON adt.advertTypeID = r.advertTypeID
              WHERE i.actualWindowID = tw.windowId
                AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
                AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
          )
        OPTION (RECOMPILE); -- чтобы не ловить плохой план из-за разных режимов
        RETURN;
    END

    -- Общий кейс: диапазон/прайслист
    SELECT tw.windowId
    FROM TariffWindow tw
    JOIN Tariff t ON t.tariffId = tw.tariffId
    WHERE (@pricelistId IS NULL OR t.pricelistId = @pricelistId)
      AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
      AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)
      AND EXISTS
      (
          SELECT 1
          FROM Issue i
          JOIN Roller r ON r.rollerID = i.rollerID
          LEFT JOIN AdvertType adt ON adt.advertTypeID = r.advertTypeID
          WHERE i.actualWindowID = tw.windowId
            AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
            AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
      )
    OPTION (RECOMPILE);
END
GO

CREATE OR ALTER PROC [dbo].[ModuleIssueRetrieve]
(
@campaignID INT = NULL,
@issueDate datetime = NULL,
@showUnconfirmed BIT = 1,
@moduleIssueId INT = NULL,
@windowID INT = NULL
)
AS
SET NOCOUNT on

if @windowID is not null
	SELECT DISTINCT
		mi.moduleIssueID,
		mi.[campaignID], 
		mi.[moduleID],
		mi.issueDate,
		mi.modulePriceListID,
		mi.positionID,
		mi.rollerID,
		m.name as moduleName,
		r.name as rollerName,
		m.name + ' - ' + r.name as NAME,
		f.[name] AS firmName,
		c.[actionID],
		a.[isConfirmed],
		r.advertTypeName,
		a.deleteDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join vRoller r on r.rollerId = mi.rollerId
		INNER JOIN [Campaign] c ON c.[campaignID] = mi.[campaignID]
		INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		INNER JOIN [Issue] i ON mi.[moduleIssueID] = i.[moduleIssueID] 
	WHERE
		(@campaignID is null or mi.campaignID = @campaignID)
		And (@issueDate is null or mi.issueDate = @issueDate)
		AND (@showUnconfirmed = 1 OR mi.[isConfirmed] = 1)
		AND (@moduleIssueId is null or mi.[moduleIssueID] = @moduleIssueId)
		AND i.actualWindowID = @windowID
	Order By 
		m.name, r.name
	OPTION (RECOMPILE)
else
	SELECT DISTINCT
		mi.moduleIssueID,
		mi.[campaignID], 
		mi.[moduleID],
		mi.issueDate,
		mi.modulePriceListID,
		mi.positionID,
		mi.rollerID,
		m.name as moduleName,
		r.name as rollerName,
		m.name + ' - ' + r.name as NAME,
		f.[name] AS firmName,
		c.[actionID],
		a.[isConfirmed],
		ip.[description] as issuePosition,
		r.advertTypeName,
		a.deleteDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join vRoller r on r.rollerId = mi.rollerId
		INNER JOIN [Campaign] c ON c.[campaignID] = mi.[campaignID]
		INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		Inner Join iIssuePosition ip On ip.positionId = mi.positionId
	WHERE
		(@campaignID is null or mi.campaignID = @campaignID)
		And (@issueDate is null or mi.issueDate = @issueDate)
		AND (@showUnconfirmed = 1 OR mi.[isConfirmed] = 1)
		AND (@moduleIssueId is null or mi.[moduleIssueID] = @moduleIssueId)
	Order By 
		m.name, r.name
	OPTION (RECOMPILE)
GO

-- Проверка: новые версии применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.Grid')) LIKE N'%ON i.actualWindowID = tw.windowId AND tw.massmediaID = @massmediaID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowWithAdvertTypeRetrieve')) LIKE N'%WHERE i.actualWindowID = tw.windowId%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowWithAdvertTypeRetrieve')) NOT LIKE N'%WHERE i.originalWindowID = tw.windowId%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ModuleIssueRetrieve')) LIKE N'%AND i.actualWindowID = @windowID%'
    PRINT N'ГОТОВО: сетка кампании — отметки, «Предметы рекламы» и выпуски модулей окна по окну выхода (3 процедуры).';
ELSE
    RAISERROR(N'25: новая версия применена не ко всем процедурам.', 16, 1);
GO
