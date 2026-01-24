CREATE PROC [dbo].[TariffWindowWithAdvertTypeRetrieve]
(
    @advertTypeId smallint,
    @pricelistId smallint = null,
    @startDate datetime = null,
    @finishDate datetime = null,
    @windowId int = null,
    @showUnconfirmed bit = 1
)
AS
BEGIN
    SET NOCOUNT ON;
/*
Оптимизация: 2025-01
Причина:
- Процедура выполнялась ~1400 ms при rows=1

Решение:
- Переписан JOIN Issue -> EXISTS
- Добавлен индекс:
  IX_Issue_OriginalWindowID_isConfirmed_rollerID
  (originalWindowID, isConfirmed, rollerID)

Результат:
- Время выполнения ~50 ms

ВАЖНО:
- Удаление/изменение индекса приведёт к резкой деградации производительности
*/


    -- Частый кейс: ищем по конкретному окну
    IF @windowId IS NOT NULL
    BEGIN
        SELECT tw.windowId
        FROM TariffWindow tw
        JOIN Tariff t ON t.tariffId = tw.tariffId
        WHERE tw.windowId = @windowId
          AND (@pricelistId IS NULL OR t.pricelistId = @pricelistId)
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)
          AND EXISTS
          (
              SELECT 1
              FROM Issue i
              JOIN Roller r ON r.rollerID = i.rollerID
              LEFT JOIN AdvertType adt ON adt.advertTypeID = r.advertTypeID
              WHERE i.originalWindowID = tw.windowId
                AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
                AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
          )
        OPTION (RECOMPILE); -- чтобы не ловить плохой план из-за разных режимов
        RETURN;
    END

    -- Общий кейс: диапазон/прайслист
    SELECT tw.windowId
    FROM TariffWindow tw
    JOIN Tariff t ON t.tariffId = tw.tariffId
    WHERE (@pricelistId IS NULL OR t.pricelistId = @pricelistId)
      AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
      AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)
      AND EXISTS
      (
          SELECT 1
          FROM Issue i
          JOIN Roller r ON r.rollerID = i.rollerID
          LEFT JOIN AdvertType adt ON adt.advertTypeID = r.advertTypeID
          WHERE i.originalWindowID = tw.windowId
            AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
            AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
      )
    OPTION (RECOMPILE);
END
