-- Проверка справочников к новому году (только чтение, ничего не меняет). Запускать в декабре, до первых акций,
-- договоров и счетов нового года: у справочников ниже конец периода задан явно (на 31.12.2026 кончаются ставки НДС
-- всех агентств и все наборы объёмных скидок), и без строк на новый год система молча считает по-другому:
--   1. AgencyTax (ставка НДС агентства): нет строки на дату — GetTaxValue даёт 0, счета идут без НДС, договор — с
--      текстом «БЕЗ НДС» вместо предмета договора (docs/tasks/web-reports.md, «Мина «без НДС»»).
--   2. DiscountRelease + DiscountValue (набор объёмных скидок станции, сущность 22): hlp_CompanyDiscountCalculate ищет
--      набор на дату начала кампании с флагом её типа (isForType1/2/3) и хотя бы одним порогом; нет такого — скидка 1,
--      кампания идёт без объёмной скидки. Проверяется каждая пара «станция + тип кампании».
--   3. PackageDiscountPriceList (пакетная скидка): нет строки на дату — пакетной скидки нет.
--   4. Pricelist (прайс-лист станции): нет прайс-листа — нет сетки и цен на эти дни.
--   5. UserDiscount (сезонный коэффициент менеджера): нет периода — менеджер не может дать скидку (fn_GetMaxUserDiscount,
--      GetUserDiscount дают 1), калькулятор считает такие дни по 1. Администраторы не проверяются.
-- В каждом разделе — только то, что нужно в новом году и не продлено на 01.01: агентства и станции кампаний уходящего
-- года; пары «станция + тип», пакетные скидки и менеджеры (акций и авторы кампаний уходящего года), у которых набор,
-- строка или период действовали на 31.12 (сезонные, закончившиеся раньше, не показываются); рядом — последний конец.
-- Пустой раздел — всё в порядке.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -W -i year-end-reference-check.sql
-- Год проверки — следующий за текущим; другой год — поменять @year ниже.

SET NOCOUNT ON;

DECLARE @year int = YEAR(GETDATE()) + 1;
DECLARE @newYear datetime = DATEFROMPARTS(@year, 1, 1);
DECLARE @oldStart datetime = DATEFROMPARTS(@year - 1, 1, 1);
DECLARE @oldEnd datetime = DATEFROMPARTS(@year - 1, 12, 31);

PRINT N'Проверка справочников на 01.01.' + CAST(@year AS nvarchar(4)) + N' (действовавшие в ' + CAST(@year - 1 AS nvarchar(4)) + N' году)';

-- Кампании уходящего года (по датам кампании; кампании без дат — по датам акции).
SELECT DISTINCT c.agencyID, c.massmediaID, c.campaignTypeID, c.modUser, a.userID AS actionUserID
INTO #used
FROM dbo.Campaign c
    JOIN dbo.Action a ON a.actionID = c.actionID
WHERE a.deleteDate IS NULL
  AND ISNULL(c.finishDate, a.finishDate) >= @oldStart
  AND ISNULL(c.startDate, a.startDate) < @newYear;

PRINT N'';
PRINT N'1. Агентства без ставки НДС (AgencyTax) на 01.01:';
SELECT ag.agencyID, ag.name AS agency,
       CONVERT(varchar(10), (SELECT MAX(t.finishDate) FROM dbo.AgencyTax t WHERE t.agencyID = ag.agencyID), 104) AS lastTaxFinish
FROM dbo.Agency ag
WHERE ag.agencyID IN (SELECT agencyID FROM #used)
  AND NOT EXISTS (SELECT 1 FROM dbo.AgencyTax t
                  WHERE t.agencyID = ag.agencyID AND t.startDate <= @newYear AND t.finishDate >= @newYear)
ORDER BY ag.name;

PRINT N'2. Радиостанции и типы кампаний без набора объёмных скидок (DiscountRelease с порогами) на 01.01:';
SELECT mm.massmediaID, mm.name AS massmedia, u.campaignTypeID,
       CONVERT(varchar(10), (SELECT MAX(dr.finishDate) FROM dbo.DiscountRelease dr
                             WHERE dr.massmediaID = u.massmediaID
                               AND ((dr.isForType1 = 1 AND u.campaignTypeID = 1) OR (dr.isForType2 = 1 AND u.campaignTypeID = 2)
                                    OR (dr.isForType3 = 1 AND u.campaignTypeID = 3))), 104) AS lastReleaseFinish
FROM (SELECT DISTINCT massmediaID, campaignTypeID FROM #used WHERE campaignTypeID IN (1, 2, 3)) u
    JOIN dbo.MassMedia mm ON mm.massmediaID = u.massmediaID
WHERE EXISTS (SELECT 1 FROM dbo.DiscountRelease dr
              WHERE dr.massmediaID = u.massmediaID AND dr.startDate <= @oldEnd AND dr.finishDate >= @oldEnd
                AND ((dr.isForType1 = 1 AND u.campaignTypeID = 1) OR (dr.isForType2 = 1 AND u.campaignTypeID = 2)
                     OR (dr.isForType3 = 1 AND u.campaignTypeID = 3))
                AND EXISTS (SELECT 1 FROM dbo.DiscountValue dv WHERE dv.discountReleaseID = dr.discountReleaseID))
  AND NOT EXISTS (SELECT 1 FROM dbo.DiscountRelease dr
                  WHERE dr.massmediaID = u.massmediaID AND dr.startDate <= @newYear AND dr.finishDate >= @newYear
                    AND ((dr.isForType1 = 1 AND u.campaignTypeID = 1) OR (dr.isForType2 = 1 AND u.campaignTypeID = 2)
                         OR (dr.isForType3 = 1 AND u.campaignTypeID = 3))
                    AND EXISTS (SELECT 1 FROM dbo.DiscountValue dv WHERE dv.discountReleaseID = dr.discountReleaseID))
ORDER BY mm.name, mm.massmediaID, u.campaignTypeID;

PRINT N'3. Пакетные скидки без строки (PackageDiscountPriceList) на 01.01:';
SELECT pd.packageDiscountId, pd.name AS packageDiscount,
       CONVERT(varchar(10), (SELECT MAX(p.finishDate) FROM dbo.PackageDiscountPriceList p WHERE p.packageDiscountID = pd.packageDiscountId), 104) AS lastFinish
FROM dbo.PackageDiscount pd
WHERE EXISTS (SELECT 1 FROM dbo.PackageDiscountPriceList p
              WHERE p.packageDiscountID = pd.packageDiscountId AND p.startDate <= @oldEnd AND p.finishDate >= @oldEnd)
  AND NOT EXISTS (SELECT 1 FROM dbo.PackageDiscountPriceList p
                  WHERE p.packageDiscountID = pd.packageDiscountId AND p.startDate <= @newYear AND p.finishDate >= @newYear)
ORDER BY pd.name;

PRINT N'4. Действующие радиостанции без прайс-листа (Pricelist) на 01.01:';
SELECT mm.massmediaID, mm.name AS massmedia,
       CONVERT(varchar(10), (SELECT MAX(p.finishDate) FROM dbo.Pricelist p WHERE p.massmediaID = mm.massmediaID), 104) AS lastPricelistFinish
FROM dbo.MassMedia mm
WHERE mm.isActive = 1
  AND mm.massmediaID IN (SELECT massmediaID FROM #used)
  AND NOT EXISTS (SELECT 1 FROM dbo.Pricelist p
                  WHERE p.massmediaID = mm.massmediaID AND p.startDate <= @newYear AND p.finishDate >= @newYear)
ORDER BY mm.name;

PRINT N'5. Менеджеры без сезонного коэффициента (UserDiscount) на 01.01:';
SELECT u.userID, u.userName,
       CONVERT(varchar(10), (SELECT MAX(d.finishDate) FROM dbo.UserDiscount d WHERE d.userID = u.userID), 104) AS lastPeriodFinish
FROM dbo.[User] u
WHERE u.isActive = 1
  AND u.isAdmin = 0
  AND (u.userID IN (SELECT actionUserID FROM #used) OR u.userID IN (SELECT modUser FROM #used))
  AND EXISTS (SELECT 1 FROM dbo.UserDiscount d
              WHERE d.userID = u.userID AND d.startDate <= @oldEnd AND d.finishDate >= @oldEnd)
  AND NOT EXISTS (SELECT 1 FROM dbo.UserDiscount d
                  WHERE d.userID = u.userID AND d.startDate <= @newYear AND d.finishDate >= @newYear)
ORDER BY u.userName;

DROP TABLE #used;
