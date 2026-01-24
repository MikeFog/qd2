CREATE   PROCEDURE [dbo].[HeadCompanyIUD]
(
    @headCompanyID INT          = NULL OUTPUT,  -- идентификатор головной компании
    @name            NVARCHAR(256) = NULL,      -- название головной компании
    @actionName      VARCHAR(32)   -- 'AddItem', 'UpdateItem', 'DeleteItem'
)
AS
BEGIN
    SET NOCOUNT ON;

    IF @actionName = 'AddItem'
    BEGIN
        INSERT INTO HeadCompany (name)
        VALUES (@name);

        IF @@ROWCOUNT <> 1
        BEGIN
            RAISERROR('InternalError', 16, 1)
            RETURN;
        END

        SET @headCompanyID = SCOPE_IDENTITY();
    END
    ELSE IF @actionName = 'DeleteItem'
    BEGIN
        DELETE FROM HeadCompany
        WHERE headCompanyID = @headCompanyID;
    END
    ELSE IF @actionName = 'UpdateItem'
    BEGIN
        UPDATE HeadCompany
        SET name = @name
        WHERE headCompanyID = @headCompanyID;
    END
END
