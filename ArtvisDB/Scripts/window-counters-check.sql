/*
    ТОЛЬКО ЧТЕНИЕ. Сверка счётчиков занятости окна (TariffWindow) с выпусками (Issue).

    Что должно быть в окне (правило всех писателей: IssueIUD, ModuleIssueIUD, PackModuleIssueID,
    IssueTransfer, CampaignTransferDay, ActionActivate/Deactivate, ActionIUD/CampaignIUD/Campaigns*Delete,
    RollerSubstitute, AgitationFraming, CampaignImport*):
      - считаются ВСЕ строки Issue окна выхода (actualWindowID): линейные, спонсорские, модульные,
        пакетные, служебные агитации; ProgramIssue в окна не входит;
      - выпуски акций из журнала удалённых (Action.deleteDate IS NOT NULL) НЕ считаются:
        ActionIUD DeleteItem снимает их со счётчиков, ActionRestore возвращает (только неподтверждённые);
      - окно без ёмкости (maxCapacity = 0) считает секунды (Roller.duration) в timeInUse*,
        capacityInUse* = 0; окно с ёмкостью (maxCapacity > 0) считает штуки в capacityInUse*, timeInUse* = 0;
      - позиции: -20 первый, -10 второй, 10 последний; подтверждённые — флаг is*PositionOccupied (0/1),
        неподтверждённые — счётчик *PositionsUnconfirmed.

    Сверка 19.10.2026 (2024–2026): подтверждённые счётчики (timeInUseConfirmed, capacityInUseConfirmed,
    is*PositionOccupied) совпали во ВСЕХ окнах Artvis (28.09), ArtvisDev и Tumen — формула верна.
    Неподтверждённые завышены (никогда не занижены) в окнах 07.07–28.10.2025 и с 12.01.2026 (Artvis 78 755 окон,
    почти все созданы до ~марта 2026; Tumen 1 600 окон 20.04–31.05.2026, один веер +7 с); новых почти нет: на ArtvisDev
    после копии Artvis от 28.09 — одно окно (13047668, станция 199, 17.10.2026 06:45, +5 с, выпусков нет; писатель не найден). Окно 13077994 из side-defects — НЕ расхождение:
    его неподтверждённый пакетный выпуск — акция 186335 из журнала удалённых.
    jobStyleU = 1 — счётчики посчитаны «как после Пересчитать остатки» старой версии (job_DeleteEmptyActions до Deploy/65
    считала и удалённые акции, О-7; исправлено 21.10.2026, тогда же отремонтированы неподтверждённые окон с этого дня); за 2024–2026 таких окон Artvis 1 770, ArtvisDev 1 493, Tumen 0 (март 2026 на Artvis — 320).

    @startDate/@finishDate — дни выхода окна (dayActual) включительно; @massmediaID NULL — все станции.
    READ UNCOMMITTED — не блокирует работу (возможны «мигающие» строки по окнам, которые правят прямо сейчас).
      sqlcmd -S <сервер> -d <база> -E -f 65001 -W -s "|" -i window-counters-check.sql
*/
SET NOCOUNT ON;
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

DECLARE @startDate date = '20261001',
        @finishDate date = '20261031',
        @massmediaID smallint = NULL,
        @detailRows int = 200;

IF OBJECT_ID('tempdb..#chk') IS NOT NULL DROP TABLE #chk;

;WITH agg AS (
    SELECT i.actualWindowID AS windowId,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 1 THEN r.duration ELSE 0 END) AS durC,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 0 THEN r.duration ELSE 0 END) AS durU,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 1 THEN 1 ELSE 0 END) AS cntC,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 0 THEN 1 ELSE 0 END) AS cntU,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 1 AND i.positionId = -20 THEN 1 ELSE 0 END) AS firstC,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 1 AND i.positionId = -10 THEN 1 ELSE 0 END) AS secondC,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 1 AND i.positionId =  10 THEN 1 ELSE 0 END) AS lastC,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 0 AND i.positionId = -20 THEN 1 ELSE 0 END) AS firstU,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 0 AND i.positionId = -10 THEN 1 ELSE 0 END) AS secondU,
        SUM(CASE WHEN a.deleteDate IS NULL AND i.isConfirmed = 0 AND i.positionId =  10 THEN 1 ELSE 0 END) AS lastU,
        -- акции из журнала удалённых: в счётчиках их быть не должно; если «Пересчитать остатки»
        -- (job_DeleteEmptyActions) их посчитал — расхождение ровно на эти величины
        SUM(CASE WHEN a.deleteDate IS NOT NULL THEN 1 ELSE 0 END) AS cntDel,
        SUM(CASE WHEN a.deleteDate IS NOT NULL THEN r.duration ELSE 0 END) AS durDel,
        SUM(CASE WHEN a.deleteDate IS NOT NULL AND i.positionId = -20 THEN 1 ELSE 0 END) AS firstDel,
        SUM(CASE WHEN a.deleteDate IS NOT NULL AND i.positionId = -10 THEN 1 ELSE 0 END) AS secondDel,
        SUM(CASE WHEN a.deleteDate IS NOT NULL AND i.positionId =  10 THEN 1 ELSE 0 END) AS lastDel,
        SUM(CASE WHEN a.deleteDate IS NOT NULL AND i.isConfirmed = 1 THEN 1 ELSE 0 END) AS cntDelConfirmed,
        SUM(CASE WHEN i.moduleIssueID IS NOT NULL THEN 1 ELSE 0 END) AS cntModule,
        SUM(CASE WHEN i.packModuleIssueID IS NOT NULL THEN 1 ELSE 0 END) AS cntPack
    FROM dbo.TariffWindow tw
        JOIN dbo.Issue i ON i.actualWindowID = tw.windowId
        JOIN dbo.Roller r ON r.rollerID = i.rollerID
        JOIN dbo.Campaign c ON c.campaignID = i.campaignID
        JOIN dbo.[Action] a ON a.actionID = c.actionID
    WHERE tw.dayActual BETWEEN @startDate AND @finishDate
        AND (@massmediaID IS NULL OR tw.massmediaID = @massmediaID)
    GROUP BY i.actualWindowID
)
SELECT tw.windowId, tw.massmediaID, tw.dayActual, tw.windowDateActual, tw.maxCapacity, tw.duration,
    tw.timeInUseConfirmed AS tc, CASE WHEN tw.maxCapacity = 0 THEN ISNULL(g.durC, 0) ELSE 0 END AS exp_tc,
    tw.timeInUseUnconfirmed AS tu, CASE WHEN tw.maxCapacity = 0 THEN ISNULL(g.durU, 0) ELSE 0 END AS exp_tu,
    tw.capacityInUseConfirmed AS cc, CASE WHEN tw.maxCapacity > 0 THEN ISNULL(g.cntC, 0) ELSE 0 END AS exp_cc,
    tw.capacityInUseUnconfirmed AS cu, CASE WHEN tw.maxCapacity > 0 THEN ISNULL(g.cntU, 0) ELSE 0 END AS exp_cu,
    CONVERT(int, tw.isFirstPositionOccupied) AS f, CASE WHEN ISNULL(g.firstC, 0) > 0 THEN 1 ELSE 0 END AS exp_f,
    CONVERT(int, tw.isSecondPositionOccupied) AS s, CASE WHEN ISNULL(g.secondC, 0) > 0 THEN 1 ELSE 0 END AS exp_s,
    CONVERT(int, tw.isLastPositionOccupied) AS l, CASE WHEN ISNULL(g.lastC, 0) > 0 THEN 1 ELSE 0 END AS exp_l,
    tw.firstPositionsUnconfirmed AS fu, ISNULL(g.firstU, 0) AS exp_fu,
    tw.secondPositionsUnconfirmed AS su, ISNULL(g.secondU, 0) AS exp_su,
    tw.lastPositionsUnconfirmed AS lu, ISNULL(g.lastU, 0) AS exp_lu,
    ISNULL(g.firstC, 0) AS firstC, ISNULL(g.secondC, 0) AS secondC, ISNULL(g.lastC, 0) AS lastC,
    ISNULL(g.cntC, 0) AS cntC, ISNULL(g.cntU, 0) AS cntU,
    ISNULL(g.cntDel, 0) AS cntDel, ISNULL(g.durDel, 0) AS durDel, ISNULL(g.cntDelConfirmed, 0) AS cntDelConfirmed,
    ISNULL(g.firstDel, 0) AS firstDel, ISNULL(g.secondDel, 0) AS secondDel, ISNULL(g.lastDel, 0) AS lastDel,
    ISNULL(g.cntModule, 0) AS cntModule, ISNULL(g.cntPack, 0) AS cntPack
INTO #chk
FROM dbo.TariffWindow tw
    LEFT JOIN agg g ON g.windowId = tw.windowId
WHERE tw.dayActual BETWEEN @startDate AND @finishDate
    AND (@massmediaID IS NULL OR tw.massmediaID = @massmediaID);

-- Признаки расхождений по каждой группе счётчиков
ALTER TABLE #chk ADD
    bad_tc AS CASE WHEN tc <> exp_tc THEN 1 ELSE 0 END,
    bad_tu AS CASE WHEN tu <> exp_tu THEN 1 ELSE 0 END,
    bad_cc AS CASE WHEN cc <> exp_cc THEN 1 ELSE 0 END,
    bad_cu AS CASE WHEN cu <> exp_cu THEN 1 ELSE 0 END,
    bad_pos AS CASE WHEN f <> exp_f OR s <> exp_s OR l <> exp_l THEN 1 ELSE 0 END,
    bad_posU AS CASE WHEN fu <> exp_fu OR su <> exp_su OR lu <> exp_lu THEN 1 ELSE 0 END,
    -- «как после Пересчитать остатки»: неподтверждённые = ожидаемые + выпуски удалённых акций
    jobStyleU AS CASE WHEN cntDel > 0
        AND tu = exp_tu + CASE WHEN maxCapacity = 0 THEN durDel ELSE 0 END
        AND cu = exp_cu + CASE WHEN maxCapacity > 0 THEN cntDel ELSE 0 END
        AND fu = exp_fu + firstDel AND su = exp_su + secondDel AND lu = exp_lu + lastDel THEN 1 ELSE 0 END;

-- 1) Итог: окон всего, с расхождением, по группам
SELECT COUNT(*) AS windows,
    SUM(CASE WHEN bad_tc + bad_tu + bad_cc + bad_cu + bad_pos + bad_posU > 0 THEN 1 ELSE 0 END) AS windows_bad,
    SUM(bad_tc) AS bad_tc, SUM(bad_tu) AS bad_tu, SUM(bad_cc) AS bad_cc, SUM(bad_cu) AS bad_cu,
    SUM(bad_pos) AS bad_pos, SUM(bad_posU) AS bad_posU,
    SUM(CASE WHEN bad_tu + bad_cu + bad_posU > 0 AND jobStyleU = 1 THEN 1 ELSE 0 END) AS unconf_bad_job_style,
    SUM(CASE WHEN cntDel > 0 THEN 1 ELSE 0 END) AS windows_with_deleted_action_issues
FROM #chk;

-- 2) Образцы расхождений: какие группы, знак и величина (окно может попасть в несколько строк)
SELECT pattern, maxCap, COUNT(*) AS windows, MIN(windowId) AS sample_window, MAX(windowId) AS sample_window2
FROM (
    SELECT windowId, CASE WHEN maxCapacity > 0 THEN 'cap' ELSE 'sec' END AS maxCap,
        CONCAT(
            CASE WHEN bad_tc = 1 THEN CONCAT('tc', CASE WHEN tc > exp_tc THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN bad_tu = 1 THEN CONCAT('tu', CASE WHEN tu > exp_tu THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN bad_cc = 1 THEN CONCAT('cc', CASE WHEN cc > exp_cc THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN bad_cu = 1 THEN CONCAT('cu', CASE WHEN cu > exp_cu THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN f <> exp_f THEN CONCAT('f', CASE WHEN f > exp_f THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN s <> exp_s THEN CONCAT('s', CASE WHEN s > exp_s THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN l <> exp_l THEN CONCAT('l', CASE WHEN l > exp_l THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN fu <> exp_fu THEN CONCAT('fu', CASE WHEN fu > exp_fu THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN su <> exp_su THEN CONCAT('su', CASE WHEN su > exp_su THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN lu <> exp_lu THEN CONCAT('lu', CASE WHEN lu > exp_lu THEN '+' ELSE '-' END, ' ') ELSE '' END,
            CASE WHEN jobStyleU = 1 THEN '[job] ' ELSE '' END,
            CASE WHEN cntC + cntU + cntDel = 0 THEN '[empty] ' ELSE '' END) AS pattern
    FROM #chk
    WHERE bad_tc + bad_tu + bad_cc + bad_cu + bad_pos + bad_posU > 0
) x
GROUP BY pattern, maxCap
ORDER BY windows DESC;

-- 3) Окна с расхождением (первые @detailRows по дате)
SELECT TOP (@detailRows) windowId, massmediaID, CONVERT(varchar(16), windowDateActual, 120) AS window_time,
    maxCapacity AS maxCap, duration AS dur,
    tc, exp_tc, tu, exp_tu, cc, exp_cc, cu, exp_cu,
    f, exp_f, s, exp_s, l, exp_l, fu, exp_fu, su, exp_su, lu, exp_lu,
    cntC, cntU, cntDel, cntModule, cntPack, jobStyleU
FROM #chk
WHERE bad_tc + bad_tu + bad_cc + bad_cu + bad_pos + bad_posU > 0
ORDER BY windowDateActual, windowId;

-- 4) Сводка по станциям и месяцам: сколько «лишних» секунд/штук/позиций (для решения о ремонте)
SELECT massmediaID, CONVERT(char(7), dayActual, 120) AS [month],
    SUM(CASE WHEN bad_tc + bad_tu + bad_cc + bad_cu + bad_pos + bad_posU > 0 THEN 1 ELSE 0 END) AS windows_bad,
    SUM(tc - exp_tc) AS d_tc, SUM(tu - exp_tu) AS d_tu, SUM(cc - exp_cc) AS d_cc, SUM(cu - exp_cu) AS d_cu,
    SUM(fu - exp_fu) + SUM(su - exp_su) + SUM(lu - exp_lu) AS d_posU
FROM #chk
GROUP BY massmediaID, CONVERT(char(7), dayActual, 120)
HAVING SUM(CASE WHEN bad_tc + bad_tu + bad_cc + bad_cu + bad_pos + bad_posU > 0 THEN 1 ELSE 0 END) > 0
ORDER BY [month], windows_bad DESC;

DROP TABLE #chk;
