/*
    Откат шага 1 (broadcast-start-neutralize-deploy.sql).

    Восстанавливает SponsorProgramPricelist.broadcastStart из снимка, сохранённого
    деплой-скриптом в dbo.bak_SponsorProgramPricelist_broadcastStart.

    Никаких захардкоженных списков: берутся фактические значения той инсталляции,
    на которой выполнялся деплой.

    Прайс-листы, созданные ПОСЛЕ нейтрализации, в снимке отсутствуют и не трогаются —
    они и должны остаться с 00:00.

    Таблица-бэкап намеренно НЕ удаляется: откат можно выполнить повторно.
    Удалять её вручную, когда решение станет окончательным (команда в конце файла).
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('dbo.bak_SponsorProgramPricelist_broadcastStart', 'U') IS NULL
BEGIN
    RAISERROR('Таблица-бэкап не найдена. Откат невозможен: деплой на этой базе не выполнялся либо бэкап удалён.', 16, 1);
    RETURN;
END

BEGIN TRANSACTION;

PRINT '--- ДО отката ---';
SELECT CONVERT(varchar(8), broadcastStart, 108) AS bs, COUNT(*) AS cnt
FROM dbo.SponsorProgramPricelist
GROUP BY CONVERT(varchar(8), broadcastStart, 108);

DECLARE @restored int;

UPDATE spp
SET broadcastStart = b.broadcastStart
FROM dbo.SponsorProgramPricelist spp
JOIN dbo.bak_SponsorProgramPricelist_broadcastStart b
  ON b.pricelistID = spp.pricelistID
WHERE spp.broadcastStart <> b.broadcastStart;

SET @restored = @@ROWCOUNT;
PRINT 'Восстановлено строк: ' + CAST(@restored AS varchar(10));

-- Информационно: что есть в снимке, но пропало из таблицы (удалённые прайс-листы)
IF EXISTS (
    SELECT 1 FROM dbo.bak_SponsorProgramPricelist_broadcastStart b
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SponsorProgramPricelist spp
                      WHERE spp.pricelistID = b.pricelistID)
)
BEGIN
    PRINT 'ВНИМАНИЕ: часть прайс-листов из снимка больше не существует:';
    SELECT b.pricelistID, b.sponsorProgramID,
           CONVERT(varchar(8), b.broadcastStart, 108) AS bsInBackup
    FROM dbo.bak_SponsorProgramPricelist_broadcastStart b
    WHERE NOT EXISTS (SELECT 1 FROM dbo.SponsorProgramPricelist spp
                      WHERE spp.pricelistID = b.pricelistID);
END

PRINT '--- ПОСЛЕ отката ---';
SELECT CONVERT(varchar(8), broadcastStart, 108) AS bs, COUNT(*) AS cnt
FROM dbo.SponsorProgramPricelist
GROUP BY CONVERT(varchar(8), broadcastStart, 108);

COMMIT TRANSACTION;
PRINT 'Зафиксировано.';

/*
    Когда решение об удалении поля станет окончательным:

    DROP TABLE dbo.bak_SponsorProgramPricelist_broadcastStart;
*/
