/*
    ДЕПЛОЙ: процедуры отдают видимый текст на языке пользователя веба (многоязычность, этап 6).
    Задача: docs/tasks/web-i18n.md. Сгенерирован из файлов ArtvisDB/dbo — объекты ниже
    совпадают с репозиторием.

    ЧТО ДЕЛАЕТ
      1. dbo.fn_Translate(@lang, @source) — перевод по iTranslation; для 'ru'/NULL — исходный текст.
      2. dbo.fn_GetTariffWindowDateRangeStr — 4-й параметр @lang, результат NVARCHAR(255)
         (единственный вызывающий — Pricelists, обновляется здесь же).
      3. 30 процедур получают последний параметр @languageCode VARCHAR(10) = 'ru' и
         переводят свои подписи («Прайс-лист от …», «Акция №», «сек.», «Все» …) один раз в
         переменную. Десктоп параметр не передаёт — результат прежний (сверено хэшами
         результатов на ArtvisDev до/после). Колонки с такими подписями становятся nvarchar.
         Объекты: Actions1, ActionsForBalance, ActionsForRollerStatistic, AgencyTaxRetrieve, Campaigns, CampaignsForActJournalRetrieve, DiscountValues, ModulePriceLists, ModulePricelistByDate, ModulePricelistPassport, PackModuleContentPassport, PackModulePricelistByDate, PackModulePricelists, PackageDiscountPriceLists, PaymentCommonActions, PricelistByDate, Pricelists, SpecialActions, SponsorPricelistByDate, SponsorPricelists, Stat_AvgDiscount, SysParams, TariffWindowRetrieve, sl_LookupMassmediaGroupd, sl_PaymentsCommon, sponsorTariffList, stat_VolumeOfRealization, stat_VolumeOfRealization2, stat_VolumeOfRealization3, stat_VolumeOfRealizationNew.

    ПРЕДУСЛОВИЕ     накачен web-i18n-translation-deploy.sql (таблица iTranslation).
    ТРАНЗАКЦИЯ      все объекты — в одной транзакции; при ошибке — откат и остановка (NOEXEC).
    QUOTED_IDENTIFIER — у каждого объекта как на проде (OFF у Campaigns, ModulePricelistByDate, PackModulePricelistByDate, PricelistByDate, SponsorPricelistByDate).
    ИДЕМПОТЕНТНОСТЬ повторный запуск безопасен (CREATE OR ALTER).
    ОТКАТ           процедуры и функцию — из предыдущего коммита; DROP FUNCTION dbo.fn_Translate.
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
GO
IF OBJECT_ID('dbo.iTranslation') IS NULL
BEGIN
    RAISERROR('Нет таблицы iTranslation: сначала web-i18n-translation-deploy.sql', 16, 1);
    SET NOEXEC ON;
END
GO
BEGIN TRANSACTION;
GO

-- ===== fn_Translate =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — fn_Translate и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
-- Перевод видимого текста для веба (docs/tasks/web-i18n.md): ключ — русский текст, как у Tr.T в C#.
-- Язык 'ru' или NULL — исходный текст без обращения к таблице (десктоп процедурам язык не передаёт).
-- Вызывать один раз в переменную в начале процедуры, а не в каждой строке результата.
CREATE OR ALTER FUNCTION [dbo].[fn_Translate]
(
    @lang   VARCHAR(10),
    @source NVARCHAR(4000)
)
RETURNS NVARCHAR(4000)
AS
BEGIN
    IF @lang IS NULL OR @lang = 'ru'
        RETURN @source;

    RETURN COALESCE(
        (SELECT [text]
         FROM   [dbo].[iTranslation]
         WHERE  [lang] = @lang
           AND  [context] = ''
           AND  [sourceHash] = CONVERT(binary(32), hashbytes('SHA2_256', @source))),
        @source);
END
GO

-- ===== fn_GetTariffWindowDateRangeStr =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — fn_GetTariffWindowDateRangeStr и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER FUNCTION [dbo].[fn_GetTariffWindowDateRangeStr]
(
	@startDate DATETIME = null,
	@finishDate DATETIME = null,
	@broadcastStart DATETIME,
	@lang VARCHAR(10) -- язык интерфейса веба (docs/tasks/web-i18n.md); 'ru' — исходный текст
)
RETURNS NVARCHAR(255)
AS
BEGIN
	DECLARE @str NVARCHAR(255)
	
	IF (@startDate IS NULL)
		BEGIN
			SET @str = dbo.fn_Translate(@lang, N'нет тарифных окон')
		END
	ELSE
		BEGIN
			IF CAST(@finishDate AS TIME) < CAST(@broadcastStart AS TIME)
				Set @finishDate = DATEADD(dd, -1, @finishDate)	
			SET @str = dbo.fn_Translate(@lang, N'тарифные окна: ') + CONVERT(VARCHAR(255), @startDate, 104) + ' - ' + CONVERT(VARCHAR(255), @finishDate, 104)
		END
		
	RETURN @str
END
GO

-- ===== Actions1 =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — Actions1 и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[Actions1]
(
@actionID int = NULL,
@firmID smallint = NULL,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@createDateBegin datetime = null, -- Новый параметр
@createDateEnd datetime = null,   -- Новый параметр
@paymentTypeId tinyint = null,
@campaignTypeId tinyint = null,
@campaignFinishDate datetime = null,
@firmId2 smallint = null,
@userID smallint = null,
@changeStartOfInterval datetime = null,
@changeEndOfInterval datetime = null,
@massmediaId smallint = null,
@agencyID smallint = null,
@issueDay datetime = null,
@issueDate datetime = null,
@rollerId smallint = null,
@isHideBlack bit = 0,
@isHideWhite bit = 0,
@paymentTypesIDString varchar(1024) = null,
@agenciesIDString varchar(1024) = NULL,
@withoutActionId INT = NULL,
@isShowActivate BIT = 0,
@isShowNotActivate BIT = 0,
@withoutActionsSince datetime = null,
@showBlack bit = 1,
@showWhite bit = 1,
@moduleID int = null,
@packModuleID int = null,
@loggedUserID smallint = null,
@managerDiscount float = null,
@massmediaGroupID smallint = null,
@showDeleted bit = 0,
@headCompanyID int = null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT on
	DECLARE @tAction NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Акция №');
	-- Проблема
	-- a.createDate <= @createDateEnd — при @createDateEnd = '2025-05-01 00:00:00' любая акция, созданная 2025-05-01 11:42, 
	--отсекается, потому что 11:42 > 00:00.
	-- Решение — сдвиг границы, а не обрезка колонки
	SET @createDateEnd = DATEADD(DAY, 1, CAST(@createDateEnd AS date));

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id)
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	declare @headCompaniesWithRecentAction table (headCompanyID int primary key)
	if @withoutActionsSince is not null
	begin
		insert into @headCompaniesWithRecentAction (headCompanyID)
		select distinct f1.headCompanyID
		from [Action] a1
			inner join [Firm] f1 on a1.firmID = f1.firmID
		where f1.headCompanyID is not null
			and a1.isConfirmed = 1
			and a1.finishDate >= @withoutActionsSince
			and (@startOfInterval is null or a1.startDate < @startOfInterval)
	end

	if @actionID is not null
	begin 
		select a.*, 
			us.userName as creator,
			@tAction + LTRIM(a.[actionID]) + ' (' + LTRIM(f.name) + ')'  as name,
			f.name as firmName,
			coalesce(x.iCount, 0) as iCount,
			coalesce(x.duration, '00:00') as duration,
			Cast(
				Case 
					When a.tariffPrice = 0 Then 1
					Else a.totalPrice/a.tariffPrice
			End  
			as decimal(5,2)) as finalRatio,
			a.startDate,
			a.finishDate
		from [Action] a
			INNER JOIN [vUser] us ON us.userID = a.userID
			INNER JOIN [Firm] f ON f.firmID = a.firmID
			left join 
			(
				select c.actionID, count(distinct i.issueID) as iCount,
					dbo.fn_Int2Time(coalesce(sum(r.duration), 0)) as duration
				from dbo.Campaign c 
					inner join Issue i on c.campaignID = i.campaignID
					inner join Roller r on i.rollerID = r.rollerID
				where c.actionID = @actionID 
				group by c.actionID
			) x on a.actionID = x.actionID
		where 
			a.actionID = @actionID 
            -- Фильтр по дате создания
            AND (@createDateBegin IS NULL OR a.createDate >= @createDateBegin)
            AND (@createDateEnd IS NULL OR a.createDate < @createDateEnd)
			and (@headCompanyID is null or f.headCompanyID = @headCompanyID)
			AND (
				(a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) 
				OR (a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) 
				or (a.deleteDate is not null and @showDeleted = 1)
				OR (@isShowNotActivate = 0 And @isShowActivate = 0 And @showDeleted = 0)
				)
	end 
	else if @issueDay is not null or @rollerId is not null or @issueDate is not null or @moduleID is not null or @packModuleID is not null
	begin 
		declare @issues table (actionID int primary key )
		insert into @issues
		select distinct c.actionID 
		from Issue i 
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
			left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
			left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
			left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
		where i.rollerID = coalesce(@rollerId, i.rollerID)
			and (@issueDate is null or ((datepart(hh, tw.windowDateOriginal) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateOriginal) = datepart(minute, @issueDate))) )
			and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
			and (@moduleID is null or mi.moduleID = @moduleID)
			and (@packModuleID is null or pmpl.packModuleID = @packModuleID)	
			and mm.massmediaGroupID = Coalesce(@massmediaGroupID, mm.massmediaGroupID)
			and tw.massmediaID = Coalesce(@massmediaId, tw.massmediaId)
										
		SELECT distinct 
			a.*, 
			us.userName as creator,
			--'Акция №' + LTRIM(a.[actionID]) + ' (' + LTRIM(f.name) + ')'  as name,
			@tAction + LTRIM(a.[actionID]) as name,
			f.name as firmName,
			Cast(
			Case 
				When a.tariffPrice = 0 Then 1
				Else a.totalPrice/a.tariffPrice
			End  
			as decimal(5,2)) as finalRatio,
			a.startDate,
			a.finishDate
		FROM 
			[Action] a
			inner join @issues i on i.actionID = a.actionID
			Inner Join Campaign c ON c.actionId = a.actionId
			Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
			INNER JOIN [Agency] ag ON c.[agencyID] = ag.[agencyID]
			INNER JOIN [vUser] us ON us.userID = a.userID
			INNER JOIN [Firm] f ON f.firmID = a.firmID
			left join @massmedias umm on c.massmediaID = umm.massmediaID
			left join GroupMember gm on us.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id
		WHERE	
			(us.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			a.isSpecial = 0 and	
			a.finishDate >= Coalesce(@startOfInterval, a.finishDate) And
			a.startDate <= Coalesce(@endOfInterval, a.startDate) And
            -- Фильтр по дате создания
            (@createDateBegin IS NULL OR a.createDate >= @createDateBegin) AND
            (@createDateEnd IS NULL OR a.createDate < @createDateEnd) AND
			c.paymentTypeId = Coalesce(@paymentTypeId, c.paymentTypeId) And
			c.campaignTypeId = Coalesce(@campaignTypeId, c.campaignTypeId) And
			c.finishDate = Coalesce(@campaignFinishDate, c.finishDate) And
			a.firmId = Coalesce(@firmId2, a.firmId) And
			a.userId = Coalesce(@userId, a.userId) And
			a.modDate >= Coalesce(@changeStartOfInterval, a.modDate) And
			a.modDate <= Coalesce(@changeEndOfInterval, a.modDate) And
			((c.[agencyID] IS NULL AND @agencyID IS NULL) OR c.agencyId = Coalesce(@agencyID, c.agencyId)) And
			a.actionId = Coalesce(@actionId, a.actionId) And
			(pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) and
			a.[actionID] = COALESCE(@actionID, a.[actionID]) AND
			a.[firmID] = COALESCE(@firmID, a.[firmID]) 
			AND (
				(a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) 
				OR (a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) 
				or (a.deleteDate is not null and @showDeleted = 1)
				)
			AND (@withoutActionId IS NULL OR a.[actionID] <> @withoutActionId)
			and (@withoutActionsSince is null or not exists(select 1 from @headCompaniesWithRecentAction h where h.headCompanyID = f.headCompanyID))
			and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			and (@headCompanyID is null or f.headCompanyID = @headCompanyID)
		order by a.actionID desc
	end
	else 
		Begin
		SELECT distinct 
			a.*, 
			us.userName as creator,
			--'Акция №' + LTRIM(a.[actionID]) + ' (' + LTRIM(f.name) + ')'  as name,
			@tAction + LTRIM(a.[actionID]) as name,
			f.name as firmName,
			Cast(
			Case 
				When a.tariffPrice = 0 Then 1
				Else a.totalPrice/a.tariffPrice
			End  
			as decimal(5,2)) as finalRatio,
			a.startDate,
			a.finishDate
		FROM 
			[Action] a
			Inner Join Campaign c ON c.actionId = a.actionId
			Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
			INNER JOIN [User] us ON us.userID = a.userID
			INNER JOIN [Firm] f ON f.firmID = a.firmID
			LEFT JOIN (
				PackModuleIssue i 
				JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
				JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
				JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
				) ON i.campaignID = c.campaignID
		where
			(a.userID = @loggedUserID 
						or @isRightToViewForeignActions = 1 
						or (
							@isRightToViewGroupActions = 1 
							AND EXISTS (
								SELECT 1 
								FROM GroupMember gm 
									JOIN fn_GetUserGroups(@loggedUserID) ug on gm.groupID = ug.id
								WHERE a.userID = gm.userID
								)
							)
						)
			and EXISTS (
					SELECT 1 
					FROM @massmedias umm 
					WHERE umm.massmediaID = CASE WHEN c.campaignTypeID=4 THEN m.massmediaID ELSE c.massmediaID END
							and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
					) 
			AND (@massmediaGroupID IS NULL 
					OR 
					EXISTS (
						SELECT 1
						FROM MassMedia mm
						WHERE mm.massmediaID = CASE WHEN c.campaignTypeID=4 THEN m.massmediaID ELSE c.massmediaID END
							AND mm.massmediaGroupID = @massmediaGroupID
						)
					)
			and	a.isSpecial = 0 and		
			(a.finishDate >= Coalesce(@startOfInterval, a.finishDate) Or (a.finishDate Is Null And @startOfInterval Is Null )) And
			(a.startDate <= Coalesce(@endOfInterval, a.startDate) Or (a.startDate Is Null And @endOfInterval Is Null ))  And
            -- Фильтр по дате создания
            (@createDateBegin IS NULL OR a.createDate >= @createDateBegin) AND
            (@createDateEnd IS NULL OR a.createDate < @createDateEnd) AND
			c.paymentTypeId = Coalesce(@paymentTypeId, c.paymentTypeId) And
			c.campaignTypeId = Coalesce(@campaignTypeId, c.campaignTypeId) And 
			c.finishDate = Coalesce(@campaignFinishDate, c.finishDate) And
			a.firmId = Coalesce(@firmId2, a.firmId) And
			a.userId = Coalesce(@userId, a.userId) And
			a.modDate >= Coalesce(@changeStartOfInterval, a.modDate) And
			a.modDate <= Coalesce(@changeEndOfInterval, a.modDate)
			and (c.massmediaID = Coalesce(@massmediaId, c.massmediaId) Or c.massmediaID Is Null)	
			and (m.massmediaID = Coalesce(@massmediaId, m.massmediaID) Or m.massmediaID Is Null)
			and ((c.[agencyID] IS NULL AND @agencyID IS NULL) OR c.agencyId = Coalesce(@agencyID, c.agencyId)) And
			a.actionId = Coalesce(@actionId, a.actionId) And
			(pt.isHidden = 0 or @isHideWhite = 0) And
			(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) and
			a.[actionID] = COALESCE(@actionID, a.[actionID]) AND
			a.[firmID] = COALESCE(@firmID, a.[firmID])
			AND (
				(a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) 
				OR (a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) 
				or (a.deleteDate is not null and @showDeleted = 1)
				)
			AND (@withoutActionId IS NULL OR a.[actionID] <> @withoutActionId)
			and (@withoutActionsSince is null or not exists(select 1 from @headCompaniesWithRecentAction h where h.headCompanyID = f.headCompanyID))
			and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			and (@headCompanyID is null or f.headCompanyID = @headCompanyID)
		order by a.actionID desc
		End
GO

-- ===== ActionsForBalance =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — ActionsForBalance и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[ActionsForBalance]
(
@firmID smallint = NULL,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@userID smallint = null,
--@paymentTypesIDString varchar(1024) = null,
@agenciesIDString varchar(1024) = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @tAction NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Акция №');
	
	Select	@startOfInterval = Convert(datetime, Convert(varchar, @startOfInterval, 112), 112)
	Select	@endOfInterval = Convert(datetime, Convert(varchar, @endOfInterval, 112), 112)
			
	CREATE TABLE #Agency(agencyID smallint)

	-- Populate temporary tables with Agency and Payment types
	IF @agenciesIDString Is Null
		INSERT INTO #Agency 
		SELECT agencyID FROM Agency
	Else
		Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString 
		
	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,
		@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)
		
	CREATE TABLE #tmp1(actionID int primary key,	summa decimal(18,2) NULL, tariffPrice decimal(18,2) null, [priceSumByCampaigns] decimal(18,2) null)
	INSERT INTO #tmp1 ([actionID], [summa], [tariffPrice], [priceSumByCampaigns]) 
SELECT distinct a.actionID, 0, 0, 0
FROM 
	[Action] a
		Inner Join Campaign c ON c.actionId = a.actionId
		Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
		inner join [#Agency] ag on c.agencyID = ag.agencyID
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
WHERE		
		(a.userID = @loggedUserID 
		 or @isRightToViewForeignActions = 1 
		 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and

		(a.isSpecial = 1 
		 or (c.campaignTypeID <> 4 
		     and umm.massmediaID is not null 
		     and ((a.userID = @loggedUserID and umm.myMassmedia = 1) 
		          or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1)))
		 or (c.campaignTypeID = 4 
		     and not exists(
				select *
				from PackModuleIssue pmi 
					inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
					inner join Module m on pmc.moduleID = m.moduleID
					left join @massmedias ummm on m.massmediaID = ummm.massmediaID
				where pmi.campaignID = c.campaignID 
				  and (
						ummm.massmediaID is null 
						or (a.userID = @loggedUserID and ummm.myMassmedia = 0)
						or (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0)
				  )
		     )
		)) and	
		a.finishDate >= Coalesce(@startOfInterval, a.finishDate) and
		a.startDate <= Coalesce(@endOfInterval, a.startDate) and
		a.userId = Coalesce(@userId, a.userId) and
		(pt.isHidden = 0 or @isHideWhite = 0) and
		(pt.isHidden = 1 or @isHideBlack = 0) and
		((pt.IsHidden = 1 and @showBlack = 1)  
		 or (pt.IsHidden = 0 and @showWhite = 1)) and
		a.[firmID] = COALESCE(@firmID, a.[firmID]) and
		a.[isConfirmed] = 1
		
	Declare cur_Companies Cursor local fast_forward
	For
	SELECT 	c.campaignID, c.campaignTypeID,
			c.startDate,
			a.[actionID],
			c.finalPrice,
			c.managerDiscount,
			c.discount,
			c.finishDate,
			ac.discount,
			c.tariffPrice
	From	campaign AS c join #tmp1 as a on c.actionID = a.actionID
			join paymenttype as pt on c.paymentTypeID = pt.paymenttypeID
			join agency as ag on ag.agencyID = c.agencyID
			inner join [Action] ac on ac.actionID = c.actionID
	Where	ag.agencyID IN (Select agencyID From #Agency)
			and (pt.isHidden = 0 or @isHideWhite = 0) And
				(pt.isHidden = 1 or @isHideBlack = 0) and
			((pt.IsHidden = 1 and @showBlack = 1)  or
			(pt.IsHidden = 0 and @showWhite = 1)) 
				
	Declare	@campaignID int, @TypeID int, @StartDate DATETIME,
			@Price decimal(18,2), @Action int,
			@FinalPrice decimal(18,2), @tariffPrice decimal(18,2), 
			@managerDiscount decimal(18,10), @discount decimal(9,4), 
			@finishDate datetime, @actiondiscount decimal(9,4)
		
	Open	cur_Companies
	Fetch	Next from cur_Companies
	Into 	@campaignID, @TypeID, @StartDate, @Action, @FinalPrice, @managerDiscount, @discount, @finishDate, @actiondiscount, @tariffPrice

	While	@@fetch_status = 0
	Begin
		If	(@startOfInterval is null and @endOfInterval IS null) OR (@endOfInterval > @finishDate and @startOfInterval < @StartDate)
		begin 
			set  @Price = @FinalPrice 
		end 
		else
		begin 
			exec GetPriceByPeriod @campaignID = @campaignID, @campaignTypeID = @TypeID, @startDate = @startOfInterval, @finishDate = @endOfInterval, @price = @price OUTPUT, @tariffPrice = @tariffPrice output
		end
		
		UPDATE [#tmp1] SET summa = summa + ISNULL(@Price, 0), [tariffPrice] = [tariffPrice] + ISNULL(@tariffPrice, 0), [priceSumByCampaigns] = [priceSumByCampaigns] + ISNULL(@tariffPrice * @managerDiscount * @discount, 0)
		WHERE [actionID] = @Action

		Fetch	Next from cur_Companies
		Into 	@campaignID, @TypeID, @StartDate, @Action, @FinalPrice, @managerDiscount, @discount, @finishDate, @actiondiscount, @tariffPrice

	end

	close		cur_Companies
	Deallocate	cur_Companies
		
	SELECT 
		ac.[actionID],
		ac.[firmID],
		ac.[startDate],
		ac.[finishDate],
		ac.[discount],
		ac.[userID],
		a.tariffPrice AS [tariffPrice],
		a.[priceSumByCampaigns] AS [priceSumByCampaigns],
		ac.[createDate],
		ac.[modDate],
		ac.[isSpecial],
		ac.[isConfirmed],
		a.summa AS totalPrice,
		us.firstName + Space(1) + us.lastName as creator,
		@tAction + LTRIM(ac.[actionID]) as name,
		f.name as firmName
	FROM 
		#tmp1 a
		INNER JOIN [Action] ac ON a.actionID = ac.actionID
		INNER JOIN [User] us ON us.userID = ac.userID
		INNER JOIN [Firm] f ON f.firmID = ac.firmID
	ORDER BY
		ac.[actionID] DESC		
END
GO

-- ===== ActionsForRollerStatistic =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — ActionsForRollerStatistic и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[ActionsForRollerStatistic]
(
@startDate datetime,
@finishDate datetime,
@rollerID int,
@massmediaString varchar(8000),
@userID smallint = null,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tAction NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Акция №');

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

SELECT distinct
	ac.*,
	us.userName as creator,
	@tAction + LTRIM(ac.[actionID]) as name,
	f.name as firmName
FROM 
	[Action] ac 
	INNER JOIN [vUser] us ON us.userID = ac.userID
	INNER JOIN [Firm] f ON f.firmID = ac.firmID
	INNER JOIN [Campaign] c ON c.actionID = ac.actionID
	INNER JOIN Issue i ON c.campaignID = i.campaignID
	INNER JOIN TariffWindow tw On tw.windowId = i.originalWindowID
	INNER JOIN dbo.fn_CreateTableFromString(@massmediaString) m on m.ID = tw.massmediaID
	inner join 
	(
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id	
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) as x on ac.userID = x.userID
where 
	i.rollerID = @rollerID AND
	tw.dayOriginal between @startDate and @finishDate and 
	ac.userID = Coalesce(@userID, ac.userID)	
ORDER BY
	ac.[actionID] DESC
GO

-- ===== AgencyTaxRetrieve =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — AgencyTaxRetrieve и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[AgencyTaxRetrieve]
(
@agencyId smallint = null,
@agencyTaxId smallint = null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tAgencyTax NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Налог для агентства ''');

Select 
	a.*,
	t.name as taxName,
	@tAgencyTax + ag.name + '''' as name
From
	AgencyTax a
	Inner Join iTax t On a.taxId = t.taxId
	Inner Join Agency ag On a.agencyId = ag.agencyId
Where
	a.agencyId = Coalesce(@agencyId, a.agencyId) And
	a.agencyTaxId = Coalesce(@agencyTaxId, a.agencyTaxId)
Order by
	a.startDate desc
GO

-- ===== Campaigns =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — Campaigns и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER OFF;
GO
CREATE OR ALTER PROC [dbo].[Campaigns]
(
@actionID int = null,
@campaignID int = null,
@massmediaID smallint = null,
@loggedUserID smallint = null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
as
set nocount on
DECLARE @tPackModuleCampaign NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Пакетная модульная кампания');
DECLARE @tPackModuleCampaignTypo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Пакетная модульня кампания');

IF (@actionID IS NOT NULL OR @campaignID IS NOT NULL /* (@campaignID IS NOT NULL AND @massmediaID IS NULL AND @actionID IS NULL)*/)
begin
	SELECT
		cm.*,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN @tPackModuleCampaign
			ELSE mm.NAME + isnull(' (' + mg.name +')', '')
		END AS name	,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		CASE cm.[campaignTypeID]
			WHEN 1 THEN 91
			WHEN 2 THEN 93
			WHEN 3 THEN 92
			WHEN 4 THEN 171
		END AS entityId,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN CAST(1 AS DECIMAL(9,4))
			ELSE a.[discount]
		END AS packDiscount,
		cm.[finalPrice] AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		LEFT JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = cm.massmediaID
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
	WHERE
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID)
	ORDER BY
		cm.campaignID
end
ELSE
begin
	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id)
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	SELECT distinct
		cm.*,
		CASE cm.[campaignTypeID]
			WHEN 4 THEN @tPackModuleCampaign
			ELSE mm.NAME + isnull(' (' + mg.name +')', '')
		END AS name	,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		CASE cm.[campaignTypeID]
			WHEN 1 THEN 91
			WHEN 2 THEN 93
			WHEN 3 THEN 92
			WHEN 4 THEN 171
		END AS entityId,
		NULL AS packmodulemassmediaID,
		a.[discount] AS packDiscount,
		cm.[finalPrice] AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		INNER JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = cm.massmediaID
			AND cm.massmediaID = COALESCE(@massmediaID, cm.massmediaID)
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
		left join GroupMember gm WITH (NOLOCK) on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID) AND
		a.isConfirmed = 1 AND cm.[campaignTypeID] <> 4
	union all
	(SELECT DISTINCT
		cm.*,
		@tPackModuleCampaignTypo AS name,
		mm.name as massmediaName,
		f.[name] AS firmName,
		ct.name as campaignTypeName,
		pt.name as paymentTypeName,
		ag.name as agencyName,
		dbo.fn_Int2Time(cm.issuesDuration) as issuesDurationString,
		u.lastName + ' ' + u.firstName as modUserName,
		171 AS entityId,
		mm.massmediaID AS packmodulemassmediaID,
		CAST(1 AS DECIMAL(9,4)) AS packDiscount,
		CAST(cm.[finalPrice] AS DECIMAL(18,2)) AS fullPrice,
		mg.name as groupName,
		a.deleteDate
	FROM
		[Campaign] cm WITH (NOLOCK)
		INNER JOIN [Action] a WITH (NOLOCK) ON cm.[actionID] = a.[actionID]
		INNER JOIN [Firm] f WITH (NOLOCK) ON a.[firmID] = f.[firmID]
		INNER JOIN iCampaignType ct WITH (NOLOCK) ON ct.campaignTypeID = cm.campaignTypeID
			AND cm.[campaignTypeID] = 4
		INNER JOIN PaymentType pt WITH (NOLOCK) ON pt.paymentTypeID = cm.paymentTypeID
		LEFT JOIN Agency ag WITH (NOLOCK) ON ag.agencyId = cm.agencyId
		LEFT OUTER JOIN [User] u WITH (NOLOCK) ON u.userId = cm.modUser
		INNER JOIN [PackModuleIssue] pmi WITH (NOLOCK) ON pmi.campaignID = cm.campaignID
		INNER JOIN [PackModuleContent] pmc WITH (NOLOCK) ON pmc.pricelistID = pmi.pricelistID
		INNER JOIN Module m WITH (NOLOCK) ON m.moduleID = pmc.moduleID
		INNER JOIN vMassMedia mm WITH (NOLOCK) ON mm.massmediaID = m.massmediaID
			AND m.massmediaID = COALESCE(@massmediaID, m.massmediaID)
		LEFT JOIN MassmediaGroup mg WITH (NOLOCK) on mg.massmediaGroupID = mm.massmediaGroupID
		left join GroupMember gm WITH (NOLOCK) on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where
		(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
		cm.actionID = COALESCE(@actionID, cm.actionID) AND
		cm.campaignID = COALESCE(@campaignID, cm.campaignID) AND
		a.isConfirmed = 1 AND cm.[campaignTypeID] = 4
	)
	ORDER BY
		cm.campaignID
end
GO

-- ===== CampaignsForActJournalRetrieve =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — CampaignsForActJournalRetrieve и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[CampaignsForActJournalRetrieve]
(
@startDate DATETIME = null,
@finishDate DATETIME = null,
@agencyID int = null,
@firmId int = null,
@showBlack bit = 1,
@showWhite bit = 1,
@actionID INT = null,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
Set Nocount On
DECLARE @tSec NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' сек.');
DECLARE @tPcs NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' шт.');

If @agencyID Is Null Begin
	RaisError('AgencyShouldBeSelected', 16, 1)
	Return
END

IF @startDate IS NULL 
	SELECT @startDate = dbo.ToShortDate(MIN(c.[startDate])) FROM [Campaign] c
	
IF @finishDate IS NULL 
	SELECT @finishDate = dbo.ToShortDate(MAX(c.finishDate)) FROM [Campaign] c

IF @finishDate < @startDate
BEGIN
	RaisError('WrongDates', 16, 1)
	Return
end

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

Declare @res Table(
	currentdate DATETIME,
	campaignId int,
	typeId smallint,
	total decimal(18,2) NULL,
	massmediaID INT null,
	mistake decimal(18,2) default 0,
	issuesCount INT default 0,
	issuesDuration timeDuration NULL default 0,
	showByDuration BIT NULL default 1
)

DECLARE @tmpDate DATETIME 
SET @tmpDate = @startDate
WHILE @tmpDate <= @finishDate
begin
	Insert Into @res
	Select distinct
		dbo.ToShortDate(CASE 
			WHEN dbo.fn_LastDateOfMonth(@tmpDate) < a.[finishDate] 
				THEN dbo.fn_LastDateOfMonth(@tmpDate) 
				ELSE a.[finishDate]
		end),
		c.campaignID,
		c.campaignTypeID,
		0,
		c.[massmediaID],
		0,
		0,
		0,
		1
	from
		[Action] a 
		inner join Campaign c ON c.[actionID] = a.[actionID]
		inner join PaymentType pt On c.paymentTypeId = pt.paymentTypeId
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	Where	
		(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
		and (
			a.isSpecial = 1 
			or c.campaignTypeID = 4 
			or umm.massmediaID is not null 
			and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
		) 
		and	a.isSpecial = 0	AND a.[actionID] = COALESCE(@actionID, a.[actionID])
		and a.firmID = isnull(@firmID, a.firmID)
		and c.agencyId = @agencyId AND
		(pt.isHidden = 0 or @showBlack = 1) And
		(pt.isHidden = 1 or @showWhite = 1) AND
		a.[isConfirmed] = 1 AND
		dbo.ToShortDate(a.[finishDate]) >= @tmpDate AND 
		(dbo.fn_LastDateOfMonth(@tmpDate) <= (@finishDate) OR dbo.ToShortDate(a.[finishDate]) <= @finishDate)
				
	SET @tmpDate = DATEADD(month, 1, dbo.fn_FirstDateOfMonth(@tmpDate))
END

Declare	
	@currentDate DATETIME,
	@typeId smallint,
	@total decimal(18,2),
	@campaignID INT,
	@massmediaID smallint,
	@campaignStartDate datetime,
	@campaignFinishDate datetime,
	@campaignFinalPrice decimal(18,2),
	@campaignAdiscount decimal(18,10),
	@mistake decimal(18,2),
	@userID smallint,
	@issuesCount INT,
	@issuesDuration timeDuration,
	@showByDuration BIT

Declare cur_comp2 Cursor local fast_forward
For
SELECT r.currentDate, r.campaignId, r.typeId, c.startDate, c.finishDate, c.finalPrice, a.discount, a.userID From @res r inner join Campaign c on r.campaignId = c.campaignId inner join [Action] a on c.actionID = a.actionID
Open cur_comp2

Fetch Next From cur_comp2 Into @currentDate, @campaignId, @typeId, @campaignStartDate,@campaignFinishDate,@campaignFinalPrice,@campaignAdiscount,@userID
While @@fetch_status = 0 BEGIN
	SET @startDate = dbo.fn_FirstDateOfMonth(@currentDate)

	IF @typeId = 4
	BEGIN
		DELETE FROM @res WHERE [campaignID] = @campaignID AND [currentdate] = @currentDate
		
		Exec GetPriceByPeriod 
			@campaignId, @typeId, @startDate, @currentDate, @total OUT
		
		CREATE TABLE #tmp(massmediaID SMALLINT, price decimal(18,2))
		INSERT INTO #tmp
		SELECT 
			m.[massmediaID], sum(mpl.[price])
		FROM [PackModuleIssue] i 
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @currentDate 
		group by m.massmediaID
			
			
		declare @sumPrice decimal(18,2)
		SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1
			
		INSERT INTO @res ([currentdate],[campaignId],[typeId],[total],[massmediaID])
		select @currentDate, @campaignId, @typeId, @total * sum(t1.price)/ @sumPrice, t1.massmediaID 
		from #tmp as t1
			inner join @massmedias mmu on t1.massmediaID = mmu.massmediaID 
				and ((@userID = @loggedUserID and mmu.myMassmedia = 1) or
					(@userID <> @loggedUserID and mmu.foreignMassmedia = 1))
		group by t1.massmediaID
		
		drop table #tmp 

		update r
		set 
			r.issuesCount = r.issuesCount + x.issuesCount,
			r.issuesDuration = r.issuesDuration + x.issuesDuration,
			r.showByDuration = x.showByDuration
		from 
			@res r
		inner join (
			select 	COUNT(*) as issuesCount, 
				SUM(rol.duration) as issuesDuration, 
				cast(case when SUM(tw.maxCapacity) > 0 then 0 else 1 end as bit) as showByDuration,
				m.massmediaID
			from Issue i 
				inner join TariffWindow tw on i.originalWindowID = tw.windowId
				INNER join Roller rol on rol.rollerID = i.rollerID
				INNER join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
				INNER JOIN [PackModuleContent] AS pmc ON pmi.[priceListID] = pmc.[pricelistID]
				INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
				INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
				left join @massmedias mmu on m.massmediaID = mmu.massmediaID 
					and ((@userID = @loggedUserID and mmu.myMassmedia = 1) or
						(@userID <> @loggedUserID and mmu.foreignMassmedia = 1))
			where i.campaignID = @campaignID and tw.massmediaID = m.massmediaID and pmi.issueDate between @startDate and @currentDate 
			group by m.massmediaID
		) as x on r.massmediaID = x.massmediaID
		where r.campaignId = @campaignID
	END
	ELSE
	begin
		Exec GetPriceByPeriod 
			@campaignId, @typeId, @startDate, @currentDate, @total OUT
		
		IF @typeId = 2
		BEGIN
			select 
				@issuesCount = COUNT(*), 
				@issuesDuration = SUM(st.duration), 
				@showByDuration = 0
			From ProgramIssue i 
				inner join SponsorTariff st on i.tariffID = st.tariffID
				inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
			Where		
				i.campaignID = @campaignID and 
				Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate)), 112), 112) between dbo.ToShortDate(@startDate) and dbo.ToShortDate(@currentDate) 
		END
		ELSE
		BEGIN
			select 
				@issuesCount = COUNT(*), 
				@issuesDuration = SUM(rol.duration), 
				@showByDuration = case when SUM(tw.maxCapacity) > 0 then 0 else 1 end
			from Issue i 
				inner join Roller rol on rol.rollerID = i.rollerID
				inner join TariffWindow tw on i.originalWindowID = tw.windowId
			where i.campaignID = @campaignID and tw.dayOriginal between dbo.ToShortDate(@startDate) and dbo.ToShortDate(@currentDate) 
		END
		
		Update @res
		Set total = @total, showByDuration = @showByDuration, issuesCount = issuesCount + @issuesCount, issuesDuration = issuesDuration + @issuesDuration
		Where currentDate = @currentDate And campaignId = @campaignId
	END
	
	if @campaignFinishDate between @startDate and @currentDate
	begin 
		Exec GetPriceByPeriod @campaignId, @typeId, @campaignStartDate, @campaignFinishDate, @total out
		set @mistake = @campaignFinalPrice - @total
		update @res set mistake = @mistake where campaignId = @campaignID
	end 
	
	Fetch Next From cur_comp2 INTO @currentDate, @campaignId, @typeId, @campaignStartDate,@campaignFinishDate,@campaignFinalPrice,@campaignAdiscount,@userID
End

CLOSE cur_comp2
DEALLOCATE cur_comp2

Select 
	r.currentDate,
	r.currentDate AS currentDate2,
	c.campaignId,
	c.actionId,
	c.startDate,
	c.finishDate,
	cast(null as decimal(18,2)) as total,
	r.total as campaignTotal,
	f.name as firmName,
	f.firmId,
	m.nameWithGroup as massmediaName,
	m.massmediaId,
	pt.name as paymentTypeName,
	u.LastName + ' ' + u.FirstName as userName,
	r.mistake,
	case when r.showByDuration = 1 then dbo.fn_Int2Time(r.issuesDuration) + @tSec else cast(r.issuesCount as nvarchar(10)) + @tPcs end as saleVolume
From 
	@res r
	Inner Join Campaign c On r.CampaignId = c.CampaignId
	Inner Join Action a On a.actionId = c.actionId
	Inner Join Firm f On f.firmId = a.firmId
	Inner Join vMassmedia m On m.massmediaId = r.massmediaID
	Inner Join PaymentType pt On pt.paymentTypeId = c.paymentTypeId
	Inner Join [User] u On u.userId = a.userId
WHERE 
	r.total IS NOT NULL AND r.total > 0
Order by
	r.currentDate asc,
	c.actionId desc
	

select top 1 1
from @res r
	inner join MassMedia mm on r.massmediaID = mm.massmediaID 
	inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
where mm.deadline < @finishDate
GO

-- ===== DiscountValues =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — DiscountValues и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC dbo.DiscountValues
(
@discountReleaseID smallint = Null,
@discountValueID smallint = Null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tDiscountForSumsOver NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Скидка для сумм более ')
DECLARE @tRub NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'р.')
SELECT 
	[discountValueID], 
	[discountReleaseID], 
	[summa], 
	[discount],
	@tDiscountForSumsOver + LTrim(Str([summa])) + @tRub as name
FROM 
	[DiscountValue]
WHERE
	[discountReleaseID] = Coalesce(@discountReleaseID, [discountReleaseID])
	AND [discountValueID] = Coalesce(@discountValueID, [discountValueID])
ORDER BY
	summa DESC
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DiscountValues] TO PUBLIC
    AS [dbo];
GO

-- ===== ModulePriceLists =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — ModulePriceLists и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[ModulePriceLists]
(
@moduleID smallint = NULL,
@modulePriceListID smallint = NULL,
@hideModulePLInThePast bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)

AS
SET NOCOUNT ON
DECLARE @tModule NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Модуль ')
DECLARE @tPriceFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' (прайс от ')
SELECT 
	mpl.*, 
	pl.broadcastStart,
	@tModule + CONVERT(varchar(10), mpl.startDate, 104) + ' - ' + CONVERT(varchar(10), mpl.finishDate, 104) + @tPriceFrom + CONVERT(varchar(10), pl.startDate, 104) + ')' as NAME,
	mm.[roltypeID]
FROM 
	[ModulePriceList] mpl
	INNER JOIN PriceList pl ON pl.priceListID = mpl.priceListID
	INNER JOIN [MassMedia] mm ON pl.[massmediaID] = mm.[massmediaID]
WHERE
	mpl.moduleID = Coalesce(@moduleID, mpl.moduleID) And
	mpl.modulePriceListID = Coalesce(@modulePriceListID, mpl.modulePriceListID)
	AND (@hideModulePLInThePast = 0 OR mpl.finishDate >= CAST(GETDATE() AS DATE))
ORDER BY
	mpl.startDate ASC
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[ModulePriceLists] TO PUBLIC
    AS [dbo];
GO

-- ===== ModulePricelistByDate =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — ModulePricelistByDate и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER OFF;
GO
CREATE OR ALTER PROC [dbo].[ModulePricelistByDate]
(
@massmediaID smallint,
@theDate datetime,
@moduleID SMALLINT = NULL,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
SELECT 
	mpl.[modulePriceListID], 
	mpl.[priceListID],
	mpl.[price],
	mpl.[moduleID],
	mpl.startDate,
	mpl.[finishDate],
	@tPricelistFrom + CONVERT(varchar(10), mpl.startDate, 104) as name
FROM 
	[ModulePricelist] mpl 
	INNER JOIN [Pricelist] pl ON mpl.[priceListID] = pl.[pricelistID]
WHERE
	mpl.moduleID = ISNULL(@moduleID, mpl.moduleID) AND
	@theDate BETWEEN mpl.[startDate] AND mpl.[finishDate] AND
	pl.[massmediaID] = @massmediaID
GO

-- ===== ModulePricelistPassport =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — ModulePricelistPassport и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[ModulePricelistPassport]
(
@moduleID smallint,
@modulePriceListID smallint = NULL,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
SELECT 
	DISTINCT(p.pricelistID) as ID,
	p.[startDate],
	@tPricelistFrom + CONVERT(varchar(10), p.startDate, 104) as name 
FROM 
	Module m
	INNER JOIN Pricelist p ON p.massmediaID = m.massmediaID
	LEFT JOIN ModulePriceList mp ON m.moduleID = mp.moduleID 
		And p.priceListID = mp.priceListID 
		And (mp.modulePriceListID <> @modulePriceListID OR @modulePriceListID IS NULL) 
WHERE 
	m.moduleID = @moduleID 
ORDER BY 
	p.startDate DESC
GO

-- ===== PackModuleContentPassport =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — PackModuleContentPassport и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[PackModuleContentPassport]
(
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
DECLARE @tModule NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Модуль ')
DECLARE @tPriceFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' (прайс от ')
-- 1. Massmedia
SELECT massmediaID as [id], nameWithGroup as [name] FROM vMassmedia  where isActive = 1 ORDER BY [name]

-- 2. Modules
EXEC ModuleList

-- 3. PriceLists
SELECT 
	mpl.[modulePriceListID], 
	mpl.[moduleID],
	@tModule + CONVERT(varchar(10), mpl.startDate, 104) + ' - ' + CONVERT(varchar(10), mpl.finishDate, 104) + @tPriceFrom + CONVERT(varchar(10), pl.startDate, 104) + ')' as NAME
FROM 
	[ModulePriceList] mpl
	INNER JOIN PriceList pl ON pl.priceListID = mpl.priceListID
ORDER BY
	mpl.startDate asc
GO

-- ===== PackModulePricelistByDate =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — PackModulePricelistByDate и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER OFF;
GO
CREATE OR ALTER PROC [dbo].[PackModulePricelistByDate]
(
@massmediaID SMALLINT = NULL,
@theDate DATETIME,
@packModuleID SMALLINT = NULL,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
SELECT 
	mpl.[priceListID],
	m.massmediaID,
	mpl.[startDate],
	@tPricelistFrom + CONVERT(varchar(10), mpl.[startDate], 104) as name,
	mpl.[finishDate],
	@packModuleID as packModuleID,
	mpl.[price], 
	mpl.rollerID
FROM 
	[PackModulePriceList] mpl
	INNER JOIN [PackModuleContent] pmc ON mpl.pricelistID = pmc.pricelistID
	INNER JOIN [Module] m ON pmc.moduleID = m.moduleID 
		AND m.massmediaID = ISNULL(@massmediaID, m.massmediaID)
WHERE
	mpl.[packModuleID] = ISNULL(@packModuleID, mpl.[packModuleID]) AND
	mpl.[startDate] <= @theDate AND mpl.[finishDate] >= @theDate -- Не находит прайс лист когда редактируешь кампанию
GO

-- ===== PackModulePricelists =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — PackModulePricelists и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[PackModulePricelists]
(
@packModuleID smallint = null,
@pricelistID smallint = null,
@hidePLInThePast bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
as

SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
DECLARE @tTo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' до ')
SELECT DISTINCT
	pl.[pricelistID], 
	pl.[packModuleID],
	pl.[startDate],
	@tPricelistFrom + CONVERT(varchar(10), pl.[startDate], 104) + @tTo + CONVERT(varchar(10), pl.finishDate, 104) as name,
	pl.[finishDate],
	pl.[price],
	pl.[extraChargeFirstRoller],
	pl.[extraChargeSecondRoller],
	pl.[extraChargeLastRoller],
	pl.rollerID,
	mm.[roltypeID]
FROM 
	[PackModulePriceList] pl
	left JOIN [PackModuleContent] pmc ON pl.[priceListID] = pmc.[pricelistID]
	left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
	left JOIN [MassMedia] mm ON m.[massmediaID] = mm.[massmediaID]
WHERE
	pl.packModuleID = Coalesce(@packModuleID, pl.packModuleID) And
	pl.[pricelistID] = Coalesce(@pricelistID, pl.[pricelistID])
	And (@hidePLInThePast = 0 or pl.finishDate > GETDATE())
ORDER BY 
	pl.[startDate] DESC
GO

-- ===== PackageDiscountPriceLists =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — PackageDiscountPriceLists и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 01.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[PackageDiscountPriceLists]
(
	@packageDiscountPriceListId INT = NULL,
	@packageDiscountID INT = NULL,
	@hidePLInThePast bit = 0,
	@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
BEGIN
	SET NOCOUNT ON;
	DECLARE @tDiscountsFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Скидки от ');
	DECLARE @tTo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' до ');

    SELECT pdpl.*,
		@tDiscountsFrom + convert(varchar,pdpl.startDate,104) + case when pdpl.finishDate is null then space(0) else @tTo + convert(varchar,pdpl.finishDate,104) end as name
    FROM 
		[PackageDiscountPriceList] pdpl 
    WHERE 
		pdpl.[packageDiscountID] = ISNULL(@packageDiscountID, pdpl.[packageDiscountID])
		AND pdpl.[packageDiscountPriceListID] = ISNULL(@packageDiscountPriceListID, pdpl.[packageDiscountPriceListID])
		And (@hidePLInThePast = 0 or pdpl.finishDate > GETDATE())
	ORDER BY
		pdpl.startDate desc
END
GO

-- ===== PaymentCommonActions =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — PaymentCommonActions и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[PaymentCommonActions]
(
@paymentID int = null,
@managerID smallint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@agencyID smallint = null,
@paymentTypeID smallint = null,
@firmID smallint = null,
--@paymentTypesIDString varchar(1024) = null,
@agenciesIDString varchar(1024) = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
DECLARE @tActionPayment NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Оплата акции №');
CREATE TABLE #Agency(agencyID smallint)
CREATE Table #PaymentType (paymentTypeID smallint)

-- Populate temporary tables with Agency and Payment types
IF @agenciesIDString Is Null
	IF @agencyID IS NULL
		INSERT INTO #Agency SELECT agencyID FROM Agency
	ELSE
		INSERT INTO #Agency VALUES(@agencyID)
Else
	Exec dbo.hlp_PopulateTableFromCommaSeparatedString '#Agency', @agenciesIDString

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

select distinct
	psoa.*,
	@tActionPayment + LTrim(psoa.actionID) as name,
	f.name as firmName,
	a.name as agencyName,
	pt.name as paymentTypeName
FROM
	[PaymentAction] psoa
	INNER JOIN [Action] soa ON soa.actionID = psoa.actionID
	inner join [Campaign] c on soa.[actionID] = c.[actionID]
	INNER JOIN Payment pso ON pso.paymentID = psoa.paymentID
	INNER JOIN Firm f ON f.firmID = pso.firmID
	INNER JOIN Agency a ON a.agencyID = pso.agencyID
	INNER JOIN PaymentType pt ON pt.paymentTypeID = pso.paymentTypeID
	left join @massmedias umm on c.massmediaID = umm.massmediaID
	left join GroupMember gm on soa.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
where
	(soa.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
			(soa.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((soa.userID = @loggedUserID and umm.myMassmedia = 1) or (soa.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
														from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(soa.userID = @loggedUserID and umm.myMassmedia = 0) or
															 (soa.userID <> @loggedUserID and umm.foreignMassmedia = 0) )))) and	
	psoa.[paymentID] = Coalesce(@paymentID, psoa.[paymentID]) And
	pso.paymentDate BETWEEN Coalesce(@startOfInterval, pso.paymentDate)
		And Coalesce(dateadd(ss, -1, dateadd(day, 1, @endOfInterval)), pso.paymentDate)	And
	soa.userID = Coalesce(@managerID, soa.[userID]) And
	pso.agencyID IN (Select agencyID From #Agency) And
	soa.firmID = Coalesce(@firmID, soa.firmID) AND 
	(pt.isHidden = 0 or @isHideWhite = 0) And
	(pt.isHidden = 1 or @isHideBlack = 0) and
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1)) 
ORDER BY
	psoa.actionID desc
GO

-- ===== PricelistByDate =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — PricelistByDate и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER OFF;
GO
CREATE OR ALTER PROC [dbo].[PricelistByDate]
(
@massmediaID SMALLINT = null,
@theDate datetime,
@moduleID smallint = NULL,
@campaignID INT = null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
IF @campaignID IS NULL 
	SELECT 
		pl.*,
		@tPricelistFrom + CONVERT(varchar(10), pl.[startDate], 104) as name,
		@moduleID as moduleID
	FROM 
		[Pricelist] pl
	WHERE
		pl.pricelistID = dbo.fn_GetPricelistIDByDate(@massmediaID, @theDate, default)
ELSE
BEGIN
	DECLARE @campaignTypeID SMALLINT
	SELECT @campaignTypeID = campaignTypeID FROM [Campaign] WHERE [campaignID] = @campaignID
	
	IF @campaignTypeID = 4
		SELECT 
			pmpl.*, 
			@tPricelistFrom + CONVERT(varchar(10), pmpl.[startDate], 104) as name
		FROM [Campaign] c 
			INNER JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
			INNER JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
		WHERE
			@theDate BETWEEN pmpl.[startDate] AND pmpl.[finishDate]
END
GO

-- ===== Pricelists =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — Pricelists и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[Pricelists]
(
    @massmediaID smallint = null,
    @pricelistID smallint = null,
    @hidePLInThePast bit = 0,
    @languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ');

    ;WITH pl0 AS
    (
        SELECT *
        FROM dbo.Pricelist pl
        WHERE pl.massmediaID = COALESCE(@massmediaID, pl.massmediaID)
          AND pl.pricelistID = COALESCE(@pricelistID, pl.pricelistID)
          AND (@hidePLInThePast = 0 OR pl.finishDate >= CAST(GETDATE() AS DATE))
    ),
    tw AS
    (
        SELECT
            t.pricelistID,
            MIN(tw.windowDateOriginal) AS minDate,
            MAX(tw.windowDateOriginal) AS maxDate
        FROM dbo.TariffWindow tw
        INNER JOIN dbo.Tariff t ON t.tariffID = tw.tariffId
        WHERE EXISTS (SELECT 1 FROM pl0 WHERE pl0.pricelistID = t.pricelistID)
        GROUP BY t.pricelistID
    )
    SELECT
        pl.*,
        @tPricelistFrom + CONVERT(varchar(10), pl.startDate, 104)
        + N' (' + dbo.fn_GetTariffWindowDateRangeStr(tw.minDate, tw.maxDate, pl.broadcastStart, @languageCode) + N')' AS name,
        CONVERT(varchar(5), pl.broadcastStart, 114) AS broadcastStartString
    FROM pl0 pl
    LEFT JOIN tw ON tw.pricelistID = pl.pricelistID
    ORDER BY pl.startDate DESC;
END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[Pricelists] TO PUBLIC
    AS [dbo];
GO

-- ===== SpecialActions =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — SpecialActions и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 17.09.2008
-- Description:	List Special Actions
-- Modification: Denis Gladkikh (dgladkikh@fogsoft.ru) 18.09.2008 - Need agency with payment Type
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[SpecialActions] 
(
	@actionID int = null,
	@startDate datetime = null, 
	@endDate datetime = null,
	@firmID int = null,
	@userID smallint = null,
	@paymentTypeID tinyint = null,
	@agencyID smallint = null,
	@loggedUserID smallint,
	@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
as 
begin 
	set nocount on;
	DECLARE @tRemainder NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Остаток № ');

declare 
	@isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select 
	@isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

    select distinct
		a.actionID, 
		f.[name] as firm, 
		u.userName as manager,
		a.startDate as date,
		a.totalPrice as price,
		a.userID,
		a.firmID,
		(@tRemainder + cast(a.actionID as varchar)) as [name],
		c.agencyID,
		c.paymentTypeID,
		ag.name as agency,
		pt.[name] as paymenttype
    from [Action] a 
		inner join Campaign c on a.actionID = c.actionID
		inner join Firm f on a.firmID = f.firmID
		inner join [User] u on a.userID = u.userID
		inner join Agency ag on c.agencyID = ag.agencyID
		inner join PaymentType pt on c.paymentTypeID = pt.paymentTypeID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where a.isSpecial = 1 
		and (a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
		and a.actionID = coalesce(@actionID, a.actionID)
		and ((@startDate is null or a.startDate >= @startDate)
		and (@endDate is null or a.startDate <= @endDate)
		and a.firmID = coalesce(@firmID, a.firmID)
		and a.userID = coalesce(@userID, a.userID)
		and c.agencyID = coalesce(@agencyID, c.agencyID)
		and c.paymentTypeID = coalesce(@paymentTypeID, c.paymentTypeID))
	order by 1
end
GO

-- ===== SponsorPricelistByDate =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — SponsorPricelistByDate и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER OFF;
GO
/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE OR ALTER PROC [dbo].[SponsorPricelistByDate]
(
@sponsorProgramID smallint,
@theDate datetime,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
IF EXISTS (
	SELECT * FROM [SponsorProgramPricelist] pl
	WHERE	sponsorProgramID = @sponsorProgramID AND	@theDate between pl.[startDate] AND pl.finishDate
	)
	SELECT 
		pl.[pricelistID], 
		pl.[sponsorProgramID],
		pl.[startDate],
		@tPricelistFrom + CONVERT(varchar(10), pl.[startDate], 104) as name,
		pl.[finishDate],
		pl.bonus,
		pl.broadcastStart
	FROM 
		[SponsorProgramPricelist] pl
	WHERE
		pl.sponsorProgramID = @sponsorProgramID AND
		@theDate between pl.[startDate] AND pl.finishDate
ELSE
	SELECT TOP 1
		pl.[pricelistID], 
		pl.[sponsorProgramID],
		pl.[startDate],
		@tPricelistFrom + CONVERT(varchar(10), pl.[startDate], 104) as name,
		pl.[finishDate],
		pl.bonus,
		pl.broadcastStart
	FROM 
		[SponsorProgramPricelist] pl
	WHERE
		pl.sponsorProgramID = @sponsorProgramID AND
		@theDate < pl.[startDate] 
	ORDER BY
		pl.[startDate]
GO

-- ===== SponsorPricelists =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — SponsorPricelists и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
/*
Modified by: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008
*/
CREATE OR ALTER PROC [dbo].[SponsorPricelists]
(
@sponsorProgramID smallint = NULL,
@pricelistID smallint = NULL,
@hideSponsorPLInThePast bit = 0,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
SET NOCOUNT ON
DECLARE @tPricelistFrom NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Прайс-лист от ')
DECLARE @tTo NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' до ')
SELECT 
	spp.*,
	dbo.fn_Int2Time(spp.bonus) as bonusString,
	@tPricelistFrom + Convert(varchar(8), startDate, 4) + @tTo + Convert(varchar(8), finishDate, 4)  as name
FROM 
	[SponsorProgramPricelist] spp
WHERE
	spp.sponsorProgramID = COALESCE(@sponsorProgramID, spp.sponsorProgramID) AND
	spp.pricelistID = COALESCE(@pricelistID, spp.pricelistID) 
	And (@hideSponsorPLInThePast = 0  Or spp.finishDate > GETDATE())
ORDER BY
	spp.finishDate DESC
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[SponsorPricelists] TO PUBLIC
    AS [dbo];
GO

-- ===== Stat_AvgDiscount =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — Stat_AvgDiscount и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 21.11.2008
-- Description:	Информация о скидках
-- =============================================
CREATE OR ALTER procedure [dbo].[Stat_AvgDiscount] 
(
@StartDay DATETIME = default,
@FinishDay DATETIME = default,
@FirmID int = default, 
@HeadCompanyID int = default, 
@MassmediaID int = default, 
@PaymentTypeID int = default,
@CampaignTypeID int = default,
@ManagerID int = default,
@AgencyID int = default,
@AdvertTypeID int = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByMassmediaGroupType bit = 0,
@IsGroupByActionID bit = 0,
@massmediaGroupID int = NULL,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@Currency int = 1,
@loggedUserID smallint,
@actionID int = NULL,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;
	DECLARE @tAction NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Акция №');

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit,
			@isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	If	@StartDay Is Null Or @FinishDay Is Null
		Begin
		Raiserror('FilterStartFinishDays', 16, 1)
		Return
		End

	CREATE TABLE #res
	(
		campaignTariffPrice decimal(18,2),
		campaignPrice decimal(18,2),
		campaignVolumeDiscount decimal(9,4),
		campaignManagerDiscount decimal(9,4),
		campaignPackDiscount decimal(9,4),
		MassmediaID SMALLINT,
		PaymentTypeID SMALLINT,
		ActionID INT,
		campaignTypeID SMALLINT,
		Manager_ID SMALLINT,
		AgencyID SMALLINT,
		AdvertType_ID smallint,
		massmediaGroupID int
	)

	Set	@StartDay = dbo.ToShortDate(@StartDay)
	Set	@FinishDay = dbo.ToShortDate(@FinishDay)

	Declare cur_companies Cursor Local fast_forward
	For
	select  distinct  c.campaignID, c.ActionID, c.massmediaID, 
			c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
			c.[startDate], mm.massmediaGroupID, a.discount, c.finalPrice, c.finishDate, c.managerDiscount, c.discount, c.tariffPrice
	From	
		Campaign c
		INNER Join [Action] a On c.ActionID = a.actionID AND a.[isConfirmed] = 1
		inner join Firm f on f.firmID = a.firmID
		inner join 
		(
			select distinct am.agencyID, max(cast(mm.foreignMassmedia as tinyint)) as foreignMassmedia from AgencyMassmedia am 
				inner join @massmedias mm on am.massmediaID = mm.massmediaID
			group by am.agencyID
		) xx on c.agencyID = xx.agencyID and (a.isSpecial = 0 or xx.foreignMassmedia = 1) 
		INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
		left join MassMedia mm on c.massmediaID = mm.massmediaID
		
		left JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
		left JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
		left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
		
		inner join @massmedias mmu on (mm.massmediaID = mmu.massmediaID 
							or m.massmediaID = mmu.massmediaID)
		inner join MassMedia mmfu on mmu.massmediaID = mmfu.massmediaID 
		inner join 
				(
					select distinct u.userID 
					from [User] u
						left join [GroupMember] gm on u.userID = gm.userID
						left join @ugroups ug on gm.groupID = ug.id
					where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
				) as x on a.userID = x.userID
	Where	c.StartDate <= @FinishDay and
			c.FinishDate >= @StartDay and
			c.AgencyID = IsNull(@AgencyID, c.AgencyID) and
			a.firmID = IsNull(@FirmID, a.firmID) and
			f.headCompanyID = IsNull(@headCompanyID, f.headCompanyID) and
			a.userID = IsNull(@ManagerID, a.userID) and
			c.PaymentTypeID = IsNull(@PaymentTypeID, c.PaymentTypeID) and
			c.campaignTypeID = IsNull(@CampaignTypeID, c.campaignTypeID) and
			(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
			(@ShowBlack <> 0 or PaymentType.isHidden = 0)  
			and (@MassmediaID is null or mmfu.massmediaID = @MassmediaID)
			and (@massmediaGroupID is null or mmfu.massmediaGroupId = @massmediaGroupID)
			and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
			and (@actionID is null or a.actionID = @actionID)

	Declare	@campaignID int, 
		@campaignPrice decimal(18,2),
		@SummaVar decimal(18,2),
		@CompStartDate datetime,
		@actionDiscount decimal(9,4),
		@sumPrice decimal(18,2),
		@finalPrice decimal(18,2),
		@cfinishDate datetime,
		@managerDiscount decimal(9,4),
		@volumeDiscount decimal(9,4),
		@campaignTariffPrice decimal(18,2)

	Open	cur_companies
	Fetch	next from cur_companies into 
		@campaignID, @ActionID, @MassmediaID, @PaymenttypeID, 
		@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @massmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate,@managerDiscount,@volumeDiscount,@campaignTariffPrice

	While	@@fetch_status = 0
	begin
		if @FinishDay < @cfinishDate or @StartDay > @CompStartDate
			exec GetPriceByPeriod @campaignId, @CampaignTypeID, @StartDay, @FinishDay, @campaignPrice out, null,@campaignTariffPrice out
		else 
			set @campaignPrice = @finalPrice

		IF @CampaignTypeID = 4
		begin
			declare @tmp table (massmediaID smallint, price decimal(18,2), tariffPrice decimal(18,2))
					
			insert into @tmp(massmediaID, price)
			select
				m.[massmediaID], sum(mpl.[price])
			from [PackModuleIssue] i 
				INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
				INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
				INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
			where 
				i.campaignID = @campaignID	and
				i.issueDate between @StartDay and @FinishDay 
			group by m.massmediaID
				
			select @sumPrice = sum(t1.price) FROM @tmp AS t1
			
			insert into #res (campaignVolumeDiscount,campaignManagerDiscount,campaignPackDiscount,[campaignPrice],campaignTariffPrice, [MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],	[AgencyID],[AdvertType_ID],	massmediaGroupID) 
			select 1,@managerDiscount, 1,@campaignPrice * sum(t1.price)/ @sumPrice, @campaignTariffPrice * sum(t1.price)/ @sumPrice, t1.massmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID,@ManagerID, @AgencyID, 0, mm.massmediaGroupID
			from @tmp as t1
				inner join MassMedia mm on t1.massmediaID = mm.massmediaID
				inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
			where t1.price > 0  
				and (@MassmediaID is null or mm.massmediaID = @MassmediaID)
				and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID)
				and ((@ManagerID = @loggedUserID and mmu.myMassmedia = 1) or (@ManagerID <> @loggedUserID and mmu.foreignMassmedia = 1))
			group by t1.massmediaID, mm.massmediaGroupID
		END
		ELSE
		begin
			if	@campaignPrice > 0 
				Insert	Into #res (campaignVolumeDiscount,campaignManagerDiscount,campaignPackDiscount,[campaignPrice],campaignTariffPrice, [MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],[AgencyID],[AdvertType_ID], massmediaGroupID) 
				Values( @volumeDiscount,@managerDiscount, @actionDiscount,@campaignPrice, @campaignTariffPrice, @MassmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID, @ManagerID, @AgencyID, 0, @massmediaGroupID)
		end
			
		fetch next from cur_companies into 
				@campaignID, @ActionID, @MassmediaID, @PaymenttypeID,
				@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @massmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate,@managerDiscount,@volumeDiscount,@campaignTariffPrice
	End	

	close cur_companies
	deallocate cur_companies

	-- output ---------------------------------------------------------
	Declare	@SQLString NVARCHAR(2500),
					@IsStarted int

	/* Build the SQL string once.*/
	Set	@SQLString = N'Select	row_number() over(order by coalesce(sum(r.campaignTariffPrice), 0)) as RowNum,'

	If	@IsGroupByPaymentType <> 0
		Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
	If	@IsGroupByCampaignType <> 0
		Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
	If	@IsGroupByMassmedia <> 0
		Set 	@SQLString = @SQLString + N'vMassMedia.Name as "massmedia", vMassMedia.groupName as "massmedia_group",'
	If	@IsGroupByMassmediaGroupType <> 0
		Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
	If	@IsGroupByFirm <> 0
		Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
	If	@IsGroupByManager <> 0
		Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as "manager",'
	If	@IsGroupByAgency <> 0
		Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
	If	@IsGroupByActionID <> 0
		Set 	@SQLString = @SQLString + N'N''' + REPLACE(@tAction, N'''', N'''''') + N''' + cast(r.actionID as varchar) as "actionID",'

	Set	@SQLString = @SQLString + N' coalesce(sum(r.campaignTariffPrice), 0) as tariffPrice
		, coalesce(avg(r.campaignVolumeDiscount), 0) as volumeDiscount
		, cast(coalesce(sum(r.campaignTariffPrice - r.campaignTariffPrice * r.campaignVolumeDiscount), 0) as decimal(18,2)) as volumeDicountPrice
		, coalesce(avg(r.campaignPackDiscount),0) as packDiscount
		, cast(coalesce(sum(r.campaignTariffPrice * r.campaignVolumeDiscount * (1 - r.campaignPackDiscount)) , 0) as decimal(18,2)) as discountPrice
		, coalesce(avg(r.campaignManagerDiscount), 0) as managerDiscount
		, cast(coalesce(sum(r.campaignTariffPrice * r.campaignVolumeDiscount * r.campaignPackDiscount * (1 - r.campaignManagerDiscount)), 0) as decimal(18,2)) as managerDicountPrice
		, coalesce(sum(r.campaignPrice), 0) as price	
	from #res as r '

	If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on r.massmediaGroupID = MassmediaGroup.massmediaGroupID '
	If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on r.PaymentTypeID = Paymenttype.PaymenttypeID'
	If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on r.campaignTypeID = iCampaignType.CampaignTypeID'
	If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on r.massmediaID = vMassMedia.massmediaID'
	If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Action on r.ActionID = Action.ActionID inner join Firm on Action.firmID = Firm.FirmID '
	If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on r.Manager_ID = [User].UserID'
	If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on r.AgencyID = Agency.AgencyID'

	If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
		@IsGroupByMassmedia + @IsGroupByFirm + 
		@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType + @IsGroupByActionID <> 0
		begin

		-- Group By part
		set	@IsStarted = 0
		Set 	@SQLString = @SQLString + N' Group by '

		if	@IsGroupByPaymentType <> 0 begin
			if	@IsStarted = 1 set @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'Paymenttype.Name'
			set	@IsStarted = 1
		end

		If	@IsGroupByCampaignType <> 0
			begin
			if	@IsStarted = 1 set @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'iCampaignType.Name'
			set	@IsStarted = 1
			end

		If	@IsGroupByMassmedia <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'vMassMedia.Name, vMassMedia.groupName'
			set	@IsStarted = 1
			end

		If	@IsGroupByFirm <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'Firm.Name'
			set	@IsStarted = 1
			end

		If	@IsGroupByManager <> 0
			begin

			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''')'
			set	@IsStarted = 1
			end

		If	@IsGroupByAgency <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set 	@SQLString = @SQLString + N'Agency.Name'
			set	@IsStarted = 1
			end
				
		If	@IsGroupByMassmediaGroupType <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set @SQLString = @SQLString + N'MassmediaGroup.Name'
			set	@IsStarted = 1
			end
		
		If	@IsGroupByActionID <> 0
			begin
			if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
			Set @SQLString = @SQLString + N'r.actionID'
			set	@IsStarted = 1
			end

		end

	EXECUTE sp_executesql @SQLString

	Drop table #res
end
GO

-- ===== SysParams =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — SysParams и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
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
		@tSysParams as [name]

end
GO

-- ===== TariffWindowRetrieve =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — TariffWindowRetrieve и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[TariffWindowRetrieve]
(
    @pricelistId int = null,
    @broadcastStart datetime = null,
    @startDate datetime = null,
    @finishDate datetime = null,
    @moduleId int = null,
    @windowId int = null,
    @actualDate datetime = NULL,
    @windowDateActual DATETIME = NULL,
    @windowDateOriginal DATETIME = NULL,
    @excludeSpecialWindows BIT = 0,
    @excludeModuleTariffs BIT = 0,
    @massmediaID INT = NULL,
    @showTrafficWindows BIT = 0,
    @showDisabledWindows bit = 1,
    @useActualTime BIT = 0,
    @languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
AS
BEGIN
    SET NOCOUNT ON;
    DECLARE @tAdWindow NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Рекламное окно ');
    -- Предотвращаем дедлоки при чтении
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;

    DECLARE @broadcasrStartHour tinyint;
    IF @broadcastStart IS NOT NULL 
        SET @broadcasrStartHour = DATEPART(hh, @broadcastStart);
    ELSE
        SET @broadcasrStartHour = 0;

    -- Используем временную таблицу вместо @tmpWindow для корректной статистики
    CREATE TABLE #tmpWindow (windowId int PRIMARY KEY);

    -------------------------------------------------------------------------
    -- 1. Наполнение списка ID окон
    -------------------------------------------------------------------------
    
    -- Точечный поиск по датам
    IF @windowDateActual IS NOT NULL AND @windowDateOriginal IS NOT NULL AND @massmediaID IS NOT NULL 
    BEGIN
        INSERT INTO #tmpWindow (windowId)
        SELECT windowID 
        FROM [TariffWindow]
        WHERE [windowDateActual] = @windowDateActual 
          AND [windowDateOriginal] = @windowDateOriginal
          AND massmediaID = @massmediaID;
    END
    -- Поиск по конкретному ID
    ELSE IF @windowId IS NOT NULL
    BEGIN
        INSERT INTO #tmpWindow (windowId) VALUES (@windowId);
    END
    -- Поиск по вхождению времени в длительность окна
    Else If @actualDate Is Not Null
    BEGIN
        INSERT INTO #tmpWindow (windowId)
        SELECT TOP 1 windowId
        FROM TariffWindow
        WHERE massmediaID = @massmediaID 
          AND windowDateActual <= @actualDate -- Это SARGable условие (быстрый поиск по индексу)
          AND DATEADD(s, duration, windowDateActual) >= @actualDate -- Проверка только для одной строки
        ORDER BY windowDateActual DESC; -- Берем самое близкое к моменту
    END
    -- Основной поиск: Модуль НЕ указан
    ELSE IF @moduleId IS NULL
    BEGIN
        INSERT INTO #tmpWindow (windowId)
        SELECT tw.windowId
        FROM TariffWindow tw
        INNER JOIN Tariff t ON t.tariffId = tw.tariffId
        WHERE (@excludeModuleTariffs = 0 OR t.[isForModuleOnly] = 0)
          AND (@pricelistId IS NULL OR t.pricelistId = @pricelistId)
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)
          AND (tw.isDisabled = 0 OR @showDisabledWindows = 1)

        UNION ALL

        SELECT DISTINCT tw.windowId
        FROM TariffWindow tw
        INNER JOIN [Pricelist] pl ON pl.[massmediaID] = tw.massmediaID 
            AND tw.dayOriginal BETWEEN pl.startDate AND pl.finishDate
        WHERE tw.tariffID IS NULL 
          AND @showTrafficWindows = 1 
          AND @excludeSpecialWindows = 0 
          AND (@pricelistId IS NULL OR pl.pricelistId = @pricelistId)
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)
          AND (tw.isDisabled = 0 OR @showDisabledWindows = 1);
    END
    -- Основной поиск: Модуль указан
    ELSE
    BEGIN
        INSERT INTO #tmpWindow (windowId)
        SELECT tw.windowId
        FROM TariffWindow tw
        INNER JOIN ModuleTariff mt ON mt.tariffId = tw.tariffId
        INNER JOIN ModulePriceList mpl ON mpl.modulePriceListID = mt.modulePriceListID
        WHERE mpl.moduleId = @moduleId
          AND mpl.pricelistId = @pricelistId
          AND mpl.startDate <= @finishDate AND mpl.finishDate >= @startDate
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate)

        UNION ALL

        SELECT DISTINCT tw.windowId
        FROM TariffWindow tw
        INNER JOIN [Pricelist] pl ON pl.[massmediaID] = tw.massmediaID
            AND tw.dayOriginal BETWEEN pl.startDate AND pl.finishDate
        INNER JOIN [Module] m ON tw.massmediaID = m.[massmediaID] AND m.moduleId = @moduleId
        INNER JOIN ModulePriceList mpl ON mpl.priceListID = pl.priceListID
        WHERE tw.tariffID IS NULL 
          AND @showTrafficWindows = 1  
          AND @excludeSpecialWindows = 0 
          AND (@pricelistId IS NULL OR pl.pricelistId = @pricelistId)
          AND mpl.startDate <= @finishDate AND mpl.finishDate >= @startDate
          AND (@startDate IS NULL OR tw.dayOriginal >= @startDate)
          AND (@finishDate IS NULL OR tw.dayOriginal <= @finishDate);
    END

    -------------------------------------------------------------------------
    -- 2. Формирование финальных наборов данных
    -------------------------------------------------------------------------

    IF @excludeSpecialWindows = 0    
    BEGIN
        -- Формируем временный набор (без бесполезного ORDER BY внутри SELECT INTO)
        SELECT
            tw.*,
            @tAdWindow + CONVERT(varchar(10), windowDateOriginal, 104) + ' ' + 
                CONVERT(varchar(5), windowDateOriginal, 108)
                + CASE WHEN windowDateOriginal != windowDateActual 
                       THEN ' (' + CONVERT(varchar(10), windowDateOriginal, 104) + ' ' + CONVERT(varchar(5), windowDateActual, 108) + ')' 
                       ELSE '' END AS [name],
            DATEPART(hh, CASE WHEN @useActualTime = 1 THEN windowDateActual ELSE windowDateOriginal END) AS [hour],
            DATEPART(mi, CASE WHEN @useActualTime = 1 THEN windowDateActual ELSE windowDateOriginal END) AS [min],
            dayOriginal AS windowDateBroadcast,
            dayActual  AS windowDateActualBroadcast
        INTO #final1 
        FROM #tmpWindow ttw
        INNER JOIN TariffWindow tw ON ttw.windowId = tw.windowId;

        -- Первый результат (сетка часов)
        SELECT DISTINCT    
            [hour],
            [min],
            price,
            CASE WHEN [hour] >= @broadcasrStartHour THEN 0 ELSE 1 END AS flag
        FROM #final1 
        ORDER BY flag, [hour], [min];

        -- Второй результат (список окон)
        SELECT f.*, 
            CASE WHEN tu.tariffID IS NULL THEN 0 ELSE 1 END AS IsTariffUnited 
        FROM #final1 f
        LEFT JOIN TariffUnion tu ON (f.tariffId = tu.tariffID OR f.tariffId = tu.tariffUnionID)
        ORDER BY f.windowDateOriginal DESC;
        
        DROP TABLE #final1;
    END
    ELSE    
    BEGIN
        SELECT
            tw.*,
            @tAdWindow + CONVERT(varchar(10), windowDateOriginal, 104) + ' ' + 
                CONVERT(varchar(5), windowDateOriginal, 108) AS [name],
            DATEPART(hh, CASE WHEN @useActualTime = 1 THEN windowDateActual ELSE windowDateOriginal END) AS [hour],
            DATEPART(mi, CASE WHEN @useActualTime = 1 THEN windowDateActual ELSE windowDateOriginal END) AS [min],
            dayOriginal AS windowDateBroadcast,
            dayActual AS windowDateActualBroadcast
        INTO #final2           
        FROM #tmpWindow ttw
        INNER JOIN TariffWindow tw ON ttw.windowId = tw.windowId;
        
        -- Первый результат
        SELECT DISTINCT    
            [hour],
            [min],
            price,
            CASE WHEN [hour] >= @broadcasrStartHour THEN 0 ELSE 1 END AS flag
        FROM #final2 
        ORDER BY flag, [hour], [min];
    
        -- Второй результат
        SELECT * FROM #final2 ORDER BY windowDateOriginal DESC;

        DROP TABLE #final2;
    END

    DROP TABLE #tmpWindow;
END
GO

-- ===== sl_LookupMassmediaGroupd =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — sl_LookupMassmediaGroupd и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[sl_LookupMassmediaGroupd] (@languageCode VARCHAR(10) = 'ru') -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
as
SET NOCOUNT ON
DECLARE @tShowAll NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Показать все');
select 0 as id, @tShowAll as [name]
union
SELECT [massmediaGroupID] as id, name FROM [dbo].[MassmediaGroup]
GO

-- ===== sl_PaymentsCommon =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — sl_PaymentsCommon и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[sl_PaymentsCommon] (@languageCode VARCHAR(10) = 'ru') -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
AS
SET NOCOUNT ON
DECLARE @tFirmPayment NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Платёж от фирмы ''');
SELECT
	p.*,
	@tFirmPayment + f.name + '''' as name,
	f.name AS firmName,
	hc.name as headCompanyName,
	a.name as agencyName,
	pt.name as paymentTypeName,
	u.LastName + Space(1) + u.firstName as userName,
	CASE 
		WHEN SUM(pa.summa) > 0 THEN	SUM(pa.summa)
		ELSE 0
	END AS consumed
	,p.summa - CASE 
		WHEN SUM(pa.summa) > 0 THEN	(SUM(pa.summa)) 
		ELSE 0
	END AS remainder
FROM
	#PaymentsCommon p2
	INNER JOIN [Payment] p ON p.paymentID = p2.paymentID
	INNER JOIN firm f ON f.firmID = p.firmID
	Inner Join HeadCompany hc on hc.headCompanyID = f.headCompanyID
	INNER JOIN agency a ON a.agencyID = p.agencyID
	INNER JOIN paymentType pt ON pt.paymentTypeID = p.paymentTypeID
	INNER JOIN [user] u ON u.userID = p.userID
	LEFT JOIN [PaymentAction] pa ON pa.paymentID = p.paymentID
GROUP BY 
	f.name, a.name, pt.name, u.LastName, u.firstName, p.[agencyID], p.[firmID], p.[isEnabled], 
	p.[paymentDate], p.[paymentID], p.[paymentTypeID], p.[userID], p.[summa], hc.name
ORDER BY
	p.paymentDate DESC
GO

-- ===== sponsorTariffList =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — sponsorTariffList и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER PROC [dbo].[sponsorTariffList]
(
@pricelistID smallint = Null,
@tariffID smallint = Null,
@time smalldatetime = Null,
@monday bit = Null,
@tuesday bit = Null,
@wednesday bit = Null,
@thursday bit = Null,
@friday bit = Null,
@saturday bit = Null,
@sunday bit = Null,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
as
SET NOCOUNT ON
DECLARE @tTariff NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Тариф ');
SELECT 
	st.*,
	Convert(varchar(5), [time], 108) as timeString,
	dbo.fn_Int2Time([duration]) as tariffDuration,
	@tTariff + 	Convert(varchar(5), [time], 108) as [name] 
FROM 
	[SponsorTariff] st
WHERE
	st.[pricelistID] = Coalesce(@pricelistID, st.pricelistID)
	And st.tariffID = Coalesce(@tariffID, st.tariffID)
	And st.time = Coalesce(@time, st.time)
	And st.monday = Coalesce(@monday, st.monday)
	And st.tuesday = Coalesce(@tuesday, st.tuesday)
	And st.wednesday = Coalesce(@wednesday, st.wednesday)
	And st.thursday = Coalesce(@thursday, st.thursday)
	And st.friday = Coalesce(@friday, st.friday)
	And st.saturday = Coalesce(@saturday, st.saturday)
	And st.sunday = Coalesce(@sunday, st.sunday)
ORDER BY
	st.[time]
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[sponsorTariffList] TO PUBLIC
    AS [dbo];
GO

-- ===== stat_VolumeOfRealization =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — stat_VolumeOfRealization и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER Procedure [dbo].[stat_VolumeOfRealization]
(
@StartDay DATETIME = default,
@FinishDay DATETIME = default,
@FirmID int = default, 
@MassmediaID int = default, 
@PaymentTypeID int = default,
@CampaignTypeID int = default,
@ManagerID int = default,
@AgencyID int = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByMassmediaGroupType bit = 0,
@massmediaGroupID int = NULL,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON
DECLARE @tAll NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Все');

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

If	@StartDay Is Null Or @FinishDay Is Null
	Begin
	Raiserror('FilterStartFinishDays', 16, 1)
	Return
	End

CREATE TABLE #tmp1
(
CompanyPrice decimal(18,2),
MassmediaID SMALLINT,
PaymentTypeID SMALLINT,
ActionID INT,
campaignTypeID SMALLINT,
Manager_ID SMALLINT,
AgencyID SMALLINT,
massmediaGroupID int
)

Set	@StartDay = dbo.ToShortDate(@StartDay)
Set	@FinishDay = dbo.ToShortDate(@FinishDay)

-- select all companies, which has appropriated 
-- start and finish dates
Declare cur_companies Cursor Local fast_forward
For
select distinct  c.campaignID, c.ActionID, c.massmediaID, 
		c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
		c.[startDate], max(coalesce(mm.massmediaGroupID,0)), a.discount, c.finalPrice, c.finishDate
From	
	Campaign c
	INNER Join [Action] a On c.ActionID = a.actionID
		AND a.[isConfirmed] = 1
	INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
	left join MassMedia mm on c.massmediaID = mm.massmediaID
	left JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
	left JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
	left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]
	inner join @massmedias mmu on (mm.massmediaID = mmu.massmediaID 
						or m.massmediaID = mmu.massmediaID)
	inner join MassMedia mmfu on mmu.massmediaID = mmfu.massmediaID 
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
Where	
	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
	c.StartDate <= @FinishDay and
	c.FinishDate >= @StartDay and
	c.AgencyID = IsNull(@AgencyID, c.AgencyID) and
	a.firmID = IsNull(@FirmID, a.firmID) and
	a.userID = IsNull(@ManagerID, a.userID)  and
	c.PaymentTypeID = IsNull(@PaymentTypeID, c.PaymentTypeID) and
	c.campaignTypeID = IsNull(@CampaignTypeID, c.campaignTypeID) and
	(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
	(@ShowBlack <> 0 or PaymentType.isHidden = 0)  
	and (@MassmediaID is null or mmfu.massmediaID = @MassmediaID)
	and (@massmediaGroupID is null or mmfu.massmediaGroupId = @massmediaGroupID)
	and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))

group by c.campaignID, c.ActionID, c.massmediaID, 
		c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
		c.[startDate], a.discount, c.finalPrice, c.finishDate

-- select all companies, which have Issues inside interval
Declare	@campaignID int, 
	@ActionID int, 
	@campaignPrice decimal(18,2),
	@SummaVar decimal(18,2),
	@CompStartDate datetime,
	@actionDiscount float,
	@sumPrice decimal(18,2),
	@finalPrice decimal(18,2),
	@cfinishDate datetime,
	@campMassmediaGroupID int,
	@mmID smallint
	
declare @tmp table (massmediaID smallint, price decimal(18,2))

Open	cur_companies
Fetch	next from cur_companies into 
	@campaignID, @ActionID, @mmID, @PaymenttypeID, 
	@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate

--Set	@FinishDay = Convert(datetime, Convert(varchar, @FinishDay, 112), 112) - 1
While	@@fetch_status = 0
begin
	if @FinishDay < @cfinishDate or @StartDay > @CompStartDate
		exec GetPriceByPeriod @campaignId, @CampaignTypeID, @StartDay, @FinishDay, @campaignPrice out
	else 
		set @campaignPrice = @finalPrice

	IF @CampaignTypeID = 4
	begin
		delete from @tmp
				
		insert into @tmp(massmediaID, price)
		select
			m.[massmediaID], sum(mpl.[price])
		from [PackModuleIssue] i 
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		where 
			i.campaignID = @campaignID	and
			i.issueDate between @StartDay and @FinishDay 
		group by m.massmediaID
			
		select @sumPrice = sum(t1.price) FROM @tmp AS t1
		
		insert into #tmp1 ([CompanyPrice],	[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],	[AgencyID],massmediaGroupID) 
		select @campaignPrice * sum(t1.price)/ @sumPrice,  t1.massmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID,@ManagerID, @AgencyID, mm.massmediaGroupID
		from @tmp as t1
			inner join MassMedia mm on t1.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
		where t1.price > 0  
			and (@MassmediaID is null or mm.massmediaID = @MassmediaID)
			and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID)
		group by t1.massmediaID, mm.massmediaGroupID
	END
	ELSE
	begin
		if	@campaignPrice > 0 
			Insert	Into #tmp1 ([CompanyPrice],[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],[AgencyID],massmediaGroupID) 
			Values(@campaignPrice,  @mmID, @PaymenttypeID, @ActionID, @CampaignTypeID, @ManagerID, @AgencyID, @campMassmediaGroupID)
	end
		
	fetch next from cur_companies into 
			@campaignID, @ActionID, @mmID, @PaymenttypeID,
			@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, @finalPrice, @cfinishDate
End	

close cur_companies
deallocate cur_companies

Select	@SummaVar = IsNull(sum(CompanyPrice), 0) From	#tmp1

-- output ---------------------------------------------------------
Declare	@SQLString NVARCHAR(2500),
				@IsStarted int

/* Build the SQL string once.*/
Set	@SQLString = N'Select	row_number() over(order by IsNull(Sum(CompanyPrice), 0)) as RowNum,'
Set	@SQLString = @SQLString + N' IsNull(Sum(CompanyPrice), 0) as  sum1'

Set @SQLString = @SQLString + N',  '

/*
If	@IsGroupByCommissionaire <> 0
	Set 	@SQLString = @SQLString + N'Dic_Commissionaire.Description as "Комиссионер",'
*/
If	@IsGroupByPaymentType <> 0
	Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
If	@IsGroupByCampaignType <> 0
	Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
If	@IsGroupByMassmedia <> 0
	Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup as "massmedia", vMassMedia.massmediaID,'
If	@IsGroupByMassmediaGroupType <> 0
	Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
If	@IsGroupByFirm <> 0
	Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
If	@IsGroupByManager <> 0
	Set 	@SQLString = @SQLString + N'[User].userName as "manager",'
If	@IsGroupByAgency <> 0
	Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + 
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType  = 0
	set		@SQLString = @SQLString + N'max(N''' + REPLACE(@tAll, N'''', N'''''') + N''') as "all",'

Set 	@SQLString = @SQLString + 
		N'case @Summa
			when	0 then 0
			else	Cast((IsNull(Sum(CompanyPrice), 0) * 100.0 / @Summa) as decimal(12,2))
		End as "percent"	
From	#tmp1'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on #tmp1.massmediaGroupID = MassmediaGroup.massmediaGroupID '
If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on #tmp1.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on #tmp1.campaignTypeID = iCampaignType.CampaignTypeID'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on #tmp1.massmediaID = vMassMedia.massmediaID'
If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Action on #tmp1.ActionID = Action.ActionID inner join Firm on Action.firmID = Firm.FirmID '
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on #tmp1.Manager_ID = [User].UserID'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on #tmp1.AgencyID = Agency.AgencyID'

Set 	@SQLString = @SQLString + N' Where CompanyPrice <> 0 '

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + 
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType <> 0
	begin

	-- Group By part
	set	@IsStarted = 0
	Set 	@SQLString = @SQLString + N' Group by '

/*
	if	@IsGroupByCommissionaire <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Dic_Commissionaire.Description'
		set	@IsStarted = 1
	end
*/
	if	@IsGroupByPaymentType <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Paymenttype.Name'
		set	@IsStarted = 1
	end

	If	@IsGroupByCampaignType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'iCampaignType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmedia <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup, vMassMedia.massmediaID'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin

		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'[User].userName'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmediaGroupType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set @SQLString = @SQLString + N'MassmediaGroup.Name'
		set	@IsStarted = 1
		end

	end

EXECUTE sp_executesql @SQLString,
	N'@Summa decimal(18,2)',
	@Summa = @SummaVar		

Drop		table #tmp1
GO

-- ===== stat_VolumeOfRealization2 =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — stat_VolumeOfRealization2 и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER Procedure [dbo].[stat_VolumeOfRealization2]
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
@IsGroupByHeadCompany bit = 0,  -- НОВЫЙ ПАРАМЕТР
@IsGroupByMassmediaGroupType bit = 0,
@IsGroupByAdvertType bit = 0,
@IsGroupByAdvertTypeTop bit = 0,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON
DECLARE @tAll NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Все');

--IF @IsGroupByHeadCompany = 1 SET @IsGroupByFirm = 0

IF @StartDay IS NULL OR @FinishDay IS NULL
BEGIN
	RAISERROR('FilterStartFinishDays', 16, 1)
	RETURN
END

SET	@StartDay = dbo.ToShortDate(@StartDay)
SET	@FinishDay = dbo.ToShortDate(@FinishDay)


-- output ---------------------------------------------------------
DECLARE	@SQLString NVARCHAR(MAX), @IsStarted int

/* Build the SQL string once.*/
SET @SQLString = N'
DECLARE @Summa money
SET @Summa=0

DECLARE @Campaign TABLE (
			campaignID int, 
			advertTypeID smallint,
			actionID int, 
			massmediaID smallint, 
			paymentTypeID smallint, 
			campaignTypeID tinyint, 
			agencyID smallint,
			startDate datetime,
			finishDate datetime,
			finalPrice money,
			userID smallint,
			firmID smallint,
			discount float,
			massmediaGroupID int,
			price money,
			INDEX i1 UNIQUE CLUSTERED (campaignID, massmediaID, advertTypeID)
			)

INSERT @Campaign
SELECT d.* 
FROM fn_statGetPrice(@startDate, @finishDate, @loggedUserID) d Inner Join Firm f On f.firmId = d.FirmId'
If	@ShowWhite = 0 OR @ShowBlack = 0 Set @SQLString = @SQLString + N' inner join Paymenttype on d.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@advertTypeID IS NOT NULL Set @SQLString = @SQLString + N' left join AdvertType at on at.advertTypeID = d.advertTypeID'

Set @SQLString = @SQLString +
'
WHERE 1=1
'
If	@ShowWhite = 0 AND @ShowBlack <> 0 Set @SQLString = @SQLString + N' AND PaymentType.isHidden <> 0'
If	@ShowBlack = 0 AND @ShowWhite <> 0 Set @SQLString = @SQLString + N' AND PaymentType.isHidden = 0'
IF	@FirmID IS NOT NULL Set @SQLString = @SQLString + N' AND d.firmID = @FirmID'
IF	@headCompanyID IS NOT NULL Set @SQLString = @SQLString + N' AND f.headCompanyID = @headCompanyID'
IF	@MassmediaID IS NOT NULL Set @SQLString = @SQLString + N' AND d.massmediaID = @MassmediaID'
IF	@PaymentTypeID IS NOT NULL Set @SQLString = @SQLString + N' AND d.paymentTypeID = @PaymentTypeID'
IF	@CampaignTypeID IS NOT NULL Set @SQLString = @SQLString + N' AND d.campaignTypeID = @CampaignTypeID'
IF	@ManagerID IS NOT NULL Set @SQLString = @SQLString + N' AND d.userID = @ManagerID'
IF	@AgencyID IS NOT NULL Set @SQLString = @SQLString + N' AND d.agencyID = @AgencyID'
IF	@massmediaGroupID IS NOT NULL Set @SQLString = @SQLString + N' AND d.massmediaGroupID = @massmediaGroupID'
IF	@advertTypeID IS NOT NULL Set @SQLString = @SQLString + N' AND @advertTypeID IN(at.parentID,d.advertTypeID)'

Set @SQLString = @SQLString +
'
SELECT	@Summa = ISNULL(sum(price), 0) FROM @Campaign
'
If @IsGroupByFirm = 1 And @IsGroupByHeadCompany = 1 and 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia
	  + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType
	  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
	Begin
Set @SQLString = @SQLString +
'
	DECLARE @res TABLE (
		RowNum int, 
		sum1 money,
		sum4 money,
		firm varchar(256), 
		head_company varchar(256), 
		hc2 varchar(256), 
		[percent] decimal(12,2),
		row_style varchar(20),
		INDEX i1 UNIQUE CLUSTERED (RowNum)
	)

	Insert Into @res(RowNum, sum1, firm, head_company, [percent])
	'
	End
Set	@SQLString = @SQLString + N'Select	row_number() over(order by IsNull(Sum(price), 0)) as RowNum,'
Set	@SQLString = @SQLString + N' IsNull(Sum(price), 0) as  sum1'
Set @SQLString = @SQLString + N',  '

If	@IsGroupByPaymentType <> 0
	Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
If	@IsGroupByCampaignType <> 0
	Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
If	@IsGroupByMassmedia <> 0
	Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup as "massmedia", vMassMedia.massmediaID,'
If	@IsGroupByMassmediaGroupType <> 0
	Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
If	@IsGroupByFirm <> 0
	Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
If	@IsGroupByHeadCompany <> 0  -- НОВАЯ ГРУППИРОВКА
	Set 	@SQLString = @SQLString + N'HeadCompany.Name as "head_company",'
If	@IsGroupByManager <> 0
	Set 	@SQLString = @SQLString + N'[User].userName as "manager",'
If	@IsGroupByAgency <> 0
	Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
If	@IsGroupByAdvertType <> 0
	Set 	@SQLString = @SQLString + N'AdvertType.Name as "adverttype",'
If	@IsGroupByAdvertTypeTop <> 0
	Set 	@SQLString = @SQLString + N'at.Name as "topAdverttype",'

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia + @IsGroupByFirm
	  + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType + @IsGroupByHeadCompany
	  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
	set		@SQLString = @SQLString + N'max(N''' + REPLACE(@tAll, N'''', N'''''') + N''') as "all",'

Set 	@SQLString = @SQLString + 
		N'case @Summa
			when	0 then 0
			else	Cast((IsNull(Sum(price), 0) * 100.0 / @Summa) as decimal(12,2))
		End as "percent"	
FROM @Campaign d'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on d.massmediaGroupID = MassmediaGroup.massmediaGroupID '
If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on d.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on d.campaignTypeID = iCampaignType.CampaignTypeID'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on d.massmediaID = vMassMedia.massmediaID'
If	@IsGroupByFirm <> 0 And @IsGroupByHeadCompany = 0  Set @SQLString = @SQLString + N' inner join Firm on d.firmID = Firm.FirmID '
If	@IsGroupByHeadCompany <> 0 Set @SQLString = @SQLString + N' inner join Firm on d.firmID = Firm.FirmID inner join HeadCompany on Firm.headCompanyID = HeadCompany.headCompanyID '  -- НОВЫЙ JOIN
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on d.userID = [User].UserID'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on d.AgencyID = Agency.AgencyID'
If	@IsGroupByAdvertType <> 0 OR @IsGroupByAdvertTypeTop <> 0 Set @SQLString = @SQLString + N' left join AdvertType on d.advertTypeID = AdvertType.AdvertTypeID'
If	@IsGroupByAdvertTypeTop <> 0 Set @SQLString = @SQLString + N' left join AdvertType at on AdvertType.parentID = at.AdvertTypeID'

Set 	@SQLString = @SQLString + N' Where price <> 0 '

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType
	  + @IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType 
	  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop + @IsGroupByHeadCompany <> 0  -- ДОБАВИЛИ В УСЛОВИЕ
	begin

	-- Group By part
	set	@IsStarted = 0
	Set 	@SQLString = @SQLString + N' Group by '

	if	@IsGroupByPaymentType <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Paymenttype.Name'
		set	@IsStarted = 1
	end

	If	@IsGroupByCampaignType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'iCampaignType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmedia <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'vMassMedia.NameWithGroup, vMassMedia.massmediaID'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByHeadCompany <> 0  -- НОВАЯ ГРУППИРОВКА В GROUP BY
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'HeadCompany.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin

		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'[User].userName'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByAdvertType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'AdvertType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByAdvertTypeTop <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'at.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmediaGroupType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set @SQLString = @SQLString + N'MassmediaGroup.Name'
		set	@IsStarted = 1
		end

	end


If @IsGroupByFirm = 1 And @IsGroupByHeadCompany = 1 and 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia
	  + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType
	  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
	Begin
	Set @SQLString = @SQLString +
	'
	Declare @c int
	Select @c = count(*) from @res

	Insert Into @res(RowNum, sum4, head_company, [percent], row_style)
	Select row_number() over (order by head_company) + @c, Sum(sum1), Head_Company, SUM([percent]), ''bold'' From @res Group By head_company having COUNT(*) > 1;

	Update @res Set hc2 = head_company;

	WITH DuplicatesCTE AS (
    SELECT 
        head_company,
        COUNT(*) as count
    FROM @res
    GROUP BY head_company
    HAVING COUNT(*) > 1
	)
	UPDATE t
	SET t.head_company = NULL
	FROM @res t
	INNER JOIN DuplicatesCTE d ON t.head_company = d.head_company
	Where t.firm Is Not Null;

	Update @res Set sum4 = sum1 Where sum1 Is Not Null And head_company Is Not Null;

	Select * From @res Order By hc2, firm
	'
	
	End

print @SQLString

EXECUTE sp_executesql @SQLString,
	N'@startDate datetime, @finishDate datetime, @loggedUserID smallint, @FirmID smallint, @MassmediaID smallint, 
		@PaymentTypeID smallint, @CampaignTypeID tinyint,
		@ManagerID smallint, @AgencyID smallint, @massmediaGroupID int, @advertTypeID smallint, @headCompanyID smallint',
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
	@headCompanyID = @headCompanyID
GO

-- ===== stat_VolumeOfRealization3 =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — stat_VolumeOfRealization3 и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER Procedure [dbo].[stat_VolumeOfRealization3]
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
    @IsGroupByHeadCompany bit = 0,  -- НОВЫЙ ПАРАМЕТР
    @IsGroupByMassmediaGroupType bit = 0,
    @IsGroupByAdvertType bit = 0,
    @IsGroupByAdvertTypeTop bit = 0,
    @ShowWhite bit = 1,
    @ShowBlack bit = 1,
    @loggedUserID smallint,
    @languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
As
BEGIN
    SET NOCOUNT ON;
    DECLARE @tAll NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Все');

    IF @StartDay IS NULL OR @FinishDay IS NULL
    BEGIN
        RAISERROR('FilterStartFinishDays', 16, 1)
        RETURN
    END

    SET @StartDay  = dbo.ToShortDate(@StartDay)
    SET @FinishDay = dbo.ToShortDate(@FinishDay)

	DECLARE @IsFirmHeadCompanyOnly bit = 0;
	DECLARE @IsAdvertTypeTopOnly   bit = 0;

	IF @IsGroupByFirm = 1 AND @IsGroupByHeadCompany = 1
	   AND 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia
			 + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType
			 + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
	BEGIN
		SET @IsFirmHeadCompanyOnly = 1;
	END

	-- НОВОЕ: спец-режим только для AdvertType + AdvertTypeTop
	IF @IsGroupByAdvertType = 1 AND @IsGroupByAdvertTypeTop = 1
	   AND 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia
			 + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType
			 + @IsGroupByFirm + @IsGroupByHeadCompany = 0
	BEGIN
		SET @IsAdvertTypeTopOnly = 1;
	END

    --------------------------------------------------------------------
    -- NEW: вместо fn_statGetPrice() создаём #Campaign и заполняем процедурой
    --------------------------------------------------------------------
    CREATE TABLE #Campaign
    (
        campaignID int NOT NULL, 
        advertTypeID smallint NULL,
        actionID int NULL, 
        massmediaID smallint NULL, 
        paymentTypeID smallint NULL, 
        campaignTypeID tinyint NULL, 
        agencyID smallint NULL,
        startDate datetime NULL,
        finishDate datetime NULL,
        finalPrice decimal(18,2) NULL,
        userID smallint NULL,
        firmID smallint NULL,
        discount decimal(9,4) NULL,
        massmediaGroupID int NULL,
        price decimal(18,2) NULL
    );

    CREATE UNIQUE CLUSTERED INDEX IX_Campaign
        ON #Campaign (campaignID, massmediaID, advertTypeID);

    INSERT INTO #Campaign
    EXEC dbo.stat_GetPrice_proc
         @StartDay,
         @FinishDay,
         @loggedUserID;

-- ... после заполнения #Campaign

	DECLARE @SQLString NVARCHAR(MAX);
	DECLARE @JoinSql   NVARCHAR(MAX) = N'';
	DECLARE @WhereSql  NVARCHAR(MAX) = N' WHERE d.price <> 0 ';
	DECLARE @GroupJoinSql NVARCHAR(MAX) = N'';

	DECLARE @IsStarted int;

	-- Базовые join'ы для фильтров/группировок
	-- Firm понадобится почти всегда (у тебя часто фильтр по headCompanyID и group-by firm/headCompany)
	SET @JoinSql += N' INNER JOIN Firm f ON f.firmId = d.firmId ';

	-- AdvertType нужен если фильтруем по advertTypeID (для parentID)
	IF @advertTypeID IS NOT NULL
		SET @JoinSql += N' LEFT JOIN AdvertType atFilter ON atFilter.advertTypeID = d.advertTypeID ';

	-- PaymentType нужен если:
	--  1) фильтруем по ShowWhite/ShowBlack
	--  2) группируем по PaymentType
	--  3) фильтруем по PaymentTypeID (можно без join, но пусть будет единообразно)
	IF (@ShowWhite = 0 OR @ShowBlack = 0) OR (@IsGroupByPaymentType <> 0)
	BEGIN
		SET @JoinSql += N' INNER JOIN PaymentType pt ON pt.PaymentTypeID = d.PaymentTypeID ';
	END

	-- ========= ЕДИНЫЙ ФИЛЬТР (Where) =========

	-- ShowWhite/ShowBlack: каноничная логика
	-- (white = pt.isHidden = 0, black = pt.isHidden <> 0)
	-- Если оба 1 -> пропускаем всё, если оба 0 -> пусто
	IF (@ShowWhite = 0 OR @ShowBlack = 0)
	BEGIN
		SET @WhereSql += N'
	  AND (
			(@ShowWhite = 1 AND pt.isHidden = 0)
		 OR (@ShowBlack = 1 AND pt.isHidden <> 0)
	  )';
	END

	IF @FirmID IS NOT NULL
		SET @WhereSql += N' AND d.firmID = @FirmID';

	IF @headCompanyID IS NOT NULL
		SET @WhereSql += N' AND f.headCompanyID = @headCompanyID';

	IF @MassmediaID IS NOT NULL
		SET @WhereSql += N' AND d.massmediaID = @MassmediaID';

	IF @PaymentTypeID IS NOT NULL
		SET @WhereSql += N' AND d.paymentTypeID = @PaymentTypeID';

	IF @CampaignTypeID IS NOT NULL
		SET @WhereSql += N' AND d.campaignTypeID = @CampaignTypeID';

	IF @ManagerID IS NOT NULL
		SET @WhereSql += N' AND d.userID = @ManagerID';

	IF @AgencyID IS NOT NULL
		SET @WhereSql += N' AND d.agencyID = @AgencyID';

	IF @massmediaGroupID IS NOT NULL
		SET @WhereSql += N' AND d.massmediaGroupID = @massmediaGroupID';

	IF @advertTypeID IS NOT NULL
		SET @WhereSql += N' AND (d.advertTypeID = @advertTypeID OR atFilter.parentID = @advertTypeID)';

	-- ========= Сборка SQL =========
	SET @SQLString = N'
	DECLARE @Summa decimal(18,2) = 0;

	SELECT @Summa = ISNULL(SUM(d.price), 0)
	FROM #Campaign d
	' + @JoinSql + CHAR(10) + @WhereSql + N';

	';

	IF @IsFirmHeadCompanyOnly = 1
	BEGIN
		SET @SQLString += N'
		DECLARE @res TABLE (
			RowNum int,
			sum4 decimal(18,2),
			sum1 decimal(18,2),
			firm varchar(256),
			head_company varchar(256),
			hc2 varchar(256),
			[percent] decimal(12,2),
			row_style varchar(20),
			INDEX i1 UNIQUE CLUSTERED (RowNum)
		);

		INSERT INTO @res(RowNum, sum1, firm, head_company, [percent])
		';
	END

	IF @IsAdvertTypeTopOnly = 1
	BEGIN
		SET @SQLString += N'
		DECLARE @resAdvert TABLE (
			RowNum int,
			sum4 decimal(18,2),
			sum1 decimal(18,2),
			adverttype varchar(256),
			topAdverttype varchar(256),
			top2 varchar(256),
			[percent] decimal(12,2),
			row_style varchar(20),
			INDEX i1 UNIQUE CLUSTERED (RowNum)
		);

		INSERT INTO @resAdvert(RowNum, sum1, adverttype, topAdverttype, [percent])
		';
	END
	-- ==== дальше твоя логика построения SELECT списка ====

	-- 1) Заголовок SELECT + поля группировки
	SET @SQLString += N'
	SELECT
		row_number() over(order by ISNULL(SUM(d.price),0)) as RowNum,
		ISNULL(SUM(d.price),0) as sum1,
	';

	IF @IsGroupByPaymentType <> 0
		SET @SQLString += N'  pt.Name as payment_type,';
	IF @IsGroupByCampaignType <> 0
		SET @SQLString += N'  iCampaignType.Name as campaign_type,';
	IF @IsGroupByMassmedia <> 0
		SET @SQLString += N'  vMassMedia.NameWithGroup as massmedia, vMassMedia.massmediaID,';
	IF @IsGroupByMassmediaGroupType <> 0
		SET @SQLString += N'  MassmediaGroup.Name as massmedia_group,';
	IF @IsGroupByFirm <> 0
		SET @SQLString += N'  f.Name as firm,';
	IF @IsGroupByHeadCompany <> 0
		SET @SQLString += N'  HeadCompany.Name as head_company,';
	IF @IsGroupByManager <> 0
		SET @SQLString += N'  [User].userName as manager,';
	IF @IsGroupByAgency <> 0
		SET @SQLString += N'  Agency.Name as agency,';
	IF @IsGroupByAdvertType <> 0
		SET @SQLString += N'  AdvertType.Name as adverttype,';
	IF @IsGroupByAdvertTypeTop <> 0
		SET @SQLString += N'  at.Name as topAdverttype,';

	IF 0 + @IsGroupByPaymentType + @IsGroupByCampaignType + @IsGroupByMassmedia + @IsGroupByFirm
		 + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType + @IsGroupByHeadCompany
		 + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop = 0
		SET @SQLString += N'  max(N''' + REPLACE(@tAll, N'''', N'''''') + N''') as [all],';

	-- 2) JOIN’ы для группировок (только если нужны, чтобы не тащить лишнее)
	-- ВНИМАНИЕ: Firm уже присоединён как f в @JoinSql для фильтра headCompany. Но для group-by firm/headcompany у тебя были другие алиасы.
	-- Чтобы не ломать существующую часть, оставим твою секцию JOIN'ов, но только добавим недостающее:
	IF @IsGroupByMassmediaGroupType <> 0 SET @GroupJoinSql  += N' inner join MassmediaGroup on d.massmediaGroupID = MassmediaGroup.massmediaGroupID ';
	IF @IsGroupByCampaignType <> 0 SET @GroupJoinSql += N' inner join iCampaignType on d.campaignTypeID = iCampaignType.CampaignTypeID';
	IF @IsGroupByMassmedia <> 0 SET @GroupJoinSql += N' inner join vMassMedia on d.massmediaID = vMassMedia.massmediaID';

	IF @IsGroupByHeadCompany <> 0 SET @GroupJoinSql += N' inner join HeadCompany on f.headCompanyID = HeadCompany.headCompanyID ';
	IF @IsGroupByManager <> 0 SET @GroupJoinSql += N' inner join [User] on d.userID = [User].UserID';
	IF @IsGroupByAgency <> 0 SET @GroupJoinSql += N' inner join Agency on d.AgencyID = Agency.AgencyID';
	IF @IsGroupByAdvertType <> 0 OR @IsGroupByAdvertTypeTop <> 0 SET @GroupJoinSql += N' left join AdvertType on d.advertTypeID = AdvertType.AdvertTypeID';
	IF @IsGroupByAdvertTypeTop <> 0 SET @GroupJoinSql += N' left join AdvertType at on AdvertType.parentID = at.AdvertTypeID';

	-- percent (теперь sum1 и @Summa на одном и том же фильтре)
	SET @SQLString += N'
		CASE @Summa
			WHEN 0 THEN 0
			ELSE CAST((ISNULL(SUM(d.price),0) * 100.0 / @Summa) as decimal(12,2))
		END as [percent]
	FROM #Campaign d
	' + @JoinSql + CHAR(10) + @GroupJoinSql + CHAR(10)  + @WhereSql + CHAR(10);

	-- 3) GROUP BY (как у тебя)
	IF 0 + @IsGroupByPaymentType + @IsGroupByCampaignType
		  + @IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType 
		  + @IsGroupByAdvertType + @IsGroupByAdvertTypeTop + @IsGroupByHeadCompany <> 0
	BEGIN
		SET @IsStarted = 0;
		SET @SQLString += N' GROUP BY ';

		IF @IsGroupByPaymentType <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'pt.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByCampaignType <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'iCampaignType.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByMassmedia <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'vMassMedia.NameWithGroup, vMassMedia.massmediaID';
			SET @IsStarted = 1;
		END

		IF @IsGroupByFirm <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'f.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByHeadCompany <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'HeadCompany.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByManager <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'[User].userName';
			SET @IsStarted = 1;
		END

		IF @IsGroupByAgency <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'Agency.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByAdvertType <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'AdvertType.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByAdvertTypeTop <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'at.Name';
			SET @IsStarted = 1;
		END

		IF @IsGroupByMassmediaGroupType <> 0 BEGIN
			IF @IsStarted = 1 SET @SQLString += N',';
			SET @SQLString += N'MassmediaGroup.Name';
			SET @IsStarted = 1;
		END
	END

	IF @IsFirmHeadCompanyOnly = 1
	BEGIN
		SET @SQLString += N'
		DECLARE @c int;
		SELECT @c = COUNT(*) FROM @res;

		INSERT INTO @res(RowNum, sum4, head_company, [percent], row_style)
		SELECT ROW_NUMBER() OVER (ORDER BY head_company) + @c,
			   SUM(sum1),
			   head_company,
			   SUM([percent]),
			   ''bold''
		FROM @res
		GROUP BY head_company
		HAVING COUNT(*) > 1;

		UPDATE @res SET hc2 = head_company;

		WITH DuplicatesCTE AS (
			SELECT head_company
			FROM @res
			GROUP BY head_company
			HAVING COUNT(*) > 1
		)
		UPDATE t
		SET t.head_company = NULL
		FROM @res t
		INNER JOIN DuplicatesCTE d ON t.head_company = d.head_company
		WHERE t.firm IS NOT NULL;

		UPDATE @res
		SET sum4 = sum1
		WHERE sum1 IS NOT NULL AND head_company IS NOT NULL;

		SELECT * FROM @res ORDER BY hc2, firm;
		';
	END

	IF @IsAdvertTypeTopOnly = 1
	BEGIN
		SET @SQLString += N'
		DECLARE @c2 int;
		SELECT @c2 = COUNT(*) FROM @resAdvert;

		-- добавляем "итоги" по верхнему уровню (topAdverttype) как жирные строки
		INSERT INTO @resAdvert(RowNum, sum4, topAdverttype, [percent], row_style)
		SELECT ROW_NUMBER() OVER (ORDER BY topAdverttype) + @c2,
			   SUM(sum1),
			   topAdverttype,
			   SUM([percent]),
			   ''bold''
		FROM @resAdvert
		GROUP BY topAdverttype
		HAVING COUNT(*) > 1;

		UPDATE @resAdvert SET top2 = topAdverttype;

		-- прячем повторяющийся верхний уровень в "детальных" строках (как head_company)
		WITH DuplicatesCTE AS (
			SELECT topAdverttype
			FROM @resAdvert
			GROUP BY topAdverttype
			HAVING COUNT(*) > 1
		)
		UPDATE t
		SET t.topAdverttype = NULL
		FROM @resAdvert t
		INNER JOIN DuplicatesCTE d ON t.topAdverttype = d.topAdverttype
		WHERE t.adverttype IS NOT NULL;

		-- sum4 = sum1 для жирных строк (как у фирм)
		UPDATE @resAdvert
		SET sum4 = sum1
		WHERE sum1 IS NOT NULL AND topAdverttype IS NOT NULL;

		SELECT * FROM @resAdvert ORDER BY top2, adverttype;
		';
	END

	--PRINT @SQLString  -- можно включить для отладки
	--return

	EXECUTE sp_executesql @SQLString,
	N'@startDate datetime, @finishDate datetime, @loggedUserID smallint,
	  @FirmID smallint, @MassmediaID smallint, @PaymentTypeID smallint, @CampaignTypeID tinyint,
	  @ManagerID smallint, @AgencyID smallint, @massmediaGroupID int, @advertTypeID smallint, @headCompanyID smallint,
	  @ShowWhite bit, @ShowBlack bit',
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
		@ShowBlack = @ShowBlack;

END
GO

-- ===== stat_VolumeOfRealizationNew =====
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — stat_VolumeOfRealizationNew и дальше не выполняются', 16, 1); SET NOEXEC ON; END
GO
SET QUOTED_IDENTIFIER ON;
GO
CREATE OR ALTER Procedure [dbo].[stat_VolumeOfRealizationNew]
(
@StartDay DATETIME = default,
@FinishDay DATETIME = default,
@FirmID int = default, 
@MassmediaID int = default, 
@PaymentTypeID int = default,
@CampaignTypeID int = default,
@ManagerID int = default,
@AgencyID int = default,
@AdvertTypeID int = default,
@IsGroupByPaymentType bit = 0,
@IsGroupByCampaignType bit = 0,
@IsGroupByMassmedia bit = 0,
@IsGroupByFirm bit = 0,
@IsGroupByManager bit = 0,
@IsGroupByAgency bit = 0,
@IsGroupByMassmediaGroupType bit = 0,
@IsGroupByAdvertType bit = 0,
@massmediaGroupID int = NULL,
@ShowWhite bit = 1,
@ShowBlack bit = 1,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
As

SET NOCOUNT ON
DECLARE @tAll NVARCHAR(200) = dbo.fn_Translate(@languageCode, N'Все');

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit,
	@isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

If	@StartDay Is Null Or @FinishDay Is Null
	Begin
	Raiserror('FilterStartFinishDays', 16, 1)
	Return
	End

CREATE TABLE #tmp1
(
CompanyPrice decimal(18,2),
MassmediaID SMALLINT,
PaymentTypeID SMALLINT,
ActionID INT,
campaignTypeID SMALLINT,
Manager_ID SMALLINT,
AgencyID SMALLINT,
massmediaGroupID int,
advertTypeID smallint,
issuePrice decimal(18,2)
)

Set	@StartDay = dbo.ToShortDate(@StartDay)
Set	@FinishDay = dbo.ToShortDate(@FinishDay)

-- select all companies, which has appropriated 
-- start and finish dates
Declare cur_companies Cursor Local fast_forward
For
select distinct  
	c.campaignID, c.ActionID, c.massmediaID, 
	c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
	--c.[startDate], max(coalesce(mm.roltypeID,0)),  max(coalesce(mm.massmediaGroupID,0)), 
	c.[startDate], mm.massmediaGroupID,  --max(coalesce(mm.massmediaGroupID,0)), 
	a.discount, c.finalPrice, c.finishDate, r.advertTypeID, i.ratio * i.tariffPrice
From	
	Campaign c
	INNER Join [Action] a On c.ActionID = a.actionID AND a.[isConfirmed] = 1
	INNER JOIN Issue i On i.campaignID = c.campaignID
	INNER Join Roller r On r.rollerID = i.rollerID
	INNER JOIN PaymentType On c.PaymentTypeID = PaymentType.PaymentTypeID
	left join MassMedia mm on c.massmediaID = mm.massmediaID	
	left JOIN [PackModuleIssue] pmi ON pmi.[campaignID] = c.[campaignID]
	left JOIN [PackModuleContent] pmc ON pmc.[pricelistID] = pmi.[pricelistID]
	left JOIN [Module] m ON pmc.[moduleID] = m.[moduleID]	
	inner join @massmedias mmu on (mm.massmediaID = mmu.massmediaID 
						or m.massmediaID = mmu.massmediaID)
	inner join MassMedia mmfu on mmu.massmediaID = mmfu.massmediaID 	
	left join GroupMember gm on a.userID = gm.userID
	left join @ugroups ug on gm.groupID = ug.id
Where	
	(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
	c.StartDate <= @FinishDay and
	c.FinishDate >= @StartDay and
	c.AgencyID = IsNull(@AgencyID, c.AgencyID) and
	a.firmID = IsNull(@FirmID, a.firmID) and
	a.userID = IsNull(@ManagerID, a.userID) and
	c.PaymentTypeID = IsNull(@PaymentTypeID, c.PaymentTypeID) and
	c.campaignTypeID = IsNull(@CampaignTypeID, c.campaignTypeID) and
	(@ShowWhite <> 0 or PaymentType.isHidden <> 0) and  
	(@ShowBlack <> 0 or PaymentType.isHidden = 0)  
	and (@MassmediaID is null or mmfu.massmediaID = @MassmediaID)
	and (@massmediaGroupID is null or mmfu.massmediaGroupId = @massmediaGroupID)
	and ((a.userID = @loggedUserID and mmu.myMassmedia = 1) or (a.userID <> @loggedUserID and mmu.foreignMassmedia = 1))
	and r.advertTypeID = IsNull(@AdvertTypeID, r.advertTypeID)
/*
group by 
	c.campaignID, c.ActionID, c.massmediaID, 
	c.PaymentTypeID, c.campaignTypeID, a.userID, c.AgencyID, 
	c.[startDate], a.discount, c.finalPrice, c.finishDate,
	mm.roltypeID
*/

-- select all companies, which have Issues inside interval
Declare	
	@campaignID int, 
	@ActionID int, 
	@campaignPrice decimal(18,2),
	@SummaVar decimal(18,2),
	@CompStartDate datetime,
	@actionDiscount decimal(9,4),
	@sumPrice decimal(18,2),
	@finalPrice decimal(18,2),
	@cfinishDate datetime,
	@campMassmediaGroupID int,
	@mmID smallint,
	@advTypeID smallint,
	@issuePrice decimal(18,2)
	
declare @tmp table (massmediaID smallint, price decimal(18,2))

Open	cur_companies
Fetch	next from cur_companies into 
	@campaignID, @ActionID, @mmID, @PaymenttypeID, 
	@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, 
	@finalPrice, @cfinishDate, @advTypeID, @issuePrice

--Set	@FinishDay = Convert(datetime, Convert(varchar, @FinishDay, 112), 112) - 1
While	@@fetch_status = 0
begin
	if @FinishDay < @cfinishDate or @StartDay > @CompStartDate
		exec GetPriceByPeriod @campaignId, @CampaignTypeID, @StartDay, @FinishDay, @campaignPrice out
	else 
		set @campaignPrice = @finalPrice

	IF @CampaignTypeID = 4
	begin
		delete from @tmp
				
		insert into @tmp(massmediaID, price)
		select
			m.[massmediaID], sum(mpl.[price])
		from [PackModuleIssue] i 
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		where 
			i.campaignID = @campaignID	and
			i.issueDate between @StartDay and @FinishDay 
		group by m.massmediaID
			
		select @sumPrice = sum(t1.price) FROM @tmp AS t1
		
		insert into #tmp1 ([CompanyPrice],	[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],	[AgencyID], massmediaGroupID) 
		select @campaignPrice * sum(t1.price)/ @sumPrice,  t1.massmediaID, @PaymenttypeID, @ActionID, @CampaignTypeID,@ManagerID, @AgencyID, mm.massmediaGroupID
		from @tmp as t1
			inner join MassMedia mm on t1.massmediaID = mm.massmediaID
			inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
		where t1.price > 0  
			and (@MassmediaID is null or mm.massmediaID = @MassmediaID)
			and (@massmediaGroupID is null or mm.massmediaGroupId = @massmediaGroupID)
		group by t1.massmediaID, mm.massmediaGroupID
	END
	ELSE
	begin
		if	@campaignPrice > 0 
			Insert	Into #tmp1 ([CompanyPrice],[MassmediaID],[PaymentTypeID],[ActionID],[campaignTypeID],[Manager_ID],[AgencyID], massmediaGroupID, advertTypeID) 
			Values(@campaignPrice,  @mmID, @PaymenttypeID, @ActionID, @CampaignTypeID, @ManagerID, @AgencyID, @campMassmediaGroupID, @advTypeID)
	end
		
	fetch next from cur_companies into 
			@campaignID, @ActionID, @mmID, @PaymenttypeID,
			@CampaignTypeID, @ManagerID, @AgencyID, @CompStartDate, @campMassmediaGroupID, @actionDiscount, 
			@finalPrice, @cfinishDate, @advTypeID, @issuePrice
End	

close cur_companies
deallocate cur_companies

Select	@SummaVar = IsNull(sum(CompanyPrice), 0) From	#tmp1

-- output ---------------------------------------------------------
Declare	@SQLString NVARCHAR(2500),
				@IsStarted int

/* Build the SQL string once.*/
Set	@SQLString = N'Select	row_number() over(order by IsNull(Sum(CompanyPrice), 0)) as RowNum,'
Set	@SQLString = @SQLString + N' IsNull(Sum(CompanyPrice), 0) as  sum1'

Set @SQLString = @SQLString + N',  '

If	@IsGroupByAdvertType <> 0
	Set 	@SQLString = @SQLString + N'AdvertType.Name as "advert_type",'
If	@IsGroupByPaymentType <> 0
	Set 	@SQLString = @SQLString + N'Paymenttype.Name as "payment_type",'
If	@IsGroupByCampaignType <> 0
	Set 	@SQLString = @SQLString + N'iCampaignType.Name as "campaign_type",'
If	@IsGroupByMassmedia <> 0
	Set 	@SQLString = @SQLString + N'vMassMedia.Name as "massmedia", vMassMedia.groupName as "massmedia_group",'
If	@IsGroupByMassmediaGroupType <> 0
	Set 	@SQLString = @SQLString + N'MassmediaGroup.Name as "massmedia_group",'
If	@IsGroupByFirm <> 0
	Set 	@SQLString = @SQLString + N'Firm.Name as "firm",'
If	@IsGroupByManager <> 0
	Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''') as "manager",'
If	@IsGroupByAgency <> 0
	Set 	@SQLString = @SQLString + N'Agency.Name as "agency",'
If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByAdvertType +
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType /*+ @IsGroupByCommissionaire*/ = 0
	set		@SQLString = @SQLString + N'max(N''' + REPLACE(@tAll, N'''', N'''''') + N''') as "all",'

Set 	@SQLString = @SQLString + 
		N'case @Summa
			when	0 then 0
			else	Cast((IsNull(Sum(CompanyPrice), 0) * 100.0 / @Summa) as decimal(18,2))
		End as "percent"	
From	#tmp1'

If	@IsGroupByMassmediaGroupType <> 0 Set @SQLString = @SQLString + N' inner join MassmediaGroup on #tmp1.massmediaGroupID = MassmediaGroup.massmediaGroupID '
If	@IsGroupByPaymentType <> 0 Set @SQLString = @SQLString + N' inner join Paymenttype on #tmp1.PaymentTypeID = Paymenttype.PaymenttypeID'
If	@IsGroupByCampaignType <> 0 Set @SQLString = @SQLString + N' inner join iCampaignType on #tmp1.campaignTypeID = iCampaignType.CampaignTypeID'
If	@IsGroupByMassmedia <> 0 Set @SQLString = @SQLString + N' inner join vMassMedia on #tmp1.massmediaID = vMassMedia.massmediaID'
If	@IsGroupByAdvertType <> 0 Set @SQLString = @SQLString + N' inner join AdvertType on #tmp1.advertTypeID = AdvertType.advertTypeID'
If	@IsGroupByFirm <> 0 Set @SQLString = @SQLString + N' inner join Action on #tmp1.ActionID = Action.ActionID inner join Firm on Action.firmID = Firm.FirmID '
If	@IsGroupByManager <> 0 Set @SQLString = @SQLString + N' inner join [User] on #tmp1.Manager_ID = [User].UserID'
If	@IsGroupByAgency <> 0 Set @SQLString = @SQLString + N' inner join Agency on #tmp1.AgencyID = Agency.AgencyID'


Set 	@SQLString = @SQLString + N' Where CompanyPrice <> 0 '

If	0 + @IsGroupByPaymentType + @IsGroupByCampaignType + 
	@IsGroupByMassmedia + @IsGroupByFirm + @IsGroupByAdvertType +
	@IsGroupByManager + @IsGroupByAgency + @IsGroupByMassmediaGroupType /*+ @IsGroupByCommissionaire*/ <> 0
	begin

	-- Group By part
	set	@IsStarted = 0
	Set 	@SQLString = @SQLString + N' Group by '

/*
	if	@IsGroupByCommissionaire <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Dic_Commissionaire.Description'
		set	@IsStarted = 1
	end
*/
	if	@IsGroupByPaymentType <> 0 begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Paymenttype.Name'
		set	@IsStarted = 1
	end

	If	@IsGroupByCampaignType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'iCampaignType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByAdvertType <> 0
		begin
		if	@IsStarted = 1 set @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'AdvertType.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByMassmedia <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'vMassMedia.Name, vMassMedia.groupName'
		set	@IsStarted = 1
		end

	If	@IsGroupByFirm <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Firm.Name'
		set	@IsStarted = 1
		end

	If	@IsGroupByManager <> 0
		begin

		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'coalesce([User].LastName, '''') + coalesce(space(1) + [User].FirstName, '''')'
		set	@IsStarted = 1
		end

	If	@IsGroupByAgency <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set 	@SQLString = @SQLString + N'Agency.Name'
		set	@IsStarted = 1
		end
	
	If	@IsGroupByMassmediaGroupType <> 0
		begin
		if	@IsStarted = 1 set  @SQLString = @SQLString + N','	
		Set @SQLString = @SQLString + N'MassmediaGroup.Name'
		set	@IsStarted = 1
		end

	end

EXECUTE sp_executesql @SQLString,
	N'@Summa decimal(18,2)',
	@Summa = @SummaVar		

Drop		table #tmp1
GO

SET QUOTED_IDENTIFIER ON;
GO
IF @@TRANCOUNT = 0 BEGIN RAISERROR('Транзакция откатилась — ничего не применено', 16, 1); SET NOEXEC ON; END
GO
COMMIT TRANSACTION;
PRINT 'Этап 6 многоязычности применён.';
GO
SET NOEXEC OFF;
GO
