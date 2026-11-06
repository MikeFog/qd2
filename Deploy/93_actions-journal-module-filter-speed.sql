-- Журнал акций: отбор по модулю и по пакетному модулю — без прохода по всем выпускам базы (06.11.2026, разбор прод-лога
-- 02–06.11.2026: agv 05.11 13:15–13:22, модуль 614 — FirmWithActions1 14–17 с, Actions1 14–22 с на каждый вызов).
-- В Actions1 и FirmWithActions1 ветка отбора по выпускам собирала акции соединением всех Issue × TariffWindow с левыми
-- ModuleIssue / PackModuleIssue и только потом фильтровала по модулю. Теперь при @moduleID / @packModuleID сначала
-- отбираются выпуски модуля (индексы Issue.moduleIssueID / packModuleIssueID), окна и кампании — только для них;
-- остальные условия (ролик, день, время, группа станций, станция) те же. Отборы без модуля — прежним запросом.
-- Параметры и выдача не меняются. Копия прода (ArtvisDev): 10–12 с → 0,2–1 с, выдача 13 вызовов до и после совпала.
--
-- После 08, 24, 28, 42 (несут более старый текст процедур). Не перезапускать после 93: 08, 24, 28, 42 и
-- ArtvisDB/Scripts/actions-createdate-end-boundary-deploy.sql, actions1-actionid-userid-deploy.sql,
-- firm-with-actions1-isolation-deploy.sql, web-i18n-sql-deploy.sql — вернут медленный отбор; если перезапустили — повторить 93.
-- В любое время, на Artvis и Tumen. Клиент и веб не нужны. Идемпотентен (CREATE OR ALTER).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 93_actions-journal-module-filter-speed.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.fn_Translate') IS NULL
BEGIN
    RAISERROR(N'93: сначала Deploy/08 (fn_Translate). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[Actions1]
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
			pkg.name as packageDiscountName,
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
			left join (PackageDiscountPriceList pkgpl
				inner join PackageDiscount pkg on pkg.packageDiscountId = pkgpl.packageDiscountID
				) on pkgpl.packageDiscountPriceListID = a.packageDiscountPriceListID
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
			-- Отбор по менеджеру из фильтра журнала: без него обычный менеджер находил
			-- чужую акцию по номеру. Загрузка карточки по номеру (Refresh) передаёт
			-- userID самой акции или не передаёт вовсе — её условие не отсекает.
			and (@userID is null or a.userID = @userID)
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
		if @moduleID is not null or @packModuleID is not null
		begin
			-- Отбор по модулю / пакетному модулю: сначала выпуски этого модуля (индексы Issue.moduleIssueID /
			-- packModuleIssueID), потом окна и кампании только для них. Общий запрос ниже проходил по всем выпускам
			-- и окнам базы: прод 05.11.2026 — 14–22 с на вызов (agv, модуль 614), копия прода — 10–12 с.
			-- option (loop join): без подсказки оптимизатор ждёт сотни тысяч выпусков модуля и всё равно сканирует Issue
			-- (подсказка у соединения дала бы ту же скорость, но предупреждение «порядок соединения задан принудительно»).
			declare @moduleIssues table (issueID int primary key, actualWindowID int not null, campaignID int not null, rollerID int not null)
			if @moduleID is not null
				insert into @moduleIssues (issueID, actualWindowID, campaignID, rollerID)
				select i.issueID, i.actualWindowID, i.campaignID, i.rollerID
				from ModuleIssue mi
					inner join Issue i on i.moduleIssueID = mi.moduleIssueID
				where mi.moduleID = @moduleID
					and (@packModuleID is null or exists(select 1 from PackModuleIssue pmi
							inner join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
						where pmi.packModuleIssueID = i.packModuleIssueID and pmpl.packModuleID = @packModuleID))
				option (loop join)
			else
				insert into @moduleIssues (issueID, actualWindowID, campaignID, rollerID)
				select i.issueID, i.actualWindowID, i.campaignID, i.rollerID
				from PackModulePriceList pmpl
					inner join PackModuleIssue pmi on pmi.pricelistID = pmpl.priceListID
					inner join Issue i on i.packModuleIssueID = pmi.packModuleIssueID
				where pmpl.packModuleID = @packModuleID
				option (loop join)

			insert into @issues
			select distinct c.actionID
			from @moduleIssues i
				inner join TariffWindow tw on i.actualWindowID = tw.windowId
				inner join Campaign c on i.campaignID = c.campaignID
				Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
			where i.rollerID = coalesce(@rollerId, i.rollerID)
				and (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
				and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
				and mm.massmediaGroupID = Coalesce(@massmediaGroupID, mm.massmediaGroupID)
				and tw.massmediaID = Coalesce(@massmediaId, tw.massmediaId)
		end
		else
		insert into @issues
		select distinct c.actionID 
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
		where i.rollerID = coalesce(@rollerId, i.rollerID)
			and (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
			and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
			and mm.massmediaGroupID = Coalesce(@massmediaGroupID, mm.massmediaGroupID)
			and tw.massmediaID = Coalesce(@massmediaId, tw.massmediaId)
										
		SELECT distinct 
			a.*, 
			us.userName as creator,
			--'Акция №' + LTRIM(a.[actionID]) + ' (' + LTRIM(f.name) + ')'  as name,
			@tAction + LTRIM(a.[actionID]) as name,
			f.name as firmName,
			pkg.name as packageDiscountName,
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
			left join (PackageDiscountPriceList pkgpl
				inner join PackageDiscount pkg on pkg.packageDiscountId = pkgpl.packageDiscountID
				) on pkgpl.packageDiscountPriceListID = a.packageDiscountPriceListID
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
			pkg.name as packageDiscountName,
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
			left join (PackageDiscountPriceList pkgpl
				inner join PackageDiscount pkg on pkg.packageDiscountId = pkgpl.packageDiscountID
				) on pkgpl.packageDiscountPriceListID = a.packageDiscountPriceListID
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

CREATE OR ALTER PROCEDURE [dbo].[FirmWithActions1]
(
@firmId smallint = null,
@startOfInterval datetime = null,
@endOfInterval datetime = null,
@createDateBegin datetime = null, -- Новый параметр
@createDateEnd datetime = null,   -- Новый параметр
@paymentTypeId smallint = null,
@campaignTypeId tinyint = null,
@campaignFinishDate datetime = null,
@firmId2 smallint = null,
@userID smallint = null,
@changeStartOfInterval datetime = null,
@changeEndOfInterval datetime = null,
@massmediaId smallint = null,
@agencyID smallint = null,
@actionID int = null,
@issueDay datetime = null,
@issueDate datetime = null,
@rollerId int = null,
@isHideBlack bit = 0,
@isHideWhite bit = 0,
@isShowActivate BIT = 0,
@isShowNotActivate BIT = 0,
@withoutActionsSince datetime = null,
@showBlack bit = 1,
@showWhite bit = 1,
@moduleID smallint = null,
@packModuleID smallint = null,
@loggedUserID smallint = null,
@managerDiscount float = null,
@massmediaGroupID int = null,
@showDeleted bit = 0,
@headCompanyId int = null
)
AS
SET NOCOUNT on
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; -- Важно для продакшена: журнальный SELECT не должен брать S-локи и попадать в дедлок с ActionRecalculate (см. project_deadlocks_prod пара №3). Как в HeadCompaniesWithActions.
	-- Проблема
	-- a.createDate <= @createDateEnd — при @createDateEnd = '2025-05-01 00:00:00' любая акция, созданная 2025-05-01 11:42, 
	--отсекается, потому что 11:42 > 00:00.
	-- Решение — сдвиг границы, а не обрезка колонки
	SET @createDateEnd = DATEADD(DAY, 1, CAST(@createDateEnd AS date));

	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

	if @issueDay is not null or @rollerId is not null or @issueDate is not null or @moduleID is not null or @packModuleID is not null
	begin 
		declare @issues table (actionID int primary key)
		if @moduleID is not null or @packModuleID is not null
		begin
			-- Отбор по модулю / пакетному модулю: сначала выпуски этого модуля (индексы Issue.moduleIssueID /
			-- packModuleIssueID), потом окна и кампании только для них. Общий запрос ниже проходил по всем выпускам
			-- и окнам базы: прод 05.11.2026 — 14–17 с на вызов (agv, модуль 614), копия прода — 10–12 с. Так же в Actions1.
			-- option (loop join): без подсказки оптимизатор ждёт сотни тысяч выпусков модуля и всё равно сканирует Issue
			-- (подсказка у соединения дала бы ту же скорость, но предупреждение «порядок соединения задан принудительно»).
			declare @moduleIssues table (issueID int primary key, actualWindowID int not null, campaignID int not null, rollerID int not null)
			if @moduleID is not null
				insert into @moduleIssues (issueID, actualWindowID, campaignID, rollerID)
				select i.issueID, i.actualWindowID, i.campaignID, i.rollerID
				from ModuleIssue mi
					inner join Issue i on i.moduleIssueID = mi.moduleIssueID
				where mi.moduleID = @moduleID
					and (@packModuleID is null or exists(select 1 from PackModuleIssue pmi
							inner join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
						where pmi.packModuleIssueID = i.packModuleIssueID and pmpl.packModuleID = @packModuleID))
				option (loop join)
			else
				insert into @moduleIssues (issueID, actualWindowID, campaignID, rollerID)
				select i.issueID, i.actualWindowID, i.campaignID, i.rollerID
				from PackModulePriceList pmpl
					inner join PackModuleIssue pmi on pmi.pricelistID = pmpl.priceListID
					inner join Issue i on i.packModuleIssueID = pmi.packModuleIssueID
				where pmpl.packModuleID = @packModuleID
				option (loop join)

			insert into @issues
			select distinct c.actionID
			from @moduleIssues i
				inner join TariffWindow tw on i.actualWindowID = tw.windowId
				inner join Campaign c on i.campaignID = c.campaignID
				Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
			where i.rollerID = coalesce(@rollerId, i.rollerID)
				and (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
				and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
				and mm.massmediaGroupID = Coalesce(@massmediaGroupID, mm.massmediaGroupID)
				and tw.massmediaID = Coalesce(@massmediaId, tw.massmediaId)
		end
		else
		insert into @issues
		select distinct c.actionID 
		from 
			Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
		where i.rollerID = coalesce(@rollerId, i.rollerID)
			and (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
			and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
			and mm.massmediaGroupID = Coalesce(@massmediaGroupID, mm.massmediaGroupID)
			and tw.massmediaID = Coalesce(@massmediaId, tw.massmediaId)

		SELECT DISTINCT
			f.*, @userID  AS userID, @startOfInterval as startOfInterval, @endOfInterval as endOfInterval, 
			@actionID as actionID /*To filtered*/, @massmediaGroupID as massmediaGroupID, @showDeleted as showDeleted, @isShowActivate as isShowActivate, @isShowNotActivate as isShowNotActivate
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
			((c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * from PackModuleIssue pmi 
															inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
															inner join Module m on pmc.moduleID = m.moduleID
															left join @massmedias ummm on m.massmediaID = ummm.massmediaID
														where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and
															 
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
			AND ((a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) OR (a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) or (a.deleteDate is not null and @showDeleted = 1))
			and (@withoutActionsSince is null or not exists(select top 1 a1.actionID
															from [Action] a1
																inner join [Firm] f1 on a1.firmID = f1.firmID
															where f1.headCompanyID = f.headCompanyID
																and a1.isConfirmed = 1
																and a1.finishDate >= @withoutActionsSince
																and (@startOfInterval is null or a1.startDate < @startOfInterval)))
			and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			and f.headCompanyId = COALESCE(@headCompanyId, f.headCompanyId)
		order by f.[name]
		OPTION (RECOMPILE); -- catch-all, см. ниже
	end
	else 
		SELECT
			f.*, 
			@userID  AS userID, @startOfInterval as startOfInterval, @endOfInterval as endOfInterval, 
			@actionID as actionID /*To filtered*/, @massmediaGroupID as massmediaGroupID, @showDeleted as showDeleted, 
			@isShowActivate as isShowActivate, @isShowNotActivate as isShowNotActivate
		FROM 
			[Firm] f
		WHERE EXISTS (
			SELECT 1 
			FROM [Action] a
				Inner Join Campaign c ON c.actionId = a.actionId
				Inner Join PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
				LEFT JOIN (
					PackModuleIssue i 
					JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
					JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
					JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
					) ON c.campaignTypeID=4 AND i.campaignID = c.campaignID
			WHERE		
				a.firmID = f.firmID
				and a.userId = IsNull(@userId, a.userId) 
				and a.isSpecial = 0			
				and a.finishDate >= IsNull(@startOfInterval, a.finishDate)
				and a.startDate <= Coalesce(@endOfInterval, a.startDate) 
                -- Фильтр по дате создания
                and (@createDateBegin IS NULL OR a.createDate >= @createDateBegin)
                and (@createDateEnd IS NULL OR a.createDate < @createDateEnd)
				AND (a.userID = @loggedUserID 
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
				and	c.paymentTypeId = Coalesce(@paymentTypeId, c.paymentTypeId) 
				And	c.campaignTypeId = Coalesce(@campaignTypeId, c.campaignTypeId) 
				And	c.finishDate = Coalesce(@campaignFinishDate, c.finishDate) 
				And	a.firmId = Coalesce(@firmId2, a.firmId) 
				and a.modDate BETWEEN COALESCE(@changeStartOfInterval,[dbo].[GetMinDate]()) AND COALESCE(@changeEndOfInterval, [dbo].[GetMaxDate]())
				and (
						@massmediaId IS NULL
						OR 
						c.campaignTypeID=4 AND m.massmediaID=@massmediaId
						OR
						c.massmediaID=@massmediaId
						)
				and ((c.[agencyID] IS NULL AND @agencyID IS NULL) OR c.agencyId = Coalesce(@agencyID, c.agencyId))
				And	a.actionId = Coalesce(@actionId, a.actionId) 
				And	(pt.isHidden = 0 or @isHideWhite = 0) 
				And	(pt.isHidden = 1 or @isHideBlack = 0) 
				and	(
						(pt.IsHidden = 1 and @showBlack = 1)  
						or
						(pt.IsHidden = 0 and @showWhite = 1)
					) 
				and	a.[actionID] = COALESCE(@actionID, a.[actionID]) 
				AND	a.[firmID] = COALESCE(@firmID, a.[firmID]) 
				AND (
						(a.[isConfirmed] = 0 AND @isShowNotActivate = 1 And a.deleteDate is null) 
						OR 
						(a.[isConfirmed] = 1 AND @isShowActivate = 1 And a.deleteDate is null) 
						or 
						(a.deleteDate is not null and @showDeleted = 1)
					)
				and (
					@withoutActionsSince is null
					or
					not exists(
						select 1
						from [Action] a1
							inner join [Firm] f1 on a1.firmID = f1.firmID
						where f1.headCompanyID = f.headCompanyID
							and a1.isConfirmed = 1
							and a1.finishDate >= @withoutActionsSince
							and (@startOfInterval is null or a1.startDate < @startOfInterval)
						)
					)
				and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			)
		AND f.headCompanyId = COALESCE(@headCompanyId, f.headCompanyId)
		order by f.[name]
		-- Catch-all запрос: без RECOMPILE план кэшируется под первый набор фильтров
		-- (например, раскрытие головной организации: @headCompanyId задан, @withoutActionsSince = NULL)
		-- и потом уходит в таймаут на других (все фирмы + «без акций с»). Как в HeadCompaniesWithActions.
		OPTION (RECOMPILE);
GO

PRINT N'ГОТОВО';
GO
