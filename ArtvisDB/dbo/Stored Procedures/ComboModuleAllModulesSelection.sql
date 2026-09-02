-- Список всех модулей для массового набора состава комбо-модуля галочками -
-- тот же механизм, что у ModulePricelist.EditTariffList («Тарифы для модуля»):
-- один плоский список с чекбоксами, isObjectSelected отмечает уже включённые.
--
-- В отличие от тарифов модуля (которые всегда одной станции), модули комбо-
-- модуля разбросаны по всем станциям, поэтому список - весь каталог модулей
-- активных станций, а не окно на одну станцию.
--
-- Колонки названы под уже существующий selector 0 сущности 82 «Модуль»
-- (name/massmediaName/groupName - тот же набор, что показывает собственный
-- журнал модулей), поэтому для пикера не понадобилась новая метаданная.
CREATE PROC [dbo].[ComboModuleAllModulesSelection]
(
@comboModuleID smallint
)
AS
SET NOCOUNT ON

SELECT
	m.moduleID,
	m.[name],
	mm.massmediaID,
	mm.[name] AS massmediaName,
	mm.groupName,
	CAST(CASE WHEN cmc.comboModuleContentID IS NULL THEN 0 ELSE 1 END AS BIT) AS isObjectSelected
FROM
	[Module] m
	INNER JOIN [vMassmedia] mm ON mm.massmediaID = m.massmediaID
	LEFT JOIN [ComboModuleContent] cmc ON cmc.moduleID = m.moduleID AND cmc.comboModuleID = @comboModuleID
WHERE
	mm.isActive = 1
	-- только модули с актуальным (действующим или будущим) прайс-листом - тот же
	-- критерий, что @hideModulePLInThePast в ModuleList. Уже включённые в комбо-модуль
	-- показываем всегда, иначе снять галочку с "просроченного" модуля будет нечем.
	AND (
		cmc.comboModuleContentID IS NOT NULL
		OR EXISTS (
			SELECT 1 FROM [ModulePriceList] mpl
			WHERE mpl.moduleID = m.moduleID
			AND mpl.finishDate >= CAST(GETDATE() AS DATE)
		)
	)
ORDER BY
	mm.[name],
	m.[name]
