-- Фирмы-заказчики: защита от дублей и проверка формата реквизитов (24-25.10.2026, продолжение Deploy/75).
-- После 75 пробелы уже не помогали, но '333-555-6666' и '3335556666' всё ещё считались разными реквизитами, а в карточках
-- копился мусор ('-', 'ИНН/КПП' в одном поле, буквы). Теперь:
-- 1. fn_FirmKey (новая): ключ для сравнения - значение без пробелов и знаков - . , / \ _ ( ) + : ; *.
--    Заглушки из одних знаков ('-', '.', '...') ключом не стираются, иначе все фирмы с заглушкой стали бы дублями друг друга.
-- 2. FirmIUD: (а) формат: ИНН - 10 или 12 цифр, КПП - 9 знаков (цифры, на 5-6 позициях ещё заглавные латинские буквы),
--    ОГРН - 13 или 15 цифр; пустое значение допустимо (иностранные заказчики и т.п.); новые отказы FirmInnFormat,
--    FirmKppFormat, FirmOgrnFormat; (б) FirmInnUnique сравнивает ключи (ИНН, КПП, счёт) и не срабатывает при пустом ИНН;
--    в базу пишется введённое без пробелов (как в 75). Расчётный счёт на формат не проверяется.
--    ВНИМАНИЕ: карточку с неверным ИНН/КПП/ОГРН нельзя сохранить, пока поле не исправлено, даже если правят другое поле:
--    на копии Artvis так 1406 из 8477 фирм (с акциями с 2026 г. - 21), на Tumen 13 из 127 (8). Предупредить пользователей.
-- 3. sp_CheckFirmINN (предупреждение в паспорте фирмы): то же, по ключу.
-- 4. iMessage + испанский (iTranslation) для трёх новых сообщений. После наката перезапустить qd2 и веб.
-- Старые записи не правятся. Параметры и выдача процедур прежние. Идемпотентен.
-- После 78 не перезапускать 75 (несёт старые тексты FirmIUD / sp_CheckFirmINN) и 02, 08, 10, ArtvisDB/Scripts nvarchar-user-text-deploy.sql, web-i18n-sql-deploy.sql.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 78_firm-requisites-key.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO
-- Ключ для сравнения реквизитов фирмы (ИНН, КПП, счёт): без пробелов и знаков препинания, чтобы
-- '333-555-6666', '333 555 6666' и '3335556666' считались одним значением. Хранится в базе то, что ввёл
-- пользователь; ключ нужен только для проверки дублей (FirmIUD, sp_CheckFirmINN).
-- Заглушки из одних знаков ('-', '.', '...') ключом не стираются (без пробелов остаются как есть),
-- иначе все фирмы с заглушкой стали бы дублями друг друга.
CREATE OR ALTER FUNCTION [dbo].[fn_FirmKey] (@value VARCHAR(20))
RETURNS VARCHAR(20)
WITH SCHEMABINDING
AS
BEGIN
    DECLARE @noSpaces VARCHAR(20) =
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@value, ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), '');

    DECLARE @key VARCHAR(20) =
        REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(@noSpaces,
            '-', ''), '.', ''), ',', ''), '/', ''), '\', ''), '_', ''), '(', ''), ')', ''), '+', ''), ':', ''), ';', ''), '*', '');

    RETURN CASE WHEN @key = '' THEN @noSpaces ELSE @key END;
END
GO

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
@actionName       VARCHAR(32)
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
                 isIdle, kpp, egrn, okved, director, reportString, registration)
            VALUES
                (@firmID, @headCompanyID, @name, @address, @phone, @fax, @email, @account, @inn, @okonh, @okpo, 
                 @bankID, @prefix, @isIdle, @kpp, @egrn, @okved, @director, @reportString, @registration);

            SET IDENTITY_INSERT [Firm] OFF;
        END
        ELSE
        BEGIN
            INSERT INTO [Firm]
                (headCompanyID, name, address, phone, fax, email, account, inn, okonh, okpo, bankID, prefix, 
                 isIdle, kpp, egrn, okved, director, reportString, registration)
            VALUES
                (@headCompanyID, @name, @address, @phone, @fax, @email, @account, @inn, @okonh, @okpo, 
                 @bankID, @prefix, @isIdle, @kpp, @egrn, @okved, @director, @reportString, @registration);

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
            registration  = @registration
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

CREATE OR ALTER PROCEDURE [dbo].[sp_CheckFirmINN]
	(
		@inn varchar(20),
		@firmID int = null 
	)
AS
BEGIN
	SET NOCOUNT ON;

	-- пробелы и знаки препинания не считаются (' 770' = '770', '333-555' = '333555'), как в FirmIUD
	set @inn = dbo.fn_FirmKey(@inn);

	select [name]
	from Firm
	where (((@inn is null or @inn = space(0)) and inn is null)
		or (@inn is not null and @inn != space(0) and dbo.fn_FirmKey(inn) like @inn + '%')) and (@firmID is null or firmID <> @firmID)
END
GO

-- Сообщения об отказе (+ испанский, где есть многоязычность веба)
DECLARE @msg1 NVARCHAR(4000) = N'ИНН указан неверно: нужно 10 цифр (организация) или 12 цифр (ИП, физическое лицо) без пробелов, дефисов, букв и знаков. Если в поле записаны ИНН и КПП вместе — разнесите их по своим полям. Если ИНН нет — оставьте поле пустым. Операция прервана.';
DECLARE @es1  NVARCHAR(4000) = N'El ИНН es incorrecto: deben ser 10 dígitos (organización) o 12 dígitos (empresario individual, persona física), sin espacios, guiones, letras ni signos. Si el campo contiene el ИНН y el КПП juntos, sepárelos en sus campos. Si no hay ИНН, deje el campo vacío. Operación cancelada.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'FirmInnFormat')
    UPDATE dbo.iMessage SET [message] = @msg1 WHERE name = 'FirmInnFormat';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('FirmInnFormat', @msg1);
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg1 AS [source], @es1 AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);

DECLARE @msg2 NVARCHAR(4000) = N'КПП указан неверно: нужно 9 знаков — цифры, на 5-й и 6-й позициях допустимы также заглавные латинские буквы; без пробелов, дефисов и других знаков. Если КПП нет — оставьте поле пустым. Операция прервана.';
DECLARE @es2  NVARCHAR(4000) = N'El КПП es incorrecto: deben ser 9 caracteres — dígitos; en las posiciones 5 y 6 también se admiten letras latinas mayúsculas; sin espacios, guiones ni otros signos. Si no hay КПП, deje el campo vacío. Operación cancelada.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'FirmKppFormat')
    UPDATE dbo.iMessage SET [message] = @msg2 WHERE name = 'FirmKppFormat';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('FirmKppFormat', @msg2);
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg2 AS [source], @es2 AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);

DECLARE @msg3 NVARCHAR(4000) = N'ОГРН указан неверно: нужно 13 цифр (организация) или 15 цифр (ИП) без пробелов, дефисов, букв и знаков. Если ОГРН нет — оставьте поле пустым. Операция прервана.';
DECLARE @es3  NVARCHAR(4000) = N'El ОГРН es incorrecto: deben ser 13 dígitos (organización) o 15 dígitos (empresario individual), sin espacios, guiones, letras ni signos. Si no hay ОГРН, deje el campo vacío. Operación cancelada.';
IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'FirmOgrnFormat')
    UPDATE dbo.iMessage SET [message] = @msg3 WHERE name = 'FirmOgrnFormat';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('FirmOgrnFormat', @msg3);
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg3 AS [source], @es3 AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);

GO
PRINT N'78: ГОТОВО';
GO

-- Дубли ИНН+КПП+счёт по ключу, которые уже есть в базе - для ручной чистки; скрипт их не трогает
SELECT firmID, name, inn, kpp, account
FROM Firm f
WHERE dbo.fn_FirmKey(inn) <> '' AND EXISTS (SELECT 1 FROM Firm g WHERE g.firmID <> f.firmID
    AND dbo.fn_FirmKey(g.inn) = dbo.fn_FirmKey(f.inn)
    AND ISNULL(dbo.fn_FirmKey(g.kpp), '') = ISNULL(dbo.fn_FirmKey(f.kpp), '')
    AND ISNULL(dbo.fn_FirmKey(g.account), '') = ISNULL(dbo.fn_FirmKey(f.account), ''))
ORDER BY dbo.fn_FirmKey(inn), firmID;