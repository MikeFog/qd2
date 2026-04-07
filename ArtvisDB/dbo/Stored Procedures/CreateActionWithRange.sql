CREATE PROCEDURE [dbo].[CreateActionWithRange]
    @massmediaString varchar(8000),
    @agencyString varchar(8000),
    @loggedUserId smallint,
    @firmID smallint,
    @paymentTypeID smallint,
    @actionID int output
AS
BEGIN
    SET NOCOUNT ON;

    -- Убираем лишние запятые в начале и в конце строки (если они есть)
    SET @massmediaString = TRIM(',' FROM @massmediaString);
    SET @agencyString = TRIM(',' FROM @agencyString);

    -- 1. Создаем запись в Action
    INSERT INTO [Action](firmID, userID, isConfirmed)
    VALUES(@firmID, @loggedUserId, 0);

    SET @actionID = SCOPE_IDENTITY();

    -- 2. Соединяем строки через порядковый номер (ordinal)
    -- Мы убрали промежуточные таблицы, теперь всё летит напрямую в Campaign
    INSERT INTO [Campaign](
        actionID, 
        campaignTypeID, 
        massmediaID, 
        paymentTypeID, 
        agencyID, 
        modUser
    )
    SELECT 
        @actionID, 
        1,                         -- campaignTypeID
        TRY_CAST(mm.value AS INT), -- massmediaID
        @paymentTypeID, 
        TRY_CAST(ag.value AS INT), -- agencyID
        @loggedUserId
    FROM STRING_SPLIT(@massmediaString, ',', 1) AS mm
    INNER JOIN STRING_SPLIT(@agencyString, ',', 1) AS ag ON mm.ordinal = ag.ordinal;

    
    -- 3. Если нужен пересчет:
    EXEC dbo.ActionRecalculate 
        @actionID = @actionID, 
        @needShow = 0, 
        @loggedUserID = @loggedUserId;
    
END