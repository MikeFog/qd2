-- Остаток по модулям за период - данные ячеек грида размещения комбо-модулями.
--
-- Набор модулей задаётся одним из двух способов:
--   @comboModuleID - состав комбо-модуля (размещение по мастеру);
--   @actionID      - модули, уже размещённые в акции (редактирование готовой акции
--                    из карточки; связь кампании с модулем существует только через
--                    выпуски, так же её выводит CampaignModulesRetrieve).
--
-- Одна строка на (модуль, день). Ячейка показывает самое заполненное окно модуля:
-- именно оно ограничивает возможность поставить ролик во весь модуль сразу.
--
-- positionFree   - выбранная позиция (первый/второй/последний ролик) свободна во
--                  ВСЕХ окнах модуля. При включённом учёте макетов смотрим и на
--                  неподтверждённые позиции;
-- advertTypeFree - выбранный предмет рекламы (наличие/отсутствие) выполняется во
--                  ВСЕХ окнах модуля. Критерий совпадения ролика с предметом
--                  рекламы - тот же, что в TariffWindowWithAdvertTypeRetrieve
--                  (advertTypeID ролика или его родитель);
-- Оба флага грид рисует жирным - как RollerIssuesGrid3.MarkCell для отдельного
-- окна, только применённым ко всем окнам модуля сразу;
-- freeTime       - минимальный остаток времени по окнам модуля, сек;
-- freeCapacity   - минимальный остаток по количеству среди окон со штучным
--                  ограничением, maxCapacity - вместимость того самого окна.
--                  Для модуля без штучных окон обе колонки NULL.
--
-- Время считается по всем окнам, в том числе штучным: hlp_IssueVerify проверяет
-- переполнение по времени независимо от maxCapacity, так что ограничение
-- реально для любого окна.
--
-- Строка возвращается только для дней, когда модуль есть целиком: окно есть на
-- каждый тариф прайс-листа модуля (та же проверка, что в IsModuleExist) и ни
-- одно из окон не отключено. В остальные дни модуля нет - ячейка пустая.
--
-- День берётся по dayOriginal - так же, как в IsModuleExist и ModuleIssueIUD,
-- то есть по исходной сетке окон, а не по перенесённой.
CREATE PROC [dbo].[ComboModuleFreeTimeRetrieve]
(
@comboModuleID smallint = NULL,
@actionID int = NULL,
@startDate datetime,
@finishDate datetime,
@showUnconfirmed bit = 0,
@positionId smallint = 0,
@advertTypeId smallint = NULL,
@advertTypePresence tinyint = 0   -- 0 - не фильтровать, 5 - есть, 10 - нет (AdvertTypePresences)
)
AS
SET NOCOUNT ON

DECLARE @modules TABLE (moduleID SMALLINT PRIMARY KEY)

IF @comboModuleID IS NOT NULL
	INSERT INTO @modules (moduleID)
	SELECT cmc.moduleID FROM [ComboModuleContent] cmc WHERE cmc.comboModuleID = @comboModuleID
ELSE
	INSERT INTO @modules (moduleID)
	SELECT DISTINCT mi.moduleID
	FROM [ModuleIssue] mi
		INNER JOIN [Campaign] c ON c.campaignID = mi.campaignID
	WHERE c.actionID = @actionID

;WITH windows AS
(
	SELECT
		mpl.moduleID,
		mpl.modulePriceListID,
		mpl.price,
		tw.dayOriginal AS issueDate,
		mt.tariffID,
		tw.isDisabled,
		tw.maxCapacity,
		tw.duration - tw.timeInUseConfirmed
			- CASE WHEN @showUnconfirmed = 1 THEN tw.timeInUseUnconfirmed ELSE 0 END AS timeLeft,
		CASE WHEN tw.maxCapacity > 0
			THEN tw.maxCapacity - tw.capacityInUseConfirmed
				- CASE WHEN @showUnconfirmed = 1 THEN tw.capacityInUseUnconfirmed ELSE 0 END
			END AS capacityLeft,
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
		END AS positionFree,
		CASE
			WHEN @advertTypePresence = 0 THEN 1
			WHEN @advertTypePresence = 5 THEN   -- есть предмет рекламы
				CASE WHEN EXISTS(
					SELECT 1 FROM [Issue] i
						INNER JOIN [Roller] r ON r.rollerID = i.rollerID
						LEFT JOIN [AdvertType] adt ON adt.advertTypeID = r.advertTypeID
					WHERE i.originalWindowID = tw.windowId
						AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
						AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
					) THEN 1 ELSE 0 END
			WHEN @advertTypePresence = 10 THEN   -- нет предмета рекламы
				CASE WHEN NOT EXISTS(
					SELECT 1 FROM [Issue] i
						INNER JOIN [Roller] r ON r.rollerID = i.rollerID
						LEFT JOIN [AdvertType] adt ON adt.advertTypeID = r.advertTypeID
					WHERE i.originalWindowID = tw.windowId
						AND (@showUnconfirmed = 1 OR i.isConfirmed = 1)
						AND (r.advertTypeID = @advertTypeId OR adt.parentID = @advertTypeId)
					) THEN 1 ELSE 0 END
			ELSE 1
		END AS advertTypeFree
	FROM
		@modules m
		INNER JOIN [ModulePriceList] mpl ON mpl.moduleID = m.moduleID
		INNER JOIN [ModuleTariff] mt ON mt.modulePriceListID = mpl.modulePriceListID
		INNER JOIN [TariffWindow] tw ON tw.tariffId = mt.tariffID
			AND tw.dayOriginal BETWEEN @startDate AND @finishDate
			AND tw.dayOriginal BETWEEN mpl.startDate AND mpl.finishDate
),
days AS
(
	SELECT
		w.moduleID,
		w.modulePriceListID,
		w.price,
		w.issueDate,
		MIN(w.timeLeft) AS freeTime,
		MIN(w.capacityLeft) AS freeCapacity,
		MIN(w.positionFree) AS positionFree,
		MIN(w.advertTypeFree) AS advertTypeFree
	FROM
		windows w
	GROUP BY
		w.moduleID,
		w.modulePriceListID,
		w.price,
		w.issueDate
	HAVING
		COUNT(DISTINCT w.tariffID) =
			(SELECT COUNT(*) FROM [ModuleTariff] mtAll
				WHERE mtAll.modulePriceListID = w.modulePriceListID)
		AND SUM(CASE WHEN w.isDisabled = 1 THEN 1 ELSE 0 END) = 0
)
SELECT
	d.moduleID,
	d.modulePriceListID,
	d.price,
	d.issueDate,
	d.freeTime,
	d.freeCapacity,
	d.positionFree,
	d.advertTypeFree,
	(SELECT MIN(w.maxCapacity)
		FROM windows w
		WHERE w.moduleID = d.moduleID
			AND w.issueDate = d.issueDate
			AND w.capacityLeft = d.freeCapacity) AS maxCapacity
FROM
	days d
ORDER BY
	d.moduleID,
	d.issueDate
