
CREATE PROCEDURE [dbo].[FirmIUD]
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
