-- Проверка: накачены ли на эту базу Deploy-скрипты, начиная с 32-го. ТОЛЬКО ЧИТАЕТ — ничего не создаёт и не меняет.
-- Для каждого скрипта смотрит те же признаки, которыми он сам проверяет себя в конце («Проверка: новые версии применены»):
-- тексты процедур (OBJECT_DEFINITION), индекс, сообщения iMessage, паспорта. Так видно не только «скрипт запускали», но и
-- «его версия сейчас действует»: если после наката запустили более старый скрипт (см. Deploy/README.md, «Не перезапускать
-- после N»), он вернул старый текст процедуры, и статус будет ЧАСТИЧНО.
--
-- Запуск: в SSMS на нужной базе (Results to Grid) или
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i check-applied-32-plus.sql
-- Нужно право VIEW DEFINITION (sysadmin / db_owner его имеют). Без него статус — «НЕ УДАЛОСЬ ПРОВЕРИТЬ», а не «НЕ ПРИМЕНЁН».
-- Клиент qd2 и веб не проверяются — это SQL-проверка (скрипты 35, 36, 39, 40 просят ещё и новую сборку).
-- Новый скрипт: одна строка в @s, его признаки — в @p (текст процедуры) и/или в блок «особые проверки».

SET NOCOUNT ON;
GO
DECLARE @r TABLE (n int NOT NULL, what nvarchar(400) NOT NULL, ok bit NOT NULL, unk bit NOT NULL DEFAULT 0, pos bit NOT NULL DEFAULT 1);
-- pos = 1: признак новой версии (есть только после наката); pos = 0: «старого фрагмента нет» — в старой версии он тоже выполняется

-- Скрипты
DECLARE @s TABLE (n int PRIMARY KEY, name nvarchar(80) NOT NULL, title nvarchar(200) NOT NULL);
INSERT @s (n, name, title) VALUES
 (32, N'32_issue-covering-index-actual-window',    N'индекс IX_Issue_campaignID_covering получает actualWindowID'),
 (33, N'33_actual-window-money',                   N'деньги, даты и отчёты по дню окна выхода (14 процедур)'),
 (34, N'34_actual-window-roller-substitute',       N'замена ролика по окну выхода (3 процедуры)'),
 (35, N'35_windows-duplicates-template-chains',    N'окна: дубли, шаблон, склейки по времени выхода (4 объекта)'),
 (36, N'36_grid-block-duration',                   N'сетка вещания: сумма под окном — все ролики блока (rpt_Grid_v3)'),
 (37, N'37_smi-position-forbidden',                N'«Локальное СМИ» и «Федеральное СМИ» не позиционируются'),
 (38, N'38_agitation-service-rollers-manual',      N'ролики обвязки агитации (7, 44, 55) ставит только система'),
 (39, N'39_tariff-passport-duration-total',        N'паспорт тарифа: «Полная продолжительность» на вкладке «Общие»'),
 (40, N'40_duration-total-not-below-duration',     N'полная продолжительность не меньше продолжительности (5 процедур)');

-- Признаки в тексте процедур: must = 1 — фрагмент должен быть, 0 — не должно быть
DECLARE @p TABLE (n int NOT NULL, obj sysname NOT NULL, pat nvarchar(300) NOT NULL, must bit NOT NULL);
INSERT @p (n, obj, pat, must) VALUES
 -- 33
 (33, N'GetIssuesPrice',                  N'%originalWindowID%', 0),
 (33, N'SetIssueRatio',                   N'%originalWindowID%', 0),
 (33, N'GetPriceByPeriod',                N'%originalWindowID%', 0),
 (33, N'rpt_GenericBill',                 N'%originalWindowID%', 0),
 (33, N'stat_Bonuses',                    N'%originalWindowID%', 0),
 (33, N'stat_GetPrice_proc',              N'%originalWindowID%', 0),
 (33, N'stat_GetPriceByMonth_proc',       N'%originalWindowID%', 0),
 (33, N'Stat_VolumeOfRealizationSec',     N'%originalWindowID%', 0),
 (33, N'statFactorAnalysis',              N'%originalWindowID%', 0),
 (33, N'MoveIssues2NewCampaign',          N'%originalWindowID%', 0),
 (33, N'ActionRecalculate',               N'%twA.windowId = i.actualWindowID%', 1),
 (33, N'ActionRecalculate',               N'%tw.windowId = i.actualWindowID -- день выхода%', 1),
 (33, N'ActionRecalculate',               N'%(VALUES (1, twA.tariffId), (2, tw.tariffId))%', 1),
 (33, N'CampaignsForActJournalRetrieve',  N'%coalesce(mi.issueDate, tw.dayOriginal)%', 1),
 (33, N'IssueTransfer',                   N'%@datesCampaignTypeID%', 1),
 (33, N'ActionActivate',                  N'%(values (1, tw.tariffId), (2, twO.tariffId))%', 1),
 -- 34
 (34, N'RollerSubstitute',                N'%d.windowID = i.originalWindowID%', 0),
 (34, N'RollerSubstitute',                N'%d.windowID = i.actualWindowID%', 1),
 (34, N'RollerSubstitutionPassport',      N'%i.originalWindowID = tw.windowId%', 0),
 (34, N'RangeSlotIssues',                 N'%twO.windowId%', 0),
 -- 35
 (35, N'TariffWindowIUD',                 N'%UIX_TariffWindow_Tariff_Date%', 1),
 (35, N'TariffWindowIUD',                 N'%f_CheckLinkedTariffWindows(@windowDateActual%', 1),
 (35, N'GenerateTariffWindowByTemplate',  N'%UIX_TariffWindow_Tariff_Date%', 1),
 (35, N'f_CheckLinkedTariffWindows',      N'%b.windowDateActual > @windowDate%', 1),
 (35, N'CheckLinkedWindows',              N'%c.windowDateActual < b.windowDateActual%', 1),
 -- 36
 (36, N'rpt_Grid_v3',                     N'%rollerDuration AS rollerDurationSum%', 1),
 (36, N'rpt_Grid_v3',                     N'%THEN 0 ELSE rollerDuration END AS rollerDurationSum%', 0),
 -- 37
 (37, N'hlp_IssueVerify',                 N'%RolType45PositionForbidden%', 1),
 (37, N'RollerSubstitute',                N'%RolType45PositionForbidden%', 1),
 (37, N'RollerSubstitute',                N'%d.windowID = i.actualWindowID%', 1),
 -- 38
 (38, N'hlp_IssueVerify',                 N'%AgitationServiceRollerManual%', 1),
 (38, N'hlp_IssueVerify',                 N'%RolType45PositionForbidden%', 1),
 (38, N'RollerSubstitute',                N'%AgitationServiceRollerManual%', 1),
 (38, N'RollerSubstitute',                N'%RolType45PositionForbidden%', 1),
 -- 40
 (40, N'TariffIUD',                       N'%ISNULL(@duration_total, duration_total)%', 1),
 (40, N'TariffIUD',                       N'%@duration_total > 0%', 0),
 (40, N'TariffWindowIUD',                 N'%@duration_total > 0%', 0),
 (40, N'TariffWindowChangeDuration',      N'%@newDuration_total > 0%', 0),
 (40, N'GenerateTariffWindowByTemplate',  N'%@duration_total > 0%', 0),
 (40, N'TariffWindowChangeDurationInDay', N'%Not (tw.duration = 0 And tw.duration_total = 0)%', 1),
 (40, N'TariffWindowChangeDurationInDay', N'%tw.duration_total > 0%', 0);

INSERT @r (n, what, ok, unk, pos)
SELECT p.n,
       p.obj + CASE WHEN o.id IS NULL THEN N': объекта нет в базе'
                    WHEN d.def IS NULL THEN N': текст процедуры не прочитан (нет права VIEW DEFINITION или она зашифрована)'
                    WHEN p.must = 1 THEN N': нет фрагмента «' + REPLACE(p.pat, N'%', N'') + N'»'
                    ELSE N': остался старый фрагмент «' + REPLACE(p.pat, N'%', N'') + N'»' END,
       CASE WHEN d.def IS NULL THEN 0
            WHEN p.must = 1 AND d.def LIKE p.pat THEN 1
            WHEN p.must = 0 AND d.def NOT LIKE p.pat THEN 1
            ELSE 0 END,
       CASE WHEN o.id IS NOT NULL AND d.def IS NULL THEN 1 ELSE 0 END,
       p.must
FROM @p p
    CROSS APPLY (SELECT OBJECT_ID(N'dbo.' + p.obj) AS id) o
    CROSS APPLY (SELECT OBJECT_DEFINITION(o.id) AS def) d;

-- Особые проверки (не текст процедуры)
-- 32: индекс
INSERT @r (n, what, ok)
SELECT 32, N'индекс IX_Issue_campaignID_covering: actualWindowID в INCLUDE',
       CASE WHEN EXISTS (
            SELECT 1
            FROM sys.indexes i
                JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
            WHERE i.object_id = OBJECT_ID(N'dbo.Issue') AND i.name = N'IX_Issue_campaignID_covering'
              AND c.name = N'actualWindowID' AND ic.is_included_column = 1) THEN 1 ELSE 0 END;

-- 37, 38: сообщения (у 35 сообщение UIX_TariffWindow_Tariff_Date не проверяется: оно появилось раньше и о накате не говорит)
INSERT @r (n, what, ok)
SELECT 37, N'iMessage: RolType45PositionForbidden', CASE WHEN EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'RolType45PositionForbidden') THEN 1 ELSE 0 END
UNION ALL
SELECT 37, N'iMessageToSubtitute: RolType45PositionForbidden', CASE WHEN EXISTS (SELECT 1 FROM dbo.iMessageToSubtitute WHERE msgError = 'RolType45PositionForbidden') THEN 1 ELSE 0 END
UNION ALL
SELECT 38, N'iMessage: AgitationServiceRollerManual', CASE WHEN EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'AgitationServiceRollerManual') THEN 1 ELSE 0 END;

-- 39: «Полная продолжительность» стоит после «Продолжительность» и до вкладки «DJin» в трёх паспортах
-- (поиск в двоичной колляции, как в самом скрипте 39)
DECLARE @fld  nvarchar(200) = N'<field caption="Полная продолжительность:" name="duration_total"/>';
DECLARE @dur  nvarchar(100) = N'name="duration"/>';
DECLARE @djin nvarchar(100) = N'caption="DJin"';

INSERT @r (n, what, ok)
SELECT 39,
       k.k + CASE WHEN v.p IS NULL THEN N': паспорт не найден'
                  ELSE N': «Полная продолжительность» не между «Продолжительность» и вкладкой «DJin»' END,
       CASE WHEN v.p IS NOT NULL
             AND CHARINDEX(@fld, v.p) > 0
             AND CHARINDEX(@dur, v.p) > 0
             AND CHARINDEX(@djin, v.p) > 0
             AND CHARINDEX(@fld, v.p) > CHARINDEX(@dur, v.p)
             AND CHARINDEX(@fld, v.p) < CHARINDEX(@djin, v.p) THEN 1 ELSE 0 END
FROM (VALUES (N'iEntity 81'), (N'iPassport TariffMass'), (N'iPassport TariffMassEdit')) k(k)
    LEFT JOIN (
        SELECT N'iEntity 81' AS k, CAST(passport AS nvarchar(max)) COLLATE Latin1_General_BIN2 AS p FROM dbo.iEntity WHERE entityID = 81
        UNION ALL
        SELECT N'iPassport ' + codeName, CAST(passport AS nvarchar(max)) COLLATE Latin1_General_BIN2 FROM dbo.iPassport WHERE codeName IN ('TariffMass', 'TariffMassEdit')
    ) v ON v.k = k.k;

-- 1. Где проверяли
SELECT @@SERVERNAME AS [Сервер], DB_NAME() AS [База], GETDATE() AS [Проверено], SUSER_SNAME() AS [Под кем];

-- 2. Итог по скриптам
SELECT s.n AS [№], s.name AS [Скрипт],
       CASE WHEN SUM(CAST(r.unk AS int)) > 0 THEN N'НЕ УДАЛОСЬ ПРОВЕРИТЬ'
            WHEN SUM(CAST(r.ok AS int)) = COUNT(*) THEN N'ПРИМЕНЁН'
            WHEN SUM(CAST(r.ok & r.pos AS int)) = 0 THEN N'НЕ ПРИМЕНЁН'
            ELSE N'ЧАСТИЧНО' END AS [Статус],
       CAST(SUM(CAST(r.ok AS int)) AS nvarchar(10)) + N' из ' + CAST(COUNT(*) AS nvarchar(10)) AS [Признаков],
       s.title AS [Что делает]
FROM @s s
    JOIN @r r ON r.n = s.n
GROUP BY s.n, s.name, s.title
ORDER BY s.n;

-- 3. Что не сошлось (пусто — всё применено). ЧАСТИЧНО = скрипт накатан, но потом запустили более старый,
--    или накатан не целиком: см. Deploy/README.md, «Не перезапускать после N» у этого скрипта
SELECT r.n AS [№], r.what AS [Не сошлось]
FROM @r r
WHERE r.ok = 0
ORDER BY r.n, r.what;

-- 4. Когда менялись объекты (ALTER) — для сверки с датой наката
SELECT o.name AS [Объект], o.modify_date AS [Изменён],
       STUFF((SELECT DISTINCT N', ' + CAST(q.n AS nvarchar(10)) FROM @p q WHERE q.obj = o.name FOR XML PATH(N'')), 1, 2, N'') AS [Скрипты]
FROM sys.objects o
WHERE o.schema_id = SCHEMA_ID(N'dbo') AND o.name IN (SELECT obj FROM @p)
ORDER BY o.modify_date DESC, o.name;
GO
