CREATE PROCEDURE [dbo].[pc_CalcDiscountsModel]
(
    @startDate       datetime,
    @campaignTypeID  tinyint = 1,
    @rows            dbo.pc_CalcRow READONLY
)
AS
BEGIN
    -- Скидки строк калькулятора цены одним вызовом (docs/tasks/web-price-calculator.md, этап К.1): «Объёмная скидка»
    -- каждой строки и «Пакетная скидка» отмеченных. Правила не дублируются: объёмная — hlp_CompanyDiscountCalculate,
    -- как ActionRecalculate, пакетная — pc_PackageDiscountCalculateModel.
    SET NOCOUNT ON;

    DECLARE @result TABLE (massmediaID smallint NOT NULL PRIMARY KEY, companyDiscount decimal(9,4) NOT NULL);
    DECLARE @massmediaID smallint, @tariffPrice decimal(18,2), @discount decimal(9,4);

    DECLARE row_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT massmediaID, tariffPrice FROM @rows;
    OPEN row_cursor;
    FETCH NEXT FROM row_cursor INTO @massmediaID, @tariffPrice;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC dbo.hlp_CompanyDiscountCalculate
            @massMediaID = @massmediaID,
            @campaignTypeID = @campaignTypeID,
            @startDate = @startDate,
            @tariffPrice = @tariffPrice,
            @discountValue = @discount OUTPUT;
        INSERT INTO @result (massmediaID, companyDiscount) VALUES (@massmediaID, @discount);
        FETCH NEXT FROM row_cursor INTO @massmediaID, @tariffPrice;
    END
    CLOSE row_cursor;
    DEALLOCATE row_cursor;

    -- пакетная — по отмеченным строкам, сумма «цена × объёмная», округлённая до копеек; как система (ActionRecalculate) —
    -- только когда станций с выходами больше одной (пакетов на одну станцию и так нет)
    DECLARE @sel dbo.pc_SelectedMassmedia, @priceTotal decimal(18,2),
            @packageDiscount decimal(9,4) = 1, @packageDiscountPriceListID int = NULL;
    INSERT INTO @sel (massmediaID, durationSec)
        SELECT massmediaID, durationSec FROM @rows WHERE isSelected = 1 AND durationSec > 0; -- без станций без выходов и до Deploy/53
    SELECT @priceTotal = ROUND(SUM(r.tariffPrice * d.companyDiscount), 2)
    FROM @rows r
        JOIN @result d ON d.massmediaID = r.massmediaID
    WHERE r.isSelected = 1;

    IF (SELECT COUNT(*) FROM @rows WHERE isSelected = 1 AND durationSec > 0) > 1
        EXEC dbo.pc_PackageDiscountCalculateModel
            @startDate = @startDate,
            @campaignTypeID = @campaignTypeID,
            @priceTotal = @priceTotal,
            @sel = @sel,
            @discountValue = @packageDiscount OUTPUT,
            @packageDiscountPriceListID = @packageDiscountPriceListID OUTPUT;

    SELECT massmediaID, companyDiscount FROM @result ORDER BY massmediaID;
    SELECT packageDiscount = ISNULL(@packageDiscount, 1), packageDiscountPriceListID = @packageDiscountPriceListID;
END
