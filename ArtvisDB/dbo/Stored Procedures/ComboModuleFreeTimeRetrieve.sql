-- Остаток свободного времени по модулям комбо-модуля за период.
--
-- Одна строка на (модуль, день). Ячейка грида размещения комбо-модулями
-- показывает freeTime - минимальный остаток по окнам модуля за этот день,
-- то есть самое заполненное окно: именно оно ограничивает возможность
-- поставить ролик во весь модуль сразу.
--
-- Строка возвращается только для дней, когда модуль есть целиком: окно есть на
-- каждый тариф прайс-листа модуля (та же проверка, что в IsModuleExist) и ни
-- одно из окон не отключено. В остальные дни модуля нет - ячейка пустая.
--
-- Окна со штучным ограничением (maxCapacity > 0) в расчёт остатка не берутся,
-- поэтому у модуля, целиком собранного из таких окон, freeTime = NULL.
--
-- День берётся по dayOriginal - так же, как в IsModuleExist и ModuleIssueIUD,
-- то есть по исходной сетке окон, а не по перенесённой.
CREATE PROC [dbo].[ComboModuleFreeTimeRetrieve]
(
@comboModuleID smallint,
@startDate datetime,
@finishDate datetime,
@showUnconfirmed bit = 0
)
AS
SET NOCOUNT ON

SELECT
	cmc.moduleID,
	mpl.modulePriceListID,
	mpl.price,
	tw.dayOriginal AS issueDate,
	MIN(CASE WHEN tw.maxCapacity = 0
			THEN tw.duration - tw.timeInUseConfirmed
				- CASE WHEN @showUnconfirmed = 1 THEN tw.timeInUseUnconfirmed ELSE 0 END
		END) AS freeTime
FROM
	[ComboModuleContent] cmc
	INNER JOIN [ModulePriceList] mpl ON mpl.moduleID = cmc.moduleID
	INNER JOIN [ModuleTariff] mt ON mt.modulePriceListID = mpl.modulePriceListID
	INNER JOIN [TariffWindow] tw ON tw.tariffId = mt.tariffID
		AND tw.dayOriginal BETWEEN @startDate AND @finishDate
		AND tw.dayOriginal BETWEEN mpl.startDate AND mpl.finishDate
WHERE
	cmc.comboModuleID = @comboModuleID
GROUP BY
	cmc.moduleID,
	mpl.modulePriceListID,
	mpl.price,
	tw.dayOriginal
HAVING
	COUNT(DISTINCT mt.tariffID) =
		(SELECT COUNT(*) FROM [ModuleTariff] mtAll
			WHERE mtAll.modulePriceListID = mpl.modulePriceListID)
	AND SUM(CASE WHEN tw.isDisabled = 1 THEN 1 ELSE 0 END) = 0
ORDER BY
	cmc.moduleID,
	tw.dayOriginal
