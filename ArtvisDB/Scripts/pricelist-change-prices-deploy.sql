-- «Сменить цену» прайс-листа: развёртывание.
--
-- Пункт на прайс-листе (сущность 80) показывает все разные цены тарифов прайс-листа (PricelistTariffPrices),
-- рядом - поле новой цены. Изменённые цены заменяются во всех тарифах прайс-листа одним UPDATE
-- (PricelistTariffPricesChange). Тарифы со сгенерированными окнами пропускаются и показываются журналом.
--
-- Объекты: процедуры PricelistTariffPrices, PricelistTariffPricesChange; iEntityAction 80/ChangeTariffPrices
-- + права групп как у «Добавить тариф массово». Скрипт идемпотентен. После прогона перезапустить qd2 и веб
-- (кэш метаданных).
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i pricelist-change-prices-deploy.sql

SET NOCOUNT ON;
GO

-------------------------------------------------------------------------------
-- 1. Процедуры
-------------------------------------------------------------------------------
-- «Сменить цену» прайс-листа: все разные цены тарифов прайс-листа.
-- tariffsCount - тарифов с этой ценой, withWindowsCount - из них с сгенерированными окнами
-- (их цену PricelistTariffPricesChange не меняет).
CREATE OR ALTER PROCEDURE [dbo].[PricelistTariffPrices]
(
@pricelistID smallint
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

SELECT
	t.price,
	COUNT(*) AS tariffsCount,
	SUM(w.hasWindows) AS withWindowsCount
FROM Tariff t
	CROSS APPLY (SELECT CASE WHEN EXISTS (SELECT 1 FROM TariffWindow tw WHERE tw.tariffId = t.tariffID) THEN 1 ELSE 0 END AS hasWindows) w
WHERE t.pricelistID = @pricelistID
GROUP BY t.price
ORDER BY t.price
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[PricelistTariffPrices] TO PUBLIC
    AS [dbo];
GO

-- «Сменить цену» прайс-листа: замена цен тарифов по списку «старая:новая».
-- @prices - пары через запятую, десятичный разделитель - точка: '1500.00:1650.00,2000:2200'.
-- Замены применяются одновременно, не цепочкой: при 100:200 и 200:300 тариф за 100 станет 200.
-- Тарифы с сгенерированными окнами не меняются (TariffIUD тоже не даёт - TariffInUse): цену
-- окон берут из самих окон, тариф с новой ценой разошёлся бы с ними.
-- Результаты: 1) changedCount - сколько тарифов изменено; 2) пропущенные тарифы (с окнами).
CREATE OR ALTER PROCEDURE [dbo].[PricelistTariffPricesChange]
(
@pricelistID smallint,
@prices varchar(max)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @map TABLE (oldPrice decimal(18,2) PRIMARY KEY, newPrice decimal(18,2) NOT NULL)

INSERT INTO @map (oldPrice, newPrice)
SELECT
	CONVERT(decimal(18,2), LEFT(s.value, CHARINDEX(':', s.value) - 1)),
	CONVERT(decimal(18,2), SUBSTRING(s.value, CHARINDEX(':', s.value) + 1, 50))
FROM STRING_SPLIT(@prices, ',') s
WHERE s.value <> ''

DELETE FROM @map WHERE oldPrice = newPrice

UPDATE t
SET price = m.newPrice
FROM Tariff t
	INNER JOIN @map m ON m.oldPrice = t.price
WHERE t.pricelistID = @pricelistID
	AND NOT EXISTS (SELECT 1 FROM TariffWindow tw WHERE tw.tariffId = t.tariffID)

SELECT @@ROWCOUNT AS changedCount

SELECT t.tariffID, t.[time], t.price
FROM Tariff t
	INNER JOIN @map m ON m.oldPrice = t.price
WHERE t.pricelistID = @pricelistID
	AND EXISTS (SELECT 1 FROM TariffWindow tw WHERE tw.tariffId = t.tariffID)
ORDER BY t.[time], t.tariffID
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[PricelistTariffPricesChange] TO PUBLIC
    AS [dbo];
GO

-------------------------------------------------------------------------------
-- 2. Метаданные (одной транзакцией)
-------------------------------------------------------------------------------

SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @entPricelist INT = 80; -- Прайс-лист (Merlin.Classes.MassmediaPricelist)

IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntity] WHERE entityID = @entPricelist AND className = 'Merlin.Classes.MassmediaPricelist')
BEGIN
	RAISERROR('Сущность 80 (Прайс-лист) не найдена или изменена - согласуйте с Merlin.Classes.Entities', 16, 1);
	RETURN;
END

BEGIN TRANSACTION;

-- ordinal 25: после «Добавить тариф массово» (20), перед «Создать копию прайс-листа» (50)
IF NOT EXISTS (SELECT 1 FROM [dbo].[iEntityAction] WHERE entityID = @entPricelist AND name = 'ChangeTariffPrices')
	INSERT INTO [dbo].[iEntityAction]
		(entityID, alias, name, ordinal_position, isHidden, isGrantingAllowed, imgResourceName, parentID)
	VALUES
		(@entPricelist, N'Сменить цену', 'ChangeTariffPrices', 25, 0, 1, NULL, NULL);

UPDATE [dbo].[iEntityAction]
SET alias = N'Сменить цену'
WHERE entityID = @entPricelist AND name = 'ChangeTariffPrices' AND alias <> N'Сменить цену';

DECLARE @newActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction] WHERE entityID = @entPricelist AND name = 'ChangeTariffPrices');
DECLARE @massActionID SMALLINT =
	(SELECT entityActionID FROM [dbo].[iEntityAction] WHERE entityID = @entPricelist AND name = 'AddTariffsMass');

-- права: те же группы, что у «Добавить тариф массово»
INSERT INTO [dbo].[GroupRight] (groupID, entityActionID)
SELECT gr.groupID, @newActionID
FROM [dbo].[GroupRight] gr
WHERE gr.entityActionID = @massActionID
	AND NOT EXISTS (SELECT 1 FROM [dbo].[GroupRight] x
	                WHERE x.groupID = gr.groupID AND x.entityActionID = @newActionID);

COMMIT TRANSACTION;

PRINT '--- Сменить цену прайс-листа: состояние ---';

SELECT 'PricelistTariffPrices (процедура)' AS [объект], COUNT(*) AS [строк], '1' AS [ожидается]
FROM sys.procedures WHERE name = 'PricelistTariffPrices'
UNION ALL SELECT 'PricelistTariffPricesChange (процедура)', COUNT(*), '1'
FROM sys.procedures WHERE name = 'PricelistTariffPricesChange'
UNION ALL SELECT 'iEntityAction (80/ChangeTariffPrices)', COUNT(*), '1'
FROM [dbo].[iEntityAction] WHERE entityID = @entPricelist AND name = 'ChangeTariffPrices'
UNION ALL SELECT 'GroupRight (новое действие)', COUNT(*), N'как у AddTariffsMass'
FROM [dbo].[GroupRight] WHERE entityActionID = @newActionID
UNION ALL SELECT 'GroupRight (AddTariffsMass, для сверки)', COUNT(*), ''
FROM [dbo].[GroupRight] WHERE entityActionID = @massActionID;
