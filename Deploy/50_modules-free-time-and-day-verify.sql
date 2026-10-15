-- Модули: остатки для веб-вкладки «Модули» и день модуля в проверке выпуска (веб, этап 6, подэтап 6.0 —
-- docs/tasks/web-action-forms.md, «SQL» п. 1–2). Две процедуры, обе общие с десктопом.
--
-- 1. ComboModuleFreeTimeRetrieve (ячейки грида комбо-модулей; веб — вкладка «Модули»):
--    - третий способ задать набор модулей — @moduleIDs (moduleID через запятую; пустые и нечисловые элементы
--      пропускаются, повторы схлопываются). @comboModuleID и @actionID работают как раньше и проверяются первыми;
--    - в конец выдачи добавлены massmediaID (станция модуля) и pricelistID (прайс-лист станции, к которому относится
--      прайс-лист модуля, — его ждёт TariffWindowRetrieve @moduleId, @pricelistId);
--    - positionFree = 0 при заданной позиции у модуля, в прайс-листе которого есть тариф со штучным ограничением 1–3:
--      такому модулю позицию отказывают ModuleIssueIUD (ModuleSetPositionForbidden) и hlp_IssueVerify
--      (MaxCapacityModuleSetPositionError). Десктоп при фильтре позиции перестаёт рисовать такие модули жирным;
--    - скорость ([SQL-02] в docs/IMPROVEMENTS.md): окна модулей читаются из TariffWindow один раз (раньше CTE считался
--      дважды и ещё раз в коррелированном подзапросе maxCapacity), окна ищутся сиком (FORCESEEK) при любом плане: старая
--      процедура зависела от первого вызова — скомпилированная на маленькой акции, считала месяц комбо-модуля 2,1 с
--      вместо 0,2 с, на большом — сканировала TariffWindow и на маленьких вызовах.
-- 2. hlp_IssueVerify (проверка при постановке и смене позиции модульного и пакетного выпуска, Д-21): окна модуля и
--    пакета за день ищутся по dayOriginal, как их вставляют ModuleIssueIUD и PackModuleIssueID и как ищет IsModuleExist
--    (индекс IX_TariffWindow_TariffID_DayOriginal), а не по dayActual без индекса; время окна переносится только внутри
--    дня, окон с dayOriginal <> dayActual нет. В пакетной ветке исправлено соединение «MassMedia mm on tw.massmediaID =
--    tw.massmediaID» (декартово произведение со всеми станциями) на mm.massmediaID = tw.massmediaID.
--
-- Проверено на ArtvisDev 15.10.2026 (старая и новая версии — временными процедурами в одном соединении):
--   ComboModuleFreeTimeRetrieve — 312 вызовов (комбо-модули 1, 3, 7 и акции 178809, 183045, 182589, 178283, 185800,
--     186316, неделя и месяц; с макетами и без; позиция 0 / первый / второй / последний; предмет рекламы без фильтра /
--     есть / нет), 120 796 строк с каждой стороны: отличаются только 3 800 строк в positionFree (1 -> 0, позиция задана,
--     модуль со штучными тарифами), остальные колонки совпали; @moduleIDs = составу комбо-модуля или модулям акции —
--     выдача совпала полностью (24 вызова, в том числе список с пробелами, повтором и мусором). Месяц комбо-модуля 1:
--     174–199 -> 123–149 мс, логических чтений 80 тыс. -> 27 тыс. (TariffWindow 72 тыс. -> 25 тыс.); акция 178809 за
--     месяц: 206–230 -> 95–104 мс, 62 тыс. -> 14,5 тыс.
--   hlp_IssueVerify — 25 774 пары вызовов на модульных (в том числе со штучными тарифами), пакетных и линейных выпусках
--     (добавление и смена позиции, макет и подтверждённая акция, позиции, переполнение, права): код и сообщение
--     совпали везде. Постановка модуля: 13,5 тыс. логических чтений и ~300 мс -> 41–59 чтений и 0–2 мс; пакет: 15,3 тыс.
--     -> 418 чтений, 40 -> 3 мс.
--
-- Порядок: SQL раньше веб-сборки этапа 6 (вкладка «Модули» зовёт @moduleIDs); клиент qd2 не нужен — десктоп совместим
-- (колонки читает по имени, новые — в конце, новый параметр необязательный). Ставить после 38 (несёт тексты обеих
-- процедур из 26 и 38; без них скрипт отказывается: «Ничего не изменено»). Идемпотентен (CREATE OR ALTER).
-- Не перезапускать после 50: 26, 37, 38 и ArtvisDB/Scripts local-promo-roller-types-deploy.sql,
-- promo-position-forbidden-deploy.sql (несут старые тексты); если перезапустили — повторить 50.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 50_modules-free-time-and-day-verify.sql

SET NOCOUNT ON;
GO
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) NOT LIKE N'%AgitationServiceRollerManual%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.ComboModuleFreeTimeRetrieve')) NOT LIKE N'%i.actualWindowID = tw.windowId%'
BEGIN
    RAISERROR(N'50: сначала Deploy/26 и 38. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

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
CREATE OR ALTER PROC [dbo].[ComboModuleFreeTimeRetrieve]
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
GO

CREATE OR ALTER PROC [dbo].[hlp_IssueVerify]
(
@issueID int,
@actionName varchar(32),
@massmediaID smallint,
@DeadLine datetime,
@windowID int,
@issueDate datetime,
@rollerDuration int,
@rightToGoBack bit,
@isAdmin bit,
@isTrafficManager bit,
@rightForMinus bit,
@campaignFinishDate datetime,
@campaignTypeID tinyint,
@isConfirmed bit,
@positionId smallint,
@timeBonus int,
@issuesDuration int,
@sumCapacity INT, 
@rollerActionTypeID TINYINT,
@msgError varchar(64) out,
@modulePricelistID int = null,
@needByTypeVerify bit = 0,
@packModulePriceListId int = null
)
AS
SET NOCOUNT ON

declare @tomorrow datetime
select @msgError = null, @tomorrow = dateadd(day, 1, Convert(datetime, Convert(varchar(8),getdate(), 112), 112)), @issueDate = Convert(datetime, Convert(varchar(8),@issueDate, 112), 112)

IF @sumCapacity IS NULL 
 set @sumCapacity = 1
 
-- check Disabled Windows 
if ((@needByTypeVerify = 0 or @campaignTypeID in (1,2,3)) 
		and exists (select * 
						from DisabledWindow dw 
							inner join TariffWindow tw on tw.windowID = @windowID and tw.massmediaID = @MassMediaId
								and dw.massmediaID = @MassMediaId and tw.windowDateActual between dw.startDate And dw.finishDate)
	or (@needByTypeVerify = 1 and @campaignTypeID = 4 and exists(select * from [PackModuleContent]  pmc
																	INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
																	INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
																	INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID] and tw.dayOriginal = @issueDate
																	inner join DisabledWindow dw on tw.windowDateActual between dw.startDate And dw.finishDate and tw.massmediaID = dw.massmediaID
																	where pmc.pricelistID = @packModulePriceListId ) ) )
begin
	set @msgError = 'DisabledWindowInsert'
	return 1
end


--set @msgError = @rollerActionTypeID
--RETURN 1

-- «Отбивка политической агитации» (7), «Локальное СМИ (агитация)» (44) и «Федеральное СМИ (агитация)» (55)
-- ставит только система (AgitationFraming пишет Issue напрямую, мимо этой проверки); человеку - отказ
-- (заказчик, 11.10.2026)
If @rollerActionTypeID In (7, 44, 55)
	Begin
		set @msgError = 'AgitationServiceRollerManual'
		RETURN 1
	End

-- В окне может быть только один открывающий (тип 4 или 44) и один закрывающий (тип 5 или 55)
-- идентификатор СМИ; 44/55 - авто-обрамление политической агитации, пары к ручным 4/5
If @rollerActionTypeID In (4, 44) And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID In (4, 44)
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType4AlreadyExistInWindow'
		RETURN 1
	End

If @rollerActionTypeID In (5, 55) And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID In (5, 55)
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType5AlreadyExistInWindow'
		RETURN 1
	End

-- Размещение агитации сразу подтверждённым выпуском (в уже активированной акции)
-- требует заполненных роликов обвязки в карточке станции - иначе обвязку не создать.
-- В черновике проверка не нужна: её выполнит активация (ActionActivate).
If @rollerActionTypeID = 6 And @isConfirmed = 1
	And Exists (
		Select 1 From MassMedia mm
		Where mm.massmediaID = @MassMediaId
			And (mm.agitationLocalRollerID Is Null
				Or mm.agitationAnnounceRollerID Is Null
				Or mm.agitationFederalRollerID Is Null)
	)
	Begin
		set @msgError = 'AgitationStationRollersNotSet'
		RETURN 1
	End

-- Анонс политической агитации (тип 7) в окне может быть только один
If @rollerActionTypeID = 7 And Exists (
	Select 1
	From
		Issue i
		Inner Join Roller r on r.rollerID = i.rollerID
	Where
		i.actualWindowID = @windowID
		And r.rolActionTypeID = 7
		And i.issueID != IsNull(@issueID, -1)
	)
	Begin
		set @msgError = 'RolType7AlreadyExistInWindow'
		RETURN 1
	End

-- Для политической агитации и её авто-обрамления спецпозиционирование не применяется
If @rollerActionTypeID In (6, 7, 44, 55) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'AgitationPositionForbidden'
		RETURN 1
	End

-- Локальное промо (8 - со спонсором, 9 - без спонсора) не позиционируется:
-- место в блоке ему задаёт тип ролика при выгрузке
If @rollerActionTypeID In (8, 9) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'PromoPositionForbidden'
		RETURN 1
	End

-- «Локальное СМИ» (4) и «Федеральное СМИ» (5) не позиционируются (заказчик, 11.10.2026):
-- место в блоке им задаёт тип ролика при выгрузке
If @rollerActionTypeID In (4, 5) And IsNull(@positionId, 0) <> 0
	Begin
		set @msgError = 'RolType45PositionForbidden'
		RETURN 1
	End

if	@campaignFinishDate < @tomorrow and @campaignTypeID <> 2 And @RightToGoBack <> 1 And @isTrafficManager = 0
	set @msgError = 'CampaignAlreadyFinished'
else if @campaignTypeID = 2 
	Begin
	Declare
		@extraChargeFirst tinyint,
		@extraChargeSecond tinyint,
		@extraChargeLast tinyint

	Select
		@extraChargeFirst = IsNull(extraChargeFirstRoller, 0),
		@extraChargeSecond = IsNull(extraChargeSecondRoller, 0),
		@extraChargeLast = IsNull(extraChargeLastRoller, 0)
	From 
		Pricelist p
		Inner Join Tariff t on p.pricelistID = t.pricelistID
		Inner Join TariffWindow tw on tw.tariffId = t.tariffID
	Where 
		tw.windowId = @windowID

	If @timeBonus < @issuesDuration + dbo.f_GetSponsorDuration(@rollerDuration, @positionId, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
		Begin
		set @msgError = 'TimeBonusExceed'
		return 1
		End
	End
if (@needByTypeVerify = 0 or @campaignTypeID in (1,2))
	select top 1 @msgError = 
		case
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And @deadLine is not null and tw.dayActual <= @deadLine And @isAdmin = 0 And @isTrafficManager = 0 then 'DeadLineViolation' 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And tw.dayOriginal < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
			when tw.isDisabled = 1 then 'DisabledInsertRoller'
			when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
			when (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
					or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
			when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
			when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
			else null 
		end 
	from [TariffWindow] tw
	where tw.[windowId] = @windowID 
else if @needByTypeVerify = 1 and @campaignTypeID = 3
	Begin
	select top 1 @msgError = 
		case 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And @deadLine is not null and tw.dayActual <= @deadLine And @isAdmin = 0 And @isTrafficManager = 0 then 'DeadLineViolation' 
			when (@actionName = 'AddItem' Or @isConfirmed = 1) And tw.dayOriginal < @tomorrow And @RightToGoBack <> 1 And @isTrafficManager = 0 then 'IncorrectIssueDate'
			when tw.isDisabled = 1 then 'DisabledInsertRoller'
			when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
			when (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
					or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
					or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
			when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
			when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
			else null 
		end 
	from [TariffWindow] tw
		Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
		Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID
	where 
		mt.modulePriceListID = @modulePricelistID
		-- день модуля и пакета - по dayOriginal, как вставка выпусков (ModuleIssueIUD, PackModuleIssueID)
		-- и IsModuleExist; индекс IX_TariffWindow_TariffID_DayOriginal; время окна переносится только
		-- внутри дня, так что dayOriginal = dayActual
		And tw.dayOriginal = @issueDate
	order by 1 desc
	if @msgError Is Null And @positionId <> 0
	Begin
	Select @msgError = 'MaxCapacityModuleSetPositionError'
	From
		ModuleTariff mt
		Inner Join Tariff t On t.tariffID = mt.tariffID
	Where
		mt.modulePriceListID = @modulePriceListId
		And t.maxCapacity > 0 And t.maxCapacity < 4
	End
	End 
else if @needByTypeVerify = 1 and @campaignTypeID = 4
	Begin
	If (@actionName = 'AddItem' Or @isConfirmed = 1) And @isAdmin = 0 And @isTrafficManager = 0 And 
		Exists
		(
		Select 1 
		from 
			[PackModuleContent]  pmc
			INNER JOIN [ModuleTariff] mt ON pmc.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on tw.massmediaID = mm.massmediaID
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayOriginal = @issueDate
			AND @issueDate <= mm.deadLine
		)
		Set @msgError = 'DeadLineViolation'

	If @msgError Is Null
		select top 1 @msgError = 
			case 
				when tw.isDisabled = 1 then 'DisabledInsertRoller'
				when @rollerActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
				when @positionId <> 0 and (( tw.isFirstPositionOccupied = 1 And @positionId = -20) 
						or (tw.isSecondPositionOccupied = 1	And @positionId = -10)
						or (tw.isLastPositionOccupied = 1	And @positionId = 10)) then 'FirstLastIssueError'
				when @rightForMinus = 0 AND @isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + @sumCapacity)) < 0) then 'WindowMaxCapacityOverflow'
				when @rightForMinus = 0 AND @isConfirmed = 1 and tw.[timeInUseConfirmed] + @rollerDuration > tw.duration then 'WindowOverflow'
				when (@actionName = 'AddItem' Or @isConfirmed = 1) And @issueDate < @tomorrow And @RightToGoBack <> 1And @isTrafficManager = 0 then 'IncorrectIssueDate'
				else null 
			end 
		from [PackModuleContent]  pmc
			INNER JOIN [ModulePriceList] mpl ON pmc.[modulePriceListID] = mpl.[modulePriceListID]
			INNER JOIN [ModuleTariff] mt ON mpl.[modulePriceListID] = mt.[modulePriceListID]
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = mt.[tariffID]
			inner join MassMedia mm on mm.massmediaID = tw.massmediaID
		WHERE 
			pmc.[pricelistID] = @packModulePriceListId
			AND tw.dayOriginal = @issueDate
		order by 1 desc
	if @msgError Is Null And @positionId <> 0
		Begin
		Select @msgError = 'MaxCapacityPackModuleSetPositionError'
		From
			PackModuleContent pmc
			Inner Join ModuleTariff mt On mt.modulePriceListID = pmc.modulePriceListID
			Inner Join Tariff t On t.tariffID = mt.tariffID
		Where
			pmc.pricelistID = @packModulePriceListId
			And t.maxCapacity > 0 And t.maxCapacity < 4
		End
	End
if @msgError is not null 
	return 1

return 0
GO

-- Проверка: новые версии обеих процедур применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.ComboModuleFreeTimeRetrieve')) LIKE N'%@moduleIDs varchar(max) = NULL%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ComboModuleFreeTimeRetrieve')) LIKE N'%mpl.positionForbidden = 1 THEN 0%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ComboModuleFreeTimeRetrieve')) LIKE N'%mpl.pricelistID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) NOT LIKE N'%tw.dayActual = @issueDate%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) LIKE N'%And tw.dayOriginal = @issueDate%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_IssueVerify')) LIKE N'%MassMedia mm on mm.massmediaID = tw.massmediaID%'
    PRINT N'ГОТОВО: ComboModuleFreeTimeRetrieve (@moduleIDs, massmediaID/pricelistID, позиция у штучных) и hlp_IssueVerify (день модуля и пакета по dayOriginal).';
ELSE
    RAISERROR(N'50: новая версия применена не ко всем процедурам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
