-- Остаток по модулям за период - данные ячеек грида размещения комбо-модулями
-- (десктоп ComboModuleGrid, веб - вкладка «Модули» страницы акции).
--
-- Набор модулей задаётся одним из трёх способов (берётся первый заданный):
--   @comboModuleID - состав комбо-модуля (размещение по мастеру);
--   @moduleIDs     - moduleID через запятую (веб: строки вкладки «Модули»); пустые
--                    и нечисловые элементы пропускаются, повторы схлопываются;
--   @actionID      - модули, уже размещённые в акции (редактирование готовой акции
--                    из карточки; связь кампании с модулем существует только через
--                    выпуски, так же её выводит CampaignModulesRetrieve).
--
-- Одна строка на (модуль, день). Ячейка показывает самое заполненное окно модуля:
-- именно оно ограничивает возможность поставить ролик во весь модуль сразу.
--
-- positionFree   - выбранная позиция (первый/второй/последний ролик) свободна во
--                  ВСЕХ окнах модуля. При включённом учёте макетов смотрим и на
--                  неподтверждённые позиции. Если в прайс-листе модуля есть тариф
--                  со штучным ограничением 1-3, позицию модулю не дадут вовсе
--                  (ModuleIssueIUD -> ModuleSetPositionForbidden, hlp_IssueVerify ->
--                  MaxCapacityModuleSetPositionError) - при заданной позиции 0;
-- advertTypeFree - выбранный предмет рекламы (наличие/отсутствие) выполняется во
--                  ВСЕХ окнах модуля. Критерий совпадения ролика с предметом
--                  рекламы - тот же, что в TariffWindowWithAdvertTypeRetrieve
--                  (advertTypeID ролика или его родитель);
-- Оба флага грид рисует жирным - как RollerIssuesGrid3.MarkCell для отдельного
-- окна, только применённым ко всем окнам модуля сразу;
-- freeTime       - минимальный остаток времени по окнам модуля, сек;
-- freeCapacity   - минимальный остаток по количеству среди окон со штучным
--                  ограничением, maxCapacity - вместимость того самого окна.
--                  Для модуля без штучных окон обе колонки NULL;
-- massmediaID    - радиостанция модуля;
-- pricelistID    - прайс-лист радиостанции, к которому относится прайс-лист модуля
--                  (его ждёт TariffWindowRetrieve @moduleId, @pricelistId).
--
-- Время считается по всем окнам, в том числе штучным: hlp_IssueVerify проверяет
-- переполнение по времени независимо от maxCapacity, так что ограничение
-- реально для любого окна.
--
-- Строка возвращается только для дней, когда модуль есть целиком: окно есть на
-- каждый тариф прайс-листа модуля (та же проверка, что в IsModuleExist) и ни
-- одно из окон не отключено. В остальные дни модуля нет - ячейка пустая.
--
-- День модуля - dayOriginal окна, как в IsModuleExist, ModuleIssueIUD и hlp_IssueVerify
-- (поле выбрано по индексу IX_TariffWindow_TariffID_DayOriginal; время окна переносится
-- только внутри дня, так что dayOriginal = dayActual). Остатки, позиции и предметы
-- рекламы - по выпускам, которые фактически выходят в окне (счётчики окна,
-- Issue.actualWindowID).
CREATE PROC [dbo].[ComboModuleFreeTimeRetrieve]
(
@comboModuleID smallint = NULL,
@actionID int = NULL,
@startDate datetime,
@finishDate datetime,
@showUnconfirmed bit = 0,
@positionId smallint = 0,
@advertTypeId smallint = NULL,
@advertTypePresence tinyint = 0,   -- 0 - не фильтровать, 5 - есть, 10 - нет (AdvertTypePresences)
@moduleIDs varchar(max) = NULL     -- третий способ задать набор модулей (см. выше)
)
AS
SET NOCOUNT ON

DECLARE @modules TABLE (moduleID SMALLINT PRIMARY KEY)

IF @comboModuleID IS NOT NULL
	INSERT INTO @modules (moduleID)
	SELECT cmc.moduleID FROM [ComboModuleContent] cmc WHERE cmc.comboModuleID = @comboModuleID
ELSE IF @moduleIDs IS NOT NULL
	INSERT INTO @modules (moduleID)
	SELECT DISTINCT TRY_CONVERT(smallint, s.value)
	FROM STRING_SPLIT(@moduleIDs, ',') s
	WHERE TRY_CONVERT(smallint, s.value) > 0
ELSE
	INSERT INTO @modules (moduleID)
	SELECT DISTINCT mi.moduleID
	FROM [ModuleIssue] mi
		INNER JOIN [Campaign] c ON c.campaignID = mi.campaignID
	WHERE c.actionID = @actionID

-- Промежуточные наборы - табличные переменные, а не #-таблицы: с #-таблицами запросы
-- перекомпилировались почти на каждом вызове (до 0,15 с компиляции).
--
-- Прайс-листы модулей, задевающие период: число тарифов (для проверки «модуль целиком»)
-- и запрет позиции из-за штучного тарифа
DECLARE @mpl TABLE
(
	modulePriceListID smallint NOT NULL PRIMARY KEY,
	moduleID smallint NOT NULL,
	massmediaID smallint NOT NULL,
	pricelistID smallint NOT NULL,
	price decimal(18, 2) NOT NULL,
	startDate datetime NOT NULL,
	finishDate datetime NOT NULL,
	tariffCount int NOT NULL,
	positionForbidden bit NOT NULL
)

INSERT INTO @mpl (modulePriceListID, moduleID, massmediaID, pricelistID, price, startDate, finishDate,
	tariffCount, positionForbidden)
SELECT
	mpl.modulePriceListID,
	mpl.moduleID,
	md.massmediaID,
	mpl.priceListID,
	mpl.price,
	mpl.startDate,
	mpl.finishDate,
	(SELECT COUNT(*) FROM [ModuleTariff] mtAll WHERE mtAll.modulePriceListID = mpl.modulePriceListID),
	CASE WHEN EXISTS (
		SELECT 1 FROM [ModuleTariff] mtCap
			INNER JOIN [Tariff] t ON t.tariffID = mtCap.tariffID
		WHERE mtCap.modulePriceListID = mpl.modulePriceListID
			AND t.maxCapacity BETWEEN 1 AND 3
		) THEN 1 ELSE 0 END
FROM
	@modules m
	INNER JOIN [Module] md ON md.moduleID = m.moduleID
	INNER JOIN [ModulePriceList] mpl ON mpl.moduleID = m.moduleID
WHERE
	mpl.startDate <= @finishDate
	AND mpl.finishDate >= @startDate

-- Окна модулей за период - один проход по TariffWindow. Индекс - под поиск вместимости
-- самого заполненного окна в итоговом запросе
DECLARE @windows TABLE
(
	moduleID smallint NOT NULL,
	issueDate smalldatetime NOT NULL,
	modulePriceListID smallint NOT NULL,
	tariffID int NOT NULL,
	tariffCount int NOT NULL,
	isDisabled bit NOT NULL,
	maxCapacity smallint NOT NULL,
	timeLeft int NOT NULL,
	capacityLeft int NULL,
	positionFree int NOT NULL,
	advertTypeFree int NOT NULL,
	INDEX IX_windows CLUSTERED (moduleID, issueDate, capacityLeft)
)

INSERT INTO @windows (moduleID, issueDate, modulePriceListID, tariffID, tariffCount, isDisabled, maxCapacity,
	timeLeft, capacityLeft, positionFree, advertTypeFree)
SELECT
	mpl.moduleID,
	tw.dayOriginal,
	mpl.modulePriceListID,
	mt.tariffID,
	mpl.tariffCount,
	tw.isDisabled,
	tw.maxCapacity,
	tw.duration - tw.timeInUseConfirmed
		- CASE WHEN @showUnconfirmed = 1 THEN tw.timeInUseUnconfirmed ELSE 0 END,
	CASE WHEN tw.maxCapacity > 0
		THEN tw.maxCapacity - tw.capacityInUseConfirmed
			- CASE WHEN @showUnconfirmed = 1 THEN tw.capacityInUseUnconfirmed ELSE 0 END
		END,
	CASE
		WHEN @positionId = -20 THEN   -- первый в блоке
			CASE WHEN tw.isFirstPositionOccupied = 0
				AND (@showUnconfirmed = 0 OR tw.firstPositionsUnconfirmed = 0) THEN 1 ELSE 0 END
		WHEN @positionId = -10 THEN   -- второй в блоке
			CASE WHEN tw.isSecondPositionOccupied = 0
				AND (@showUnconfirmed = 0 OR tw.secondPositionsUnconfirmed = 0) THEN 1 ELSE 0 END
		WHEN @positionId = 10 THEN    -- последний в блоке
			CASE WHEN tw.isLastPositionOccupied = 0
				AND (@showUnconfirmed = 0 OR tw.lastPositionsUnconfirmed = 0) THEN 1 ELSE 0 END
		ELSE 1
	END,
	CASE
		WHEN @advertTypePresence = 0 THEN 1
		WHEN @advertTypePresence = 5 THEN   -- есть предмет рекламы
			CASE WHEN EXISTS(
				SELECT 1 FROM [Issue] i
					INNER JOIN [Roller] r ON r.rollerID = i.rollerID
					INNER JOIN [Campaign] c ON c.campaignID = i.campaignID
					INNER JOIN [Action] a ON a.actionID = c.actionID
					LEFT JOIN [AdvertType] adt ON adt.advertTypeID = r.advertTypeID
				WHERE i.actualWindowID = tw.windowId
					AND a.deleteDate IS NULL   -- выпуски удалённых акций (журнал удалённых) не в счёт
					AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
					AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
				) THEN 1 ELSE 0 END
		WHEN @advertTypePresence = 10 THEN   -- нет предмета рекламы
			CASE WHEN NOT EXISTS(
				SELECT 1 FROM [Issue] i
					INNER JOIN [Roller] r ON r.rollerID = i.rollerID
					INNER JOIN [Campaign] c ON c.campaignID = i.campaignID
					INNER JOIN [Action] a ON a.actionID = c.actionID
					LEFT JOIN [AdvertType] adt ON adt.advertTypeID = r.advertTypeID
				WHERE i.actualWindowID = tw.windowId
					AND a.deleteDate IS NULL   -- выпуски удалённых акций (журнал удалённых) не в счёт
					AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
					AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
				) THEN 1 ELSE 0 END
		ELSE 1
	END
FROM
	@mpl mpl
	INNER JOIN [ModuleTariff] mt ON mt.modulePriceListID = mpl.modulePriceListID
	INNER JOIN [TariffWindow] tw WITH (FORCESEEK) ON tw.tariffId = mt.tariffID   -- поиск по (tariffId, dayOriginal) при любом плане: план, собранный на большом вызове, иначе сканирует TariffWindow
		AND tw.dayOriginal BETWEEN @startDate AND @finishDate
		AND tw.dayOriginal BETWEEN mpl.startDate AND mpl.finishDate

-- Сначала по тарифу (окно тарифа за день в норме одно), затем по дню: число тарифов
-- без COUNT(DISTINCT)
;WITH tariffs AS
(
	SELECT
		w.moduleID,
		w.modulePriceListID,
		w.issueDate,
		w.tariffCount,
		MAX(CASE WHEN w.isDisabled = 1 THEN 1 ELSE 0 END) AS isDisabled,
		MIN(w.timeLeft) AS timeLeft,
		MIN(w.capacityLeft) AS capacityLeft,
		MIN(w.positionFree) AS positionFree,
		MIN(w.advertTypeFree) AS advertTypeFree
	FROM
		@windows w
	GROUP BY
		w.moduleID,
		w.modulePriceListID,
		w.issueDate,
		w.tariffCount,
		w.tariffID
),
days AS
(
	SELECT
		t.moduleID,
		t.modulePriceListID,
		t.issueDate,
		MIN(t.timeLeft) AS freeTime,
		MIN(t.capacityLeft) AS freeCapacity,
		MIN(t.positionFree) AS positionFree,
		MIN(t.advertTypeFree) AS advertTypeFree
	FROM
		tariffs t
	GROUP BY
		t.moduleID,
		t.modulePriceListID,
		t.issueDate,
		t.tariffCount
	HAVING
		COUNT(*) = t.tariffCount
		AND MAX(t.isDisabled) = 0
)
SELECT
	d.moduleID,
	d.modulePriceListID,
	mpl.price,
	d.issueDate,
	d.freeTime,
	d.freeCapacity,
	CASE WHEN @positionId <> 0 AND mpl.positionForbidden = 1 THEN 0 ELSE d.positionFree END AS positionFree,
	d.advertTypeFree,
	cap.maxCapacity,
	mpl.massmediaID,
	mpl.pricelistID
FROM
	days d
	INNER JOIN @mpl mpl ON mpl.modulePriceListID = d.modulePriceListID
	-- вместимость самого заполненного штучного окна: наименьшая среди окон модуля за день
	-- с тем же остатком
	OUTER APPLY (
		SELECT MIN(w.maxCapacity) AS maxCapacity
		FROM @windows w
		WHERE w.moduleID = d.moduleID
			AND w.issueDate = d.issueDate
			AND w.capacityLeft = d.freeCapacity
		) cap
ORDER BY
	d.moduleID,
	d.issueDate
