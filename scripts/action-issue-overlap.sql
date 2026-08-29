-- Пересечение выпусков двух рекламных акций в одном рекламном окне (TariffWindow).
-- На входе: два ActionID. На выходе: выпуски из Issue обеих акций,
-- которые попали в один и тот же originalWindowID.

DECLARE @actionA INT = 0;   -- <-- первая акция
DECLARE @actionB INT = 0;   -- <-- вторая акция

;WITH ActIssue AS (
    SELECT  i.issueID,
            i.originalWindowID,
            i.campaignID,
            c.actionID,
            i.rollerID,
            i.moduleIssueID,
            i.packModuleIssueID,
            i.ratio,
            i.tariffPrice,
            i.isConfirmed
    FROM    dbo.Issue i
            JOIN dbo.Campaign c ON c.campaignID = i.campaignID
    WHERE   c.actionID IN (@actionA, @actionB)
),
OverlapWindows AS (
    SELECT  originalWindowID
    FROM    ActIssue
    GROUP BY originalWindowID
    HAVING  COUNT(DISTINCT CASE WHEN actionID = @actionA THEN 1 END) > 0
        AND COUNT(DISTINCT CASE WHEN actionID = @actionB THEN 1 END) > 0
)
SELECT  ai.originalWindowID,
        tw.massmediaID,
        tw.windowDateOriginal,
        tw.windowDateActual,
        tw.duration,
        ai.actionID,
        ai.campaignID,
        ai.issueID,
        ai.rollerID,
        ai.moduleIssueID,
        ai.packModuleIssueID,
        ai.ratio,
        ai.tariffPrice,
        ai.isConfirmed
FROM    ActIssue ai
        JOIN OverlapWindows ow ON ow.originalWindowID = ai.originalWindowID
        JOIN dbo.TariffWindow tw ON tw.windowId = ai.originalWindowID
ORDER BY ai.originalWindowID, ai.actionID, ai.issueID;
