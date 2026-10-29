-- «Изменение затронет акции» в вебе (29.10.2026, docs/tasks/web-discount-affected-actions.md): при правке и удалении скидки,
-- которой уже посчитаны акции (объёмная — сущности 22, 23; пакетная — 189, 190, 191), веб показывает то же окно, что десктоп
-- (DiscountAffectedActionsForm с 23.09.2026). Логика вопроса переехала в общее ядро (DiscountChangeImpact), десктоп не меняется.
--
-- Что в скрипте:
--   1) DiscountChangeAffectedActions: новый параметр @languageCode (умолчание 'ru' — десктоп его не передаёт, веб подставляет
--      язык пользователя), статус акции «Подтверждена» / «Макет» — через fn_Translate. Выборка не меняется: на ArtvisDev
--      выдача до и после совпала построчно (22 UpdateItem, 23 DeleteItem, 191 UpdateItem).
--   2) Испанские переводы окна (9 строк).
--
-- Когда: после 08 (fn_Translate, iTranslation) и discount-applied-pricelist-id-deploy.sql (сама процедура); нужен новый веб
-- (окна без него нет), десктоп — по желанию (окно то же). После наката перезапустить веб. Идемпотентен (CREATE OR ALTER, MERGE).
-- Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 86_discount-affected-actions-web.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

IF OBJECT_ID('dbo.fn_Translate') IS NULL
    RAISERROR(N'86: нет функции fn_Translate (многоязычность — Deploy/08). Сначала накатить 08.', 16, 1);
GO

CREATE OR ALTER PROCEDURE [dbo].[DiscountChangeAffectedActions]
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
@count tinyint = NULL,
@languageCode VARCHAR(10) = 'ru'        -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

DECLARE @campaigns TABLE (campaignID int PRIMARY KEY)   -- объёмная скидка: по кампаниям
DECLARE @actions TABLE (actionID int PRIMARY KEY)       -- пакетная скидка: по акциям

DECLARE @tConfirmed NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Подтверждена');
DECLARE @tDraft NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Макет');

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
		CASE WHEN a.isConfirmed = 1 THEN @tConfirmed ELSE @tDraft END AS status,
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
		CASE WHEN a.isConfirmed = 1 THEN @tConfirmed ELSE @tDraft END,
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
GO

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (VALUES
        (N'Изменение затронет акции', N'El cambio afectará a campañas'),
        (N'Скидка уже используется в рекламных кампаниях. Внесённые изменения могут повлиять на сумму этих кампаний и акций.', N'El descuento ya se usa en pautas publicitarias. Los cambios pueden afectar al importe de estas pautas y campañas.'),
        (N'Скидка уже используется в акциях. Внесённые изменения могут повлиять на сумму этих акций.', N'El descuento ya se usa en campañas. Los cambios pueden afectar al importe de estas campañas.'),
        (N'Сами акции сейчас не пересчитываются: скидка у них может измениться при следующем пересчёте (любая правка выпусков).', N'Las campañas no se recalculan ahora: su descuento puede cambiar en el próximo recálculo (cualquier cambio de las emisiones).'),
        (N'Сохранить изменения?', N'¿Guardar los cambios?'),
        (N'Начало акции', N'Inicio de la campaña'),
        (N'Окончание акции', N'Fin de la campaña'),
        (N'Скидка кампании', N'Descuento de la pauta'),
        (N'Подтверждена', N'Confirmada')
        ) AS src ([source], [text])
       ON dst.lang = 'es' AND dst.context = ''
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES ('es', '', src.[source], src.[text]);
GO

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
BEGIN
    DECLARE @missing int = (SELECT COUNT(*) FROM (VALUES
        (N'Изменение затронет акции'),
        (N'Скидка уже используется в рекламных кампаниях. Внесённые изменения могут повлиять на сумму этих кампаний и акций.'),
        (N'Скидка уже используется в акциях. Внесённые изменения могут повлиять на сумму этих акций.'),
        (N'Сами акции сейчас не пересчитываются: скидка у них может измениться при следующем пересчёте (любая правка выпусков).'),
        (N'Сохранить изменения?'),
        (N'Начало акции'),
        (N'Окончание акции'),
        (N'Скидка кампании'),
        (N'Подтверждена')
    ) AS v ([source]) WHERE NOT EXISTS
        (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
            AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
    IF @missing = 0 AND EXISTS(SELECT * FROM sys.parameters
            WHERE object_id = OBJECT_ID('dbo.DiscountChangeAffectedActions') AND name = '@languageCode')
        PRINT N'ГОТОВО: «Изменение затронет акции» для веба — процедура и переводы (es) на месте.';
    ELSE
        RAISERROR(N'86: процедура или часть переводов не записаны.', 16, 1);
END
GO
