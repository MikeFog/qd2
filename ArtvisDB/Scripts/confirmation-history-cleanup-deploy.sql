/***************************************************************************************************
  Удаление «Истории подтверждений» (ConfirmationHistory) — деплой на ПРОД и на базы заказчиков

  Зачем. Журнал «Подтверждения» (сущность 129) недоступен: пункта miConfirmationHistory нет в
  iMenu, таблица пуста во всех базах. Пишут в неё только три процедуры выпусков — блок в конце
  IssueIUD / ModuleIssueIUD / PackModuleIssueID («если работали под грантором — записать, кто
  подтвердил»). Удаляем всё: таблицы ConfirmationHistory и iConfirmationType, три процедуры,
  метаданные сущности 129, запись в историю из процедур выпусков и её очистку в DeleteHistory.
  Данные не сохраняются (решение владельца, 2026-09-20).

  ЧТО НЕ ТРОГАЕТСЯ: сам режим «грантор» (кнопка в CampaignForm, параметр @grantorID в процедурах
  выпусков, права грантора) — он работает как раньше, только без записи в журнал.

  Процедуры выпусков правятся ПРЯМО ИЗ РАЗВЁРНУТОГО ОПРЕДЕЛЕНИЯ (OBJECT_DEFINITION): вырезается
  ровно один хвостовой блок, форма которого проверяется до буквы; иначе скрипт останавливается.
  Так в базу не уедут чужие правки репозитория, которых на этой базе ещё нет.

  ── ПОРЯДОК ДЕПЛОЯ ────────────────────────────────────────────────────────────────────────────
  0. BACKUP DATABASE <база> TO DISK='...' WITH COPY_ONLY, INIT;
  1. Запустить ЭТОТ скрипт ЦЕЛИКОМ, от sysadmin (иначе OBJECT_DEFINITION = NULL и скрипт
     остановится). Он одним батчем, без GO, в транзакции. Для проверки — оставить ROLLBACK в
     конце; для применения — заменить на COMMIT. Настройки SET QUOTED_IDENTIFIER/ANSI_NULLS ON
     выставляются в самом скрипте (у этих процедур они ON).
  2. Рестарт клиентского приложения (сброс кеша метаданных iEntity).
     Клиент нового билда (без сущности ConfirmationHistory) выкатывать после скрипта или вместе с ним.

  Скрипт безопасен для баз без этих объектов (например, Univer: таблицы нет) и для повторного запуска.

  ⚠ НЕ разбивать скрипт на батчи через GO с `SET XACT_ABORT ON`: при ошибке транзакция
     откатится, а sqlcmd продолжит следующие батчи в автокоммите (так уже ломали dev).
***************************************************************************************************/

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 0. Проверки
-------------------------------------------------------------------------------------------------
DECLARE @refs NVARCHAR(MAX);
SELECT @refs = STRING_AGG(CAST(o.name AS NVARCHAR(MAX)), N', ')
FROM sys.sql_modules m
JOIN sys.objects o ON o.object_id = m.object_id
WHERE (m.definition LIKE N'%ConfirmationHistory%' OR m.definition LIKE N'%iConfirmationType%')
  AND o.name NOT IN (N'ConfirmationHistories', N'ConfirmationHistoryFilter', N'ConfirmationHistoryID',
                     N'IssueIUD', N'ModuleIssueIUD', N'PackModuleIssueID', N'DeleteHistory');
IF @refs IS NOT NULL
BEGIN
    RAISERROR(N'Остановлено: на ConfirmationHistory/iConfirmationType ещё ссылаются: %s.', 16, 1, @refs);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 129 AND ISNULL(tableName, N'') <> N'ConfirmationHistory')
   OR EXISTS (SELECT 1 FROM dbo.iEntity WHERE parentId = 129)
   OR EXISTS (SELECT 1 FROM dbo.iEntityRelation WHERE parentEntityID = 129 OR childEntityID = 129)
   OR EXISTS (SELECT 1 FROM dbo.iMenu WHERE codeName = N'miConfirmationHistory')
BEGIN
    RAISERROR(N'Остановлено: сущность 129 не та, что ожидалась, или на неё есть связи/пункт меню.', 16, 1);
    ROLLBACK TRANSACTION;
    RETURN;
END;

IF OBJECT_ID(N'dbo.ConfirmationHistory', N'U') IS NOT NULL AND EXISTS (SELECT 1 FROM dbo.ConfirmationHistory)
    PRINT N'ВНИМАНИЕ: в ConfirmationHistory есть строки — они будут удалены вместе с таблицей (решение владельца: не сохранять).';

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 1. Процедуры выпусков: убрать хвостовой блок «запись в историю подтверждений»
-------------------------------------------------------------------------------------------------
DECLARE @name SYSNAME, @def NVARCHAR(MAX), @new NVARCHAR(MAX), @p INT, @pos INT, @hdr INT, @x NVARCHAR(MAX), @q INT;

DECLARE @issueProcs TABLE(n SYSNAME);
INSERT INTO @issueProcs VALUES (N'IssueIUD'), (N'ModuleIssueIUD'), (N'PackModuleIssueID');

DECLARE cur CURSOR LOCAL FAST_FORWARD FOR SELECT n FROM @issueProcs;
OPEN cur;
FETCH NEXT FROM cur INTO @name;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + @name, N'P') IS NOT NULL
    BEGIN
        SET @def = OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + @name));
        IF @def IS NULL
        BEGIN
            RAISERROR(N'Остановлено: определение %s недоступно (нужен sysadmin).', 16, 1, @name);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        SET @p = CHARINDEX(N'Exec ConfirmationHistoryID', @def);
        IF @p > 0
        BEGIN
            -- кроме этого вызова, в определении нет ни одного упоминания ConfirmationHistory
            IF CHARINDEX(N'ConfirmationHistory', @def) <> @p + 5   -- длина «Exec » (LEN хвостовой пробел не считает)
               OR CHARINDEX(N'ConfirmationHistory', @def, @p + LEN(N'Exec ConfirmationHistoryID')) > 0
            BEGIN
                RAISERROR(N'Остановлено: в %s есть и другие упоминания ConfirmationHistory.', 16, 1, @name);
                ROLLBACK TRANSACTION;
                RETURN;
            END;

            -- начало блока: последний «IF @grantorID» перед вызовом
            SET @x = LEFT(@def, @p - 1);
            SET @q = CHARINDEX(REVERSE(N'IF @grantorID'), REVERSE(@x));
            IF @q = 0
            BEGIN
                RAISERROR(N'Остановлено: в %s не найдено начало блока IF @grantorID.', 16, 1, @name);
                ROLLBACK TRANSACTION;
                RETURN;
            END;
            SET @pos = LEN(@x) - @q - LEN(N'IF @grantorID') + 2;

            -- форма блока без пробелов и переводов строк — до буквы
            SET @x = SUBSTRING(@def, @pos, LEN(@def));
            SET @x = REPLACE(REPLACE(REPLACE(REPLACE(@x, CHAR(13), N''), CHAR(10), N''), CHAR(9), N''), N' ', N'');
            IF @x NOT LIKE N'IF@grantorIDisnotNulland@actionNamein(''AddItem'',''UpdateItem'')BEGIN%'
               OR @x NOT LIKE N'%ExecConfirmationHistoryID@confirmationTypeID=2,@userID=@loggedUserId,@grantorID=@grantorID,@description=@msg,@actionName=''AddItem''END'
               OR @x NOT LIKE N'%DECLARE@msgnvarchar(4000)%'
            BEGIN
                RAISERROR(N'Остановлено: блок в %s не такой, как ожидалось — удалить вручную.', 16, 1, @name);
                ROLLBACK TRANSACTION;
                RETURN;
            END;

            SET @new = RTRIM(LEFT(@def, @pos - 1));
            SET @hdr = 0;
            SET @q = CHARINDEX(N'CREATE', @new);
            WHILE @q > 0 AND @hdr = 0
            BEGIN
                -- «CREATE» из комментария заголовка («Create date: …») не годится: нужен CREATE PROC[EDURE]
                SET @x = LTRIM(REPLACE(REPLACE(REPLACE(SUBSTRING(@new, @q + 6, 20), CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
                IF @x LIKE N'PROC%' SET @hdr = @q;
                ELSE SET @q = CHARINDEX(N'CREATE', @new, @q + 6);
            END;
            IF @hdr = 0
            BEGIN
                RAISERROR(N'Остановлено: в %s не найден заголовок CREATE.', 16, 1, @name);
                ROLLBACK TRANSACTION;
                RETURN;
            END;
            SET @new = STUFF(@new, @hdr, 6, N'ALTER');

            EXEC sys.sp_executesql @new;

            IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.' + @name)) LIKE N'%ConfirmationHistory%'
            BEGIN
                RAISERROR(N'Остановлено: после правки %s всё ещё ссылается на ConfirmationHistory.', 16, 1, @name);
                ROLLBACK TRANSACTION;
                RETURN;
            END;
        END;
    END;
    FETCH NEXT FROM cur INTO @name;
END;
CLOSE cur;
DEALLOCATE cur;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 2. DeleteHistory: убрать очистку ConfirmationHistory
-------------------------------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.DeleteHistory', N'P') IS NOT NULL
BEGIN
    SET @def = OBJECT_DEFINITION(OBJECT_ID(N'dbo.DeleteHistory'));
    IF @def IS NULL
    BEGIN
        RAISERROR(N'Остановлено: определение DeleteHistory недоступно (нужен sysadmin).', 16, 1);
        ROLLBACK TRANSACTION;
        RETURN;
    END;

    SET @p = CHARINDEX(N'delete from ConfirmationHistory', @def);
    IF @p > 0
    BEGIN
        SET @q = CHARINDEX(N'where dateCreated < @lastDate', @def, @p);
        IF @q = 0 OR @q - @p > 60
        BEGIN
            RAISERROR(N'Остановлено: форма delete в DeleteHistory не такая, как ожидалось.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;
        SET @q = @q + LEN(N'where dateCreated < @lastDate');

        -- после удаляемого оператора должен остаться только закрывающий end
        SET @x = SUBSTRING(@def, @q, LEN(@def));
        SET @x = REPLACE(REPLACE(REPLACE(REPLACE(@x, CHAR(13), N''), CHAR(10), N''), CHAR(9), N''), N' ', N'');
        IF @x <> N'end'
        BEGIN
            RAISERROR(N'Остановлено: после delete в DeleteHistory не только end.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;

        SET @new = RTRIM(LEFT(@def, @p - 1)) + NCHAR(13) + NCHAR(10) + N'end';
        SET @hdr = 0;
        SET @q = CHARINDEX(N'CREATE', @new);
        WHILE @q > 0 AND @hdr = 0
        BEGIN
            -- «CREATE» из комментария заголовка («Create date: …») не годится: нужен CREATE PROC[EDURE]
            SET @x = LTRIM(REPLACE(REPLACE(REPLACE(SUBSTRING(@new, @q + 6, 20), CHAR(13), N' '), CHAR(10), N' '), CHAR(9), N' '));
            IF @x LIKE N'PROC%' SET @hdr = @q;
            ELSE SET @q = CHARINDEX(N'CREATE', @new, @q + 6);
        END;
        IF @hdr = 0
        BEGIN
            RAISERROR(N'Остановлено: в DeleteHistory не найден заголовок CREATE.', 16, 1);
            ROLLBACK TRANSACTION;
            RETURN;
        END;
        SET @new = STUFF(@new, @hdr, 6, N'ALTER');

        EXEC sys.sp_executesql @new;
    END;
END;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 3. Метаданные сущности 129 и её процедур
-------------------------------------------------------------------------------------------------
DECLARE @sp TABLE(id INT PRIMARY KEY);
INSERT INTO @sp SELECT storedProcedureID FROM dbo.iStoredProcedure
WHERE name IN (N'ConfirmationHistories', N'ConfirmationHistoryFilter', N'ConfirmationHistoryID');

DECLARE @act TABLE(id INT PRIMARY KEY);
INSERT INTO @act SELECT entityActionID FROM dbo.iEntityAction WHERE entityID = 129;

DELETE FROM dbo.GroupRight        WHERE entityActionID IN (SELECT id FROM @act);
DELETE FROM dbo.UserAdditionRight WHERE entityActionID IN (SELECT id FROM @act);
DELETE FROM dbo.iEntityAction     WHERE entityActionID IN (SELECT id FROM @act);

DELETE FROM dbo.iModuleProcedure  WHERE entityID = 129 OR storedProcedureID IN (SELECT id FROM @sp);
DELETE FROM dbo.iTableAlias       WHERE storedProcedureID IN (SELECT id FROM @sp);

DELETE FROM dbo.iEntityAttribute  WHERE entityID = 129;
DELETE FROM dbo.iEntity           WHERE entityID = 129;

DELETE FROM dbo.iStoredProcedure  WHERE storedProcedureID IN (SELECT id FROM @sp);

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 4. DROP процедур и таблиц
-------------------------------------------------------------------------------------------------
DROP PROCEDURE IF EXISTS dbo.ConfirmationHistories;
DROP PROCEDURE IF EXISTS dbo.ConfirmationHistoryFilter;
DROP PROCEDURE IF EXISTS dbo.ConfirmationHistoryID;

DROP TABLE IF EXISTS dbo.ConfirmationHistory;
DROP TABLE IF EXISTS dbo.iConfirmationType;

-------------------------------------------------------------------------------------------------
-- РАЗДЕЛ 5. Проверки (должны быть все нули / OK)
-------------------------------------------------------------------------------------------------
SELECT leftover_objects = COUNT(*) FROM sys.objects
    WHERE name IN (N'ConfirmationHistory', N'iConfirmationType', N'ConfirmationHistories', N'ConfirmationHistoryFilter', N'ConfirmationHistoryID');
SELECT modules_still_referencing = COUNT(*) FROM sys.sql_modules
    WHERE definition LIKE N'%ConfirmationHistory%' OR definition LIKE N'%iConfirmationType%';
SELECT leftover_iEntity = COUNT(*) FROM dbo.iEntity WHERE entityID = 129;
SELECT leftover_iEntityAction = COUNT(*) FROM dbo.iEntityAction WHERE entityID = 129;
SELECT leftover_iEntityAttribute = COUNT(*) FROM dbo.iEntityAttribute WHERE entityID = 129;
SELECT leftover_iModuleProcedure = COUNT(*) FROM dbo.iModuleProcedure WHERE entityID = 129;
SELECT leftover_iStoredProcedure = COUNT(*) FROM dbo.iStoredProcedure WHERE name LIKE N'ConfirmationHistor%';
SELECT orphan_iModuleProcedure = COUNT(*) FROM dbo.iModuleProcedure mp WHERE NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure sp WHERE sp.storedProcedureID = mp.storedProcedureID);
SELECT orphan_iTableAlias = COUNT(*) FROM dbo.iTableAlias ta WHERE NOT EXISTS (SELECT 1 FROM dbo.iStoredProcedure sp WHERE sp.storedProcedureID = ta.storedProcedureID);
SELECT orphan_GroupRight = COUNT(*) FROM dbo.GroupRight gr WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAction ea WHERE ea.entityActionID = gr.entityActionID);
SELECT orphan_UserAdditionRight = COUNT(*) FROM dbo.UserAdditionRight ur WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAction ea WHERE ea.entityActionID = ur.entityActionID);
-- настройки процедур сохранены (ожидается 1 / 1), режим грантора на месте (@grantorID в определении):
SELECT o.name, m.uses_quoted_identifier, m.uses_ansi_nulls, has_grantorID = CASE WHEN m.definition LIKE N'%@grantorID%' THEN 1 ELSE 0 END
FROM sys.sql_modules m JOIN sys.objects o ON o.object_id = m.object_id
WHERE o.name IN (N'IssueIUD', N'ModuleIssueIUD', N'PackModuleIssueID', N'DeleteHistory') ORDER BY o.name;
-- стартовые метаданные должны отработать:
EXEC dbo.ProcedureConfigurationRetrieve;

PRINT N'=== проверьте вывод выше. Для применения: заменить ROLLBACK на COMMIT ===';
ROLLBACK TRANSACTION;
-- COMMIT TRANSACTION;
