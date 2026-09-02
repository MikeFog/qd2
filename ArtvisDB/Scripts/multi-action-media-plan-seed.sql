-- Сводный медиаплан «График размещения по нескольким акциям».
--
-- Добавляет один пункт меню в ветку «Трафик» (рядом с «Журналом
-- подтверждённых рекламных акций»). Диспетчеризация — MDIForm.cs по
-- codeName 'miMultiActionMediaPlan'.
--
-- Права на пункт (GroupMenu) НЕ раздаются намеренно — их назначают
-- администраторы штатными средствами.
--
-- Скрипт идемпотентен, повторный прогон безопасен.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i multi-action-media-plan-seed.sql
--
-- Процедуры разворачиваются отдельно (файлы в ArtvisDB\dbo\Stored Procedures):
--   MediaPlanRetrieve_v2, GetUniqueMMsForAction (изменены),
--   MultiActionMediaPlanActions (новая).

SET NOCOUNT ON;

DECLARE @menuTrafficParent SMALLINT =
    (SELECT parentID FROM [dbo].[iMenu] WHERE codeName = 'miTrafficManagement');

IF @menuTrafficParent IS NULL
BEGIN
    RAISERROR('Ветка меню «Трафик» (miTrafficManagement) не найдена — проверьте базу', 16, 1);
    RETURN;
END

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMenu] WHERE codeName = 'miMultiActionMediaPlan')
    INSERT INTO [dbo].[iMenu] (name, parentID, position, codeName, align, isPublic, isObsolete)
    VALUES (N'График размещения по нескольким акциям', @menuTrafficParent, 55,
            'miMultiActionMediaPlan', 'Left', 0, 0);

-------------------------------------------------------------------------------
-- Отчёт
-------------------------------------------------------------------------------

PRINT '--- Сводный медиаплан: состояние метаданных ---';

SELECT m.menuID, m.parentID, p.name AS parentMenu, m.position, m.codeName, m.name
FROM [dbo].[iMenu] m
    LEFT JOIN [dbo].[iMenu] p ON p.menuID = m.parentID
WHERE m.codeName = 'miMultiActionMediaPlan';

SELECT 'iMenu' AS [объект], COUNT(*) AS [строк], '1' AS [ожидается]
FROM [dbo].[iMenu] WHERE codeName = 'miMultiActionMediaPlan';
