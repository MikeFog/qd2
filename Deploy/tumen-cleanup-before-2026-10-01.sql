-- Курган (база Tumen): очистка акций и оплат до 30.09.2026 включительно (06.11.2026, просьба Миши).
-- Разовый скрипт только для базы Tumen, в нумерованный пакет Deploy не входит.
--
-- Что удаляется:
--   1. Все оплаты с датой до 30.09.2026 включительно. Сначала снимается их привязка к акциям (PaymentAction), потом сами оплаты.
--   2. Все акции, начавшиеся до 30.09.2026 включительно (живые и из журнала удалённых), и акции без дат, созданные до 01.10.2026
--      (пустые заготовки). Вместе с акцией — её выпуски, кампании, модульные/пакетные/программные выпуски, история
--      менеджерской скидки, счета (Bill), привязки оплат.
--   Остаётся акция 74 «Оформление эфира» (Григорий Агамов, 01.01–31.12.2026) — единственная актуальная из этого периода.
--
-- Загрузка окон: у живых удаляемых акций счётчики занятости окон (TariffWindow) уменьшаются тем же расчётом, что в
-- ActionIUD 'DeleteItem' (перенос в журнал удалённых). У акций из журнала удалённых счётчики уже уменьшены при переносе.
-- Журнал удалённых выпусков (LogDeletedIssue) и сообщения администраторам не пишутся: это чистка, а не удаление пользователем.
--
-- Защиты (при несоответствии — откат, ничего не изменено):
--   - база называется Tumen;
--   - акция 74 есть, не в журнале удалённых, начинается до 01.10.2026 и заканчивается после 30.09.2026;
--   - кроме акции 74 нет других акций, которые начались до 01.10.2026, а закончились после 30.09.2026;
--   - ни одна удаляемая оплата не привязана к остающейся акции (иначе изменился бы остаток оплаты живой акции);
--   - среди удаляемых выпусков нет политической агитации (тип 6): для неё нужно снимать обвязку окон, здесь это не сделано.
--
-- Скрипт применяется сразу (COMMIT в конце). Перед запуском — резервная копия базы.
-- Пока идёт скрипт (до минуты), qd2 и веб на этой базе могут ждать.
--   sqlcmd -S <сервер> -d Tumen -E -f 65001 -I -b -i tumen-cleanup-before-2026-10-01.sql

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

DECLARE @border datetime = '20261001';   -- всё, что раньше этой даты, удаляется
DECLARE @keepActionID int = 74;           -- «Оформление эфира», Григорий Агамов
DECLARE @n int;

IF DB_NAME() <> N'Tumen'
BEGIN
    RAISERROR(N'Остановлено: скрипт для базы Tumen, а запущен на %s. Ничего не изменено.', 16, 1, @@SERVERNAME);
    RETURN;
END

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM dbo.[Action]
               WHERE actionID = @keepActionID AND deleteDate IS NULL
                 AND startDate < @border AND finishDate >= @border)
BEGIN
    RAISERROR(N'Остановлено: акция 74 «Оформление эфира» не найдена, удалена или не переходит через 01.10.2026. Ничего не изменено.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

CREATE TABLE #delAction (actionID int PRIMARY KEY, isLive bit NOT NULL);

INSERT INTO #delAction (actionID, isLive)
SELECT a.actionID, CASE WHEN a.deleteDate IS NULL THEN 1 ELSE 0 END
FROM dbo.[Action] a
WHERE a.actionID <> @keepActionID
  AND (a.startDate < @border OR (a.startDate IS NULL AND a.createDate < @border));

SELECT @n = COUNT(*)
FROM #delAction d INNER JOIN dbo.[Action] a ON a.actionID = d.actionID
WHERE a.finishDate >= @border;

IF @n > 0
BEGIN
    SELECT a.actionID, a.startDate, a.finishDate, a.deleteDate, f.name AS firm
    FROM #delAction d
        INNER JOIN dbo.[Action] a ON a.actionID = d.actionID
        INNER JOIN dbo.Firm f ON f.firmID = a.firmID
    WHERE a.finishDate >= @border;
    RAISERROR(N'Остановлено: кроме акции 74 есть ещё %d акц. с началом до 01.10.2026 и окончанием после 30.09.2026 (список выше). Ничего не изменено.', 16, 1, @n);
    ROLLBACK TRANSACTION;
    RETURN;
END

CREATE TABLE #delPayment (paymentID int PRIMARY KEY);

INSERT INTO #delPayment (paymentID)
SELECT paymentID FROM dbo.Payment WHERE paymentDate < @border;

SELECT @n = COUNT(*)
FROM dbo.PaymentAction pa
    INNER JOIN #delPayment p ON p.paymentID = pa.paymentID
WHERE pa.actionID NOT IN (SELECT actionID FROM #delAction);

IF @n > 0
BEGIN
    SELECT pa.paymentID, pm.paymentDate, pa.actionID, pa.summa, a.startDate, a.finishDate
    FROM dbo.PaymentAction pa
        INNER JOIN #delPayment p ON p.paymentID = pa.paymentID
        INNER JOIN dbo.Payment pm ON pm.paymentID = pa.paymentID
        INNER JOIN dbo.[Action] a ON a.actionID = pa.actionID
    WHERE pa.actionID NOT IN (SELECT actionID FROM #delAction);
    RAISERROR(N'Остановлено: %d привязок оплат до 01.10.2026 к остающимся акциям (список выше). Ничего не изменено.', 16, 1, @n);
    ROLLBACK TRANSACTION;
    RETURN;
END

IF EXISTS (SELECT 1
           FROM dbo.Issue i
               INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
               INNER JOIN #delAction d ON d.actionID = c.actionID
               INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
           WHERE r.rolActionTypeID = 6)
BEGIN
    RAISERROR(N'Остановлено: среди удаляемых выпусков есть политическая агитация (тип 6). Ничего не изменено.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

DECLARE @actLive int, @actDeleted int, @campaigns int, @issues int, @payments int, @paySum decimal(18, 2), @links int;

SELECT @actLive = SUM(CASE WHEN isLive = 1 THEN 1 ELSE 0 END), @actDeleted = SUM(CASE WHEN isLive = 0 THEN 1 ELSE 0 END) FROM #delAction;
SELECT @campaigns = COUNT(*) FROM dbo.Campaign c INNER JOIN #delAction d ON d.actionID = c.actionID;
SELECT @issues = COUNT(*) FROM dbo.Issue i INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID INNER JOIN #delAction d ON d.actionID = c.actionID;
SELECT @payments = COUNT(*), @paySum = ISNULL(SUM(pm.summa), 0) FROM dbo.Payment pm INNER JOIN #delPayment p ON p.paymentID = pm.paymentID;

PRINT N'К удалению: акций ' + CAST(ISNULL(@actLive, 0) AS nvarchar(10)) + N' живых и ' + CAST(ISNULL(@actDeleted, 0) AS nvarchar(10))
    + N' из журнала удалённых, кампаний ' + CAST(@campaigns AS nvarchar(10)) + N', выпусков ' + CAST(@issues AS nvarchar(10))
    + N'; оплат ' + CAST(@payments AS nvarchar(10)) + N' на сумму ' + CAST(@paySum AS nvarchar(30)) + N'.';

-- 1. Оплаты: снять привязку к акциям, затем удалить
DELETE pa
FROM dbo.PaymentAction pa
WHERE pa.paymentID IN (SELECT paymentID FROM #delPayment)
   OR pa.actionID IN (SELECT actionID FROM #delAction);
SET @links = @@ROWCOUNT;

DELETE pm FROM dbo.Payment pm INNER JOIN #delPayment p ON p.paymentID = pm.paymentID;
SET @n = @@ROWCOUNT;
PRINT N'Привязок оплат к акциям снято: ' + CAST(@links AS nvarchar(10)) + N'; оплат удалено: ' + CAST(@n AS nvarchar(10)) + N'.';

-- 2. Загрузка окон: у живых удаляемых акций уменьшить счётчики (расчёт из ActionIUD 'DeleteItem')
UPDATE tw
SET timeInUseConfirmed       = CASE WHEN tw.maxCapacity = 0 THEN tw.timeInUseConfirmed - t1.duration ELSE tw.timeInUseConfirmed END,
    timeInUseUnconfirmed     = CASE WHEN tw.maxCapacity = 0 THEN tw.timeInUseUnconfirmed - t1.durationU ELSE tw.timeInUseUnconfirmed END,
    capacityInUseConfirmed   = CASE WHEN tw.maxCapacity > 0 THEN tw.capacityInUseConfirmed - t1.countIssues ELSE tw.capacityInUseConfirmed END,
    capacityInUseUnconfirmed = CASE WHEN tw.maxCapacity > 0 THEN tw.capacityInUseUnconfirmed - t1.countIssuesU ELSE tw.capacityInUseUnconfirmed END,
    isFirstPositionOccupied  = CASE WHEN t1.firstCount > 0 THEN 0 ELSE tw.isFirstPositionOccupied END,
    isSecondPositionOccupied = CASE WHEN t1.secondCount > 0 THEN 0 ELSE tw.isSecondPositionOccupied END,
    isLastPositionOccupied   = CASE WHEN t1.lastCount > 0 THEN 0 ELSE tw.isLastPositionOccupied END,
    firstPositionsUnconfirmed  = tw.firstPositionsUnconfirmed - t1.firstCountU,
    secondPositionsUnconfirmed = tw.secondPositionsUnconfirmed - t1.secondCountU,
    lastPositionsUnconfirmed   = tw.lastPositionsUnconfirmed - t1.lastCountU
FROM dbo.TariffWindow tw
    INNER JOIN (
        SELECT i.actualWindowID AS windowID,
            SUM(CASE WHEN i.isConfirmed = 1 THEN r.duration ELSE 0 END) AS duration,
            SUM(CASE WHEN i.isConfirmed = 0 THEN r.duration ELSE 0 END) AS durationU,
            SUM(CASE WHEN i.isConfirmed = 1 THEN 1 ELSE 0 END) AS countIssues,
            SUM(CASE WHEN i.isConfirmed = 0 THEN 1 ELSE 0 END) AS countIssuesU,
            SUM(CASE WHEN i.isConfirmed = 1 AND i.positionId = -20 THEN 1 ELSE 0 END) AS firstCount,
            SUM(CASE WHEN i.isConfirmed = 1 AND i.positionId = -10 THEN 1 ELSE 0 END) AS secondCount,
            SUM(CASE WHEN i.isConfirmed = 1 AND i.positionId = 10 THEN 1 ELSE 0 END) AS lastCount,
            SUM(CASE WHEN i.isConfirmed = 0 AND i.positionId = -20 THEN 1 ELSE 0 END) AS firstCountU,
            SUM(CASE WHEN i.isConfirmed = 0 AND i.positionId = -10 THEN 1 ELSE 0 END) AS secondCountU,
            SUM(CASE WHEN i.isConfirmed = 0 AND i.positionId = 10 THEN 1 ELSE 0 END) AS lastCountU
        FROM dbo.Issue i
            INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
            INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
            INNER JOIN #delAction d ON d.actionID = c.actionID AND d.isLive = 1
        GROUP BY i.actualWindowID
    ) t1 ON t1.windowID = tw.windowId;
PRINT N'Окон с пересчитанной загрузкой: ' + CAST(@@ROWCOUNT AS nvarchar(10)) + N'.';

-- 3. Выпуски, затем акции (каскадом: кампании, модульные/пакетные/программные выпуски, история скидки, счета)
DELETE i
FROM dbo.Issue i
    INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
    INNER JOIN #delAction d ON d.actionID = c.actionID;
SET @n = @@ROWCOUNT;
PRINT N'Выпусков удалено: ' + CAST(@n AS nvarchar(10)) + N'.';

DELETE a FROM dbo.[Action] a INNER JOIN #delAction d ON d.actionID = a.actionID;
SET @n = @@ROWCOUNT;
PRINT N'Акций удалено: ' + CAST(@n AS nvarchar(10)) + N'.';

-- 4. Проверка
IF EXISTS (SELECT 1 FROM dbo.Payment WHERE paymentDate < @border)
   OR EXISTS (SELECT 1 FROM dbo.[Action] WHERE actionID <> @keepActionID
                AND (startDate < @border OR (startDate IS NULL AND createDate < @border)))
   OR NOT EXISTS (SELECT 1 FROM dbo.[Action] WHERE actionID = @keepActionID)
   OR EXISTS (SELECT 1 FROM dbo.Campaign c WHERE NOT EXISTS (SELECT 1 FROM dbo.[Action] a WHERE a.actionID = c.actionID))
BEGIN
    RAISERROR(N'Остановлено: после удаления остались оплаты/акции до 01.10.2026 или пропала акция 74. Всё откатено.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END

COMMIT TRANSACTION;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT). Из периода до 01.10.2026 осталась только акция 74. ===';
GO
