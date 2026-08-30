/*
    ПРОД-ДЕПЛОЙ: GetIssuesPrice / SetIssueRatio — CROSS APPLY вместо
    INNER JOIN Issue x TariffWindow по диапазону дат.
    Ветка feature/recalc-join-fix (коммит 80155cf).

    ЗАЧЕМ
      Прямой INNER JOIN Issue -> TariffWindow с фильтром dayOriginal BETWEEN
      оптимизатор строил как Hash Match и в build-фазу вычитывал ВЕСЬ срез
      TariffWindow за период по всем СМИ (~165 тыс. строк на месяц), чтобы
      сматчить его с десятками выпусков одной кампании. Обе процедуры зовутся
      из ActionRecalculate в курсоре по каждой кампании акции.
      Замер на восстановленном проде: GetIssuesPrice ~30 -> 0,2 мс на вызов;
      ActionRecalculate (9 кампаний) 558 -> 66 мс.
      Эквивалентность A/B (issues-join-fix-ab.sql): 121 акция / 337 кампаний /
      0 расхождений old vs new.

    ПОЧЕМУ SET QUOTED_IDENTIFIER ON / SET ANSI_NULLS ON ОБЯЗАТЕЛЬНЫ
      В базе есть индекс по вычисляемому столбцу TariffWindow.windowTime.
      Модуль, развёрнутый с QUOTED_IDENTIFIER OFF, ломает планы с участием
      TariffWindow (ошибка 1934 при DML по Issue / TariffWindow). Настройки
      SET фиксируются вместе с текстом модуля в момент ALTER, поэтому их надо
      выставить в ОТДЕЛЬНОМ батче перед каждым ALTER.

    ИДЕМПОТЕНТНОСТЬ
      Повторный запуск просто перезальёт те же тела с теми же SET-опциями.

    ОТКАТ
      git show master:"ArtvisDB/dbo/Stored Procedures/GetIssuesPrice.sql"
      git show master:"ArtvisDB/dbo/Stored Procedures/SetIssueRatio.sql"
      В каждой заменить CREATE на ALTER и развернуть теми же двумя батчами
      SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; GO  ...  GO.

    РЯДОМ
      roller-agitation-index-deploy.sql — индекс IX_Roller_rolActionTypeID
      под проверку AgitationMixError в IssueIUD / ModuleIssueIUD.

    ПОСЛЕ ДЕПЛОЯ
      Перезапуск клиентов не требуется (сигнатуры процедур не менялись), но
      если разворачивается вместе с другими правками — перезапустить, т.к.
      SqlHelperParameterCache кэширует сигнатуры на время жизни процесса.

    ЗАПУСК
      sqlcmd -S <прод-сервер> -d <прод-БД> -E -b -I -i issues-join-fix-deploy.sql
      (-I не обязателен: скрипт сам выставляет QUOTED_IDENTIFIER в нужных батчах;
       -b — чтобы sqlcmd прервался на первой ошибке)
      либо открыть в SSMS на нужной БД и выполнить целиком.
*/

-- При необходимости раскомментировать и подставить имя прод-БД:
-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO

/* ── Преполёт: та ли база ──────────────────────────────────────────────── */
IF OBJECT_ID('dbo.GetIssuesPrice') IS NULL
   OR OBJECT_ID('dbo.SetIssueRatio') IS NULL
   OR OBJECT_ID('dbo.ActionRecalculate') IS NULL
   OR OBJECT_ID('dbo.TariffWindow') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: не найдены dbo.GetIssuesPrice / dbo.SetIssueRatio / dbo.ActionRecalculate / dbo.TariffWindow. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
PRINT 'БД      : ' + DB_NAME();
PRINT 'Сервер  : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
PRINT 'GetIssuesPrice до : ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.GetIssuesPrice')) LIKE '%CROSS APPLY%'
                                    THEN 'уже CROSS APPLY' ELSE 'старая версия (join)' END;
PRINT 'SetIssueRatio  до : ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.SetIssueRatio'))  LIKE '%CROSS APPLY%'
                                    THEN 'уже CROSS APPLY' ELSE 'старая версия (join)' END;
GO

/* ── GetIssuesPrice ───────────────────────────────────────────────────── */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
ALTER           Procedure [dbo].[GetIssuesPrice]
(
@campaignID int,
@campaignTypeID int,
@startDate datetime,
@finishDate DATETIME,
@price decimal(18,2) = 0 out
)
as
SET NOCOUNT on
SET @startDate = dbo.ToShortDate(@startDate)
SET @finishDate = dbo.ToShortDate(@finishDate)

If @campaignTypeID = 1	Begin
	-- CROSS APPLY + TOP 1 вместо inner join: у кампании десятки-сотни выпусков,
	-- но прямой join оптимизатор строит как Hash Match и в build-фазу вычитывает
	-- весь срез TariffWindow за период по ВСЕМ СМИ (~165 тыс. строк на месяц),
	-- чтобы сматчить их с выпусками одной кампании. 30 мс вместо 0.2 мс на вызов,
	-- а ActionRecalculate зовёт это в курсоре по каждой кампании акции.
	-- windowId — PK TariffWindow, совпадение не более одного: семантика та же.
	Select
		@price = Sum(i.[tariffPrice])
	From
		Issue i
		Cross Apply
		(
			Select Top 1 1 As matched
			From TariffWindow tw
			Where tw.windowId = i.originalWindowID and
				tw.dayOriginal between @startDate and @finishDate
		) w
	Where
		i.campaignID = @campaignID
End

Else If @campaignTypeID = 2 Begin
	Select
		@price = Sum(i.[tariffPrice])
	From
		ProgramIssue i
		inner join SponsorTariff st on i.tariffID = st.tariffID
		inner join SponsorProgramPriceList pl on pl.priceListID = st.priceListID
	Where
		i.campaignID = @campaignID	and
		i.issueDate between DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), @startDate))
			and DATEADD(mi, DATEPART(mi, pl.broadcastStart), DATEADD(hh, DATEPART(hh, pl.broadcastStart), dateadd(day, 1, @finishDate)))
End

Else If @campaignTypeID = 3 Begin
	Select
		@price = Sum(i.[tariffPrice])
	From
		ModuleIssue i
	Where
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate
End

Else If @campaignTypeID = 4 Begin
	Select
		@price = Sum(i.[tariffPrice])
	From
		[PackModuleIssue] i
	Where
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate
End
GO

/* ── SetIssueRatio ────────────────────────────────────────────────────── */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
ALTER PROC [dbo].[SetIssueRatio]
(
    @campaignID int,
    @campaignTypeID int,
    @startDate datetime,
    @finishDate datetime,
    @ratio float
)
AS
BEGIN
    SET NOCOUNT ON;

    CREATE TABLE #issue
    (
        issueID int NOT NULL PRIMARY KEY
    );

    SET @startDate  = CONVERT(datetime, CONVERT(varchar(8), @startDate, 112), 112);
    SET @finishDate = CONVERT(datetime, CONVERT(varchar(8), @finishDate, 112), 112);

    -- CROSS APPLY + TOP 1 вместо INNER JOIN — см. комментарий в GetIssuesPrice:
    -- прямой join читает весь срез TariffWindow за период по всем СМИ.
    INSERT INTO #issue (issueID)
    SELECT i.issueID
    FROM Issue i
        CROSS APPLY
        (
            SELECT TOP 1 1 AS matched
            FROM TariffWindow tw
            WHERE tw.windowId = i.originalWindowID
              AND tw.dayOriginal BETWEEN @startDate AND @finishDate
        ) w
    WHERE
        i.campaignId = @campaignID;

    UPDATE i WITH (ROWLOCK)
    SET i.ratio = @ratio
    FROM Issue i
        INNER JOIN #issue x ON x.issueID = i.issueID
    WHERE i.ratio <> @ratio;

    IF @campaignTypeID = 2
        UPDATE i
        SET i.Ratio = @ratio
        FROM ProgramIssue i
            INNER JOIN Campaign c
                ON i.campaignId = @campaignID
               AND c.campaignID = i.campaignID
            INNER JOIN SponsorTariff st
                ON i.tariffID = st.tariffID
            INNER JOIN SponsorProgramPriceList pl
                ON st.pricelistID = pl.pricelistID
        WHERE
            i.issueDate BETWEEN
                DATEADD(mi, DATEPART(mi, pl.broadcastStart),
                    DATEADD(hh, DATEPART(hh, pl.broadcastStart), @startDate))
                AND
                DATEADD(mi, DATEPART(mi, pl.broadcastStart),
                    DATEADD(hh, DATEPART(hh, pl.broadcastStart), @finishDate))
            AND i.Ratio <> @ratio;

    IF @campaignTypeID = 3
        UPDATE ModuleIssue
        SET ratio = @ratio
        WHERE
            campaignId = @campaignID
            AND issueDate BETWEEN @startDate AND @finishDate
            AND ratio <> @ratio;

    IF @campaignTypeID = 4
        UPDATE [PackModuleIssue]
        SET [ratio] = @ratio
        WHERE
            [campaignID] = @campaignID
            AND [issueDate] BETWEEN @startDate AND @finishDate
            AND [ratio] <> @ratio;
END
GO

/* ── Проверка ─────────────────────────────────────────────────────────── */
SET NOEXEC OFF;
GO
SELECT
    [процедура]           = o.name,
    [quoted_identifier]   = m.uses_quoted_identifier,   -- ожидается 1
    [ansi_nulls]          = m.uses_ansi_nulls,          -- ожидается 1
    [cross_apply_в_теле]  = CASE WHEN m.definition LIKE '%CROSS APPLY%' THEN 1 ELSE 0 END, -- ожидается 1
    [изменена]            = o.modify_date
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name IN ('GetIssuesPrice', 'SetIssueRatio')
ORDER BY o.name;
GO
PRINT 'Готово. Ожидается: обе строки quoted_identifier=1, ansi_nulls=1, cross_apply_в_теле=1.';
GO
