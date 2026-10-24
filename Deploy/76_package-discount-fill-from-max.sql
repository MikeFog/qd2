-- Пакетная скидка: «процент заполнения» (PackageDiscountPriceList.eachVolume) — от максимальной длительности кампании
-- акции для акций, начавшихся с даты системной настройки; для более ранних — как прежде, от средней.
--
-- Было: кампания проходит порог прайс-листа, если её длительность выходов (Campaign.issuesDuration) не меньше
-- eachVolume % от СРЕДНЕЙ длительности кампаний акции. Стало (для акций с датой начала >= даты настройки): не меньше
-- eachVolume % от МАКСИМАЛЬНОЙ длительности. Пример: три кампании, самая длинная — 100 мин, «процент заполнения» 60 —
-- каждая из остальных должна быть не короче 60 мин. Остальные условия выбора прайс-листа не менялись.
--
-- Настройка — «Протектор → Администрирование → Настройки...», новое поле «Дата активации нового алгоритма
-- пакетной скидки» (31.12.2099 = выкл.); хранится в iInternalVariable, имя 'PackageDiscountMaxBasisFrom'. Дата включена в новый
-- алгоритм (начало акции >= даты). В паспорте у даты нет пустого значения, поэтому «выключено» — 31.12.2099; пока строки
-- в iInternalVariable нет, диалог показывает 31.12.2099, и «ОК» без правки ничего не включает. СКРИПТ НЕ ЗАДАЁТ ДАТУ:
-- до решения о дате везде работает прежний алгоритм (от средней), расчёт не меняется.
--
-- Что меняется:
--   1. f_SysParamsPackageDiscountMaxBasisFrom (новая) — читает дату настройки (NULL — не задана);
--   2. hlp_ActionDiscountCalculate — выбор пакетной скидки акции (зовёт ActionRecalculate при > 1 кампании);
--   3. pc_PackageDiscountCalculateModel — то же правило в калькуляторе цены и Шаблоне №3, чтобы расчёт обещал ту же скидку;
--   4. SysParams / SysParamsUpdate — чтение и запись настройки;
--   5. паспорт сущности 214 «Системные настройки» — поле с датой.
-- Уже посчитанные акции сами не пересчитываются: скидка меняется при следующем пересчёте акции (любая правка размещения).
--
-- Порядок: после 08 (SysParams с языком), 53 (pc_PackageDiscountCalculateModel), ArtvisDB/Scripts/
-- discount-applied-pricelist-id-deploy.sql (hlp_ActionDiscountCalculate с ID прайс-листа) — без них «Ничего не изменено».
-- Не перезапускать после 76: 08, 53, 65, discount-applied-pricelist-id-deploy.sql, web-i18n-sql-deploy.sql (несут старые
-- тексты процедур); если перезапустили — повторить 76. Идемпотентен (CREATE OR ALTER, IF NOT EXISTS). Ожидается «ГОТОВО».
-- После наката перезапустить qd2 и Протектор (метаданные читаются при старте).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 76_package-discount-fill-from-max.sql

SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.pc_PackageDiscountCalculateModel') IS NULL
   OR CHARINDEX(N'packageDiscountPriceListID', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_ActionDiscountCalculate')), N'')) = 0
   OR CHARINDEX(N'@languageCode', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.SysParams')), N'')) = 0
BEGIN
    RAISERROR(N'76: сначала 08, 53 и discount-applied-pricelist-id-deploy.sql. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO
-- ===== 1. функция настройки =====
-- =============================================
-- Дата начала акции, с которой «процент заполнения» пакетной скидки (PackageDiscountPriceList.eachVolume)
-- считается от МАКСИМАЛЬНОЙ длительности кампании акции, а не от средней. Настройка «Системные настройки»
-- (iInternalVariable, 'PackageDiscountMaxBasisFrom'). NULL — настройка не задана: везде считается от средней.
-- Читают hlp_ActionDiscountCalculate (система) и pc_PackageDiscountCalculateModel (калькулятор цены).
-- =============================================
CREATE OR ALTER FUNCTION f_SysParamsPackageDiscountMaxBasisFrom
(
)
returns datetime
AS
BEGIN
	declare @fromDate datetime
	select top 1 @fromDate = cast([value] as datetime) from iInternalVariable where [name] = 'PackageDiscountMaxBasisFrom'
	return @fromDate
END
GO
-- ===== 2. система: выбор пакетной скидки акции =====
CREATE OR ALTER PROC [dbo].[hlp_ActionDiscountCalculate]
(
@actionID int,
@startDate datetime,
@discountValue decimal(9,4) output,
@packageDiscountPriceListID int = NULL output -- какой прайс-лист дал скидку (NULL — ни один)
)
AS
SET NOCOUNT on

DECLARE @avgDuration float, @maxDuration float, @baseDuration float, @campaignsCount tinyint, @priceByCampaigns decimal(18,2)

Select @priceByCampaigns = Sum([price]) From Campaign where actionID = @actionID

-- ИСПРАВЛЕНО: COUNT(*) вместо COUNT(DISTINCT c.massmediaID)
-- Это гарантирует, что каждая кампания (включая тип 3) учитывается отдельно
-- ИСПРАВЛЕНО: пустые кампании (без единого размещения) в расчёте пакета не участвуют -
-- иначе они тянут вниз avgDuration и завышают @campaignsCount, срывая пакетную скидку.
-- Предикат тот же, что ActionRecalculate использует для oldTotalCount.
SELECT @avgDuration=AVG(CAST(c.issuesDuration AS float)), @maxDuration=MAX(CAST(c.issuesDuration AS float)), @campaignsCount=COUNT(*)
FROM Campaign c
WHERE c.actionID=@actionID and c.campaignTypeID < 4
	and (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0

SELECT @avgDuration=COALESCE(@avgDuration,0), @maxDuration=COALESCE(@maxDuration,0), @campaignsCount=COALESCE(@campaignsCount,0)

-- «Процент заполнения» прайс-листа (eachVolume) считается от средней длительности кампаний акции; для акций, начавшихся
-- с даты системной настройки «PackageDiscountMaxBasisFrom» (Протектор: «Настройки...»), — от максимальной. Дата не задана —
-- везде от средней. @startDate — начало акции (ActionRecalculate передаёт MIN(Campaign.startDate)).
DECLARE @maxBasisFrom datetime = dbo.f_SysParamsPackageDiscountMaxBasisFrom()
SELECT @baseDuration = CASE WHEN @maxBasisFrom IS NOT NULL AND @startDate >= @maxBasisFrom THEN @maxDuration ELSE @avgDuration END

-- Самый выгодный клиенту прайс-лист; при равных скидках — меньший ID, чтобы ссылка была однозначной
SELECT @discountValue = 1, @packageDiscountPriceListID = NULL

SELECT TOP 1 @discountValue=pl.discount, @packageDiscountPriceListID=pl.packageDiscountPriceListID FROM (
		SELECT m.packageDiscountPriceListID, count(DISTINCT c.massmediaID) AS campaignsCount
		FROM Campaign c
			JOIN (
				PackageDiscountMassmedia m 
					JOIN PackageDiscountPriceList p ON p.packageDiscountPriceListID=m.packageDiscountPriceListID
				) ON c.massmediaID = m.massmediaID
														AND (
																(c.campaignTypeID=1 AND m.isForType1=1)
																OR (c.campaignTypeID=2 AND m.isForType2=1)
																OR (c.campaignTypeID=3 AND m.isForType3=1)
																)
														AND CAST(c.issuesDuration as float) >= @baseDuration*p.eachVolume/100
		WHERE
			c.actionID=@actionID
			-- пустые кампании исключаем и здесь, чтобы HAVING сравнивал только реальные
			and (ISNULL(c.issuesCount, 0) + ISNULL(c.programsCount, 0)) > 0
		GROUP BY
			m.packageDiscountPriceListID
		-- ИСПРАВЛЕНО: count(c.massmediaID) вместо count(DISTINCT m.massmediaID)
		-- Считаем количество сопоставлённых кампаний, а не уникальных massmedia
		HAVING 
			count(c.massmediaID)=@campaignsCount
		) t
	JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID=t.packageDiscountPriceListID
	JOIN PackageDiscount d ON d.packageDiscountId=pl.packageDiscountID
WHERE 
	d.count = t.campaignsCount
	AND @startDate BETWEEN pl.startDate AND pl.finishDate
	and pl.value <= @priceByCampaigns
ORDER BY
	pl.discount, pl.packageDiscountPriceListID
GO
-- ===== 3. калькулятор цены / Шаблон №3 =====
CREATE OR ALTER PROC [dbo].[pc_PackageDiscountCalculateModel]
(
    @startDate datetime,
    @campaignTypeID tinyint,     -- у вас всегда 1
    @priceTotal decimal(18,2),           -- сумма по выбранным станциям (аналог Sum(Campaign.price))
    @sel dbo.pc_SelectedMassmedia READONLY,
    @discountValue decimal(9,4) OUTPUT,
    @packageDiscountPriceListID int OUTPUT
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @campaignsCount int;
	Declare @avgDurationSec float, @maxDurationSec float, @baseDurationSec float;
    -- станции без выходов в пакете не участвуют, как пустые кампании в hlp_ActionDiscountCalculate: иначе они
    -- тянули вниз среднюю длительность и завышали число станций, и пакет срывался
    SELECT @campaignsCount = COUNT(DISTINCT massmediaID), @avgDurationSec = AVG(CAST(durationSec as float)), @maxDurationSec = MAX(CAST(durationSec as float)) FROM @sel WHERE durationSec > 0;

    -- как в hlp_ActionDiscountCalculate: «процент заполнения» (eachVolume) — от средней длительности, а для акций,
    -- начавшихся с даты системной настройки «PackageDiscountMaxBasisFrom», — от максимальной
    DECLARE @maxBasisFrom datetime = dbo.f_SysParamsPackageDiscountMaxBasisFrom();
    SET @baseDurationSec = CASE WHEN @maxBasisFrom IS NOT NULL AND @startDate >= @maxBasisFrom THEN @maxDurationSec ELSE @avgDurationSec END;

    SET @discountValue = 1;
    SET @packageDiscountPriceListID = NULL;

    ;WITH t AS
    (
        SELECT
            m.packageDiscountPriceListID,
            campaignsCount = COUNT(DISTINCT s.massmediaID)
        FROM @sel s
        JOIN PackageDiscountMassmedia m
            ON m.massmediaID = s.massmediaID
           AND (
                (@campaignTypeID=1 AND m.isForType1=1) OR
                (@campaignTypeID=2 AND m.isForType2=1) OR
                (@campaignTypeID=3 AND m.isForType3=1)
           )
        JOIN PackageDiscountPriceList p
            ON p.packageDiscountPriceListID = m.packageDiscountPriceListID
           AND @startDate BETWEEN p.startDate AND p.finishDate
           AND p.value <= @priceTotal
        WHERE
            s.durationSec > 0
            AND CAST(s.durationSec as float) >= @baseDurationSec * p.eachVolume / 100.0
        GROUP BY
            m.packageDiscountPriceListID
        HAVING
            -- пакет должен включать ровно столько станций, сколько выбрано (как в hlp_ActionDiscountCalculate)
            COUNT(DISTINCT m.massmediaID) = @campaignsCount
            AND COUNT(DISTINCT s.massmediaID) = @campaignsCount
    )
    SELECT TOP 1
        @discountValue = COALESCE(pl.discount, 1),
        @packageDiscountPriceListID = pl.packageDiscountPriceListID
    FROM t
    JOIN PackageDiscountPriceList pl ON pl.packageDiscountPriceListID = t.packageDiscountPriceListID
    JOIN PackageDiscount d ON d.packageDiscountId = pl.packageDiscountID
    WHERE
        d.count = t.campaignsCount
    ORDER BY
        pl.discount ASC,  -- самый выгодный клиенту (минимальный коэффициент)
        pl.packageDiscountPriceListID;  -- при равных — меньший ID, как в hlp_ActionDiscountCalculate
END
GO
-- ===== 4. чтение настроек =====
-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	Получить системные настройки
-- =============================================
CREATE OR ALTER procedure [dbo].[SysParams] (@languageCode VARCHAR(10) = 'ru') -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
as 
begin 
	set nocount on;
	DECLARE @tSysParams NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Системные настройки');
    
    select 
		dbo.f_SysParamsDaysLog() as daysLog, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'DaysHistorySave'), 365) as daysHistorySave, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'DeletedActionsLifetime'), 365) as DeletedActionsLifetime, 
		coalesce((select top 1 cast([value] as int) from iInternalVariable where [name] = 'UnconfirmedActionsLifetime'), 365) as UnconfirmedActionsLifetime, 
		-- в паспорте у даты нет пустого значения (при «ОК» она всегда сохраняется): «не задана» показываем как 31.12.2099 = выключено
		coalesce(dbo.f_SysParamsPackageDiscountMaxBasisFrom(), '20991231') as packageDiscountMaxBasisFrom, 
		@tSysParams as [name]

end
GO
-- ===== 5. запись настроек =====
-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 05.11.2008
-- Description:	Обновить параметры системы
-- =============================================
CREATE OR ALTER procedure [dbo].[SysParamsUpdate] 
(
	@daysLog tinyint = null,
	@daysHistorySave smallint = null,
	@deletedActionsLifetime smallint,
	@unconfirmedActionsLifetime smallint,
	@packageDiscountMaxBasisFrom datetime = null, -- NULL — настройка не задана (удаляется)
	@actionName varchar(16) = null 
)
as 
begin 
	set nocount on;

	if @actionName <> 'UpdateItem'
		return

    declare @PARAM_NAME_DAYS_LOG char(7)
	select @PARAM_NAME_DAYS_LOG = 'DaysLog'
	
	declare @PARAM_NAME_DAYS_HISTORY_SAVE char(15)
	select @PARAM_NAME_DAYS_HISTORY_SAVE = 'DaysHistorySave'
	
	declare @daysLogB binary(500), @daysHistorySaveB binary(500)
	if @daysLog is not null 
		set @daysLogB = cast ((@daysLog) as binary(500))
	else 
		set @daysLogB = cast (5 as binary(500))
		
	if @daysHistorySave is not null 
		set @daysHistorySaveB = cast ((@daysHistorySave) as binary(500))
	else 
		set @daysHistorySaveB = cast (365 as binary(500))
	
	if exists(select * from iInternalVariable where [name] = @PARAM_NAME_DAYS_LOG)
		update iInternalVariable set [value] = @daysLogB where [name] = @PARAM_NAME_DAYS_LOG
	else 
		insert into iInternalVariable ([name],[value]) 	values (@PARAM_NAME_DAYS_LOG,@daysLogB) 
	
	if exists(select * from iInternalVariable where [name] = @PARAM_NAME_DAYS_HISTORY_SAVE)
		update iInternalVariable set [value] = @daysHistorySaveB where [name] = @PARAM_NAME_DAYS_HISTORY_SAVE
	else 
		insert into iInternalVariable ([name],[value]) 	values (@PARAM_NAME_DAYS_HISTORY_SAVE,@daysHistorySaveB) 

	update iInternalVariable set [value] = Cast(@deletedActionsLifetime as binary(500)) where [name] = 'DeletedActionsLifetime'

	update iInternalVariable set [value] = Cast(@unconfirmedActionsLifetime as binary(500)) where [name] = 'UnconfirmedActionsLifetime'

	-- Дата начала акции, с которой «процент заполнения» пакетной скидки считается от максимальной длительности
	-- (hlp_ActionDiscountCalculate, pc_PackageDiscountCalculateModel); не задана — везде от средней
	if @packageDiscountMaxBasisFrom is null
		delete from iInternalVariable where [name] = 'PackageDiscountMaxBasisFrom'
	else if exists(select * from iInternalVariable where [name] = 'PackageDiscountMaxBasisFrom')
		update iInternalVariable set [value] = Cast(Cast(Cast(@packageDiscountMaxBasisFrom as date) as datetime) as binary(500)) where [name] = 'PackageDiscountMaxBasisFrom'
	else
		insert into iInternalVariable ([name],[value]) values ('PackageDiscountMaxBasisFrom', Cast(Cast(Cast(@packageDiscountMaxBasisFrom as date) as datetime) as binary(500)))
end
GO
-- ===== 6. Паспорт сущности 214 «Системные настройки»: поле с датой =====
DECLARE @field nvarchar(400) = N'<field caption="Дата активации нового алгоритма пакетной скидки:" name="packageDiscountMaxBasisFrom" type="df_date"/>';
IF NOT EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214)
    RAISERROR(N'Сущности 214 «Системные настройки» нет — паспорт не правится.', 16, 1);
ELSE IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%name="packageDiscountMaxBasisFrom"%')
BEGIN
    -- ранняя редакция скрипта писала type="date": такого типа нет (есть df_date), поле не создавалось и диалог «Настройки...» падал
    UPDATE dbo.iEntity
    SET passport = REPLACE(CAST(passport AS nvarchar(max)), N'name="packageDiscountMaxBasisFrom" type="date"', N'name="packageDiscountMaxBasisFrom" type="df_date"')
    WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%name="packageDiscountMaxBasisFrom" type="date"%';
    -- ранняя редакция называла поле «Пакетная скидка — заполнение от максимума, акции с даты (31.12.2099 = выкл.):»
    UPDATE dbo.iEntity
    SET passport = REPLACE(CAST(passport AS nvarchar(max)), N'Пакетная скидка — заполнение от максимума, акции с даты (31.12.2099 = выкл.):', N'Дата активации нового алгоритма пакетной скидки:')
    WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%Пакетная скидка — заполнение от максимума, акции с даты%';
    PRINT N'Паспорт «Системных настроек» уже содержит поле даты.';
END
ELSE IF EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%</page>%')
BEGIN
    UPDATE dbo.iEntity
    SET passport = REPLACE(CAST(passport AS nvarchar(max)), N'</page>', NCHAR(9) + @field + NCHAR(13) + NCHAR(10) + NCHAR(9) + N'</page>')
    WHERE entityID = 214;
    PRINT N'В паспорт «Системных настроек» добавлено поле даты.';
END
ELSE
    RAISERROR(N'В паспорте 214 нет </page> — поле не добавлено.', 16, 1);
GO
SET NOEXEC OFF;
GO
IF OBJECT_ID(N'dbo.f_SysParamsPackageDiscountMaxBasisFrom') IS NOT NULL
   AND CHARINDEX(N'f_SysParamsPackageDiscountMaxBasisFrom', OBJECT_DEFINITION(OBJECT_ID(N'dbo.hlp_ActionDiscountCalculate'))) > 0
   AND CHARINDEX(N'f_SysParamsPackageDiscountMaxBasisFrom', OBJECT_DEFINITION(OBJECT_ID(N'dbo.pc_PackageDiscountCalculateModel'))) > 0
   AND CHARINDEX(N'packageDiscountMaxBasisFrom', OBJECT_DEFINITION(OBJECT_ID(N'dbo.SysParamsUpdate'))) > 0
   AND EXISTS (SELECT 1 FROM dbo.iEntity WHERE entityID = 214 AND CAST(passport AS nvarchar(max)) LIKE N'%packageDiscountMaxBasisFrom%')
    PRINT N'ГОТОВО: «процент заполнения» пакетной скидки — от максимума для акций с даты настройки (дата не задана — от средней)';
ELSE
    PRINT N'ОШИБКА: что-то не накатилось';
GO
