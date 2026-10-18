-- Калькулятор цены, этап К.1 (docs/tasks/web-price-calculator.md, §4.3 п. 3): скидки строк одним вызовом для веба.
-- Новые объекты, существующие не меняются; десктоп их не зовёт.
--
-- 1. Тип dbo.pc_CalcRow — строка калькулятора: станция, «Цена кампании» (уже округлённая до копеек), хронометраж
--    строки в секундах, отмечена ли.
-- 2. dbo.pc_CalcDiscountsModel(@startDate, @campaignTypeID = 1, @rows) — две выборки: «Объёмная скидка» по каждой
--    строке (тем же hlp_CompanyDiscountCalculate, что ActionRecalculate и калькулятор десктопа) и «Пакетная скидка»
--    отмеченных (тем же pc_PackageDiscountCalculateModel, что десктоп, по сумме «цена × объёмная», округлённой до копеек;
--    как система — только когда станций с выходами больше одной). Веб зовёт её один раз вместо вызова на строку.
--
-- Порядок: в любое время до веб-калькулятора, с 53 — в любом порядке (станции без выходов процедура отбирает сама);
-- клиент qd2 не нужен.
-- Идемпотентен: тип создаётся, если его нет, процедура — CREATE OR ALTER. Тип занят процедурой — чтобы поменять его,
-- сначала удалить процедуру.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 54_price-calculator-discounts.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF TYPE_ID(N'dbo.pc_CalcRow') IS NULL
EXEC (N'CREATE TYPE [dbo].[pc_CalcRow] AS TABLE (
    [massmediaID] SMALLINT        NOT NULL PRIMARY KEY,
    [tariffPrice] DECIMAL (18, 2) NOT NULL,
    [durationSec] INT             NOT NULL,
    [isSelected]  BIT             NOT NULL);');
GO

CREATE OR ALTER PROCEDURE [dbo].[pc_CalcDiscountsModel]
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
GO

-- Проверка: тип и процедура на месте.
IF TYPE_ID(N'dbo.pc_CalcRow') IS NOT NULL
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.pc_CalcDiscountsModel')) LIKE N'%EXEC dbo.pc_PackageDiscountCalculateModel%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.pc_CalcDiscountsModel')) LIKE N'%WHERE isSelected = 1 AND durationSec > 0%'
    PRINT N'ГОТОВО: тип pc_CalcRow и процедура pc_CalcDiscountsModel.';
ELSE
    RAISERROR(N'54: тип или процедура не созданы.', 16, 1);
GO
