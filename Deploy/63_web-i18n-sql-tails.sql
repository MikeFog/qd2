-- Веб: испанские подписи, которые собирают шесть процедур чтения (docs/tasks/web-i18n.md, «хвосты»). Все шесть общие
-- для десктопа и веба; десктоп язык не передаёт, у параметра умолчание 'ru', поэтому у него выдача прежняя байт в байт.
-- У испанского пользователя веба по-русски оставались:
--   DiscountReleases                 — узлы «Администрация → Скидки» и заголовок карточки: «Скидки от 01.11.2026 до 31.12.2026»;
--   sl_TariffRetrieve + Tariffs      — заголовок карточки тарифа и текст удаления: «Тариф 20:30»;
--   TariffPassport                   — список «Блок:» карточки тарифа: «20:31,пн,вт,ср,чт,пт»;
--   stat_VolumeOfRealizationByMonth3 — отчёт «За период (с разбивкой по месяцам)», колонка «Период»: «… 2026 г.»
--                                      (месяц — данные iMonthName, «г.» — код; шаблон «{0} {1} г.» переводится);
--   PaymentsCommon                   — журнал оплат /payments: заголовок детали «Платёж от фирмы '…'» (sl_PaymentsCommon
--                                      язык умеет с Deploy/08, но PaymentsCommon его не передавал).
-- Плюс 8 испанских строк (дни недели «пн»…«вс» и шаблон периода); «Скидки от », « до », «Тариф », «Платёж от фирмы '»
-- переведены ещё в Deploy/08.
--
-- Порядок: после Deploy/08 (fn_Translate, iTranslation), 15 (sl_TariffRetrieve с row_style), 59 (TariffPassport с
-- исправленными вт/ср) и ArtvisDB/Scripts/discount-release-finish-date-deploy.sql (явная дата окончания набора скидок:
-- DiscountRelease.finishDate NOT NULL) — без них скрипт отказывается: «Ничего не изменено». Клиент qd2 не нужен.
-- После наката ПЕРЕЗАПУСТИТЬ ВЕБ: параметры процедур кэшируются при первом вызове, без перезапуска веб новый параметр
-- не передаёт (подписи остаются русскими, ошибок нет).
-- Не перезапускать после 63: 15 и ArtvisDB/Scripts/tariff-list-sort-deploy.sql (вернут sl_TariffRetrieve без параметра —
-- Tariffs упадёт «too many arguments», список тарифов перестанет открываться и в qd2, и в вебе), 29, 59,
-- ArtvisDB/Scripts/discount-clone-deploy.sql, ArtvisDB/Scripts/discount-release-finish-date-deploy.sql (тихо вернут
-- русские подписи); если перезапустили — повторить 63.
-- Одна транзакция (XACT_ABORT): всё или ничего. Идемпотентен (CREATE OR ALTER, MERGE). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 63_web-i18n-sql-tails.sql

SET NOCOUNT ON;
GO
IF OBJECT_ID(N'dbo.fn_Translate') IS NULL OR OBJECT_ID(N'dbo.iTranslation') IS NULL
   OR CHARINDEX(N'@languageCode', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.sl_PaymentsCommon')), N'')) = 0
   OR CHARINDEX(N'row_style', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.sl_TariffRetrieve')), N'')) = 0
   OR COLUMNPROPERTY(OBJECT_ID(N'dbo.DiscountRelease'), N'finishDate', N'AllowsNull') = 1
   OR (CHARINDEX(N'case t.tuesday when 1 then '',вт''', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffPassport')), N'')) = 0
       AND CHARINDEX(N'@languageCode', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffPassport')), N'')) = 0)
   OR CHARINDEX(N'stat_GetPriceByMonth_proc', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_VolumeOfRealizationByMonth3')), N'')) = 0
   OR CHARINDEX(N'Exec sl_TariffRetrieve', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.Tariffs')), N'')) = 0
   OR CHARINDEX(N'Exec sl_PaymentsCommon', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.PaymentsCommon')), N'')) = 0
BEGIN
    RAISERROR(N'63: сначала Deploy/08, 15, 59 и ArtvisDB/Scripts/discount-release-finish-date-deploy.sql. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET XACT_ABORT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка; у всех шести процедур тоже ON
GO
BEGIN TRANSACTION;
GO

-- ===== Переводы (es) =====
MERGE dbo.iTranslation AS dst
USING (VALUES
    (N'пн', N'lu'),
    (N'вт', N'ma'),
    (N'ср', N'mi'),
    (N'чт', N'ju'),
    (N'пт', N'vi'),
    (N'сб', N'sá'),
    (N'вс', N'do'),
    (N'{0} {1} г.', N'{0} {1}')
    ) AS src ([source], [text])
   ON dst.lang = 'es' AND dst.context = ''
  AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
    UPDATE SET [text] = src.[text]
WHEN NOT MATCHED BY TARGET THEN
    INSERT (lang, context, [source], [text]) VALUES ('es', '', src.[source], src.[text]);
GO

-- ===== 1. DiscountReleases =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR(N'63: транзакция откатилась — DiscountReleases и дальше не выполняются.', 16, 1); SET NOEXEC ON; END
GO
CREATE OR ALTER PROCEDURE [dbo].[DiscountReleases]
(
@massmediaID smallint = NULL,
@discountReleaseID smallint = NULL,
@hideDiscountsInThePast bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
as
set nocount on
DECLARE @tDiscountsFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Скидки от ');
DECLARE @tTo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' до ');
SELECT 
	dr.*,
	@tDiscountsFrom + Convert(varchar(10), dr.[startDate], 104) + CASE WHEN dr.[finishDate] IS NOT NULL THEN @tTo + Convert(varchar(10), dr.[finishDate], 104) ELSE '' END as name
FROM 
	[DiscountRelease] dr
WHERE
	dr.[massmediaID] = Coalesce(@massmediaID, dr.[massmediaID])
	AND dr.[discountReleaseID] = Coalesce(@discountReleaseID, dr.[discountReleaseID])
	And (@hideDiscountsInThePast = 0 or dr.finishDate >= CAST(GETDATE() AS date))
ORDER BY
	dr.startDate DESC
GO

-- ===== 2. sl_TariffRetrieve =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR(N'63: транзакция откатилась — sl_TariffRetrieve и дальше не выполняются.', 16, 1); SET NOEXEC ON; END
GO
CREATE OR ALTER PROCEDURE [dbo].[sl_TariffRetrieve] (@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tTariff NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Тариф ');

SELECT 
	t.*,
	CONVERT(varchar(5), t.[time], 108) as timeString, 
	dbo.fn_Int2Time(t.[duration]) as durationString, 
	dbo.fn_Int2Time(t.[duration_total]) as durationString_total, 
	@tTariff + CONVERT(varchar(5), t.[time], 108) as name ,
	cast(case when tu.tariffUnionID is null then 0 else 1 end as bit) as isUnionEnable,
	tu.tariffUnionID as tariffUnionID,
	-- тариф объединён (TariffUnion) любой стороной — строка подсвечивается в списке
	-- тем же цветом, что ячейка в трафик-менеджменте (TrafficGrid.MarkCellAsUnited)
	case when tu.tariffID is not null
		or exists (select 1 from TariffUnion tp where tp.tariffUnionID = t.tariffID)
		then 'united' end as row_style
FROM 
	[Tariff] t
	Inner Join #Tariff t2 On t.tariffId = t2.tariffId
	left join TariffUnion tu on t.tariffID = tu.tariffID
-- при одинаковом времени первым идёт тариф, действующий раньше в неделе (будни выше выходных):
-- дни тарифов в одно время не пересекаются, поэтому первый день недели однозначен.
ORDER BY
	t.[time] asc,
	CASE WHEN t.monday = 1 THEN 1 WHEN t.tuesday = 1 THEN 2 WHEN t.wednesday = 1 THEN 3
		WHEN t.thursday = 1 THEN 4 WHEN t.friday = 1 THEN 5 WHEN t.saturday = 1 THEN 6
		WHEN t.sunday = 1 THEN 7 ELSE 8 END asc,
	t.tariffID asc
Drop Table #Tariff
GO

-- ===== 3. Tariffs =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR(N'63: транзакция откатилась — Tariffs и дальше не выполняются.', 16, 1); SET NOEXEC ON; END
GO
CREATE OR ALTER PROCEDURE [dbo].[Tariffs]
(
@pricelistID smallint = NULL,
@tariffID int = NULL,
@excludeModuleTariffs bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

Create Table #Tariff(tariffId int)

IF @tariffID IS NULL BEGIN

	Insert Into #Tariff(tariffId)

	SELECT 
		t.tariffId
	FROM 
		[Tariff] t
	WHERE
		pricelistID = COALESCE(@pricelistID, t.pricelistID)
		AND (@excludeModuleTariffs = 0 Or t.isForModuleOnly = 0)

END
ELSE BEGIN
	Insert Into #Tariff(tariffId) Values(@tariffID)
END

Exec sl_TariffRetrieve @languageCode = @languageCode
GO

-- ===== 4. TariffPassport =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR(N'63: транзакция откатилась — TariffPassport и дальше не выполняются.', 16, 1); SET NOEXEC ON; END
GO
CREATE OR ALTER PROCEDURE [dbo].[TariffPassport]
(
	@pricelistID smallint = null,
	@tariffId int = null,
	@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @tMo NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'пн');
	DECLARE @tTu NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'вт');
	DECLARE @tWe NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'ср');
	DECLARE @tTh NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'чт');
	DECLARE @tFr NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'пт');
	DECLARE @tSa NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'сб');
	DECLARE @tSu NVARCHAR(20) = N',' + dbo.fn_Translate(@languageCode, N'вс');



Declare @nextTariffId int
Set @nextTariffId = dbo.fn_FindTariffIDForChain(@tariffId, @pricelistID)

select 
	(CONVERT(varchar(5), t.[time], 108) 
	+ case t.monday when 1 then @tMo else '' end 
	+ case t.tuesday when 1 then @tTu else '' end 
	+ case t.wednesday when 1 then @tWe else '' end 
	+ case t.thursday when 1 then @tTh else '' end 
	+ case t.friday when 1 then @tFr else '' end 
	+ case t.saturday when 1 then @tSa else '' end 
	+ case t.sunday when 1 then @tSu else '' end 
	+ case when t.pricelistID = @pricelistID then '' else ' (' + mm.[name] + ')' end) as name, 
	t.tariffID as id 
from Tariff t	
	inner join Pricelist pl on t.pricelistID = pl.pricelistID
	inner join vMassmedia mm on pl.massmediaID = mm.massmediaID
where t.pricelistID = @pricelistID
	And t.tariffID = @nextTariffId 
		

Select blockTypeId as Id, [name] + ' (' + code + ')' as name, blockTypeId From BlockType
END
GO

-- ===== 5. stat_VolumeOfRealizationByMonth3 =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR(N'63: транзакция откатилась — stat_VolumeOfRealizationByMonth3 и дальше не выполняются.', 16, 1); SET NOEXEC ON; END
GO
CREATE OR ALTER PROCEDURE [dbo].[stat_VolumeOfRealizationByMonth3]
(
    @StartDay DATETIME = NULL,
    @FinishDay DATETIME = NULL,
    @FirmID smallint = NULL, 
    @headCompanyID smallint = NULL, 
    @MassmediaID smallint = NULL, 
    @PaymentTypeID smallint = NULL,
    @CampaignTypeID tinyint = NULL,
    @ManagerID smallint = NULL,
    @AgencyID smallint = NULL, 
    @massmediaGroupID int = NULL,
    @advertTypeID smallint = NULL,
    @IsGroupByMassmedia bit = 0,
    @IsGroupByPaymentType bit = 0,
    @IsGroupByCampaignType bit = 0,
    @IsGroupByManager bit = 0,
    @IsGroupByAgency bit = 0,
    @IsGroupByFirm bit = 0,
    @IsGroupByHeadCompany bit = 0, 
    @IsGroupByMassmediaGroupType bit = 0,
    @IsGroupByAdvertType bit = 0,
    @IsGroupByAdvertTypeTop bit = 0,
    @ShowWhite bit = 1,
    @ShowBlack bit = 1,
    @loggedUserID smallint,
    @languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;
    -- «Январь 2026 г.»: месяц — данные (iMonthName), шаблон подписи — перевод.
    DECLARE @tPeriod NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'{0} {1} г.');

    IF @IsGroupByHeadCompany = 1 SET @IsGroupByFirm = 0;

    IF @StartDay IS NULL OR @FinishDay IS NULL
    BEGIN
        RAISERROR('FilterStartFinishDays', 16, 1);
        RETURN;
    END

    SET @StartDay  = dbo.ToShortDate(@StartDay);
    SET @FinishDay = dbo.ToShortDate(@FinishDay);

    --------------------------------------------------------------------
    -- Источник данных: 1:1 с fn_statGetPriceByMonth
    --------------------------------------------------------------------
    CREATE TABLE #Campaign
    (
        y smallint NOT NULL,
        m tinyint NOT NULL,
        campaignID int NOT NULL, 
        advertTypeID smallint NULL,
        actionID int NOT NULL, 
        massmediaID smallint NOT NULL, 
        paymentTypeID smallint NOT NULL, 
        campaignTypeID tinyint NOT NULL, 
        agencyID smallint NULL,
        startDate datetime NOT NULL,
        finishDate datetime NOT NULL,
        finalPrice decimal(18,2) NULL,
        userID smallint NOT NULL,
        firmID smallint NOT NULL,
        discount decimal(9,4) NULL,
        massmediaGroupID int NULL,
        price decimal(18,2) NOT NULL
    );

    INSERT INTO #Campaign
    EXEC dbo.stat_GetPriceByMonth_proc
         @StartDay,
         @FinishDay,
         @loggedUserID;

    CREATE UNIQUE CLUSTERED INDEX IX_Campaign
        ON #Campaign (y, m, campaignID, massmediaID, advertTypeID);

    --------------------------------------------------------------------
    -- output ---------------------------------------------------------
    --------------------------------------------------------------------
    DECLARE @SQLString NVARCHAR(MAX), @IsStarted int;

    SET @SQLString = N'
DECLARE @Summa decimal(18,2);
SET @Summa = 0;

SELECT @Summa = ISNULL(SUM(d.price), 0)
FROM #Campaign d
INNER JOIN Firm f ON f.firmId = d.firmId
';

    IF @ShowWhite = 0 OR @ShowBlack = 0
        SET @SQLString += N'INNER JOIN Paymenttype pt ON d.PaymentTypeID = pt.PaymenttypeID
';

    IF @advertTypeID IS NOT NULL OR @IsGroupByAdvertTypeTop <> 0
        SET @SQLString += N'LEFT JOIN AdvertType at ON at.advertTypeID = d.advertTypeID
';

    SET @SQLString += N'WHERE 1=1
';

    IF @ShowWhite = 0 AND @ShowBlack <> 0 SET @SQLString += N' AND pt.isHidden <> 0
';
    IF @ShowBlack = 0 AND @ShowWhite <> 0 SET @SQLString += N' AND pt.isHidden = 0
';
    IF @FirmID IS NOT NULL SET @SQLString += N' AND d.firmID = @FirmID
';
    IF @headCompanyID IS NOT NULL SET @SQLString += N' AND f.headCompanyID = @headCompanyID
';
    IF @MassmediaID IS NOT NULL SET @SQLString += N' AND d.massmediaID = @MassmediaID
';
    IF @PaymentTypeID IS NOT NULL SET @SQLString += N' AND d.paymentTypeID = @PaymentTypeID
';
    IF @CampaignTypeID IS NOT NULL SET @SQLString += N' AND d.campaignTypeID = @CampaignTypeID
';
    IF @ManagerID IS NOT NULL SET @SQLString += N' AND d.userID = @ManagerID
';
    IF @AgencyID IS NOT NULL SET @SQLString += N' AND d.agencyID = @AgencyID
';
    IF @massmediaGroupID IS NOT NULL SET @SQLString += N' AND d.massmediaGroupID = @massmediaGroupID
';
    IF @advertTypeID IS NOT NULL SET @SQLString += N' AND @advertTypeID IN(at.parentID, d.advertTypeID)
';

    --------------------------------------------------------------------
    -- SELECT часть
    --------------------------------------------------------------------
    SET @SQLString += N'
SELECT
    ROW_NUMBER() OVER(ORDER BY d.y, d.m) AS RowNum,
    REPLACE(REPLACE(@tPeriod, N''{0}'', MAX(iMonthName.name)), N''{1}'', CAST(d.[y] AS varchar)) AS [period],
';

    IF @IsGroupByMassmedia <> 0
        SET @SQLString += N'vMassMedia.NameWithGroup as mmName,  ';
    IF @IsGroupByFirm <> 0
        SET @SQLString += N'f.Name as firmName,';
    IF @IsGroupByHeadCompany <> 0
        SET @SQLString += N'hc.Name as head_company,';
    IF @IsGroupByManager <> 0
        SET @SQLString += N'COALESCE(u.LastName, '''') + COALESCE(SPACE(1) + u.FirstName, '''') as manager,';
    IF @IsGroupByAgency <> 0
        SET @SQLString += N'ag.Name as agencyName,';
    IF @IsGroupByPaymentType <> 0
        SET @SQLString += N'pt.Name as payment_type,';
    IF @IsGroupByCampaignType <> 0
        SET @SQLString += N'ct.Name as campaign_type,';
    IF @IsGroupByMassmediaGroupType <> 0
        SET @SQLString += N'mg.Name as massmedia_group,';
    IF @IsGroupByAdvertType <> 0
        SET @SQLString += N'adt.Name as adverttype,';
    IF @IsGroupByAdvertTypeTop <> 0
        SET @SQLString += N'atTop.Name as topAdverttype,';

    SET @SQLString += N'
    ISNULL(SUM(d.price), 0) as price,
    COUNT(DISTINCT d.actionID) as action_cnt,
    COUNT(DISTINCT f.headCompanyID) as headcompany_cnt,
	ISNULL(SUM(d.price), 0) / COUNT(DISTINCT d.actionID) as average_bill_action,
	ISNULL(SUM(d.price), 0) / COUNT(DISTINCT f.headCompanyID) as average_bill_head_company
FROM #Campaign d
INNER JOIN iMonthName ON d.m = iMonthName.number
INNER JOIN Firm f ON f.firmId = d.firmId
';

    -- JOIN’ы только при необходимости колонок/групп
    IF @IsGroupByMassmediaGroupType <> 0
        SET @SQLString += N'INNER JOIN MassmediaGroup mg ON d.massmediaGroupID = mg.massmediaGroupID
';
    IF @IsGroupByPaymentType <> 0 OR @ShowWhite = 0 OR @ShowBlack = 0
        SET @SQLString += N'INNER JOIN Paymenttype pt ON d.paymentTypeID = pt.paymentTypeID
';
    IF @IsGroupByCampaignType <> 0
        SET @SQLString += N'INNER JOIN iCampaignType ct ON d.campaignTypeID = ct.CampaignTypeID
';
    IF @IsGroupByMassmedia <> 0
        SET @SQLString += N'INNER JOIN vMassMedia ON d.massmediaID = vMassMedia.massmediaID
';
    IF @IsGroupByManager <> 0
        SET @SQLString += N'INNER JOIN [User] u ON d.userID = u.UserID
';
    IF @IsGroupByAgency <> 0
        SET @SQLString += N'INNER JOIN Agency ag ON d.AgencyID = ag.AgencyID
';
    IF @IsGroupByHeadCompany <> 0
        SET @SQLString += N'INNER JOIN HeadCompany hc ON f.headCompanyID = hc.headCompanyID
';
    IF @IsGroupByAdvertType <> 0
        SET @SQLString += N'LEFT JOIN AdvertType adt ON d.advertTypeID = adt.AdvertTypeID
';
    IF @IsGroupByAdvertTypeTop <> 0
        SET @SQLString += N'LEFT JOIN AdvertType adt2 ON d.advertTypeID = adt2.AdvertTypeID
LEFT JOIN AdvertType atTop ON adt2.parentID = atTop.AdvertTypeID
';

    -- фильтр "price <> 0" как у тебя
    SET @SQLString += N'WHERE d.price <> 0
';


	-- ===== APPLY SAME FILTERS AS FOR @Summa =====

	-- White/Black (используем pt, т.к. join pt у тебя уже добавляется когда надо)
	IF @ShowWhite = 0 AND @ShowBlack <> 0
		SET @SQLString += N' AND pt.isHidden <> 0
	';
	IF @ShowBlack = 0 AND @ShowWhite <> 0
		SET @SQLString += N' AND pt.isHidden = 0
	';
	IF @ShowWhite = 0 AND @ShowBlack = 0
		SET @SQLString += N' AND 1=0
	';

	IF @FirmID IS NOT NULL
		SET @SQLString += N' AND d.firmID = @FirmID
	';

	IF @headCompanyID IS NOT NULL
		SET @SQLString += N' AND f.headCompanyID = @headCompanyID
	';

	IF @MassmediaID IS NOT NULL
		SET @SQLString += N' AND d.massmediaID = @MassmediaID
	';

	IF @PaymentTypeID IS NOT NULL
		SET @SQLString += N' AND d.paymentTypeID = @PaymentTypeID
	';

	IF @CampaignTypeID IS NOT NULL
		SET @SQLString += N' AND d.campaignTypeID = @CampaignTypeID
	';

	IF @ManagerID IS NOT NULL
		SET @SQLString += N' AND d.userID = @ManagerID
	';

	IF @AgencyID IS NOT NULL
		SET @SQLString += N' AND d.agencyID = @AgencyID
	';

	IF @massmediaGroupID IS NOT NULL
		SET @SQLString += N' AND d.massmediaGroupID = @massmediaGroupID
	';

	IF @advertTypeID IS NOT NULL
	BEGIN
		-- у тебя в part 2 нет join at/atFilter; есть только adt/adt2/atTop для группировок.
		-- поэтому фильтр по parentID делаем через EXISTS (без доп. join'ов)
		SET @SQLString += N'
	 AND EXISTS (
		 SELECT 1
		 FROM AdvertType a
		 WHERE a.advertTypeID = d.advertTypeID
		   AND (a.advertTypeID = @advertTypeID OR a.parentID = @advertTypeID)
	 )
	';
	END

    -- Group by
    SET @SQLString += N'GROUP BY d.y, d.m';

    IF @IsGroupByMassmediaGroupType <> 0 SET @SQLString += N', mg.Name';
    IF @IsGroupByPaymentType <> 0 SET @SQLString += N', pt.Name';
    IF @IsGroupByCampaignType <> 0 SET @SQLString += N', ct.Name';
    IF @IsGroupByMassmedia <> 0 SET @SQLString += N', vMassMedia.NameWithGroup';
    IF @IsGroupByFirm <> 0 SET @SQLString += N', f.Name';
    IF @IsGroupByHeadCompany <> 0 SET @SQLString += N', hc.Name';
    IF @IsGroupByManager <> 0 SET @SQLString += N', u.lastName, u.firstName';
    IF @IsGroupByAgency <> 0 SET @SQLString += N', ag.Name';
    IF @IsGroupByAdvertType <> 0 SET @SQLString += N', adt.Name';
    IF @IsGroupByAdvertTypeTop <> 0 SET @SQLString += N', atTop.Name';

    EXECUTE sp_executesql @SQLString,
        N'@startDate datetime, @finishDate datetime, @loggedUserID smallint,
          @FirmID smallint, @MassmediaID smallint, @PaymentTypeID smallint, @CampaignTypeID tinyint,
          @ManagerID smallint, @AgencyID smallint, @massmediaGroupID int, @advertTypeID smallint, @headCompanyID smallint,
		  @ShowWhite bit, @ShowBlack bit, @tPeriod nvarchar(200)',
        @startDate = @StartDay, 
        @finishDate = @FinishDay, 
        @loggedUserID = @loggedUserID, 
        @FirmID = @FirmID,
        @MassmediaID = @MassmediaID,
        @PaymentTypeID = @PaymentTypeID,
        @CampaignTypeID = @CampaignTypeID,
        @ManagerID = @ManagerID,
        @AgencyID = @AgencyID,
        @massmediaGroupID = @massmediaGroupID,
        @advertTypeID = @advertTypeID,
        @headCompanyID = @headCompanyID,
		@ShowWhite = @ShowWhite,
		@ShowBlack = @ShowBlack,
		@tPeriod = @tPeriod;
END
GO

-- ===== 6. PaymentsCommon =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR(N'63: транзакция откатилась — PaymentsCommon и дальше не выполняются.', 16, 1); SET NOEXEC ON; END
GO
CREATE OR ALTER PROCEDURE [dbo].[PaymentsCommon]
(
	@paymentID int = Null,
	@startOfInterval datetime = Null,
	@endOfInterval datetime = Null,
	@firmID smallint = Null,
	@headCompanyId smallint = Null,
	@paymentTypeID smallint = Null,
	@agencyID smallint  = Null,
	@isNotClosedOnly bit = 0,
	@agenciesIDString varchar(1024) = null,
	@actionID INT = NULL,
	@isHideWhite BIT = 0,
	@isHideBlack BIT = 0,
	@filterAgencies bit = 0,
	@loggedUserID int,
	@showBlack bit = 1,
	@showWhite bit = 1,
	@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

CREATE TABLE #PaymentsCommon(paymentID int)

-- Проверка роли пользователя
DECLARE @isAdmin bit = 0
DECLARE @isBookKeeper bit = 0

SELECT 
	@isAdmin = IsAdmin,
	@isBookKeeper = IsBookKeeper
FROM [dbo].[user]
WHERE userID = @loggedUserID

IF @agenciesIDString is not Null 
BEGIN -- баланс для фирмы
	CREATE TABLE #Agency (agencyID smallint)

	Exec hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString
	
	INSERT INTO #PaymentsCommon(paymentID)
	SELECT DISTINCT 
		p.paymentID
	FROM
		[Payment] p
		INNER JOIN firm f ON f.firmID = p.firmID
		INNER JOIN HeadCompany hc ON hc.headCompanyID = f.headCompanyID
		INNER JOIN agency a ON a.agencyID = p.agencyID
		INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
		INNER JOIN [user] u ON u.userID = p.userID 
		LEFT JOIN [PaymentAction] pa ON pa.[paymentID] = p.[paymentID]
			AND pa.[actionID] = COALESCE(@actionID, pa.[actionID])
	WHERE
		-- Фильтр по фирме и головной компании
		p.firmID = Coalesce(@firmID, p.firmID)
		AND f.headCompanyID = Coalesce(@headCompanyId, f.headCompanyID)
		
		-- Фильтр по периоду
		AND p.paymentDate >= Coalesce(@startOfInterval, p.paymentDate)
		AND p.paymentDate <= Coalesce(@endOfInterval, p.paymentDate)
		
		-- Фильтр по списку агентств
		AND p.agencyID IN (SELECT agencyID From #Agency)
		
		-- Фильтр по типу платежа (белый/черный)
		AND (
			(pt.isHidden = 0 OR @isHideWhite = 0) 
			AND (pt.isHidden = 1 OR @isHideBlack = 0)
			AND (
				(pt.IsHidden = 1 AND @showBlack = 1)  
				OR (pt.IsHidden = 0 AND @showWhite = 1)
			)
		)
		
		-- Фильтр по правам доступа пользователя к агентству
		AND (
			@isAdmin = 1  -- Админу показываем все агентства
			OR @isBookKeeper = 1  -- Бухгалтеру показываем все агентства
			OR @filterAgencies = 0  -- Флаг отключен - показываем все агентства
			OR (
				-- Флаг включен и пользователь не админ/бухгалтер - проверяем права доступа
				EXISTS(
					SELECT 1 
					FROM Campaign cam 
					INNER JOIN [Action] act ON cam.actionID = act.actionID 
					WHERE a.agencyID = cam.agencyID 
						AND @loggedUserID = act.userID
				) 
				OR EXISTS(
					SELECT 1 
					FROM Payment pay 
					WHERE a.agencyID = pay.agencyID 
						AND @loggedUserID = pay.userID
				)
			)
		)
		
		-- Фильтр видимости платежей для обычных пользователей
		AND (
			@isAdmin = 1  -- Админу показываем все платежи
			OR @isBookKeeper = 1  -- Бухгалтеру показываем все платежи
			OR p.summa > ISNULL((SELECT SUM(pa2.[summa]) FROM [PaymentAction] pa2 WHERE pa2.[paymentID] = p.[paymentID]), 0)  -- Платеж не полностью распределен
			OR EXISTS(
				-- Платеж распределен, но есть Action пользователя
				SELECT 1 
				FROM [PaymentAction] pa2
				INNER JOIN [Action] act ON pa2.actionID = act.actionID
				WHERE pa2.[paymentID] = p.[paymentID] 
					AND act.userID = @loggedUserID
			)
		)
END
ELSE 
BEGIN
	INSERT INTO #PaymentsCommon(paymentID)
	SELECT DISTINCT
		p.paymentID
	FROM
		[Payment] p
		INNER JOIN firm f ON f.firmID = p.firmID
		INNER JOIN HeadCompany hc ON hc.headCompanyID = f.headCompanyID
		INNER JOIN agency a ON a.agencyID = p.agencyID
		INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
		INNER JOIN [user] u ON u.userID = p.userID
		LEFT JOIN [PaymentAction] pa ON pa.[paymentID] = p.[paymentID]
	WHERE
		-- Фильтр по фирме и головной компании
		p.firmID = Coalesce(@firmID, p.firmID)
		AND f.headCompanyID = Coalesce(@headCompanyId, f.headCompanyID)
		
		-- Фильтр по типу платежа и агентству
		AND p.paymentTypeID = Coalesce(@paymentTypeID, p.paymentTypeID)
		AND p.agencyID = Coalesce(@agencyID, p.agencyID)
		
		-- Фильтр по незакрытым платежам
		AND (
			@isNotClosedOnly = 0 
			OR p.summa > ISNULL((SELECT SUM(pa.[summa]) FROM [PaymentAction] pa WHERE pa.[paymentID] = p.[paymentID]), 0)
		)
		
		-- Фильтр по периоду
		AND p.paymentDate >= Coalesce(@startOfInterval, p.paymentDate)
		AND p.paymentDate <= Coalesce(@endOfInterval, p.paymentDate)
		
		-- Фильтр по конкретному платежу
		AND p.paymentID = Coalesce(@paymentID, p.paymentID)
		
		-- Фильтр по действию
		AND (@actionID IS NULL OR pa.[actionID] = @actionID)
		
		-- Фильтр по типу платежа (белый/черный)
		AND (
			(pt.isHidden = 0 OR @isHideWhite = 0) 
			AND (pt.isHidden = 1 OR @isHideBlack = 0)
			AND (
				(pt.IsHidden = 1 AND @showBlack = 1)  
				OR (pt.IsHidden = 0 AND @showWhite = 1)
			)
		)
		
		-- Фильтр по правам доступа пользователя к агентству
		AND (
			@isAdmin = 1  -- Админу показываем все агентства
			OR @isBookKeeper = 1  -- Бухгалтеру показываем все агентства
			OR @filterAgencies = 0  -- Флаг отключен - показываем все агентства
			OR (
				-- Флаг включен и пользователь не админ/бухгалтер - проверяем права доступа
				EXISTS(
					SELECT 1 
					FROM Campaign cam 
					INNER JOIN [Action] act ON cam.actionID = act.actionID 
					WHERE a.agencyID = cam.agencyID 
						AND @loggedUserID = act.userID
				) 
				OR EXISTS(
					SELECT 1 
					FROM Payment pay 
					WHERE a.agencyID = pay.agencyID 
						AND @loggedUserID = pay.userID
				)
			)
		)
		
		-- Фильтр видимости платежей для обычных пользователей
		AND (
			@isAdmin = 1  -- Админу показываем все платежи
			OR @isBookKeeper = 1  -- Бухгалтеру показываем все платежи
			OR p.summa > ISNULL((SELECT SUM(pa2.[summa]) FROM [PaymentAction] pa2 WHERE pa2.[paymentID] = p.[paymentID]), 0)  -- Платеж не полностью распределен
			OR EXISTS(
				-- Платеж распределен, но есть Action пользователя
				SELECT 1 
				FROM [PaymentAction] pa2
				INNER JOIN [Action] act ON pa2.actionID = act.actionID
				WHERE pa2.[paymentID] = p.[paymentID] 
					AND act.userID = @loggedUserID
			)
		)
END

Exec sl_PaymentsCommon @languageCode = @languageCode
GO

IF @@TRANCOUNT > 0 COMMIT TRANSACTION;
GO

-- ===== Проверка =====
DECLARE @bad nvarchar(1000) = N'';
IF CHARINDEX(N'@languageCode', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.DiscountReleases')), N'')) = 0 SET @bad += N' DiscountReleases';
IF CHARINDEX(N'@languageCode', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.sl_TariffRetrieve')), N'')) = 0 SET @bad += N' sl_TariffRetrieve';
IF CHARINDEX(N'Exec sl_TariffRetrieve @languageCode = @languageCode', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.Tariffs')), N'')) = 0 SET @bad += N' Tariffs';
IF CHARINDEX(N'@tTu', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffPassport')), N'')) = 0 SET @bad += N' TariffPassport';
IF CHARINDEX(N'@tPeriod', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_VolumeOfRealizationByMonth3')), N'')) = 0 SET @bad += N' stat_VolumeOfRealizationByMonth3';
IF CHARINDEX(N'Exec sl_PaymentsCommon @languageCode = @languageCode', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.PaymentsCommon')), N'')) = 0 SET @bad += N' PaymentsCommon';
DECLARE @missing int = (SELECT COUNT(*) FROM (VALUES
    (N'пн'),
    (N'вт'),
    (N'ср'),
    (N'чт'),
    (N'пт'),
    (N'сб'),
    (N'вс'),
    (N'{0} {1} г.')
    ) AS v ([source]) WHERE NOT EXISTS
    (SELECT 1 FROM dbo.iTranslation t WHERE t.lang = 'es' AND t.context = ''
        AND t.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', v.[source]))));
IF @missing > 0 SET @bad += N' переводы: ' + CAST(@missing AS nvarchar(10));
IF @bad = N''
    PRINT N'ГОТОВО: шесть процедур передают язык, переводы (es) на месте. Перезапустите веб.';
ELSE
    RAISERROR(N'63: не применено:%s', 16, 1, @bad);
GO
SET NOEXEC OFF;
GO
