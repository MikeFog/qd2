-- =============================================
-- Дата начала акции, с которой «процент заполнения» пакетной скидки (PackageDiscountPriceList.eachVolume)
-- считается от МАКСИМАЛЬНОЙ длительности кампании акции, а не от средней. Настройка «Системные настройки»
-- (iInternalVariable, 'PackageDiscountMaxBasisFrom'). NULL — настройка не задана: везде считается от средней.
-- Читают hlp_ActionDiscountCalculate (система) и pc_PackageDiscountCalculateModel (калькулятор цены).
-- =============================================
CREATE FUNCTION f_SysParamsPackageDiscountMaxBasisFrom
(
)
returns datetime
AS
BEGIN
	declare @fromDate datetime
	select top 1 @fromDate = cast([value] as datetime) from iInternalVariable where [name] = 'PackageDiscountMaxBasisFrom'
	return @fromDate
END
