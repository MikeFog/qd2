/*
    Шаг 1 плана docs/broadcast-start.md — нейтрализация broadcastStart в данных.

    Что делает:
      1) сохраняет ПОЛНЫЙ снимок SponsorProgramPricelist.broadcastStart в таблицу
         dbo.bak_SponsorProgramPricelist_broadcastStart;
      2) обнуляет broadcastStart (03:00 -> 00:00) в SponsorProgramPricelist.

    Зачем: после обнуления вся арифметика broadcastStart по системе становится
    тождественным преобразованием, без единой правки кода. Это проверка гипотезы
    «поле мертво» ценой одного UPDATE.

    Почему безопасно (проверено на ArtvisDev и Artvis, см. docs/broadcast-start.md §2):
      - нет ни одного SponsorTariff со временем раньше 03:00 -> ветвления по дням
        недели не срабатывают ни разу;
      - нет ни одного ProgramIssue в зоне 00:00-03:00 -> сдвиг границ отбора на 3 часа
        не меняет множество отобранных строк;
      - ActionRecalculate оборачивает сдвинутую дату в dbo.ToShortDate(), поэтому
        3 часа не попадают в Campaign.startDate/finishDate.
      - контрольный замер до/после на ArtvisDev (15 324 строки выдачи ключевых
        процедур) дал ноль различий по существу.

    Pricelist (линейный тракт) НЕ ТРОГАЕТСЯ: там уже везде 00:00.

    ПОВТОРНЫЙ ЗАПУСК БЕЗОПАСЕН: если таблица-бэкап уже существует, она НЕ
    перезаписывается (иначе второй запуск сохранил бы уже обнулённые значения и
    уничтожил возможность отката).

    Откат: broadcast-start-neutralize-rollback.sql — восстанавливает значения
    из таблицы-бэкапа, без всяких захардкоженных списков.
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

PRINT '--- ДО ---';
SELECT CONVERT(varchar(8), broadcastStart, 108) AS bs, COUNT(*) AS cnt
FROM dbo.SponsorProgramPricelist
GROUP BY CONVERT(varchar(8), broadcastStart, 108);

-------------------------------------------------------------------------------
-- 1. Снимок текущих значений
-------------------------------------------------------------------------------
IF OBJECT_ID('dbo.bak_SponsorProgramPricelist_broadcastStart', 'U') IS NULL
BEGIN
    SELECT
        pricelistID,
        sponsorProgramID,
        broadcastStart,
        savedAt = SYSDATETIME()
    INTO dbo.bak_SponsorProgramPricelist_broadcastStart
    FROM dbo.SponsorProgramPricelist;

    PRINT 'Снимок сохранён, строк: ' + CAST(@@ROWCOUNT AS varchar(10));
END
ELSE
BEGIN
    DECLARE @inBackup int;
    SELECT @inBackup = COUNT(*) FROM dbo.bak_SponsorProgramPricelist_broadcastStart;
    PRINT 'Таблица-бэкап уже существует — снимок НЕ перезаписан (это защита от';
    PRINT 'повторного запуска). Строк в бэкапе: ' + CAST(@inBackup AS varchar(10));
END

-------------------------------------------------------------------------------
-- 2. Страховка: не обнулять, если бэкапа почему-то нет или он пуст
-------------------------------------------------------------------------------
IF OBJECT_ID('dbo.bak_SponsorProgramPricelist_broadcastStart', 'U') IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.bak_SponsorProgramPricelist_broadcastStart)
BEGIN
    ROLLBACK TRANSACTION;
    RAISERROR('Бэкап не создан или пуст. Изменения отменены, ничего не тронуто.', 16, 1);
    RETURN;
END

-------------------------------------------------------------------------------
-- 3. Обнуление
-------------------------------------------------------------------------------
DECLARE @affected int;

UPDATE dbo.SponsorProgramPricelist
SET broadcastStart = '19000101'
WHERE broadcastStart <> '19000101';

SET @affected = @@ROWCOUNT;
PRINT 'Обнулено строк: ' + CAST(@affected AS varchar(10));

PRINT '--- ПОСЛЕ ---';
SELECT CONVERT(varchar(8), broadcastStart, 108) AS bs, COUNT(*) AS cnt
FROM dbo.SponsorProgramPricelist
GROUP BY CONVERT(varchar(8), broadcastStart, 108);

COMMIT TRANSACTION;
PRINT 'Зафиксировано.';
PRINT '';
PRINT 'Таблицу dbo.bak_SponsorProgramPricelist_broadcastStart НЕ удалять до тех пор,';
PRINT 'пока решение об удалении поля не станет окончательным.';
