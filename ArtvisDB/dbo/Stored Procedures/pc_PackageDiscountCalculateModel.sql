CREATE   PROC [dbo].[pc_PackageDiscountCalculateModel]
(
    @startDate datetime,
    @campaignTypeID tinyint,     -- у вас всегда 1
    @priceTotal money,           -- сумма по выбранным станциям (аналог Sum(Campaign.price))
    @sel dbo.pc_SelectedMassmedia READONLY,
    @discountValue float OUTPUT,
    @packageDiscountPriceListID int OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @campaignsCount int;
	Declare @avgDurationSec float;
    SELECT @campaignsCount = COUNT(DISTINCT massmediaID), @avgDurationSec = AVG(CAST(durationSec as float)) FROM @sel;

    SET @discountValue = 1;
    SET @packageDiscountPriceListID = NULL;

    ;WITH t AS
    (
        SELECT
            m.packageDiscountPriceListID,
            campaignsCount = COUNT(DISTINCT s.massmediaID)
        FROM @sel s
        JOIN PackageDiscountMassmedia m
            ON m.massmediaID = s.massmediaID
           AND (
                (@campaignTypeID=1 AND m.isForType1=1) OR
                (@campaignTypeID=2 AND m.isForType2=1) OR
                (@campaignTypeID=3 AND m.isForType3=1)
           )
        JOIN PackageDiscountPriceList p
            ON p.packageDiscountPriceListID = m.packageDiscountPriceListID
           AND @startDate BETWEEN p.startDate AND p.finishDate
           AND p.value <= @priceTotal
        WHERE
            CAST(s.durationSec as float) >= @avgDurationSec * p.eachVolume / 100.0
        GROUP BY
            m.packageDiscountPriceListID
        HAVING
            -- пакет должен включать ровно столько станций, сколько выбрано (как в hlp_ActionDiscountCalculate)
            COUNT(DISTINCT m.massmediaID) = @campaignsCount
            AND COUNT(DISTINCT s.massmediaID) = @campaignsCount
    )
    SELECT TOP 1
        @discountValue = COALESCE(pl.discount, 1),
        @packageDiscountPriceListID = pl.packageDiscountPriceListID
    FROM t
    JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID = t.packageDiscountPriceListID
    JOIN PackageDiscount d ON d.packageDiscountId = pl.packageDiscountID
    WHERE
        d.count = t.campaignsCount
    ORDER BY
        pl.discount ASC;  -- самый выгодный клиенту (минимальный коэффициент)
END
