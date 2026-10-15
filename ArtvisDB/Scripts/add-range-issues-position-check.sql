-- Проверка Д-15 (docs/action-forms.md), Deploy/48: AddRangeIssues выбирает окно получаса с учётом позиции и того, влезает ли ролик.
-- Не меняет данные: всё в транзакциях с откатом. Запускать от sysadmin на dev-базе:
--   sqlcmd -S "(local)\sqlexpress" -d ArtvisDev -E -f 65001 -I -W -s "|" -i add-range-issues-position-check.sql
-- В конце — сводка PASS/FAIL по каждому случаю.
--
-- Идентификаторы — данные ArtvisDev на 15.10.2026; на другой базе или после обновления копии подставить: станцию с двумя
-- подходящими окнами в одном получасе (TariffWindow: maxCapacity = 0, не заблокировано, тариф не «только для модулей»), слот не раньше
-- завтрашнего дня (иначе пользователи без права «вернуться назад» получают IncorrectIssueDate раньше любых других отказов), акцию-
-- черновик с линейной кампанией на этой станции, ролики её фирмы (6 и 15 с), пользователей: 3 — админ с правом «минус» и «назад»,
-- 66 — с правом «назад», без права «минус», трафик-менеджер (случай F: там действует WindowOverflow). Окно с большим свободным
-- временем — @best (163 с), второе, узкое — @other (43 с).
--
-- Ожидаемо после Deploy/48:
--   A ok (@best)   B ok (@other)   C ok (@other)   D FirstLastIssueError   E ok (@best)
--   F FirstLastIssueError (подтверждённая акция, узкое соседнее окно не берётся — клиенты повторят вызов без позиции)
--   G PositionErrorForTheSameAction (черновик, узкое соседнее окно не берётся — ролик не переполняет его молча)
-- До Deploy/48: A ok; B PositionErrorForTheSameAction; C FirstLastIssueError; D FirstLastIssueError; E ok.
SET NOCOUNT ON;
SET XACT_ABORT OFF;

DECLARE @slot datetime = '20261016 06:30', @best int = 12927485, @other int = 12927484, @before int;
DECLARE @res TABLE (ord int, [случай] nvarchar(60), [итог] nvarchar(200), [окно] int, [ожидалось итог] nvarchar(200), [ожидалось окно] int);

BEGIN TRAN;

-- A. окна свободны, позиция «первый» (-20): лучшее по времени окно (@best)
SELECT @before = ISNULL(MAX(issueID), 0) FROM Issue;
BEGIN TRY
    EXEC dbo.AddRangeIssues @issueDate = @slot, @rollerID = 124865, @rollerDuration = 6, @positionId = -20, @loggedUserId = 3, @actionID = 186056, @campaignIDs = '411474';
    INSERT @res VALUES (1, N'A: свободны оба', N'ok', (SELECT TOP 1 actualWindowID FROM Issue WHERE issueID > @before AND campaignID = 411474), N'ok', @best);
END TRY
BEGIN CATCH
    INSERT @res VALUES (1, N'A: свободны оба', ERROR_MESSAGE(), NULL, N'ok', @best);
END CATCH

-- B. в этой же акции в лучшем окне уже стоит «первый» (из A): второй клик с той же позицией.
--    Было: PositionErrorForTheSameAction на лучшем окне. Должно: окно @other
SELECT @before = ISNULL(MAX(issueID), 0) FROM Issue;
BEGIN TRY
    EXEC dbo.AddRangeIssues @issueDate = @slot, @rollerID = 124865, @rollerDuration = 6, @positionId = -20, @loggedUserId = 3, @actionID = 186056, @campaignIDs = '411474';
    INSERT @res VALUES (2, N'B: «первый» этой акции уже в лучшем окне', N'ok', (SELECT TOP 1 actualWindowID FROM Issue WHERE issueID > @before AND campaignID = 411474), N'ok', @other);
END TRY
BEGIN CATCH
    INSERT @res VALUES (2, N'B: «первый» этой акции уже в лучшем окне', ERROR_MESSAGE(), NULL, N'ok', @other);
END CATCH

-- C. «второй» (-10) занят подтверждённым выпуском в лучшем окне (флаг): было FirstLastIssueError. Должно: окно @other
UPDATE TariffWindow SET isSecondPositionOccupied = 1 WHERE windowId = @best;
SELECT @before = ISNULL(MAX(issueID), 0) FROM Issue;
BEGIN TRY
    EXEC dbo.AddRangeIssues @issueDate = @slot, @rollerID = 124865, @rollerDuration = 6, @positionId = -10, @loggedUserId = 3, @actionID = 186056, @campaignIDs = '411474';
    INSERT @res VALUES (3, N'C: «второй» занят в лучшем окне', N'ok', (SELECT TOP 1 actualWindowID FROM Issue WHERE issueID > @before AND campaignID = 411474), N'ok', @other);
END TRY
BEGIN CATCH
    INSERT @res VALUES (3, N'C: «второй» занят в лучшем окне', ERROR_MESSAGE(), NULL, N'ok', @other);
END CATCH

-- D. «последний» (10) занят в обоих окнах: FirstLastIssueError
UPDATE TariffWindow SET isLastPositionOccupied = 1 WHERE windowId IN (@best, @other);
BEGIN TRY
    EXEC dbo.AddRangeIssues @issueDate = @slot, @rollerID = 124865, @rollerDuration = 6, @positionId = 10, @loggedUserId = 3, @actionID = 186056, @campaignIDs = '411474';
    INSERT @res VALUES (4, N'D: «последний» занят в обоих', N'ok', NULL, N'FirstLastIssueError', NULL);
END TRY
BEGIN CATCH
    INSERT @res VALUES (4, N'D: «последний» занят в обоих', ERROR_MESSAGE(), NULL, N'FirstLastIssueError', NULL);
END CATCH

-- E. без позиции (0), «второй» занят в лучшем: позиция не мешает — лучшее окно, как раньше
SELECT @before = ISNULL(MAX(issueID), 0) FROM Issue;
BEGIN TRY
    EXEC dbo.AddRangeIssues @issueDate = @slot, @rollerID = 124865, @rollerDuration = 6, @positionId = 0, @loggedUserId = 3, @actionID = 186056, @campaignIDs = '411474';
    INSERT @res VALUES (5, N'E: без позиции', N'ok', (SELECT TOP 1 actualWindowID FROM Issue WHERE issueID > @before AND campaignID = 411474), N'ok', @best);
END TRY
BEGIN CATCH
    INSERT @res VALUES (5, N'E: без позиции', ERROR_MESSAGE(), NULL, N'ok', @best);
END CATCH

ROLLBACK;

-- F. подтверждённая акция, пользователь с правом «назад», без права «минус» (66): «первый» занят в лучшем окне, в узком соседнем
--    позиция свободна, но ролик не влезает (занято 40 из 43 с). Раньше процедура брала лучшее окно -> FirstLastIssueError -> клиент
--    повторял вызов без позиции. Не должна подменять отказ по позиции переполнением узкого окна (WindowOverflow): FirstLastIssueError
BEGIN TRAN;
UPDATE Action SET isConfirmed = 1 WHERE actionID = 186056;
UPDATE TariffWindow SET isFirstPositionOccupied = 1 WHERE windowId = @best;
UPDATE TariffWindow SET timeInUseConfirmed = 40 WHERE windowId = @other;
BEGIN TRY
    EXEC dbo.AddRangeIssues @issueDate = @slot, @rollerID = 124865, @rollerDuration = 6, @positionId = -20, @loggedUserId = 66, @actionID = 186056, @campaignIDs = '411474';
    INSERT @res VALUES (6, N'F: подтверждённая, узкое соседнее окно', N'ok', NULL, N'FirstLastIssueError', NULL);
END TRY
BEGIN CATCH
    INSERT @res VALUES (6, N'F: подтверждённая, узкое соседнее окно', ERROR_MESSAGE(), NULL, N'FirstLastIssueError', NULL);
END CATCH
ROLLBACK;

-- G. черновик: «первый» этой акции уже в лучшем окне, в узком соседнем занято 30 из 43 с, ролик 15 с (126900) не влезает.
--    IssueIUD у черновика переполнение не проверяет и молча положил бы выпуск в узкое окно (ошибка потом при активации).
--    Должно: чистый отказ PositionErrorForTheSameAction
BEGIN TRAN;
EXEC dbo.AddRangeIssues @issueDate = @slot, @rollerID = 126900, @rollerDuration = 15, @positionId = -20, @loggedUserId = 3, @actionID = 186056, @campaignIDs = '411474';
UPDATE TariffWindow SET timeInUseConfirmed = 30 WHERE windowId = @other;
SELECT @before = ISNULL(MAX(issueID), 0) FROM Issue;
BEGIN TRY
    EXEC dbo.AddRangeIssues @issueDate = @slot, @rollerID = 126900, @rollerDuration = 15, @positionId = -20, @loggedUserId = 3, @actionID = 186056, @campaignIDs = '411474';
    INSERT @res VALUES (7, N'G: черновик, узкое соседнее окно', N'ok', (SELECT TOP 1 actualWindowID FROM Issue WHERE issueID > @before AND campaignID = 411474), N'PositionErrorForTheSameAction', NULL);
END TRY
BEGIN CATCH
    INSERT @res VALUES (7, N'G: черновик, узкое соседнее окно', ERROR_MESSAGE(), NULL, N'PositionErrorForTheSameAction', NULL);
END CATCH
ROLLBACK;

SELECT [случай], [итог], [окно],
       CASE WHEN [итог] = [ожидалось итог] AND ISNULL([окно], 0) = ISNULL([ожидалось окно], 0) THEN N'PASS' ELSE N'FAIL' END AS [результат]
FROM @res ORDER BY ord;

SELECT N'после отката: выпусков в кампании 411474 (было 93 на 15.10.2026)' AS [проверка], COUNT(*) AS [значение] FROM Issue WHERE campaignID = 411474
UNION ALL SELECT N'флаги занятости окон (f+s+l, должно быть 0)', SUM(CONVERT(int, isFirstPositionOccupied) + CONVERT(int, isSecondPositionOccupied) + CONVERT(int, isLastPositionOccupied)) FROM TariffWindow WHERE windowId IN (12927484, 12927485)
UNION ALL SELECT N'акция 186056 подтверждена (должно быть 0)', CONVERT(int, isConfirmed) FROM Action WHERE actionID = 186056
UNION ALL SELECT N'timeInUseConfirmed узкого окна (должно быть 0)', timeInUseConfirmed FROM TariffWindow WHERE windowId = 12927484;
