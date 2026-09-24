CREATE PROCEDURE [dbo].[DiscountChangeAffectedActions]
(
-- Кого заденет правка скидки: акции, в которых уже посчитана эта скидка (Campaign.discountReleaseID,
-- Action.packageDiscountPriceListID) и которым правка может её поменять при следующем пересчёте.
-- Только чтение. Параметры — те же, что у процедуры записи сущности (клиент передаёт параметры объекта).
@entityID int,                          -- 22 набор объёмной скидки, 23 порог набора, 189 пакет, 190 станция пакета, 191 прайс-лист пакета
@actionName varchar(32),                -- AddItem / UpdateItem / DeleteItem
@discountReleaseID int = NULL,
@discountValueID int = NULL,
@packageDiscountPriceListID int = NULL,
@packageDiscountID int = NULL,
@packageDiscountMassmediaID int = NULL,
@massmediaID smallint = NULL,
@startDate datetime = NULL,
@finishDate datetime = NULL,
@isForType1 bit = 0,
@isForType2 bit = 0,
@isForType3 bit = 0,
@summa decimal(18,2) = NULL,
@discount decimal(9,4) = NULL,
@value decimal(18,2) = NULL,
@eachVolume tinyint = NULL,              -- как в PackageDiscountPriceListIUD
@count tinyint = NULL
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @campaigns TABLE (campaignID int PRIMARY KEY)   -- объёмная скидка: по кампаниям
DECLARE @actions TABLE (actionID int PRIMARY KEY)       -- пакетная скидка: по акциям

SET @startDate = CAST(@startDate AS date)
SET @finishDate = CAST(@finishDate AS date)

IF @entityID = 22 AND @actionName = 'UpdateItem'
	-- Даты: кампания выпадает из периода; флаги: снят тип этой кампании
	INSERT INTO @campaigns
	SELECT c.campaignID
	FROM Campaign c
	WHERE c.discountReleaseID = @discountReleaseID
		AND (c.startDate < @startDate OR c.startDate >= DATEADD(DAY, 1, @finishDate)
			OR (c.campaignTypeID = 1 AND @isForType1 = 0)
			OR (c.campaignTypeID = 2 AND @isForType2 = 0)
			OR (c.campaignTypeID = 3 AND @isForType3 = 0))

ELSE IF @entityID = 23 AND @actionName IN ('AddItem', 'UpdateItem', 'DeleteItem')
	-- Любая правка порогов набора; нажатие ОК без изменений не в счёт
	INSERT INTO @campaigns
	SELECT c.campaignID
	FROM Campaign c
	WHERE c.discountReleaseID IN (@discountReleaseID,
			(SELECT discountReleaseID FROM DiscountValue WHERE discountValueID = @discountValueID))
		AND NOT (@actionName = 'UpdateItem' AND EXISTS(
			SELECT * FROM DiscountValue
			WHERE discountValueID = @discountValueID AND discountReleaseID = @discountReleaseID
				AND summa = @summa AND discount = @discount))

ELSE IF @entityID = 191 AND @actionName = 'UpdateItem'
	-- Даты: акция выпадает из периода; значения: любое изменение скидки, порога суммы, процента заполнения
	INSERT INTO @actions
	SELECT a.actionID
	FROM [Action] a
		JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID = a.packageDiscountPriceListID
	WHERE pl.packageDiscountPriceListID = @packageDiscountPriceListID
		AND (a.startDate < @startDate OR a.startDate > @finishDate
			OR pl.discount <> @discount OR pl.value <> @value OR pl.eachVolume <> @eachVolume)

ELSE IF @entityID = 190 AND @actionName IN ('UpdateItem', 'DeleteItem')
	-- Станция убрана или изменена: задеты акции, у которых есть кампания на этой станции.
	-- Добавление станции посчитанный пакет не ломает (все кампании акции уже совпали с прайс-листом).
	INSERT INTO @actions
	SELECT DISTINCT a.actionID
	FROM PackageDiscountMassmedia pm
		JOIN [Action] a ON a.packageDiscountPriceListID = pm.packageDiscountPriceListID
		JOIN Campaign c ON c.actionID = a.actionID AND c.massmediaID = pm.massmediaID
	WHERE pm.packageDiscountMassmediaID = @packageDiscountMassmediaID
		AND NOT (@actionName = 'UpdateItem'
			AND pm.massmediaID = @massmediaID AND pm.isForType1 = @isForType1
			AND pm.isForType2 = @isForType2 AND pm.isForType3 = @isForType3)

ELSE IF @entityID = 189 AND @actionName = 'UpdateItem'
	-- Число станций пакета — условие применения всех его прайс-листов; имя не в счёт
	INSERT INTO @actions
	SELECT a.actionID
	FROM PackageDiscount pd
		JOIN PackageDiscountPriceList pl ON pl.packageDiscountID = pd.packageDiscountId
		JOIN [Action] a ON a.packageDiscountPriceListID = pl.packageDiscountPriceListID
	WHERE pd.packageDiscountId = @packageDiscountID AND pd.[count] <> @count

SELECT
	ROW_NUMBER() OVER (ORDER BY x.startDate DESC, x.actionID, x.massmedia) AS rowID,
	x.*
FROM (
	SELECT a.actionID, c.campaignID, mm.name AS massmedia, f.name AS firm, u.userName AS manager,
		a.startDate, a.finishDate,
		CASE WHEN a.isConfirmed = 1 THEN N'Подтверждена' ELSE N'Макет' END AS status,
		c.discount
	FROM @campaigns t
		JOIN Campaign c ON c.campaignID = t.campaignID
		JOIN [Action] a ON a.actionID = c.actionID
		LEFT JOIN MassMedia mm ON mm.massmediaID = c.massmediaID
		LEFT JOIN Firm f ON f.firmID = a.firmID
		LEFT JOIN [User] u ON u.userID = a.userID
	WHERE a.deleteDate IS NULL
	UNION ALL
	SELECT a.actionID, NULL, NULL, f.name, u.userName,
		a.startDate, a.finishDate,
		CASE WHEN a.isConfirmed = 1 THEN N'Подтверждена' ELSE N'Макет' END,
		a.discount
	FROM @actions t
		JOIN [Action] a ON a.actionID = t.actionID
		LEFT JOIN Firm f ON f.firmID = a.firmID
		LEFT JOIN [User] u ON u.userID = a.userID
	WHERE a.deleteDate IS NULL
) x
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DiscountChangeAffectedActions] TO PUBLIC
    AS [dbo];

