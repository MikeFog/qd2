/*
    ЭКВИВАЛЕНТНОСТЬ: dbo.GetUniqueMMsForAction, старая форма (строка на каждый
    выпуск) vs новая (DISTINCT по станции через ROW_NUMBER + RECOMPILE).
    Ветка hotfix/getuniquemms-perf.

    Сравнивается множество (massmediaID, name, agencyID) из ПЕРВОГО result set
    процедуры. Второй result set (SELECT DISTINCT ... campaignTypeID = 2) этой
    веткой не тронут и не проверяется.

    INSERT ... EXEC требует, чтобы КАЖДЫЙ result set процедуры совпадал по
    форме с целевой таблицей — а у обеих версий (старой и новой) реальной
    dbo.GetUniqueMMsForAction второй result set другой формы (2 колонки).
    Поэтому сравниваются не сами боевые процедуры, а их "пробы" — копии с
    единственным (первым) SELECT, без второго блока.

    ПОДГОТОВКА (один раз, любой порядок относительно деплоя новой версии):

      1. GetUniqueMMsForAction_old_probe — старая форма, без второго SELECT:

           git show master:"ArtvisDB/dbo/Stored Procedures/GetUniqueMMsForAction.sql"

         Переименовать в GetUniqueMMsForAction_old_probe, удалить блок
         "Select distinct ... campaignTypeID = 2" целиком (включая ключевое
         слово END после него — END переносится на конец первого SELECT).

      2. GetUniqueMMsForAction_new_probe — новая форма (эта ветка), без
         второго SELECT: взять ArtvisDB/dbo/Stored Procedures/GetUniqueMMsForAction.sql
         из ветки hotfix/getuniquemms-perf, тот же приём (переименовать,
         вырезать второй SELECT).

      Развернуть обе с SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; GO ... GO.

      3. Выполнить этот скрипт. Ожидаемый результат — «расхождений: 0».

      4. Убрать пробы:
           DROP PROCEDURE dbo.GetUniqueMMsForAction_old_probe, dbo.GetUniqueMMsForAction_new_probe;

    Результат прогона на ArtvisDev 04.09.2026: 26 акций (25 стратифицированных
    по campaignTypeID 1-4 последних по modDate + 1 сводный сценарий по трём
    из них), 0 расхождений, 0 ошибок по (massmediaID, name, agencyID).
*/
SET NOCOUNT ON;

IF OBJECT_ID('dbo.GetUniqueMMsForAction_old_probe') IS NULL OR OBJECT_ID('dbo.GetUniqueMMsForAction_new_probe') IS NULL
BEGIN
    RAISERROR('Нужны обе пробы: GetUniqueMMsForAction_old_probe и _new_probe. См. ПОДГОТОВКА в заголовке скрипта.', 16, 1);
    RETURN;
END

/* Стратифицированная выборка: недавние подтверждённые акции разных типов */
CREATE TABLE #cases (n INT IDENTITY, label nvarchar(200), actionIDString varchar(8000) NULL, actionID int NULL, isFact bit);
INSERT INTO #cases (label, actionIDString, actionID, isFact)
SELECT TOP 25 N'акция ' + CONVERT(varchar,x.actionID) + N' (тип ' + CONVERT(varchar,x.campaignTypeID) + N')', NULL, x.actionID, 1
FROM (
    SELECT c.actionID, c.campaignTypeID, rn = ROW_NUMBER() OVER (PARTITION BY c.campaignTypeID ORDER BY MAX(a.modDate) DESC)
    FROM Campaign c JOIN [Action] a ON a.actionID = c.actionID
    WHERE a.isConfirmed = 1
    GROUP BY c.actionID, c.campaignTypeID
) x
WHERE x.rn <= 7
ORDER BY x.campaignTypeID;

-- сводный сценарий тоже проверим, если найдётся хотя бы 2 акции одного типа
INSERT INTO #cases (label, actionIDString, actionID, isFact)
SELECT TOP 1 N'сводный: ' + STRING_AGG(CONVERT(varchar,y.actionID), ','), STRING_AGG(CONVERT(varchar,y.actionID), ',') + ',', NULL, 1
FROM (SELECT TOP 3 actionID FROM #cases) y;

DECLARE @n INT = 1, @tot INT = (SELECT COUNT(*) FROM #cases), @totalDiv INT = 0, @totalErr INT = 0;
DECLARE @label nvarchar(200), @actionIDString varchar(8000), @actionID int, @isFact bit;

WHILE @n <= @tot
BEGIN
    SELECT @label=label, @actionIDString=actionIDString, @actionID=actionID, @isFact=isFact FROM #cases WHERE n=@n;
    BEGIN TRY
        IF OBJECT_ID('tempdb..#old') IS NOT NULL DROP TABLE #old;
        IF OBJECT_ID('tempdb..#new') IS NOT NULL DROP TABLE #new;
        CREATE TABLE #old (massmediaID int, [name] nvarchar(200), [date] datetime NULL, rollerID int NULL, agencyID int);
        CREATE TABLE #new (massmediaID int, [name] nvarchar(200), [date] datetime NULL, rollerID int NULL, agencyID int);

        INSERT INTO #old EXEC dbo.GetUniqueMMsForAction_old_probe @actionID=@actionID, @isFact=@isFact, @actionIDString=@actionIDString;
        INSERT INTO #new EXEC dbo.GetUniqueMMsForAction_new_probe @actionID=@actionID, @isFact=@isFact, @actionIDString=@actionIDString;

        DECLARE @div INT =
            (SELECT COUNT(*) FROM (SELECT DISTINCT massmediaID,[name],agencyID FROM #old EXCEPT SELECT DISTINCT massmediaID,[name],agencyID FROM #new) a)
          + (SELECT COUNT(*) FROM (SELECT DISTINCT massmediaID,[name],agencyID FROM #new EXCEPT SELECT DISTINCT massmediaID,[name],agencyID FROM #old) b);
        SET @totalDiv += @div;
        IF @div > 0
            PRINT @label + N': РАСХОЖДЕНИЕ = ' + CONVERT(varchar,@div);
    END TRY
    BEGIN CATCH
        SET @totalErr += 1;
        PRINT @label + N': ОШИБКА - ' + ERROR_MESSAGE();
    END CATCH
    SET @n += 1;
END

SELECT [акций проверено] = @tot, [расхождений] = @totalDiv, [ошибок] = @totalErr;
DROP TABLE #cases;
