-- Фирмы-заказчики: пробелы в ИНН / КПП / ОГРН / расчётном счёте больше не обходят защиту от дублей (23.10.2026).
-- Пользователи ставили пробел в начале ИНН, и две фирмы с одним ИНН+КПП+счётом проходили проверку FirmInnUnique.
-- 1. FirmIUD: перед проверкой и записью из inn, kpp, egrn и account убираются все пробелы (обычные, неразрывные, табуляции,
--    переводы строк), у name - пробелы по краям; сравнение с уже записанными фирмами тоже без пробелов.
-- 2. sp_CheckFirmINN (предупреждение в паспорте фирмы, десктоп): то же самое, прежний вызов не меняется.
-- Старые записи не правятся (в Artvis 32 ИНН, 30 КПП и 48 ОГРН с пробелами; 14 групп дублей после очистки - см. запрос в
-- конце, чистить вручную). Параметры и выдача процедур прежние. Идемпотентен (CREATE OR ALTER).
-- Не перезапускать после 75: 02, 08, 10 и ArtvisDB/Scripts nvarchar-user-text-deploy.sql, web-i18n-sql-deploy.sql (несут старые тексты FirmIUD / sp_CheckFirmINN).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 75_firm-requisites-trim.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
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

    -- Проверка уникальности (хранимые значения старых записей тоже приводим к чистому виду)
    IF @actionName IN ('AddItem', 'UpdateItem')
       AND EXISTS(
           SELECT 1
           FROM Firm
           CROSS APPLY (SELECT
               inn     = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Firm.inn,     ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), ''),
               kpp     = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Firm.kpp,     ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), ''),
               account = REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(Firm.account, ' ', ''), CHAR(9), ''), CHAR(10), ''), CHAR(13), ''), CHAR(160), '')
           ) n
           WHERE ISNULL(@firmID, -1) <> firmID
             AND n.inn = @inn
             AND ISNULL(n.kpp, '') = ISNULL(@kpp, '')
             AND ISNULL(n.account, '') = ISNULL(@account, '')
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

	-- пробелы не считаются (' 770' и '770' - один ИНН), как в FirmIUD
	set @inn = replace(replace(replace(replace(replace(@inn, ' ', ''), char(9), ''), char(10), ''), char(13), ''), char(160), '');

	select [name]
	from Firm
	where (((@inn is null or @inn = space(0)) and inn is null)
		or (@inn is not null and @inn != space(0)
			and replace(replace(replace(replace(replace(inn, ' ', ''), char(9), ''), char(10), ''), char(13), ''), char(160), '') like @inn + '%')) and (@firmID is null or firmID <> @firmID)
END
GO
PRINT N'75: ГОТОВО';
GO

-- Дубли ИНН+КПП+счёт, которые уже есть в базе (без пробелов) - для ручной чистки; скрипт их не трогает
SELECT firmID, name, inn, kpp, account
FROM Firm f
WHERE REPLACE(inn, ' ', '') <> '' AND EXISTS (SELECT 1 FROM Firm g WHERE g.firmID <> f.firmID
    AND REPLACE(g.inn, ' ', '') = REPLACE(f.inn, ' ', '')
    AND ISNULL(REPLACE(g.kpp, ' ', ''), '') = ISNULL(REPLACE(f.kpp, ' ', ''), '')
    AND ISNULL(REPLACE(g.account, ' ', ''), '') = ISNULL(REPLACE(f.account, ' ', ''), ''))
ORDER BY REPLACE(inn, ' ', ''), firmID;