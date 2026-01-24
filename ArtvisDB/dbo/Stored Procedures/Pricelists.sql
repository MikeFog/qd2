
CREATE PROC dbo.Pricelists
(
    @massmediaID smallint = null,
    @pricelistID smallint = null
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH pl0 AS
    (
        SELECT *
        FROM dbo.Pricelist pl
        WHERE pl.massmediaID = COALESCE(@massmediaID, pl.massmediaID)
          AND pl.pricelistID = COALESCE(@pricelistID, pl.pricelistID)
    ),
    tw AS
    (
        SELECT
            t.pricelistID,
            MIN(tw.windowDateOriginal) AS minDate,
            MAX(tw.windowDateOriginal) AS maxDate
        FROM dbo.TariffWindow tw
        INNER JOIN dbo.Tariff t ON t.tariffID = tw.tariffId
        WHERE EXISTS (SELECT 1 FROM pl0 WHERE pl0.pricelistID = t.pricelistID)
        GROUP BY t.pricelistID
    )
    SELECT
        pl.*,
        N'Прайс-лист от ' + CONVERT(varchar(10), pl.startDate, 104)
        + N' (' + dbo.fn_GetTariffWindowDateRangeStr(tw.minDate, tw.maxDate, pl.broadcastStart) + N')' AS name,
        CONVERT(varchar(5), pl.broadcastStart, 114) AS broadcastStartString
    FROM pl0 pl
    LEFT JOIN tw ON tw.pricelistID = pl.pricelistID
    ORDER BY pl.startDate DESC;
END

	




