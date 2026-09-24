CREATE PROC [dbo].[Pricelists]
(
    @massmediaID smallint = null,
    @pricelistID smallint = null,
    @hidePLInThePast bit = 0,
    @languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ');

    ;WITH pl0 AS
    (
        SELECT *
        FROM dbo.Pricelist pl
        WHERE pl.massmediaID = COALESCE(@massmediaID, pl.massmediaID)
          AND pl.pricelistID = COALESCE(@pricelistID, pl.pricelistID)
          AND (@hidePLInThePast = 0 OR pl.finishDate >= CAST(GETDATE() AS DATE))
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
        @tPricelistFrom + CONVERT(varchar(10), pl.startDate, 104)
        + N' (' + dbo.fn_GetTariffWindowDateRangeStr(tw.minDate, tw.maxDate, pl.broadcastStart, @languageCode) + N')' AS name,
        CONVERT(varchar(5), pl.broadcastStart, 114) AS broadcastStartString
    FROM pl0 pl
    LEFT JOIN tw ON tw.pricelistID = pl.pricelistID
    ORDER BY pl.startDate DESC;
END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Pricelists] TO PUBLIC
    AS [dbo];

