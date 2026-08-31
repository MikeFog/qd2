/*
    ПРОД-ДЕПЛОЙ: dbo.Firms — добавить OPTION (RECOMPILE) на основной SELECT.
    Ветка hotfix/firms-plan-stability.

    ПОВОД
      31.08.2026 ~13:00-14:00 прод встал: процедура Firms крутилась на залипшем
      плане (parameter sniffing) — 7,5 с и ~53 тыс. логических чтений на вызов,
      сотни вызовов в час, очередь в CPU на 4 ядрах Express, вымывание кэша.
      Диалог «выбор фирм» открывался больше минуты. Снялось разово через
      EXEC sp_recompile 'dbo.Firms' — но вернётся при следующем неудачном плане.

    ПОЧЕМУ RECOMPILE
      Firms — «catch-all»: набор (@param IS NULL OR col = @param) + подзапрос ai,
      который при @userId IS NULL ранжирует ВСЕ подтверждённые Action по фирме.
      Один кэш-план физически не может быть хорош для всех комбинаций параметров.
      Компиляция запроса — единицы мс; вызывается он редко (открытие диалога),
      поэтому per-call recompile дешевле любого риска plan-sniffing.
      Это временная мера — полноценный фикс (переписать подзапрос ai на
      OUTER APPLY TOP 1 + индекс Action(FirmID, isConfirmed, finishDate DESC))
      см. в задачах.

    QUOTED_IDENTIFIER ON
      Firms уже развёрнута с QI ON / ANSI_NULLS ON (sys.sql_modules). ALTER
      выполняется в отдельном батче с теми же SET-опциями, чтобы не сбить их.

    ИДЕМПОТЕНТНОСТЬ
      Повторный запуск просто перезальёт то же тело.

    ОТКАТ
      git show master:"ArtvisDB/dbo/Stored Procedures/Firms.sql"
      (заменить CREATE на ALTER, развернуть теми же двумя батчами SET ... GO).

    ЗАПУСК
      sqlcmd -S <прод-сервер> -d <прод-БД> -E -b -I -i firms-plan-stability-deploy.sql
      либо открыть в SSMS на нужной БД и выполнить целиком.
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO

/* ── Преполёт: та ли база ──────────────────────────────────────────────── */
IF OBJECT_ID('dbo.Firms') IS NULL OR OBJECT_ID('dbo.Firm') IS NULL OR OBJECT_ID('dbo.Action') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: не найдены dbo.Firms / dbo.Firm / dbo.Action. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
PRINT 'БД     : ' + DB_NAME();
PRINT 'Сервер : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
PRINT 'Firms до: ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.Firms')) LIKE '%OPTION (RECOMPILE)%'
                          THEN 'уже с OPTION (RECOMPILE)' ELSE 'без RECOMPILE (залипающий план)' END;
GO

/* ── ALTER dbo.Firms ──────────────────────────────────────────────────── */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
ALTER PROCEDURE [dbo].[Firms]
(
@firmID           SMALLINT    = NULL,
@headCompanyID    INT         = NULL,   -- новый параметр
@ShowActive       BIT         = 1,
@ShowInactive     BIT         = 0,
@lastDateBefore   DATETIME    = NULL,
@lastDateAfter    DATETIME    = NULL,
@userId           INT         = NULL,
@ShowWithAction   BIT         = 1,
@ShowWithoutAction BIT        = 1,
@name varchar(256) = null
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

	If @firmID Is Not Null
		Select @ShowActive = isIdle, @ShowInactive = ~isIdle From Firm where firmID = @firmID

    SELECT
        f.*,
        ai.finishDate AS lastDate,
        u.userName   AS lastManager,
		hc.name as headCompanyName
    FROM
        [Firm] f
		Left Join HeadCompany hc on hc.headCompanyID = f.headCompanyID
        LEFT JOIN
        (
            SELECT ActionId, FirmID, userID, finishDate
            FROM (
                SELECT
                    ActionId,
                    userID,
                    FirmID,
                    finishDate,
                    ROW_NUMBER() OVER (PARTITION BY FirmID ORDER BY finishDate DESC) AS rn
                FROM Action
                WHERE userID = ISNULL(@userId, userID) and isConfirmed = 1
            ) AS RankedActions
            WHERE rn = 1
        ) AS ai
            ON f.firmID = ai.firmID
        LEFT JOIN [User] u
            ON u.userID = ai.userID
    WHERE
        f.firmID = COALESCE(@firmID, f.firmID)
        AND (@headCompanyID IS NULL OR f.HeadCompanyID = @headCompanyID)   -- фильтрация по новой колонке
        AND ((f.isIdle = 1 AND @ShowActive   = 1) OR (f.isIdle = 0 AND @ShowInactive = 1))
        AND ((ai.userID IS NULL AND @ShowWithoutAction = 1) OR (ai.userID IS NOT NULL AND @ShowWithAction = 1))
        AND (@lastDateBefore IS NULL OR @lastDateBefore > finishDate)
        AND (@lastDateAfter  IS NULL OR @lastDateAfter  < finishDate)
        AND (@userId IS NULL OR ai.userID = ISNULL(@userId, ai.userID))
		AND (@name IS NULL OR f.name LIKE '%' + @name + '%')
    ORDER BY
        [name]
    OPTION (RECOMPILE);
END
GO

/* ── Сбросить оставшийся кэш-план (на всякий случай) ──────────────────── */
SET NOEXEC OFF;
GO
EXEC sp_recompile 'dbo.Firms';
GO

/* ── Проверка ─────────────────────────────────────────────────────────── */
SELECT
    [процедура]         = o.name,
    [quoted_identifier] = m.uses_quoted_identifier,   -- ожидается 1
    [ansi_nulls]        = m.uses_ansi_nulls,          -- ожидается 1
    [есть_RECOMPILE]    = CASE WHEN m.definition LIKE '%OPTION (RECOMPILE)%' THEN 1 ELSE 0 END, -- ожидается 1
    [изменена]          = o.modify_date
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name = 'Firms';
GO
PRINT 'Готово. Ожидается: quoted_identifier=1, ansi_nulls=1, есть_RECOMPILE=1.';
PRINT 'Контроль после нагрузки: sys.dm_exec_procedure_stats для dbo.Firms должен';
PRINT 'показывать execution_count, растущий с elapsed_ms_avg в десятках мс, не в секундах.';
GO
