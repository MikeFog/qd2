-- Возврат цены окон, испорченных старой TariffWindowChangePrice (исправлена Deploy/22, 07.10.2026).
--
-- «Изменить цену...» меняла цену окон ВСЕХ станций с тем же временем и ценой. Задета одна станция —
-- «Дорожное радио (Переславль)», строка 06:15: тариф 15 ₽, в окнах 10 ₽ (поиск по всем окнам с 09.07.2024 —
-- ArtvisDB/Scripts/tariff-window-price-damage-check.sql; прод 07.10.2026 — ровно эти наборы, выпусков нет):
--   35 окон тарифа 140048 — 04–07.03.2025 и 01–31.12.2025 (кто и когда — неизвестно, логов нет);
--   103 окна тарифа 142296 — 20.09–31.12.2026 (agv 20.09.2026 18:33 «15 -> 10, 20.09–31.12.2026» по
--   прайс-листу RFM (Переславль), подтверждено логом и сравнением копий прода до/после).
--
-- Что делает: окнам этих двух тарифов в 06:15 с ценой 10 ₽ возвращает цену тарифа (15 ₽). Окна, где уже стоят
-- выпуски, НЕ трогает — их цена выпуска посчитана от 10 ₽, такие решаем отдельно (скрипт их перечислит).
-- Одна транзакция, сразу COMMIT. Идемпотентен: повторный запуск ничего не меняет.
-- Клиент не нужен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 23_road-radio-0615-price-restore.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

SET XACT_ABORT ON;
BEGIN TRAN;

DECLARE @target TABLE (windowId int PRIMARY KEY, tariffPrice decimal(18,2), hasIssues bit);

INSERT INTO @target (windowId, tariffPrice, hasIssues)
SELECT tw.windowId, t.price,
    CASE WHEN EXISTS (SELECT 1 FROM dbo.Issue i WHERE i.actualWindowID = tw.windowId OR i.originalWindowID = tw.windowId) THEN 1 ELSE 0 END
FROM dbo.TariffWindow tw
    JOIN dbo.Tariff t ON t.tariffID = tw.tariffId
WHERE tw.tariffId IN (140048, 142296)
    AND tw.massmediaID = 174
    AND CONVERT(varchar(5), tw.windowDateOriginal, 108) = '06:15'
    AND tw.price = 10
    AND t.price = 15;

SELECT N'найдено окон по 10 ₽' AS step, COUNT(*) AS windows, SUM(CASE WHEN hasIssues = 1 THEN 1 ELSE 0 END) AS with_issues FROM @target;

-- Окна с выпусками не трогаем — перечислить.
SELECT N'пропущено: в окне есть выпуски' AS step, tw.windowId, CONVERT(varchar(16), tw.windowDateActual, 120) AS window_time
FROM @target x JOIN dbo.TariffWindow tw ON tw.windowId = x.windowId
WHERE x.hasIssues = 1;

UPDATE tw SET tw.price = x.tariffPrice
FROM dbo.TariffWindow tw
    JOIN @target x ON x.windowId = tw.windowId
WHERE x.hasIssues = 0 AND tw.price = 10;

SELECT N'возвращена цена 15 ₽' AS step, @@ROWCOUNT AS windows;

-- Контроль: у этих тарифов в 06:15 не осталось окон по 10 ₽ без выпусков.
SELECT N'осталось по 10 ₽ (без выпусков)' AS step, COUNT(*) AS windows
FROM dbo.TariffWindow tw
WHERE tw.tariffId IN (140048, 142296) AND tw.massmediaID = 174
    AND CONVERT(varchar(5), tw.windowDateOriginal, 108) = '06:15' AND tw.price = 10
    AND NOT EXISTS (SELECT 1 FROM dbo.Issue i WHERE i.actualWindowID = tw.windowId OR i.originalWindowID = tw.windowId);

COMMIT;
PRINT N'=== ГОТОВО: изменения применены и зафиксированы (COMMIT).';
GO
