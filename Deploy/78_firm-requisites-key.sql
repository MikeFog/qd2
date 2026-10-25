-- Фирмы-заказчики: защита от дублей не обходится знаками препинания в ИНН / КПП / счёте (24.10.2026, продолжение Deploy/75).
-- После 75 пробелы уже не помогали, но '333-555-6666' и '3335556666' всё ещё считались разными реквизитами.
-- 1. Новая функция fn_FirmKey: ключ для сравнения - значение без пробелов и знаков - . , / \ _ ( ) + : ; *.
--    Заглушки из одних знаков ('-', '.', '...') ключом не стираются, иначе все фирмы с заглушкой стали бы дублями друг друга.
-- 2. FirmIUD: проверка FirmInnUnique сравнивает ключи (ИНН, КПП, счёт); в базу по-прежнему пишется то, что ввёл пользователь
--    (только без пробелов, как в 75), значения вида '7603059720/760301001' не портятся.
-- 3. sp_CheckFirmINN (предупреждение в паспорте фирмы): то же, по ключу.
-- Старые записи не правятся. Параметры и выдача процедур прежние. Идемпотентен (CREATE OR ALTER).
-- После 78 не перезапускать 75 (несёт старые тексты FirmIUD / sp_CheckFirmINN) и 02, 08, 10, ArtvisDB/Scripts nvarchar-user-text-deploy.sql, web-i18n-sql-deploy.sql.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 78_firm-requisites-key.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
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

    -- Проверка уникальности: сравнение по ключу без знаков препинания ('333-555-6666' = '3335556666')
    IF @actionName IN ('AddItem', 'UpdateItem')
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