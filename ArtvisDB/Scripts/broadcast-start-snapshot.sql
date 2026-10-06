/***************************************************************************************************
  broadcastStart, шаг 3: контрольный замер «до/после» (docs/broadcast-start.md, §7)

  ТОЛЬКО ДЛЯ ArtvisDev. Ничего не меняет: всё выполняется внутри одной транзакции, которая в конце
  откатывается; пишущие процедуры вызываются в точках сохранения (SAVE TRAN) и тоже откатываются.
  Счётчики IDENTITY у ProgramIssue и TariffWindow перед каждой пробной вставкой ставятся на исходное
  значение (чтобы новые ID совпадали между прогонами) и в конце возвращаются.

  Выводит текстом результаты всех процедур слоя 1 на реальных данных, секциями «#### …».
  Порядок строк внутри секции не важен — сравнение сортирует строки (broadcast-start-snapshot-compare.ps1).

  Что вызывается
    Спонсорский тракт, проход 1 — все спонсорские кампании с выпусками:
      SponsorCampaignPrograms, ProgramIssuesDays, ProgramIssues, CampaignDaysTreePassport, GetIssuesPrice,
      GetPriceByPeriod, SetIssueRatio, SponsorCampaignProgramDelete (админ и не админ), ProgramIssueIUD
      (пробное добавление +1 и +7 дней) — по кампании целиком и по каждому дню выпусков;
      по акциям: ActionRecalculate (итоги кампаний и ratio выпусков после пересчёта), rpt_GenericBill;
      отчёты за периоды: stat_VolumeOfRealization3 / ByMonth3 (→ stat_GetPrice_proc / ByMonth_proc),
      stat_Bonuses, stat_SponsorBusiness, CampaignsForActJournalRetrieve; SponsorPricelistByDate.
    Спонсорский тракт, проход 2 — граница суток: у части кампаний первый выпуск переносится ровно на
      00:00 своего дня, последний — на 00:00 следующего дня (в данных таких выпусков нет, а именно
      там включительные границы BETWEEN отличаются от «приведения к дате»); те же вызовы для них.
    Линейный тракт: TariffWindowIUD (UpdateItem без изменений и со сменой времени, AddItem),
      MediaPlanRetrieve_v2, Pricelists, ModulePriceLists, TariffPassport, CampaignDaysTreePassport,
      RollerSubstitutionPassport, PricelistIUD и sponsorPLIUD (UpdateItem с теми же значениями).

  Запуск (PowerShell, из папки ArtvisDB\Scripts; ~10–20 минут). ВСЁ ЭТО ВРЕМЯ qd2 на ArtvisDev не
  откроется: DBCC CHECKIDENT внутри транзакции держит Sch-M на ProgramIssue/TariffWindow до её конца.
  -y 4000 обрезает картинки (печать/подпись) в счёте — иначе вывод ~330 МБ.
    sqlcmd -S "(local)\sqlexpress" -d ArtvisDev -E -f 65001 -I -y 4000 -s "|" -i broadcast-start-snapshot.sql -o bs-before.txt
    ... правки процедур ...
    sqlcmd ... -o bs-after.txt
    .\broadcast-start-snapshot-compare.ps1 bs-before.txt bs-after.txt
  Перед первой правкой снять ДВА «до» и сравнить их между собой — так видно, что замер детерминирован.
***************************************************************************************************/

SET NOCOUNT ON;
SET DATEFORMAT dmy;
SET XACT_ABORT OFF;

DECLARE @admin SMALLINT = (SELECT userID FROM [User] WHERE loginName = 'sveta');
DECLARE @plain SMALLINT = (SELECT userID FROM [User] WHERE loginName = 'fog');
IF dbo.f_IsAdmin(@admin) <> 1 OR dbo.f_IsAdmin(@plain) <> 0
BEGIN
    RAISERROR(N'Остановлено: нужен админ sveta и не админ fog (ArtvisDev).', 16, 1);
    RETURN;
END;

DECLARE @identPI BIGINT = IDENT_CURRENT('dbo.ProgramIssue'),
        @identTW BIGINT = IDENT_CURRENT('dbo.TariffWindow');
DECLARE @reseedPI NVARCHAR(200) = N'DBCC CHECKIDENT (''dbo.ProgramIssue'', RESEED, ' + CAST(@identPI AS NVARCHAR(20)) + N') WITH NO_INFOMSGS;',
        @reseedTW NVARCHAR(200) = N'DBCC CHECKIDENT (''dbo.TariffWindow'', RESEED, ' + CAST(@identTW AS NVARCHAR(20)) + N') WITH NO_INFOMSGS;';

BEGIN TRANSACTION;

DECLARE @sec NVARCHAR(400), @pass INT = 1,
        @c INT, @a INT, @ag INT, @cs DATETIME, @cf DATETIME, @d DATETIME, @d2 DATETIME,
        @p DECIMAL(18,2), @tp DECIMAL(18,2), @tx DECIMAL(18,2), @total DECIMAL(18,2),
        @iid INT, @prog SMALLINT, @tar INT, @idate DATETIME, @iprice DECIMAL(18,2), @adv SMALLINT, @shift INT, @newID INT;

-------------------------------------------------------------------------------------------------
-- СПОНСОРСКИЙ ТРАКТ: проход 1 — все данные, проход 2 — выпуски на границе суток
-------------------------------------------------------------------------------------------------
DECLARE @work TABLE (campaignID INT PRIMARY KEY, actionID INT, startDate DATETIME, finishDate DATETIME);
DECLARE @acts TABLE (actionID INT PRIMARY KEY);
-- journal = 1: на этом периоде звать и журнал актов (он медленный: квартал по агентству — до 6 мин)
DECLARE @periods TABLE (n INT IDENTITY PRIMARY KEY, s DATETIME, f DATETIME, journal BIT NOT NULL DEFAULT 0);

WHILE @pass <= 2
BEGIN
    DELETE @work; DELETE @acts; DELETE @periods;

    IF @pass = 1
    BEGIN
        INSERT @work
        SELECT c.campaignID, c.actionID, c.startDate, c.finishDate
        FROM Campaign c
        WHERE c.campaignTypeID = 2 AND EXISTS (SELECT 1 FROM ProgramIssue p WHERE p.campaignID = c.campaignID);

        -- кварталы 2024–2026 и отдельные дни с наибольшим числом спонсорских выпусков
        INSERT @periods (s, f)
        SELECT DATEFROMPARTS(y, q * 3 - 2, 1), EOMONTH(DATEFROMPARTS(y, q * 3, 1))
        FROM (VALUES (2024), (2025), (2026)) Y(y) CROSS JOIN (VALUES (1), (2), (3), (4)) Q(q)
        ORDER BY y, q;
        INSERT @periods (s, f, journal)
        SELECT TOP (6) dbo.ToShortDate(issueDate), dbo.ToShortDate(issueDate), 1
        FROM ProgramIssue GROUP BY dbo.ToShortDate(issueDate) ORDER BY COUNT(*) DESC, dbo.ToShortDate(issueDate);
        -- и два месяца с наибольшим числом спонсорских выпусков
        INSERT @periods (s, f, journal)
        SELECT TOP (2) DATEFROMPARTS(YEAR(issueDate), MONTH(issueDate), 1), EOMONTH(MIN(issueDate)), 1
        FROM ProgramIssue GROUP BY DATEFROMPARTS(YEAR(issueDate), MONTH(issueDate), 1)
        ORDER BY COUNT(*) DESC, DATEFROMPARTS(YEAR(issueDate), MONTH(issueDate), 1);
    END
    ELSE
    BEGIN
        SAVE TRANSACTION midnight;

        -- 40 кампаний с ≥ 2 выпусками разных дней: первый выпуск → 00:00 его дня, последний → 00:00 следующего
        DECLARE @mid TABLE (campaignID INT PRIMARY KEY, firstID INT, lastID INT);
        INSERT @mid
        SELECT TOP (40) p.campaignID, MIN(p.issueID), MAX(p.issueID)
        FROM ProgramIssue p
        GROUP BY p.campaignID
        HAVING COUNT(DISTINCT dbo.ToShortDate(p.issueDate)) >= 2
        ORDER BY p.campaignID;

        UPDATE p SET issueDate = dbo.ToShortDate(p.issueDate)
        FROM ProgramIssue p JOIN @mid m ON p.issueID = m.firstID;
        UPDATE p SET issueDate = DATEADD(DAY, 1, dbo.ToShortDate(p.issueDate))
        FROM ProgramIssue p JOIN @mid m ON p.issueID = m.lastID;

        SELECT N'#### P2 midnight issues', p.issueID, p.campaignID, p.issueDate
        FROM ProgramIssue p JOIN @mid m ON p.issueID IN (m.firstID, m.lastID);

        INSERT @work
        SELECT c.campaignID, c.actionID, c.startDate, c.finishDate
        FROM Campaign c JOIN @mid m ON m.campaignID = c.campaignID;

        -- однодневные периоды вокруг перенесённых выпусков (первые 5 кампаний — отчёты тяжёлые)
        INSERT @periods (s, f, journal)
        SELECT DISTINCT TOP (10) dd.d, dd.d, 1
        FROM (SELECT TOP (5) * FROM @mid ORDER BY campaignID) m
        JOIN ProgramIssue p ON p.issueID IN (m.firstID, m.lastID)
        CROSS APPLY (VALUES (DATEADD(DAY, -1, dbo.ToShortDate(p.issueDate))), (dbo.ToShortDate(p.issueDate))) dd(d)
        ORDER BY dd.d;
    END;

    INSERT @acts SELECT DISTINCT actionID FROM @work;

    ---------------------------------------------------------------------------------------------
    -- по кампаниям
    ---------------------------------------------------------------------------------------------
    DECLARE cc CURSOR LOCAL STATIC FOR SELECT campaignID, startDate, finishDate FROM @work ORDER BY campaignID;
    OPEN cc;
    FETCH NEXT FROM cc INTO @c, @cs, @cf;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sec = CONCAT(N'#### P', @pass, N' camp ', @c, N' ');

        SELECT @sec + N'SponsorCampaignPrograms';  EXEC dbo.SponsorCampaignPrograms @campaignID = @c;
        SELECT @sec + N'ProgramIssuesDays';        EXEC dbo.ProgramIssuesDays @campaignID = @c;
        SELECT @sec + N'ProgramIssues';            EXEC dbo.ProgramIssues @campaignID = @c;
        SELECT @sec + N'ProgramIssues period';     EXEC dbo.ProgramIssues @campaignID = @c, @startDate = @cs, @finishDate = @cf;
        SELECT @sec + N'CampaignDaysTreePassport'; EXEC dbo.CampaignDaysTreePassport @campaignID = @c, @campaignTypeID = 2;

        SET @p = NULL;
        EXEC dbo.GetIssuesPrice @campaignID = @c, @campaignTypeID = 2, @startDate = @cs, @finishDate = @cf, @price = @p OUT;
        SELECT @sec + N'GetIssuesPrice whole', @p;
        SELECT @p = NULL, @tp = NULL, @tx = NULL;
        EXEC dbo.GetPriceByPeriod @campaignID = @c, @campaignTypeID = 2, @startDate = @cs, @finishDate = @cf,
             @price = @p OUT, @tariffPrice = @tp OUT, @taxPrice = @tx OUT, @withTax = 1;
        SELECT @sec + N'GetPriceByPeriod whole', @p, @tp, @tx;

        SAVE TRANSACTION w;
        BEGIN TRY
            EXEC dbo.SetIssueRatio @campaignID = @c, @campaignTypeID = 2, @startDate = @cs, @finishDate = @cf, @ratio = 0.5;
            SELECT @sec + N'SetIssueRatio whole', issueID, ratio FROM ProgramIssue WHERE campaignID = @c;
        END TRY BEGIN CATCH SELECT @sec + N'SetIssueRatio whole ERR ' + ERROR_MESSAGE(); END CATCH;
        ROLLBACK TRANSACTION w;

        SAVE TRANSACTION w;
        BEGIN TRY
            EXEC dbo.SponsorCampaignProgramDelete @campaignID = @c, @loggedUserID = @admin, @actionName = 'DeleteItem';
            SELECT @sec + N'ProgramDelete whole', COUNT(*) FROM ProgramIssue WHERE campaignID = @c;
        END TRY BEGIN CATCH SELECT @sec + N'ProgramDelete whole ERR ' + ERROR_MESSAGE(); END CATCH;
        ROLLBACK TRANSACTION w;

        -- по дням выпусков
        DECLARE dd CURSOR LOCAL STATIC FOR
            SELECT DISTINCT dbo.ToShortDate(issueDate) FROM ProgramIssue WHERE campaignID = @c ORDER BY 1;
        OPEN dd;
        FETCH NEXT FROM dd INTO @d;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sec = CONCAT(N'#### P', @pass, N' camp ', @c, N' day ', CONVERT(CHAR(10), @d, 104), N' ');
            SET @d2 = DATEADD(DAY, 1, @d);

            SELECT @sec + N'SponsorCampaignPrograms'; EXEC dbo.SponsorCampaignPrograms @campaignID = @c, @issueDate = @d;
            SELECT @sec + N'ProgramIssues';           EXEC dbo.ProgramIssues @campaignID = @c, @issueDate = @d;
            SELECT @sec + N'ProgramIssues period';    EXEC dbo.ProgramIssues @campaignID = @c, @startDate = @d, @finishDate = @d;

            SET @p = NULL;
            EXEC dbo.GetIssuesPrice @campaignID = @c, @campaignTypeID = 2, @startDate = @d, @finishDate = @d, @price = @p OUT;
            SELECT @sec + N'GetIssuesPrice', @p;
            SELECT @p = NULL, @tp = NULL, @tx = NULL;
            EXEC dbo.GetPriceByPeriod @campaignID = @c, @campaignTypeID = 2, @startDate = @d, @finishDate = @d,
                 @price = @p OUT, @tariffPrice = @tp OUT, @taxPrice = @tx OUT, @withTax = 1;
            SELECT @sec + N'GetPriceByPeriod', @p, @tp, @tx;

            SAVE TRANSACTION w;
            EXEC dbo.SetIssueRatio @campaignID = @c, @campaignTypeID = 2, @startDate = @d, @finishDate = @d, @ratio = 0.5;
            SELECT @sec + N'SetIssueRatio d..d', issueID, ratio FROM ProgramIssue WHERE campaignID = @c AND ratio = 0.5;
            ROLLBACK TRANSACTION w;
            SAVE TRANSACTION w;
            EXEC dbo.SetIssueRatio @campaignID = @c, @campaignTypeID = 2, @startDate = @d, @finishDate = @d2, @ratio = 0.5;
            SELECT @sec + N'SetIssueRatio d..d+1', issueID, ratio FROM ProgramIssue WHERE campaignID = @c AND ratio = 0.5;
            ROLLBACK TRANSACTION w;

            SAVE TRANSACTION w;
            BEGIN TRY
                EXEC dbo.SponsorCampaignProgramDelete @campaignID = @c, @loggedUserID = @admin, @actionName = 'DeleteItem', @issueDate = @d;
                SELECT @sec + N'ProgramDelete admin', COUNT(*) FROM ProgramIssue WHERE campaignID = @c;
            END TRY BEGIN CATCH SELECT @sec + N'ProgramDelete admin ERR ' + ERROR_MESSAGE(); END CATCH;
            ROLLBACK TRANSACTION w;
            SAVE TRANSACTION w;
            BEGIN TRY
                EXEC dbo.SponsorCampaignProgramDelete @campaignID = @c, @loggedUserID = @plain, @actionName = 'DeleteItem', @issueDate = @d;
                SELECT @sec + N'ProgramDelete plain', COUNT(*) FROM ProgramIssue WHERE campaignID = @c;
            END TRY BEGIN CATCH SELECT @sec + N'ProgramDelete plain ERR ' + ERROR_MESSAGE(); END CATCH;
            ROLLBACK TRANSACTION w;

            FETCH NEXT FROM dd INTO @d;
        END;
        CLOSE dd; DEALLOCATE dd;

        -- пробное добавление выпуска: первый, средний и последний выпуск кампании, +1 и +7 дней
        DECLARE ii CURSOR LOCAL STATIC FOR
            SELECT p.issueID, p.programID, p.tariffID, p.issueDate, p.tariffPrice, p.advertTypeID, s.shift
            FROM ProgramIssue p
            CROSS JOIN (VALUES (1), (7)) s(shift)
            WHERE p.campaignID = @c
              AND p.issueID IN (
                    SELECT MIN(issueID) FROM ProgramIssue WHERE campaignID = @c
                    UNION SELECT MAX(issueID) FROM ProgramIssue WHERE campaignID = @c
                    UNION SELECT MIN(issueID) FROM (SELECT TOP (50) PERCENT issueID FROM ProgramIssue
                                                    WHERE campaignID = @c ORDER BY issueID DESC) h)
            ORDER BY p.issueID, s.shift;
        OPEN ii;
        FETCH NEXT FROM ii INTO @iid, @prog, @tar, @idate, @iprice, @adv, @shift;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SET @sec = CONCAT(N'#### P', @pass, N' camp ', @c, N' ProgramIssueIUD add from ', @iid, N' +', @shift, N' ');
            SAVE TRANSACTION w;
            EXEC sp_executesql @reseedPI;
            BEGIN TRY
                SET @newID = NULL;
                SET @idate = DATEADD(DAY, @shift, @idate);
                SELECT @sec;
                EXEC dbo.ProgramIssueIUD @issueID = @newID OUT, @campaignID = @c, @programID = @prog, @tariffID = @tar,
                     @issueDate = @idate, @tariffPrice = @iprice, @loggedUserID = @admin, @isConfirmed = 1,
                     @advertTypeID = @adv, @actionName = 'AddItem';
                SELECT @sec + N'new', @newID;
            END TRY BEGIN CATCH SELECT @sec + N'ERR ' + ERROR_MESSAGE(); END CATCH;
            ROLLBACK TRANSACTION w;
            FETCH NEXT FROM ii INTO @iid, @prog, @tar, @idate, @iprice, @adv, @shift;
        END;
        CLOSE ii; DEALLOCATE ii;

        FETCH NEXT FROM cc INTO @c, @cs, @cf;
    END;
    CLOSE cc; DEALLOCATE cc;

    ---------------------------------------------------------------------------------------------
    -- по акциям: пересчёт и счёт
    ---------------------------------------------------------------------------------------------
    DECLARE aa CURSOR LOCAL STATIC FOR SELECT actionID FROM @acts ORDER BY actionID;
    OPEN aa;
    FETCH NEXT FROM aa INTO @a;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sec = CONCAT(N'#### P', @pass, N' action ', @a, N' ');

        SAVE TRANSACTION w;
        BEGIN TRY
            SET @total = NULL;
            EXEC dbo.ActionRecalculate @actionID = @a, @loggedUserID = @admin, @totalPrice = @total OUT;
            SELECT @sec + N'ActionRecalculate total', @total;
            SELECT @sec + N'ActionRecalculate campaigns', campaignID, startDate, finishDate, discount, tariffPrice, price,
                   finalPrice, issuesCount, issuesDuration, timeBonus, programsCount, managerDiscount, discountReleaseID
            FROM Campaign WHERE actionID = @a;
            SELECT @sec + N'ActionRecalculate ratio', p.issueID, p.ratio
            FROM ProgramIssue p JOIN Campaign c ON c.campaignID = p.campaignID WHERE c.actionID = @a;
        END TRY BEGIN CATCH SELECT @sec + N'ActionRecalculate ERR ' + ERROR_MESSAGE(); END CATCH;
        ROLLBACK TRANSACTION w;

        DECLARE bb CURSOR LOCAL STATIC FOR
            SELECT DISTINCT c.agencyID, m.s, m.f
            FROM Campaign c
            CROSS APPLY (SELECT CAST(NULL AS DATETIME) s, CAST(NULL AS DATETIME) f
                         UNION
                         SELECT DATEFROMPARTS(YEAR(p.issueDate), MONTH(p.issueDate), 1), EOMONTH(p.issueDate)
                         FROM ProgramIssue p WHERE p.campaignID = c.campaignID
                         UNION
                         SELECT dbo.ToShortDate(p.issueDate), dbo.ToShortDate(p.issueDate)
                         FROM ProgramIssue p WHERE p.campaignID = c.campaignID AND @pass = 2) m
            WHERE c.actionID = @a AND c.campaignTypeID = 2 AND c.agencyID IS NOT NULL
            ORDER BY 1, 2, 3;
        OPEN bb;
        FETCH NEXT FROM bb INTO @ag, @d, @d2;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT CONCAT(@sec, N'rpt_GenericBill agency ', @ag, N' ', CONVERT(CHAR(10), @d, 104), N'-', CONVERT(CHAR(10), @d2, 104));
            BEGIN TRY
                EXEC dbo.rpt_GenericBill @actionId = @a, @agencyId = @ag, @beginDate = @d, @endDate = @d2;
            END TRY BEGIN CATCH SELECT @sec + N'rpt_GenericBill ERR ' + ERROR_MESSAGE(); END CATCH;
            FETCH NEXT FROM bb INTO @ag, @d, @d2;
        END;
        CLOSE bb; DEALLOCATE bb;

        FETCH NEXT FROM aa INTO @a;
    END;
    CLOSE aa; DEALLOCATE aa;

    ---------------------------------------------------------------------------------------------
    -- отчёты за периоды
    ---------------------------------------------------------------------------------------------
    DECLARE @journal BIT;
    DECLARE pp CURSOR LOCAL STATIC FOR SELECT s, f, journal FROM @periods ORDER BY n;
    OPEN pp;
    FETCH NEXT FROM pp INTO @d, @d2, @journal;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SET @sec = CONCAT(N'#### P', @pass, N' period ', CONVERT(CHAR(10), @d, 104), N'-', CONVERT(CHAR(10), @d2, 104), N' ');
        SELECT @sec + N'stat_VolumeOfRealization3';
        EXEC dbo.stat_VolumeOfRealization3 @StartDay = @d, @FinishDay = @d2, @loggedUserID = @admin;
        SELECT @sec + N'stat_VolumeOfRealizationByMonth3';
        EXEC dbo.stat_VolumeOfRealizationByMonth3 @StartDay = @d, @FinishDay = @d2, @loggedUserID = @admin;
        SELECT @sec + N'stat_Bonuses';
        EXEC dbo.stat_Bonuses @periodStartDate = @d, @periodFinishDate = @d2;
        SELECT @sec + N'stat_SponsorBusiness';
        EXEC dbo.stat_SponsorBusiness @StartDay = @d, @FinishDay = @d2, @loggedUserID = @admin;
        SELECT @sec + N'stat_SponsorBusiness all';
        EXEC dbo.stat_SponsorBusiness @StartDay = @d, @FinishDay = @d2, @ShowBusyOnly = 0, @loggedUserID = @admin;
        -- журнал: только агентства со спонсорскими кампаниями (broadcastStart — в спонсорской ветке)
        DECLARE ja CURSOR LOCAL STATIC FOR
            SELECT DISTINCT agencyID FROM Campaign WHERE campaignTypeID = 2 AND agencyID IS NOT NULL AND @journal = 1 ORDER BY 1;
        OPEN ja;
        FETCH NEXT FROM ja INTO @ag;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            SELECT CONCAT(@sec, N'CampaignsForActJournalRetrieve agency ', @ag);
            EXEC dbo.CampaignsForActJournalRetrieve @startDate = @d, @finishDate = @d2, @agencyID = @ag, @loggedUserID = @admin;
            FETCH NEXT FROM ja INTO @ag;
        END;
        CLOSE ja; DEALLOCATE ja;
        FETCH NEXT FROM pp INTO @d, @d2, @journal;
    END;
    CLOSE pp; DEALLOCATE pp;

    IF @pass = 2 ROLLBACK TRANSACTION midnight;
    SET @pass += 1;
END;

-- прайс-листы спонсорских программ на даты
DECLARE sp CURSOR LOCAL STATIC FOR
    SELECT s.sponsorProgramID, CONVERT(DATETIME, q.d, 104)   -- явно: FETCH в datetime не смотрит на DATEFORMAT сессии
    FROM (SELECT DISTINCT sponsorProgramID FROM SponsorProgramPricelist) s
    CROSS JOIN (VALUES ('01.01.2024'), ('01.07.2024'), ('01.01.2025'), ('01.07.2025'), ('01.01.2026'),
                       ('01.07.2026'), ('31.12.2026'), ('01.01.2027')) q(d)
    ORDER BY 1, 2;
OPEN sp;
FETCH NEXT FROM sp INTO @prog, @d;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT CONCAT(N'#### SponsorPricelistByDate ', @prog, N' ', CONVERT(CHAR(10), @d, 104));
    EXEC dbo.SponsorPricelistByDate @sponsorProgramID = @prog, @theDate = @d;
    FETCH NEXT FROM sp INTO @prog, @d;
END;
CLOSE sp; DEALLOCATE sp;

-------------------------------------------------------------------------------------------------
-- ЛИНЕЙНЫЙ ТРАКТ
-------------------------------------------------------------------------------------------------
-- TariffWindowIUD UpdateItem: 150 окон в часе 00 и 150 «случайных» (2026)
DECLARE @wid INT, @wa DATETIME, @wo DATETIME, @dur INT, @durT INT, @wprice DECIMAL(18,2), @mm INT,
        @dis BIT, @prev INT, @next INT, @mode INT;
DECLARE tw CURSOR LOCAL STATIC FOR
    SELECT w.windowId, w.windowDateActual, w.windowDateOriginal, w.duration, w.duration_total, w.price, w.massmediaID,
           w.isDisabled, w.windowPrevId, w.windowNextId, m.mode
    FROM (SELECT TOP (150) windowId FROM TariffWindow
          WHERE windowDateActual >= '01.01.2026' AND DATEPART(HOUR, windowDateActual) = 0 ORDER BY windowId DESC
          UNION
          SELECT TOP (150) windowId FROM TariffWindow
          WHERE windowDateActual >= '01.01.2026' AND windowId % 997 = 0 ORDER BY windowId DESC) s
    JOIN TariffWindow w ON w.windowId = s.windowId
    CROSS JOIN (VALUES (0), (1), (2)) m(mode)
    ORDER BY w.windowId, m.mode;
OPEN tw;
FETCH NEXT FROM tw INTO @wid, @wa, @wo, @dur, @durT, @wprice, @mm, @dis, @prev, @next, @mode;
WHILE @@FETCH_STATUS = 0
BEGIN
    -- 0 — те же значения, 1 — то же число 00:00, 2 — то же число 23:59
    SET @wa = CASE @mode WHEN 0 THEN @wa
                         WHEN 1 THEN dbo.ToShortDate(@wa)
                         ELSE DATEADD(MINUTE, 1439, dbo.ToShortDate(@wa)) END;
    SET @sec = CONCAT(N'#### TariffWindowIUD update ', @wid, N' mode ', @mode, N' ');
    SELECT @sec;
    SAVE TRANSACTION w;
    BEGIN TRY
        EXEC dbo.TariffWindowIUD @windowId = @wid, @windowDateActual = @wa, @windowDateOriginal = @wo, @duration = @dur,
             @duration_total = @durT, @price = @wprice, @massmediaID = @mm, @isDisabled = @dis,
             @windowPrevId = @prev, @windowNextId = @next, @actionName = 'UpdateItem';
    END TRY BEGIN CATCH SELECT @sec + N'ERR ' + ERROR_MESSAGE(); END CATCH;
    ROLLBACK TRANSACTION w;
    FETCH NEXT FROM tw INTO @wid, @wa, @wo, @dur, @durT, @wprice, @mm, @dis, @prev, @next, @mode;
END;
CLOSE tw; DEALLOCATE tw;

-- TariffWindowIUD AddItem: 5 радиостанций, разное время суток, последний день прайс-листа
DECLARE ta CURSOR LOCAL STATIC FOR
    SELECT pl.massmediaID, t.dt
    FROM (SELECT TOP (5) massmediaID, MAX(finishDate) fin FROM Pricelist
          WHERE '15.06.2026' BETWEEN startDate AND finishDate GROUP BY massmediaID ORDER BY massmediaID) pl
    CROSS APPLY (VALUES (CAST('15.06.2026 00:00' AS DATETIME)), ('15.06.2026 00:30'), ('15.06.2026 12:00'),
                        ('15.06.2026 23:59'), (pl.fin), (DATEADD(HOUR, 10, pl.fin))) t(dt)
    ORDER BY 1, 2;
OPEN ta;
FETCH NEXT FROM ta INTO @mm, @wa;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sec = CONCAT(N'#### TariffWindowIUD add ', @mm, N' ', CONVERT(CHAR(19), @wa, 120), N' ');
    SELECT @sec;
    SAVE TRANSACTION w;
    EXEC sp_executesql @reseedTW;
    BEGIN TRY
        EXEC dbo.TariffWindowIUD @windowDateActual = @wa, @windowDateOriginal = @wa, @duration = 60, @duration_total = 60,
             @price = 100, @massmediaID = @mm, @actionName = 'AddItem';
    END TRY BEGIN CATCH SELECT @sec + N'ERR ' + ERROR_MESSAGE(); END CATCH;
    ROLLBACK TRANSACTION w;
    FETCH NEXT FROM ta INTO @mm, @wa;
END;
CLOSE ta; DEALLOCATE ta;

-- MediaPlanRetrieve_v2: по 15 последних акций с линейными, модульными и спонсорскими кампаниями
DECLARE @fact BIT;
DECLARE mp CURSOR LOCAL STATIC FOR
    SELECT a.actionID, f.fact
    FROM (SELECT actionID FROM (SELECT TOP (15) c.actionID FROM Campaign c WHERE c.campaignTypeID = 1
                                  AND EXISTS (SELECT 1 FROM Issue i WHERE i.campaignID = c.campaignID)
                                GROUP BY c.actionID ORDER BY c.actionID DESC) x
          UNION SELECT actionID FROM (SELECT TOP (15) c.actionID FROM Campaign c WHERE c.campaignTypeID IN (3, 4)
                                GROUP BY c.actionID ORDER BY c.actionID DESC) y
          UNION SELECT actionID FROM (SELECT TOP (15) c.actionID FROM Campaign c WHERE c.campaignTypeID = 2
                                  AND EXISTS (SELECT 1 FROM ProgramIssue p WHERE p.campaignID = c.campaignID)
                                GROUP BY c.actionID ORDER BY c.actionID DESC) z) a
    CROSS JOIN (VALUES (CAST(1 AS BIT)), (CAST(0 AS BIT))) f(fact)
    ORDER BY 1, 2;
OPEN mp;
FETCH NEXT FROM mp INTO @a, @fact;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT CONCAT(N'#### MediaPlanRetrieve_v2 action ', @a, N' fact ', @fact);
    EXEC dbo.MediaPlanRetrieve_v2 @actionId = @a, @isFact = @fact;
    FETCH NEXT FROM mp INTO @a, @fact;
END;
CLOSE mp; DEALLOCATE mp;

SELECT N'#### Pricelists';       EXEC dbo.Pricelists;
SELECT N'#### ModulePriceLists'; EXEC dbo.ModulePriceLists;

-- TariffPassport: по прайс-листам и по тарифам в часы 00 и 23 + каждый 20-й
DECLARE tp CURSOR LOCAL STATIC FOR SELECT pricelistID FROM Pricelist ORDER BY 1;
OPEN tp;
FETCH NEXT FROM tp INTO @iid;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT CONCAT(N'#### TariffPassport pricelist ', @iid);
    EXEC dbo.TariffPassport @pricelistID = @iid;
    FETCH NEXT FROM tp INTO @iid;
END;
CLOSE tp; DEALLOCATE tp;
DECLARE tt CURSOR LOCAL STATIC FOR
    SELECT tariffID FROM Tariff WHERE DATEPART(HOUR, [time]) IN (0, 23) OR tariffID % 20 = 0 ORDER BY 1;
OPEN tt;
FETCH NEXT FROM tt INTO @iid;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT CONCAT(N'#### TariffPassport tariff ', @iid);
    EXEC dbo.TariffPassport @tariffId = @iid;
    FETCH NEXT FROM tt INTO @iid;
END;
CLOSE tt; DEALLOCATE tt;

-- CampaignDaysTreePassport: по 60 кампаний типов 1, 3, 4 — сначала с выпусками в окнах часа 00
DECLARE @ctype TINYINT;
DECLARE ct CURSOR LOCAL STATIC FOR
    SELECT campaignID, campaignTypeID FROM (
        SELECT c.campaignID, c.campaignTypeID,
               ROW_NUMBER() OVER (PARTITION BY c.campaignTypeID ORDER BY h.hasMidnight DESC, c.campaignID DESC) rn
        FROM Campaign c
        CROSS APPLY (SELECT CASE WHEN EXISTS (SELECT 1 FROM Issue i JOIN TariffWindow w ON w.windowId = i.actualWindowID
                                              WHERE i.campaignID = c.campaignID AND DATEPART(HOUR, w.windowDateActual) = 0)
                                 THEN 1 ELSE 0 END hasMidnight) h
        WHERE c.campaignTypeID IN (1, 3, 4)) x
    WHERE rn <= 60 ORDER BY 2, 1;
OPEN ct;
FETCH NEXT FROM ct INTO @c, @ctype;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT CONCAT(N'#### CampaignDaysTreePassport ', @c, N' type ', @ctype);
    EXEC dbo.CampaignDaysTreePassport @campaignID = @c, @campaignTypeID = @ctype;
    FETCH NEXT FROM ct INTO @c, @ctype;
END;
CLOSE ct; DEALLOCATE ct;

-- RollerSubstitutionPassport: 150 пар кампания+ролик (линейные/модульные/пакетные)
DECLARE @rol INT;
DECLARE rs CURSOR LOCAL STATIC FOR
    SELECT TOP (150) i.campaignID, c.campaignTypeID, i.rollerID
    FROM Issue i JOIN Campaign c ON c.campaignID = i.campaignID
    WHERE c.campaignTypeID IN (1, 3, 4)
    GROUP BY i.campaignID, c.campaignTypeID, i.rollerID
    ORDER BY i.campaignID DESC, i.rollerID;
OPEN rs;
FETCH NEXT FROM rs INTO @c, @ctype, @rol;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT CONCAT(N'#### RollerSubstitutionPassport ', @c, N' roller ', @rol);
    EXEC dbo.RollerSubstitutionPassport @campaignID = @c, @campaignTypeID = @ctype, @rollerID = @rol;
    FETCH NEXT FROM rs INTO @c, @ctype, @rol;
END;
CLOSE rs; DEALLOCATE rs;

-------------------------------------------------------------------------------------------------
-- ПРАЙС-ЛИСТЫ: UpdateItem с теми же значениями
-------------------------------------------------------------------------------------------------
DECLARE @plID SMALLINT, @ps DATETIME, @pf DATETIME, @e1 TINYINT, @e2 TINYINT, @e3 TINYINT, @bonus SMALLINT, @alone BIT;
DECLARE pl CURSOR LOCAL STATIC FOR
    SELECT pricelistID, massmediaID, startDate, finishDate, extraChargeFirstRoller, extraChargeSecondRoller, extraChargeLastRoller
    FROM Pricelist ORDER BY 1;
OPEN pl;
FETCH NEXT FROM pl INTO @plID, @mm, @ps, @pf, @e1, @e2, @e3;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sec = CONCAT(N'#### PricelistIUD update ', @plID, N' ');
    SELECT @sec;
    SAVE TRANSACTION w;
    BEGIN TRY
        EXEC dbo.PricelistIUD @pricelistID = @plID OUT, @massmediaID = @mm, @startDate = @ps, @finishDate = @pf,
             @extraChargeFirstRoller = @e1, @extraChargeSecondRoller = @e2, @extraChargeLastRoller = @e3, @actionName = 'UpdateItem';
    END TRY BEGIN CATCH SELECT @sec + N'ERR ' + ERROR_MESSAGE(); END CATCH;
    ROLLBACK TRANSACTION w;
    FETCH NEXT FROM pl INTO @plID, @mm, @ps, @pf, @e1, @e2, @e3;
END;
CLOSE pl; DEALLOCATE pl;

DECLARE spl CURSOR LOCAL STATIC FOR
    SELECT pricelistID, sponsorProgramID, startDate, finishDate, bonus, isStandAlone FROM SponsorProgramPricelist ORDER BY 1;
OPEN spl;
FETCH NEXT FROM spl INTO @plID, @prog, @ps, @pf, @bonus, @alone;
WHILE @@FETCH_STATUS = 0
BEGIN
    SET @sec = CONCAT(N'#### sponsorPLIUD update ', @plID, N' ');
    SELECT @sec;
    SAVE TRANSACTION w;
    BEGIN TRY
        EXEC dbo.sponsorPLIUD @pricelistID = @plID, @sponsorProgramID = @prog, @startDate = @ps, @finishDate = @pf,
             @bonus = @bonus, @isStandAlone = @alone, @actionName = 'UpdateItem';
    END TRY BEGIN CATCH SELECT @sec + N'ERR ' + ERROR_MESSAGE(); END CATCH;
    ROLLBACK TRANSACTION w;
    FETCH NEXT FROM spl INTO @plID, @prog, @ps, @pf, @bonus, @alone;
END;
CLOSE spl; DEALLOCATE spl;

-------------------------------------------------------------------------------------------------
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
EXEC sp_executesql @reseedPI;
EXEC sp_executesql @reseedTW;
SELECT N'#### END trancount', @@TRANCOUNT;
