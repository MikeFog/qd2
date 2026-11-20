-- Фирмы-заказчики: кто и когда создал и последним изменил карточку (20.11.2026, просьба Миши — давно хотел). Десктоп и веб.
--
--   1. Firm: колонки createDate (по умолчанию GETDATE()), createUserID, modDate, modUserID. У фирм, созданных раньше, — пусто
--      (создателя старых фирм не узнать; дату первой акции не подставляем — это не дата создания).
--   2. FirmIUD (CREATE OR ALTER, текст Deploy/78 плюс аудит): новый необязательный @loggedUserID (его подставляет DataAccessor —
--      клиент менять не нужно); AddItem пишет дату и автора создания; UpdateItem пишет дату и автора изменения, только если
--      сохранение что-то меняет (Ok без правок их не трогает; строки сравниваются с учётом регистра). Карточка, импорт фирм,
--      «Группы компаний» сохраняют через FirmIUD. Служебная фирма агитации (AgitationFraming) получает только дату создания.
--   3. Firms (CREATE OR ALTER, текст firms-plan-stability плюс два поля): creator, modifier — имена пользователей.
--   4. Карточка фирмы (паспорт сущности 16), «Общие», под «Активным объектом»: «Создана:», «Создатель:», «Изменена:»,
--      «Изменил:» — только для чтения, как в карточке акции. Разделитель над «Активным объектом» убран.
--   5. Журнал «Фирмы-заказчики» — только его набор колонок (selector 1, Deploy/100), в конце: «Дата создания», «Создатель»,
--      «Дата изменения», «Изменил». В выборе фирмы и других таблицах фирм их нет.
--   6. Испанский: недостающие подписи (уже переведённые не трогаем).
--
-- После 78, 100 и ArtvisDB/Scripts/firms-plan-stability-deploy.sql (и 08). В любое время, на Artvis и Tumen. Клиент не нужен
-- (колонки журнала видны с новыми qd2 и вебом из 100); после наката перезапустить qd2 и веб (метаданные и параметры процедур
-- читаются при старте). Идемпотентен. После 101 не перезапускать 10, 75, 78 и ArtvisDB/Scripts/firms-plan-stability-deploy.sql,
-- nvarchar-user-text-deploy.sql (старый текст FirmIUD / Firms — аудит перестанет писаться или показываться); если перезапустили —
-- повторить 101. Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 101_firm-created-modified.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO
IF OBJECT_ID(N'dbo.Firm') IS NULL OR OBJECT_ID(N'dbo.FirmIUD') IS NULL
   OR NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 1)
BEGIN
    RAISERROR(N'101: сначала 100 (набор колонок журнала фирм). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO

-- =====================================================================
-- 1. Таблица Firm
-- =====================================================================
IF COL_LENGTH(N'dbo.Firm', N'createDate') IS NULL
    ALTER TABLE dbo.Firm ADD createDate DATETIME NULL CONSTRAINT DF_Firm_createDate DEFAULT (GETDATE());
IF COL_LENGTH(N'dbo.Firm', N'createUserID') IS NULL
    ALTER TABLE dbo.Firm ADD createUserID SMALLINT NULL;
IF COL_LENGTH(N'dbo.Firm', N'modDate') IS NULL
    ALTER TABLE dbo.Firm ADD modDate DATETIME NULL;
IF COL_LENGTH(N'dbo.Firm', N'modUserID') IS NULL
    ALTER TABLE dbo.Firm ADD modUserID SMALLINT NULL;
GO

-- =====================================================================
-- 2. FirmIUD
-- =====================================================================
CREATE OR ALTER PROCEDURE [dbo].[FirmIUD]
(
@firmID           SMALLINT     OUT,
@headCompanyID    INT         = NULL,        
@name             NVARCHAR(256) = NULL,
@address          NVARCHAR(256)  = NULL,
@phone            VARCHAR(32)   = NULL,
@fax              VARCHAR(32)   = NULL,
@account          VARCHAR(20)   = NULL,
@email            VARCHAR(256) = NULL,
@inn              VARCHAR(20)   = NULL,
@kpp              VARCHAR(16)   = NULL,
@okonh            VARCHAR(20)   = NULL,
@okpo             VARCHAR(20)   = NULL,
@egrn             VARCHAR(16)   = NULL,
@okved            VARCHAR(16)   = NULL,
@bankID           SMALLINT      = NULL,
@prefix           NVARCHAR(16)  = NULL,
@isIdle           TINYINT       = 0,
@director         NVARCHAR(32)   = NULL,
@reportString     NVARCHAR(256) = NULL,
@registration     NVARCHAR(256) = NULL,
@actionName       VARCHAR(32),
@loggedUserID     SMALLINT      = NULL         -- подставляет DataAccessor: кто создал / изменил (Deploy/101)
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    -- Реквизиты хранятся и сравниваются без пробелов (обычных, неразрывных, табуляций, переводов строк):
    -- иначе ' 7701234567' и '7701234567' считались разными и проходили проверку уникальности
    SET @name    = LTRIM(RTRIM(@name));
    SET @inn     = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@inn,     ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), '');
    SET @kpp     = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@kpp,     ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), '');
    SET @egrn    = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@egrn,    ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), '');
    SET @account = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@account, ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), '');

    -- Формат реквизитов (пустое значение допустимо - так оставляют поле, когда ИНН/КПП/ОГРН нет, например у иностранных заказчиков).
    -- Правка старой карточки с мусором в поле (заглушка '-', 'ИНН/КПП' в одном поле) не сохранится, пока поле не исправлено.
    IF @actionName IN ('AddItem', 'UpdateItem')
    BEGIN
        SET @kpp = UPPER(@kpp);

        IF ISNULL(@inn, '') <> '' AND (@inn LIKE '%[^0-9]%' OR LEN(@inn) NOT IN (10, 12))
        BEGIN
            RAISERROR('FirmInnFormat', 16, 1);
            RETURN;
        END

        IF ISNULL(@kpp, '') <> ''
           AND NOT (LEN(@kpp) = 9 AND @kpp COLLATE Latin1_General_BIN LIKE '[0-9][0-9][0-9][0-9][0-9A-Z][0-9A-Z][0-9][0-9][0-9]')
        BEGIN
            RAISERROR('FirmKppFormat', 16, 1);
            RETURN;
        END

        IF ISNULL(@egrn, '') <> '' AND (@egrn LIKE '%[^0-9]%' OR LEN(@egrn) NOT IN (13, 15))
        BEGIN
            RAISERROR('FirmOgrnFormat', 16, 1);
            RETURN;
        END
    END

    -- Проверка уникальности: сравнение по ключу без знаков препинания ('333-555-6666' = '3335556666');
    -- без ИНН фирмы дублями не считаются
    IF @actionName IN ('AddItem', 'UpdateItem')
       AND ISNULL(@inn, '') <> ''
       AND EXISTS(
           SELECT 1
           FROM Firm
           WHERE ISNULL(@firmID, -1) <> firmID
             AND dbo.fn_FirmKey(Firm.inn) = dbo.fn_FirmKey(@inn)
             AND ISNULL(dbo.fn_FirmKey(Firm.kpp), '') = ISNULL(dbo.fn_FirmKey(@kpp), '')
             AND ISNULL(dbo.fn_FirmKey(Firm.account), '') = ISNULL(dbo.fn_FirmKey(@account), '')
       )
    BEGIN
        RAISERROR('FirmInnUnique', 16, 1);
        RETURN;
    END

    IF @actionName = 'AddItem'
    BEGIN
        -- 1) Создаём головную организацию если ее не выбрали
		If @headCompanyID Is Null
			EXEC [dbo].[HeadCompanyIUD]
				@headCompanyID = @headCompanyID OUTPUT,
				@name          = @name,
				@actionName    = 'AddItem';

        -- 2) Вставляем фирму с привязкой к HeadCompany
        IF @firmID IS NOT NULL
        BEGIN
            SET IDENTITY_INSERT [Firm] ON;

            INSERT INTO [Firm]
                (firmID, headCompanyID, name, address, phone, fax, email, account, inn, okonh, okpo, bankID, prefix, 
                 isIdle, kpp, egrn, okved, director, reportString, registration, createDate, createUserID)
            VALUES
                (@firmID, @headCompanyID, @name, @address, @phone, @fax, @email, @account, @inn, @okonh, @okpo, 
                 @bankID, @prefix, @isIdle, @kpp, @egrn, @okved, @director, @reportString, @registration, GETDATE(), @loggedUserID);

            SET IDENTITY_INSERT [Firm] OFF;
        END
        ELSE
        BEGIN
            INSERT INTO [Firm]
                (headCompanyID, name, address, phone, fax, email, account, inn, okonh, okpo, bankID, prefix, 
                 isIdle, kpp, egrn, okved, director, reportString, registration, createDate, createUserID)
            VALUES
                (@headCompanyID, @name, @address, @phone, @fax, @email, @account, @inn, @okonh, @okpo, 
                 @bankID, @prefix, @isIdle, @kpp, @egrn, @okved, @director, @reportString, @registration, GETDATE(), @loggedUserID);

            IF @@ROWCOUNT <> 1
            BEGIN
                RAISERROR('InternalError', 16, 1);
                RETURN;
            END

            SET @firmID = SCOPE_IDENTITY();
        END

        EXEC [dbo].[Firms]
            @firmID        = @firmID,
            @ShowActive    = 1,
            @ShowInactive  = 1
    END
    ELSE IF @actionName = 'DeleteItem'
    BEGIN
        DELETE FROM [Firm] WHERE firmID = @firmID;
		DELETE FROM HeadCompany WHERE NOT EXISTS (SELECT 1 FROM Firm WHERE HeadCompany.headCompanyID = Firm.headCompanyID)
    END
    ELSE IF @actionName = 'UpdateItem'
    BEGIN
        -- «Изменена» / «Изменил» (Deploy/101) ставим, только если сохранение что-то меняет: Ok без правок
        -- последнего изменившего не подменяет. Строки — с учётом регистра («ООО» → «Ооо» — изменение).
        DECLARE @changed BIT = 0;
        IF EXISTS (
            SELECT headCompanyID, name COLLATE Cyrillic_General_BIN, address COLLATE Cyrillic_General_BIN,
                   phone COLLATE Cyrillic_General_BIN, fax COLLATE Cyrillic_General_BIN, email COLLATE Cyrillic_General_BIN,
                   account COLLATE Cyrillic_General_BIN, inn COLLATE Cyrillic_General_BIN, kpp COLLATE Cyrillic_General_BIN,
                   okonh COLLATE Cyrillic_General_BIN, okpo COLLATE Cyrillic_General_BIN, egrn COLLATE Cyrillic_General_BIN,
                   okved COLLATE Cyrillic_General_BIN, bankID, prefix COLLATE Cyrillic_General_BIN, isIdle,
                   director COLLATE Cyrillic_General_BIN, reportString COLLATE Cyrillic_General_BIN,
                   registration COLLATE Cyrillic_General_BIN
            FROM [Firm] WHERE firmID = @firmID
            EXCEPT
            SELECT COALESCE(@headCompanyID, headCompanyID), @name COLLATE Cyrillic_General_BIN, @address COLLATE Cyrillic_General_BIN,
                   @phone COLLATE Cyrillic_General_BIN, @fax COLLATE Cyrillic_General_BIN, @email COLLATE Cyrillic_General_BIN,
                   @account COLLATE Cyrillic_General_BIN, @inn COLLATE Cyrillic_General_BIN, @kpp COLLATE Cyrillic_General_BIN,
                   @okonh COLLATE Cyrillic_General_BIN, @okpo COLLATE Cyrillic_General_BIN, @egrn COLLATE Cyrillic_General_BIN,
                   @okved COLLATE Cyrillic_General_BIN, @bankID, @prefix COLLATE Cyrillic_General_BIN, CAST(@isIdle AS BIT),
                   @director COLLATE Cyrillic_General_BIN, @reportString COLLATE Cyrillic_General_BIN,
                   @registration COLLATE Cyrillic_General_BIN
            FROM [Firm] WHERE firmID = @firmID)
            SET @changed = 1;

        -- Обновляем саму фирму
        UPDATE [Firm]
        SET headCompanyID = COALESCE(@headCompanyID, headCompanyID),
            name          = @name,
            address       = @address,
            phone         = @phone,
            fax           = @fax,
            email         = @email,
            account       = @account,
            inn           = @inn,
            kpp           = @kpp,
            okonh         = @okonh,
            okpo          = @okpo,
            egrn          = @egrn,
            okved         = @okved,
            bankID        = @bankID,
            prefix        = @prefix,
            isIdle        = @isIdle,
            director      = @director,
            reportString  = @reportString,
            registration  = @registration,
            modDate       = CASE WHEN @changed = 1 THEN GETDATE() ELSE modDate END,
            modUserID     = CASE WHEN @changed = 1 THEN @loggedUserID ELSE modUserID END
        WHERE firmID = @firmID;

		DELETE FROM HeadCompany WHERE NOT EXISTS (SELECT 1 FROM Firm WHERE HeadCompany.headCompanyID = Firm.headCompanyID)

        -- Обновляем журнал фирм с фильтрацией по HeadCompany
        EXEC [dbo].[Firms]
            @firmID        = @firmID,
            @ShowActive    = 1,
            @ShowInactive  = 1
    END
END
GO

-- =====================================================================
-- 3. Firms
-- =====================================================================
-- OPTION (RECOMPILE) на основном SELECT: процедура — «catch-all» с набором
-- (@param IS NULL OR col = @param) и подзапросом ai, который при @userId IS NULL
-- ранжирует ВСЕ подтверждённые Action по фирме. Один закэшированный план не
-- годится для всех комбинаций параметров: 31.08.2026 залипший план дал 7,5 с и
-- 53 тыс. чтений на вызов (× сотни вызовов), сервер встал. Компиляция этого
-- запроса — единицы мс, вызывается он редко (открытие диалога выбора фирм),
-- поэтому per-call recompile дешевле любого риска plan-sniffing.
CREATE OR ALTER PROCEDURE [dbo].[Firms]
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
@name nvarchar(256) = null
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
		hc.name as headCompanyName,
        cu.userName  AS creator,     -- кто создал / последним изменил карточку (Deploy/101)
        mu.userName  AS modifier
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
        LEFT JOIN [User] cu ON cu.userID = f.createUserID
        LEFT JOIN [User] mu ON mu.userID = f.modUserID
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
GRANT EXECUTE ON OBJECT::[dbo].[Firms] TO PUBLIC AS [dbo];
GO

-- =====================================================================
-- 4. Карточка фирмы: «Создана», «Создатель», «Изменена», «Изменил»
-- =====================================================================
IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 16 AND CAST(passport AS nvarchar(max)) NOT LIKE N'%name="creator"%')
BEGIN
    IF (SELECT (LEN(CAST(passport AS nvarchar(max))) - LEN(REPLACE(CAST(passport AS nvarchar(max)), N'name="isIdle"/>', N'')))
               / LEN(N'name="isIdle"/>')
        FROM dbo.iEntity WHERE entityID = 16) <> 1
        RAISERROR(N'101: в карточке фирмы не найдено ровно одно поле «Активный объект» (isIdle) — карточка не изменена.', 16, 1);
    ELSE
        UPDATE dbo.iEntity
        SET passport = REPLACE(CAST(passport AS nvarchar(max)), N'name="isIdle"/>',
            N'name="isIdle"/>' + NCHAR(13) + NCHAR(10)
            + NCHAR(9) + NCHAR(9) + N'<separator />' + NCHAR(13) + NCHAR(10)
            + NCHAR(9) + NCHAR(9) + N'<label caption="Создана:" name="createDate" type="df_datetime" />' + NCHAR(13) + NCHAR(10)
            + NCHAR(9) + NCHAR(9) + N'<label caption="Создатель:" name="creator" />' + NCHAR(13) + NCHAR(10)
            + NCHAR(9) + NCHAR(9) + N'<label caption="Изменена:" name="modDate" type="df_datetime" />' + NCHAR(13) + NCHAR(10)
            + NCHAR(9) + NCHAR(9) + N'<label caption="Изменил:" name="modifier" />')
        WHERE entityID = 16;
END
GO

-- Разделитель над «Активным объектом» убран (Миша, 20.11.2026): «Активный объект» идёт сразу за строками договора,
-- а разделитель отделяет его от блока «Создана / Изменена».
UPDATE dbo.iEntity
SET passport = REPLACE(REPLACE(CAST(passport AS nvarchar(max)),
        N'<separator />' + NCHAR(13) + NCHAR(10) + NCHAR(9) + NCHAR(9) + N'<field caption="Активный объект"', N'<field caption="Активный объект"'),
        N'<separator />' + NCHAR(10) + NCHAR(9) + NCHAR(9) + N'<field caption="Активный объект"', N'<field caption="Активный объект"')
WHERE entityID = 16;
GO

-- =====================================================================
-- 5. Журнал фирм (набор 1): колонки в конце
-- =====================================================================
INSERT INTO dbo.iEntityAttribute (entityID, alias, name, ordinal_position, selector, dataType)
SELECT 16, v.alias, v.name, v.pos, 1, NULL
FROM (VALUES (N'Дата создания', 'createDate', 35), (N'Создатель', 'creator', 40),
             (N'Дата изменения', 'modDate', 45), (N'Изменил', 'modifier', 50)) v (alias, name, pos)
WHERE NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute a WHERE a.entityID = 16 AND a.selector = 1 AND a.name = v.name);
GO

-- =====================================================================
-- 6. Испанский — только недостающие
-- =====================================================================
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'Создана:', N'Creada:'),
        (N'Создатель:', N'Creador:'),
        (N'Изменена:', N'Modificada:'),
        (N'Изменил:', N'Modificado por:'),
        (N'Дата создания', N'Fecha de creación'),
        (N'Создатель', N'Creador'),
        (N'Дата изменения', N'Fecha de modificación'),
        (N'Изменил', N'Modificado por')
        ) AS src ([source], [text])
       ON dst.lang = 'es' AND dst.context = ''
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES ('es', '', src.[source], src.[text]);
GO

-- =====================================================================
-- Проверка
-- =====================================================================
SELECT selector, ordinal_position, name, alias FROM dbo.iEntityAttribute WHERE entityID = 16 ORDER BY selector, ordinal_position;
IF COL_LENGTH(N'dbo.Firm', N'createDate') IS NOT NULL AND COL_LENGTH(N'dbo.Firm', N'createUserID') IS NOT NULL
   AND COL_LENGTH(N'dbo.Firm', N'modDate') IS NOT NULL AND COL_LENGTH(N'dbo.Firm', N'modUserID') IS NOT NULL
   AND CHARINDEX(N'@loggedUserID', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.FirmIUD')), N'')) > 0
   AND CHARINDEX(N'modUserID     = CASE', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.FirmIUD')), N'')) > 0
   AND CHARINDEX(N'AS modifier', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.Firms')), N'')) > 0
   AND CHARINDEX(N'OPTION (RECOMPILE)', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.Firms')), N'')) > 0
   AND EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 16 AND CAST(passport AS nvarchar(max)) LIKE N'%name="modifier"%')
   AND NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 16
        AND (CAST(passport AS nvarchar(max)) LIKE N'%<separator />' + NCHAR(13) + NCHAR(10) + NCHAR(9) + NCHAR(9) + N'<field caption="Активный объект"%'
          OR CAST(passport AS nvarchar(max)) LIKE N'%<separator />' + NCHAR(10) + NCHAR(9) + NCHAR(9) + N'<field caption="Активный объект"%'))
   AND (SELECT COUNT(*) FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 1 AND name IN ('createDate', 'creator', 'modDate', 'modifier')) = 4
   AND NOT EXISTS (SELECT 1 FROM dbo.iEntityAttribute WHERE entityID = 16 AND selector = 0 AND name IN ('createDate', 'creator', 'modDate', 'modifier'))
    PRINT N'ГОТОВО: фирмы — создание и изменение в карточке и в журнале.';
ELSE
    RAISERROR(N'101: аудит фирм записан не полностью.', 16, 1);
GO
SET NOEXEC OFF;
GO
