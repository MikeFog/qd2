CREATE   PROC [dbo].[pc_PackageDiscountCalculateModel]
(
    @startDate datetime,
    @campaignTypeID tinyint,     -- у вас всегда 1
    @priceTotal decimal(18,2),           -- сумма по выбранным станциям (аналог Sum(Campaign.price))
    @sel dbo.pc_SelectedMassmedia READONLY,
    @discountValue decimal(9,4) OUTPUT,
    @packageDiscountPriceListID int OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @campaignsCount int;
	Declare @avgDurationSec float, @maxDurationSec float, @baseDurationSec float;
    -- станции без выходов в пакете не участвуют, как пустые кампании в hlp_ActionDiscountCalculate: иначе они
    -- тянули вниз среднюю длительность и завышали число станций, и пакет срывался
    SELECT @campaignsCount = COUNT(DISTINCT massmediaID), @avgDurationSec = AVG(CAST(durationSec as float)), @maxDurationSec = MAX(CAST(durationSec as float)) FROM @sel WHERE durationSec > 0;

    -- как в hlp_ActionDiscountCalculate: «процент заполнения» (eachVolume) — от средней длительности, а для акций,
    -- начавшихся с даты системной настройки «PackageDiscountMaxBasisFrom», — от максимальной
    DECLARE @maxBasisFrom datetime = dbo.f_SysParamsPackageDiscountMaxBasisFrom();
    SET @baseDurationSec = CASE WHEN @maxBasisFrom IS NOT NULL AND @startDate >= @maxBasisFrom THEN @maxDurationSec ELSE @avgDurationSec END;

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
            s.durationSec > 0
            AND CAST(s.durationSec as float) >= @baseDurationSec * p.eachVolume / 100.0
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
        pl.discount ASC,  -- самый выгодный клиенту (минимальный коэффициент)
        pl.packageDiscountPriceListID;  -- при равных — меньший ID, как в hlp_ActionDiscountCalculate
END