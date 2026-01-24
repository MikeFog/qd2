
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
@director         VARCHAR(32)   = NULL,
@reportString     VARCHAR(256) = NULL,
@registration     VARCHAR(256) = NULL,
@actionName       VARCHAR(32)
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    -- Проверка уникальности
    IF @actionName IN ('AddItem', 'UpdateItem') 
       AND EXISTS(
           SELECT 1 
           FROM Firm 
           WHERE ISNULL(@firmID, -1) <> firmID 
             AND inn = @inn 
             AND ISNULL(kpp, '') = ISNULL(@kpp, '') 
             AND ISNULL(account, '') = ISNULL(@account, '')
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
