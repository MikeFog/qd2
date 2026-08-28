/*  Диагностика: осиротевшая обвязка политагитации.

    Ищем окна, где ЕСТЬ выпуски обвязки (типы 44/7/55 в кампаниях служебной
    фирмы «(Служебная) Обвязка политагитации»), но в ЦЕПОЧКЕ окон НЕТ
    подтверждённого ролика политической агитации (тип 6). Такую обвязку должен
    был снять AgitationFraming 'CleanupWindow'; если она осталась - в эфире висит
    идентификатор СМИ / анонс без агитации.

    Про интервалы-исключения (agitationExcludeIntervals):
      - анонс (тип 7) добавляется ВСЕГДА, от интервалов не зависит
        => выпуск типа 7 без агитации = однозначный сирота;
      - идентификаторы СМИ (44/55) в интервале-исключении не добавляются
        => их ОТСУТСТВИЕ в сиротской цепочке само по себе ни о чём не говорит,
           но их НАЛИЧИЕ без агитации - тоже сирота.
    Поэтому запрос флагует ЛЮБОЙ из типов 44/7/55 без подтверждённой агитации,
    а колонка intervalNote показывает, попадает ли окно в интервал-исключение
    (для 44/55 это объясняет, почему их могло не быть исходно).

    Только чтение. Чинить - отдельно (точечно удалить сиротские выпуски и
    поправить счётчики TariffWindow, либо прогнать CleanupWindow по этим окнам).
*/

SET NOCOUNT ON;

DECLARE @SERVICE_FIRM_NAME nvarchar(64) = N'(Служебная) Обвязка политагитации';

;WITH serviceCampaign AS (
    SELECT c.campaignID
    FROM Campaign c
        INNER JOIN [Action] a ON a.actionID = c.actionID
        INNER JOIN Firm f      ON f.firmID  = a.firmID
    WHERE f.[name] = @SERVICE_FIRM_NAME
      AND a.deleteDate IS NULL
),
framing AS (   -- выпуски обвязки с привязкой к голове цепочки
    SELECT  i.issueID,
            i.actualWindowID,
            i.isConfirmed,
            r.rolActionTypeID,
            dbo.fn_AgitationChainFirst(i.actualWindowID) AS chainFirst
    FROM Issue i
        INNER JOIN serviceCampaign sc ON sc.campaignID = i.campaignID
        INNER JOIN Roller r           ON r.rollerID    = i.rollerID
    WHERE r.rolActionTypeID IN (7, 44, 55)
),
chainAgitation AS (   -- головы цепочек, где есть агитация (тип 6)
    SELECT  dbo.fn_AgitationChainFirst(i.actualWindowID) AS chainFirst,
            MAX(CAST(i.isConfirmed AS int)) AS hasConfirmed,   -- 1 = есть подтверждённая
            COUNT(*)                        AS type6Count       -- всего роликов типа 6 (вкл. черновые)
    FROM Issue i
        INNER JOIN Roller r ON r.rollerID = i.rollerID
    WHERE r.rolActionTypeID = 6
    GROUP BY dbo.fn_AgitationChainFirst(i.actualWindowID)
)
SELECT
    tw.massmediaID,
    mm.[name]                                   AS massmedia,
    f.chainFirst,
    f.actualWindowID,
    tw.windowDateActual,
    f.issueID,
    f.rolActionTypeID,
    CASE f.rolActionTypeID
        WHEN 44 THEN N'44 идентификатор местного СМИ'
        WHEN 7  THEN N'7 анонс агитации'
        WHEN 55 THEN N'55 идентификатор федерального СМИ'
    END                                         AS slot,
    f.isConfirmed                               AS framingConfirmed,
    CASE
        WHEN ca.chainFirst IS NULL              THEN N'агитации в цепочке нет вообще'
        WHEN ca.hasConfirmed = 0               THEN N'агитация только черновик (isConfirmed=0)'
    END                                         AS reason,
    ISNULL(ca.type6Count, 0)                    AS type6InChain,
    CASE WHEN EXISTS (
        SELECT 1
        FROM dbo.fn_AgitationExcludeIntervals(mm.agitationExcludeIntervals) x
        WHERE (DATEPART(hour, tw.windowDateActual) * 60 + DATEPART(minute, tw.windowDateActual))
                  BETWEEN x.startMin AND x.finishMin
          AND x.dayMask & CAST(POWER(2, (DATEPART(weekday, tw.windowDateActual) + @@DATEFIRST - 2) % 7) AS int) <> 0
    ) THEN N'окно в интервале-исключении (44/55 тут штатно не создаются)'
      ELSE N'' END                              AS intervalNote
FROM framing f
    INNER JOIN TariffWindow tw ON tw.windowId = f.actualWindowID
    INNER JOIN MassMedia mm    ON mm.massmediaID = tw.massmediaID
    LEFT  JOIN chainAgitation ca ON ca.chainFirst = f.chainFirst
WHERE ca.chainFirst IS NULL          -- агитации в цепочке нет
   OR ca.hasConfirmed = 0           -- ...или она только черновик => обвязку не удерживает
ORDER BY tw.massmediaID, tw.windowDateActual, f.rolActionTypeID;

-- Сводка: сколько сиротских выпусков обвязки и в скольких цепочках
;WITH serviceCampaign AS (
    SELECT c.campaignID
    FROM Campaign c
        INNER JOIN [Action] a ON a.actionID = c.actionID
        INNER JOIN Firm f      ON f.firmID  = a.firmID
    WHERE f.[name] = @SERVICE_FIRM_NAME AND a.deleteDate IS NULL
),
framing AS (
    SELECT i.issueID, r.rolActionTypeID,
           dbo.fn_AgitationChainFirst(i.actualWindowID) AS chainFirst
    FROM Issue i
        INNER JOIN serviceCampaign sc ON sc.campaignID = i.campaignID
        INNER JOIN Roller r           ON r.rollerID    = i.rollerID
    WHERE r.rolActionTypeID IN (7, 44, 55)
),
chainAgitation AS (
    SELECT dbo.fn_AgitationChainFirst(i.actualWindowID) AS chainFirst,
           MAX(CAST(i.isConfirmed AS int)) AS hasConfirmed
    FROM Issue i INNER JOIN Roller r ON r.rollerID = i.rollerID
    WHERE r.rolActionTypeID = 6
    GROUP BY dbo.fn_AgitationChainFirst(i.actualWindowID)
)
SELECT
    COUNT(*)                                            AS orphanFramingIssues,
    COUNT(DISTINCT f.chainFirst)                        AS orphanChains,
    SUM(CASE WHEN f.rolActionTypeID = 7  THEN 1 ELSE 0 END) AS orphan_type7,
    SUM(CASE WHEN f.rolActionTypeID = 44 THEN 1 ELSE 0 END) AS orphan_type44,
    SUM(CASE WHEN f.rolActionTypeID = 55 THEN 1 ELSE 0 END) AS orphan_type55
FROM framing f
    LEFT JOIN chainAgitation ca ON ca.chainFirst = f.chainFirst
WHERE ca.chainFirst IS NULL OR ca.hasConfirmed = 0;
