-- Фактическое окно и фактическое время, первый шаг (docs/tasks/window-actual-switch.md, правило 07.10.2026;
-- docs/tasks/window-actual-open-questions.md §10, пункты 2–3 первой очереди). Параметры и выдача процедур не меняются.
--
-- 1. Журналы акций (подтверждённые, макеты; три уровня дерева — HeadCompaniesWithActions, FirmWithActions1, Actions1):
--    отбор «Время рекламного выпуска» сравнивает с фактическим временем окна (windowDateActual), а не со временем
--    по расписанию. Пример на ArtvisDev, 15.10.2026: «12:20» находил 2 акции вместо 9, «12:42» — 7 вместо 0.
--    Окно выпуска здесь пока исходное (i.originalWindowID) — его переключение требует индекса, отдельный шаг.
-- 2. Удаление выпуска, дня/ролика, кампании, акции (IssueIUD, CampaignsIssueDelete, CampaignIUD, ActionIUD):
--    запреты «прошедший день» (PastIssue) и «закрытый трафиком период» (DeadLineViolationDelete) и уведомление
--    админам «удалил выпуски, дата выхода которых раньше чем через N дней» считаются по дню окна, где выпуск
--    реально выходит (i.actualWindowID), а не по дню окна, куда его изначально поставили. Так уже проверяется
--    добавление (hlp_IssueVerify) и перенос (IssueTransfer). Журнал удалённых выпусков (LogDeletedIssue) не тронут.
--    Модули и пакеты (CampaignModuleIssueDelete, CampaignPackDayDelete) по-прежнему считают по дню модуля.
--
-- Идемпотентен. Клиент не нужен (десктоп и веб зовут те же процедуры).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 24_actual-window-journal-time-and-delete-rights.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [dbo].[HeadCompaniesWithActions]
    @startOfInterval datetime = NULL,
    @endOfInterval datetime = NULL,
    @createDateBegin datetime = NULL,
    @createDateEnd datetime = NULL,
    @firmId2 smallint = NULL,
    @showBlack BIT = 0,
    @showWhite BIT = 0,
    @actionID int = NULL,
    @headCompanyID int = NULL,
    @userID smallint = NULL,
    @loggedUserID smallint = NULL,
    @agencyID smallint = NULL,
    @massmediaID smallint = NULL,
    @massmediaGroupID int = NULL,
    @campaignTypeID tinyint = NULL,
    @paymentTypeID smallint = NULL,
    @isShowActivate BIT = 0,
    @isShowNotActivate BIT = 0,
    @rollerId int = null,
    @moduleID smallint = null,
    @packModuleID smallint = null,
    @showDeleted BIT = 0,
    @issueDate datetime = null,
    @issueDay datetime = null,
    @campaignFinishDate datetime = null,
    @withoutActionsSince datetime = null,
    @managerDiscount decimal(8,2) = null,
    @changeStartOfInterval datetime = null,
    @changeEndOfInterval datetime = null
AS
BEGIN
    SET NOCOUNT ON;
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED; -- Важно для продакшена

	-- Проблема
	-- a.createDate <= @createDateEnd — при @createDateEnd = '2025-05-01 00:00:00' любая акция, созданная 2025-05-01 11:42, 
	--отсекается, потому что 11:42 > 00:00.
	-- Решение — сдвиг границы, а не обрезка колонки
	SET @createDateEnd = DATEADD(DAY, 1, CAST(@createDateEnd AS date));

	-- Права на просмотр чужих/групповых акций и видимость по СМИ (как в FirmWithActions1/Actions1)
	declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia)
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit
	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id)
	select * from dbo.[fn_GetUserGroups](@loggedUserID)

    SELECT
        hc.*,
        @userID AS userID,
        @startOfInterval AS startOfInterval,
        @endOfInterval AS endOfInterval,
        @actionID AS actionID,
        @massmediaGroupID AS massmediaGroupID,
        @showDeleted AS showDeleted,
        @isShowActivate AS isShowActivate,
        @isShowNotActivate AS isShowNotActivate
    FROM HeadCompany hc
    WHERE (@headCompanyID IS NULL OR hc.headCompanyID = @headCompanyID)
      AND EXISTS (
        -- Начинаем проверку условий "вглубь"
        SELECT 1
        FROM Firm f
            INNER JOIN Action a ON f.firmID = a.firmID
            INNER JOIN Campaign c ON a.actionID = c.actionID
            INNER JOIN PaymentType pt ON c.paymentTypeID = pt.paymentTypeID
        WHERE f.headCompanyID = hc.headCompanyID
          -- Права на чужие/групповые акции (та же логика, что в FirmWithActions1/Actions1)
          AND (a.userID = @loggedUserID OR @isRightToViewForeignActions = 1
               OR (@isRightToViewGroupActions = 1 AND EXISTS (
                     SELECT 1 FROM GroupMember gm
                     INNER JOIN @ugroups ug ON gm.groupID = ug.id
                     WHERE a.userID = gm.userID
                   )))
          -- Видимость по СМИ пользователя
          AND (
                (c.campaignTypeID <> 4 AND EXISTS (
                    SELECT 1 FROM @massmedias umm
                    WHERE umm.massmediaID = c.massmediaID
                      AND ((a.userID = @loggedUserID AND umm.myMassmedia = 1) OR (a.userID <> @loggedUserID AND umm.foreignMassmedia = 1))
                ))
                OR (c.campaignTypeID = 4 AND EXISTS (
                    SELECT 1 FROM PackModuleIssue pmi
                    INNER JOIN PackModulePriceList pmpl ON pmi.pricelistID = pmpl.priceListID
                    INNER JOIN PackModuleContent pmc ON pmpl.priceListID = pmc.pricelistID
                    INNER JOIN Module m ON pmc.moduleID = m.moduleID
                    INNER JOIN @massmedias umm ON umm.massmediaID = m.massmediaID
                    WHERE pmi.campaignID = c.campaignID
                      AND ((a.userID = @loggedUserID AND umm.myMassmedia = 1) OR (a.userID <> @loggedUserID AND umm.foreignMassmedia = 1))
                ))
          )
          -- Выпуски/окна/модули подключаются только если задан хотя бы один из этих фильтров.
          -- Иначе Issue (миллионы строк) и TariffWindow в план не попадают вовсе.
          AND (
                (@issueDate IS NULL AND @issueDay IS NULL AND @packModuleID IS NULL
                 AND @rollerId IS NULL AND @moduleID IS NULL)
                OR EXISTS (
                    SELECT 1
                    FROM Issue i
                        LEFT JOIN TariffWindow tw ON i.originalWindowID = tw.windowId
                        LEFT JOIN ModuleIssue mi ON i.moduleIssueID = mi.moduleIssueID
                        LEFT JOIN PackModuleIssue pmi ON i.packModuleIssueID = pmi.packModuleIssueID
                        LEFT JOIN PackModulePriceList pmpl ON pmi.pricelistID = pmpl.priceListID
                    WHERE i.campaignID = c.campaignID
                      AND (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
                      AND (@issueDay is null or tw.dayOriginal = @issueDay)
                      AND (@packModuleID is null or pmpl.packModuleID = @packModuleID)
                      AND (@rollerId is null or i.rollerID = @rollerId)
                      AND (@moduleID is null or mi.moduleID = @moduleID)
                )
          )
          -- Сохранена исходная семантика: кампании с finishDate IS NULL не проходят фильтр
          AND ((@campaignFinishDate IS NULL AND c.finishDate IS NOT NULL) OR c.finishDate = @campaignFinishDate)
          AND (@withoutActionsSince is null or not exists(select top 1 a1.actionID
												from [Action] a1
													inner join [Firm] f1 on a1.firmID = f1.firmID
												where f1.headCompanyID = hc.headCompanyID
													and a1.isConfirmed = 1
													and a1.finishDate >= @withoutActionsSince
													and (@startOfInterval is null or a1.startDate < @startOfInterval)))
        and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
          -- Фильтры дат (SARGable)
          AND (@startOfInterval IS NULL OR a.finishDate >= @startOfInterval)
          AND (@endOfInterval IS NULL OR a.startDate <= @endOfInterval)
          AND (@createDateBegin IS NULL OR a.createDate >= @createDateBegin)
          AND (@createDateEnd IS NULL OR a.createDate < @createDateEnd)
		  AND (@changeStartOfInterval IS NULL OR a.modDate >= @changeStartOfInterval)
          AND (@changeEndOfInterval IS NULL OR a.modDate <= @changeEndOfInterval)
          
          -- Фильтры фирмы и действий
          AND (@firmId2 IS NULL OR a.firmID = @firmId2)
          AND (@actionID IS NULL OR a.actionID = @actionID)
          AND (@userID IS NULL OR a.userID = @userID)
          
          -- Белый/Черный нал
          AND ((@showBlack = 1 AND pt.IsHidden = 1) OR (@showWhite = 1 AND pt.IsHidden = 0))
          
          -- Состояние активации/удаления
          AND (
                (@isShowActivate = 0 AND @isShowNotActivate = 0 AND @showDeleted = 0)
                OR (@isShowActivate = 1 AND a.isConfirmed = 1 AND a.deleteDate IS NULL)
                OR (@isShowNotActivate = 1 AND a.isConfirmed = 0 AND a.deleteDate IS NULL)
                OR (@showDeleted = 1 AND a.deleteDate IS NOT NULL)
          )

          -- Фильтры кампании
          AND (@agencyID IS NULL OR c.agencyID = @agencyID)
          AND (@campaignTypeID IS NULL OR c.campaignTypeID = @campaignTypeID)
          AND (@paymentTypeID IS NULL OR c.paymentTypeID = @paymentTypeID)

          -- Сложная логика MassMedia
          AND (
            @massmediaID IS NULL 
            OR (c.campaignTypeID <> 4 AND c.massmediaID = @massmediaID)
            OR (c.campaignTypeID = 4 AND EXISTS (
                -- Проверяем наличие медиа в пакете только если кампания - пакет
                SELECT 1 FROM PackModuleIssue pmi
                INNER JOIN PackModulePriceList pmpl ON pmi.pricelistID = pmpl.priceListID
                INNER JOIN PackModuleContent pmc ON pmpl.priceListID = pmc.pricelistID
                INNER JOIN Module m ON pmc.moduleID = m.moduleID
                WHERE pmi.campaignID = c.campaignID AND m.massmediaID = @massmediaID
            ))
          )

          -- Сложная логика MassMediaGroup
          AND (
            @massmediaGroupID IS NULL
            OR (c.campaignTypeID <> 4 AND EXISTS (SELECT 1 FROM MassMedia mm WHERE mm.massmediaID = c.massmediaID AND mm.massmediaGroupID = @massmediaGroupID))
            OR (c.campaignTypeID = 4 AND EXISTS (
                SELECT 1 FROM PackModuleIssue pmi
                INNER JOIN PackModulePriceList pmpl ON pmi.pricelistID = pmpl.priceListID
                INNER JOIN PackModuleContent pmc ON pmpl.priceListID = pmc.pricelistID
                INNER JOIN Module m ON pmc.moduleID = m.moduleID
                INNER JOIN MassMedia mm2 ON m.massmediaID = mm2.massmediaID
                WHERE pmi.campaignID = c.campaignID AND mm2.massmediaGroupID = @massmediaGroupID
            ))
          )
    )
    ORDER BY hc.name
    -- Catch-all запрос с 28 необязательными параметрами: без RECOMPILE план кэшируется
    -- под первый набор фильтров и потом деградирует на других (таймауты на проде).
    OPTION (RECOMPILE);
END
GO

CREATE OR ALTER PROC [dbo].[FirmWithActions1]
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
		insert into @issues
		select distinct c.actionID 
		from 
			Issue i 
			inner join TariffWindow tw on i.originalWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID
			Inner Join MassMedia mm On mm.massmediaID = tw.massmediaID
			left join ModuleIssue mi on i.moduleIssueID = mi.moduleIssueID
			left join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
			left join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
		where i.rollerID = coalesce(@rollerId, i.rollerID)
			and (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
			and (@issueDay is null or (@issueDay is not null and (tw.dayOriginal = @issueDay)) )
			and (@moduleID is null or mi.moduleID = @moduleID)
			and (@packModuleID is null or pmpl.packModuleID = @packModuleID)
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
			and (@issueDate is null or ((datepart(hh, tw.windowDateActual) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateActual) = datepart(minute, @issueDate))) )
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

CREATE OR ALTER PROCEDURE [dbo].[IssueIUD]
(
@issueID int = NULL OUT,
@rollerID int = NULL,
@rollerDuration smallint = NULL,
@windowID int = NULL,
@tariffWindowPrice decimal(18,2) = NULL,
@campaignID int = NULL,
@issueDate datetime = NULL,
@positionId int = 0,
@ratio decimal(18,10) = 1,
@moduleIssueID int = NULL,
@packModuleIssueID int = NULL,
@isConfirmed bit = NULL,
@loggedUserId smallint,
@massmediaID SMALLINT = NULL,
@actionName varchar(32),
@grantorID SMALLINT = NULL
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
DECLARE	
	@isAdmin bit,	
	@IsTrafficManager bit,	
	@rightForMinus bit, 
	@rightToGoBack bit,
	@campaignTypeID smallint,
	@finishDate datetime,
	@startDate datetime,
	@totalRollersDuration int,
	@timeBonus int,
	@issuesDuration int,
	@extraChargeFirst tinyint,
	@extraChargeSecond tinyint,
	@extraChargeLast tinyint,
	@issuePrice decimal(18,2),
	@deadLine datetime,
	@res smallint,
	@msgError varchar(64),
	@rolActionTypeID TINYINT,
	@actionID int,
	@managerDiscount decimal(18,10),
	@campaignStartDate datetime,
	@campaignFinishDate datetime

If @campaignID Is Null
	Select @campaignID = campaignID From Issue Where issueID = @issueID

EXEC hlp_GetMainUserCredentials
	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT, @grantorID 

SELECT 
	@actionID = c.[actionID],
	@massmediaID = ISNULL(@massmediaID, c.massmediaID),
	@campaignTypeID	= c.campaignTypeID,
	@finishDate = c.finishDate,
	@timeBonus = c.timeBonus,
	@issuesDuration = c.issuesDuration,
	@deadLine = m.deadLine,
	@isConfirmed = a.[isConfirmed],
	@managerDiscount = c.managerDiscount,
	@campaignStartDate = case when coalesce(c.startDate, @issueDate) > @issueDate then @issueDate else coalesce(c.startDate, @issueDate) end,
	@campaignFinishDate = case when coalesce(c.finishDate, @issueDate) < @issueDate then @issueDate else coalesce(c.finishDate, @issueDate) end
FROM 
	Campaign c
	LEFT Join Massmedia m On m.massmediaId = c.massmediaId
	INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
WHERE 
	c.campaignID = @campaignID

If @windowID Is Null and @issueID is not null
	Select @windowID = originalWindowID From Issue Where issueID = @issueID

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

-- Only admin is allowed to delete issue which is in the past already
if @IsConfirmed = 1 and @actionName = 'DeleteItem' and @IsAdmin <> 1 And @IsTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
				where i.issueID = @issueID and tw.dayOriginal <= dbo.ToShortDate(getdate()))
begin
	raiserror('PastIssue', 16, 1)
	return
end

-- только админ может удалять выпуск активированной акции, если траффик-менеджер уже закрыл период
if @IsConfirmed = 1 and @actionName = 'DeleteItem' and @IsAdmin <> 1  And @IsTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
				where i.issueID = @issueID and tw.dayOriginal <= dbo.ToShortDate(@deadLine))
begin
	raiserror('DeadLineViolationDelete', 16, 1)
	return
end 

--select @loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate
if @actionName in ('AddItem', 'UpdateItem') 
	and dbo.[fn_IsAcceptRatioForUser](@loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0
begin 
	 raiserror('CannotChangeCampaignWithMaxDiscount', 16, 1)
	 return
end

-- Длительность ролика и цена окна при добавлении - из базы, а не от клиента: в открытой
-- давно форме кампании они могли устареть, и цена с проверкой окна разошлись бы с
-- занятостью окна, которую процедура всё равно считает по Roller.duration.
if @actionName = 'AddItem'
begin
	SELECT @rollerDuration = [duration] FROM [Roller] WHERE [rollerID] = @rollerID
	SELECT @tariffWindowPrice = [price] FROM [TariffWindow] WHERE [windowId] = @windowID
end

-- Ролик выпуска здесь не меняется: цена и проверка окна в ветке UpdateItem считаются по
-- ролику выпуска. Замена ролика - RollerSubstitute.
if @actionName = 'UpdateItem' and @rollerID is not null
	and not exists (select 1 from [Issue] where [issueID] = @issueID and [rollerID] = @rollerID)
begin
	raiserror('InternalError', 16, 1)
	return
end

if @actionName in ('AddItem', 'UpdateItem')
	SELECT @rolActionTypeID = [rolActionTypeID] FROM [Roller] WHERE [rollerID] = @rollerID

-- Нельзя смешивать политическую агитацию (тип 6) с другой рекламой в одной акции:
-- отчётность перед избиркомом ведётся отдельно по каждому кандидату
if @actionName in ('AddItem', 'UpdateItem') and @rolActionTypeID is not null
	and ((@rolActionTypeID = 6 and exists (
			select 1
			from Issue i
				inner join Campaign c on c.campaignID = i.campaignID
				inner join Roller r on r.rollerID = i.rollerID
			where c.actionID = @actionID
				and r.rolActionTypeID not in (6, 7, 44, 55)
				and i.issueID != IsNull(@issueID, -1)
		))
		or (@rolActionTypeID not in (6, 7, 44, 55) and exists (
			select 1
			from Issue i
				inner join Campaign c on c.campaignID = i.campaignID
				inner join Roller r on r.rollerID = i.rollerID
			where c.actionID = @actionID
				and r.rolActionTypeID = 6
				and i.issueID != IsNull(@issueID, -1)
		)))
begin
	raiserror('AgitationMixError', 16, 1)
	return
end

if (@actionName in ('DeleteItem', 'UpdateItem'))
BEGIN
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When i.isConfirmed = 1 AND [maxCapacity] = 0 
					Then timeInUseConfirmed - r.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When i.isConfirmed = 0 AND [maxCapacity] = 0
					Then timeInUseUnconfirmed - r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When (i.isConfirmed = 1 AND [maxCapacity] > 0) 
					Then capacityInUseConfirmed - 1
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When (i.isConfirmed = 0 AND [maxCapacity] > 0)
					Then capacityInUseUnconfirmed - 1
				Else capacityInUseUnconfirmed
			End,
		isFirstPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = -20 Then 0
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = -10 Then 0
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = 10 Then 0
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = 
			Case  
				When i.isConfirmed = 0 And i.positionId = -20 Then firstPositionsUnconfirmed - 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed = 
			Case 
				When i.isConfirmed = 0 And i.positionId = -10 Then secondPositionsUnconfirmed - 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed = 
			Case	
				When i.isConfirmed = 0 And i.positionId = 10 Then lastPositionsUnconfirmed - 1
				Else	lastPositionsUnconfirmed
			End
	From
		Issue i 
		Inner Join Roller r On r.rollerId = i.rollerId
	Where
		TariffWindow.windowId = i.actualWindowId
		and i.issueID = @issueID
END

IF @actionName = 'AddItem' BEGIN
	If Exists (Select 1 From Roller where rollerID = @rollerID And advertTypeID Is Null And @isConfirmed = 1) Begin
		RAISERROR('RollerWithoutAdvertType', 16, 1)
		RETURN 
	End

	--select 1, @issueDate

	Exec @res = hlp_IssueVerify	
		Null,
		@actionName, @massmediaID, @deadLine, @windowID, @issueDate, @rollerDuration,
		@rightToGoBack,	@isAdmin, @IsTrafficManager, @rightForMinus, @finishDate,
		@campaignTypeID, @isConfirmed, @positionId, @timeBonus,
		@issuesDuration, NULL, @rolActionTypeID, @msgError out
	IF @res = 1 BEGIN
		RAISERROR(@msgError, 16, 1)
		RETURN 
	END

	-- нельзя добавить несколько 'первых' роликов в окно в рамках одной акции, даже если 
	-- это макет. Такую акцию потом не активировать без ошибок
	If @positionId <> 0 And Exists (
		Select 1 
		From 
			Issue i Inner Join Campaign c On c.campaignID = i.campaignID
		Where
			i.originalWindowID = @windowID
			And i.positionId = @positionId
			And c.actionID = @actionID
		)
		Begin
			RAISERROR('PositionErrorForTheSameAction', 16, 1)
			RETURN 
		End
	
	SELECT @issuePrice = dbo.fn_GetIssuePrice(
		@rollerDuration, @tariffWindowPrice, 1, @positionId, @extraChargeFirst, 
		@extraChargeSecond, @extraChargeLast)

	declare @activationdate datetime 
	if @isConfirmed = 1
		set @activationdate = getdate()
	else 
		set @activationdate = null

	-- Add issue
	INSERT INTO [Issue](rollerID, actualWindowID, originalWindowId, campaignID, positionId, ratio, moduleIssueID, [packModuleIssueID], isConfirmed, [tariffPrice], grantorID, activationDate)
	VALUES(@rollerID, @windowID, @windowID, @campaignID, @positionId, @ratio, @moduleIssueID, @packModuleIssueID, @isConfirmed, @issuePrice, @grantorID, @activationDate)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return
	end

	SET @issueID = SCOPE_IDENTITY()

	-- Политическая агитация: размещение в уже активированной акции создаёт сразу
	-- подтверждённый выпуск минуя ActionActivate - обвязку добавляем здесь.
	-- В черновике обвязка не нужна: её создаст активация.
	IF @rolActionTypeID = 6 AND @isConfirmed = 1
		EXEC AgitationFraming
			@actionName = 'InsertForWindow',
			@windowID = @windowID,
			@loggedUserID = @loggedUserId
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	-- Политическая агитация: до удаления запоминаем тип ролика и окно, чтобы после
	-- удаления последнего типа 6 снять авто-обвязку (44/7/55) служебной акции
	DECLARE @delRolActionTypeID tinyint, @delActualWindowID int
	SELECT @delRolActionTypeID = r.rolActionTypeID, @delActualWindowID = i.actualWindowID
	FROM Issue i INNER JOIN Roller r ON r.rollerID = i.rollerID
	WHERE i.issueID = @issueID

	IF (@isConfirmed = 1)
	BEGIN
		If @rollerID Is Null
			Select @rollerID = rollerID From Issue Where issueID = @issueID

		DECLARE @actualDate DATETIME
		SELECT @actualDate = tw.windowDateOriginal FROM [Issue] i inner join TariffWindow tw on i.originalWindowID = tw.windowId WHERE i.[issueID] = @issueID
		EXEC [LogDeletedIssueInsert] @loggedUserId, @actionId, @rollerID, @actualDate, @massmediaID
		
		if exists(SELECT * FROM [Issue] i inner join TariffWindow tw on i.actualWindowID = tw.windowId 
		and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog() 
		WHERE i.[issueID] = @issueID) 
		begin 
			exec SayAdminThatIssuesDelete @loggedUserID, @actionID
		end 
	END 
	
	DELETE FROM [Issue] WHERE IssueID = @IssueID

	IF @delRolActionTypeID = 6
		EXEC AgitationFraming
			@actionName = 'CleanupWindow',
			@windowID = @delActualWindowID,
			@loggedUserID = @loggedUserId
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	DECLARE @oldPositionID INT 
	SELECT 
		@windowID = CASE WHEN @windowID IS NULL THEN i.[actualWindowID] ELSE @windowID END,
		@rollerID = CASE WHEN @rollerID IS NULL THEN i.rollerID ELSE @rollerID END,
		@campaignID = CASE WHEN @campaignID IS NULL THEN i.campaignID ELSE @campaignID END,
		@positionId = CASE WHEN @positionId IS NULL THEN i.[positionId] ELSE @positionId END,
		@ratio = CASE WHEN @ratio IS NULL THEN i.ratio ELSE @ratio END,
		@oldPositionID = i.[positionId],
		@tariffWindowPrice = tw.[price],
		@rollerDuration = r.[duration],
		@ratio = i.[ratio],
		@issuePrice = i.[tariffPrice],
		@issuesDuration = @issuesDuration -
			case 
				when @campaignTypeID = 2 then dbo.f_GetSponsorDuration(r.[duration], i.[positionId], @extraChargeFirst, @extraChargeSecond, @extraChargeLast)
					else @rollerDuration 
			end
	FROM [Issue] i 
		inner join Roller r on i.rollerID = r.rollerID
		INNER JOIN [TariffWindow] tw ON i.[actualWindowID] = tw.[windowId]
	WHERE 
		i.[issueID] = @issueID	

	-- @rollerID мог прийти пустым и быть взят из выпуска выше: тип ролика - по нему
	SELECT @rolActionTypeID = [rolActionTypeID] FROM [Roller] WHERE [rollerID] = @rollerID
	
	Exec @res = hlp_IssueVerify	
		@issueID,
		@actionName, @massmediaID, @deadLine, @windowID, @issueDate, @rollerDuration,
		@rightToGoBack,	@isAdmin, @IsTrafficManager, @rightForMinus, @finishDate,
		@campaignTypeID, @isConfirmed, @positionId, @timeBonus,
		@issuesDuration, NULL, @rolActionTypeID, @msgError out
	IF @res = 1 BEGIN
		RAISERROR(@msgError, 16, 1)
		RETURN 
	end
	
	DECLARE @tariffPrice decimal(18,2)
	
	IF @oldPositionID != @positionId
		SELECT @issuePrice = dbo.fn_GetIssuePrice(@rollerDuration, @tariffWindowPrice, 1, @positionId, @extraChargeFirst, @extraChargeSecond, @extraChargeLast)

	UPDATE	
		[Issue]
	SET	
		rollerID = @rollerID, 
		actualWindowID = @windowID, 
		positionId = @positionId, 
		ratio = @ratio,
		[tariffPrice] = @issuePrice
	WHERE		
		IssueID = @IssueID
end

if (@actionName in ('AddItem', 'UpdateItem'))
BEGIN
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When i.isConfirmed = 1 AND [maxCapacity] = 0 
					Then timeInUseConfirmed + r.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When i.isConfirmed = 0 AND [maxCapacity] = 0 
					Then timeInUseUnconfirmed + r.duration
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When (i.isConfirmed = 1 AND [maxCapacity] > 0) 
					Then capacityInUseConfirmed + 1
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When (i.isConfirmed = 0 AND [maxCapacity] > 0)
					Then capacityInUseUnconfirmed + 1
				Else capacityInUseUnconfirmed
			End,
		isFirstPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = -20 Then 1
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = -10 Then 1
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When i.isConfirmed = 1 And i.positionId = 10 Then 1
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = 
			Case  
				When i.isConfirmed = 0 And i.positionId = -20 Then firstPositionsUnconfirmed + 1
				Else firstPositionsUnconfirmed
			End,
		secondPositionsUnconfirmed = 
			Case 
				When i.isConfirmed = 0 And i.positionId = -10 Then secondPositionsUnconfirmed + 1
				Else secondPositionsUnconfirmed
			End,
		lastPositionsUnconfirmed = 
			Case	
				When i.isConfirmed = 0 And i.positionId = 10 Then lastPositionsUnconfirmed + 1
				Else	lastPositionsUnconfirmed
			End
	From
		Issue i
		Inner Join Roller r On r.rollerId = i.rollerId
	Where
		TariffWindow.windowId = i.actualWindowId
		and i.issueID = @issueID
END
GO

CREATE OR ALTER PROCEDURE [dbo].[CampaignsIssueDelete]
(
	@campaignID INT,
	@rollerID INT = NULL,
	@actionName VARCHAR(32),
	@issueDate DATETIME = NULL,
	@loggedUserId SMALLINT,
	@massmediaID smallint
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	if @actionName <> 'DeleteItem'
		return 
	
	declare @issues table (issueID int primary key)
	declare 
		@actionID int,
		@IsConfirmed bit,
		@deadLine datetime,
		@IsAdmin bit,
		@IsTrafficManager bit
	Set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
	Set @IsTrafficManager = dbo.f_IsTrafficManager(@loggedUserID)

	select @deadLine = deadLine from MassMedia where massmediaID = @massmediaID
	
	select @actionID = c.actionID, @IsConfirmed = a.isConfirmed
	from Campaign c inner join Action a on a.actionID = c.actionID
	where c.campaignID = @campaignID

	insert into @issues 
	select i.issueID
	from Issue i
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	where i.campaignID = @campaignID 
		and tw.massmediaID = @massmediaID 
		and i.rollerID = coalesce(@rollerID, i.rollerID)
		and (@issueDate is null or tw.dayOriginal = Convert(datetime, Convert(varchar(8), @issueDate, 112), 112))

	if @IsConfirmed = 1 and @IsAdmin  = 0 And @IsTrafficManager = 0
		and exists(select * from @issues it 
					inner join Issue i on it.issueID = i.issueID 
					inner join TariffWindow tw on i.actualWindowID = tw.windowId
							and tw.dayOriginal <= dbo.ToShortDate(getdate()))
	begin 
		raiserror('PastIssue', 16, 1)
		return
	end

	if @IsConfirmed = 1 and @IsAdmin  = 0 And @IsTrafficManager = 0
		and exists(select * from @issues it 
					inner join Issue i on it.issueID = i.issueID 
					inner join TariffWindow tw on i.actualWindowID = tw.windowId
							and tw.dayOriginal <= dbo.ToShortDate(@deadLine))
	begin 
		raiserror('DeadLineViolationDelete', 16, 1)
		return
	end

	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then timeInUseConfirmed - t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.durationU
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed - t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssuesU
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 0
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 0
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 0
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCountU,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCountU,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCountU
	from
		(select i.actualWindowID as windowID, 
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as duration, 
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durationU, 
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as countIssues,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as countIssuesU,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end, 0)) as lastCount,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end, 0)) as firstCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end, 0)) as secondCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end, 0)) as lastCountU
		 from 
			@issues i0 
			inner join Issue i on i0.issueID = i.issueID
			Inner Join Roller r On r.rollerId = i.rollerId
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID
		
	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, @actionID, i.rollerID, tw.windowDateOriginal, tw.massmediaID 
	from @issues it 
		inner join Issue i on it.issueID = i.issueID 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	where i.isConfirmed = 1
	
	if exists(select *
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
		where i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 
	
	declare @pissues table(issueID int primary key)
	declare @missues table(issueID int primary key)
	
	insert into @pissues 	
	select pmi.packModuleIssueID 
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join PackModuleIssue pmi on pmi.packModuleIssueID = i.packModuleIssueID 
			inner join Issue ii on pmi.packModuleIssueID = ii.packModuleIssueID 
		group by pmi.packModuleIssueID 
		having count(distinct i.issueID) = count(distinct ii.issueID)
										
	insert into @missues 
	select pmi.moduleIssueID 
		from @issues it 
			inner join Issue i on it.issueID = i.issueID 
			inner join ModuleIssue pmi on pmi.moduleIssueID = i.moduleIssueID 
			inner join Issue ii on pmi.moduleIssueID = ii.moduleIssueID 
		group by pmi.moduleIssueID 
		having count(distinct i.issueID) = count(distinct ii.issueID)

	-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
	-- окна с подтверждённой агитацией запоминаем до удаления и снимаем обвязку после
	declare @agitWindows table (windowID int primary key)
	insert into @agitWindows (windowID)
	select distinct i.actualWindowID
	from @issues it
		inner join Issue i on it.issueID = i.issueID
		inner join Roller r on r.rollerID = i.rollerID
	where r.rolActionTypeID = 6 and i.isConfirmed = 1

	delete from i from @issues it inner join Issue i on it.issueID = i.issueID
	delete from i from @missues it inner join ModuleIssue i on it.issueID = i.moduleIssueID
	delete from i from @pissues it inner join PackModuleIssue i on it.issueID = i.packModuleIssueID

	declare @agitWindowID int
	declare cur_agit_del cursor local for select windowID from @agitWindows

	open cur_agit_del
	fetch next from cur_agit_del into @agitWindowID
	while @@FETCH_STATUS = 0
	begin
		exec AgitationFraming
			@actionName = 'CleanupWindow',
			@windowID = @agitWindowID,
			@loggedUserID = @loggedUserId

		fetch next from cur_agit_del into @agitWindowID
	end
	close cur_agit_del
	deallocate cur_agit_del
end
GO

CREATE OR ALTER PROCEDURE [dbo].[CampaignIUD]
(
@campaignID int OUT,
@actionID int = NULL,
@campaignTypeID tinyint = NULL,
@massmediaID smallint = NULL,
@paymentTypeID smallint = NULL,
@agencyID smallint = NULL,
@loggedUserId smallint,
@actionName varchar(32),
@needShow bit = 1,
@managerDiscount decimal(18, 10) = 1
)
WITH EXECUTE AS OWNER
as
set nocount on

declare @IsAdmin bit, @IsTraffic bit
set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @IsTraffic = dbo.f_IsTrafficManager(@loggedUserID)

-- Only admin is allowed to delete issue which is in the past already
if @actionName = 'DeleteItem' and @IsAdmin <> 1 and @IsTraffic <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
				where i.campaignID = @campaignID and tw.dayOriginal <= dbo.ToShortDate(getdate()))
begin
	raiserror('PastIssue', 16, 1)
	return
end 

if @actionID is null and @campaignID is not null
	select @actionID = c.actionID from Campaign c where c.campaignID = @campaignID

IF @actionName = 'AddItem' begin
	if @agencyID is null 
	begin 
		raiserror('CannotAddCampaignWithoutAgency', 16, 1)
		return
	end

	if @IsAdmin = 0 and @IsTraffic = 0 and exists(select * 
				from [Action] a 
					inner join Campaign c on a.actionID = c.actionID
				where a.actionID = @actionID and a.finishDate < getdate() and a.isConfirmed = 1)
	begin 
		raiserror('CannotAddCampaignInConfirmedFinishedAction', 16, 1)
		return 
	end 

	select @managerDiscount = [dbo].[fn_GetMaxUserDiscount](@loggedUserId, GETDATE(), [dbo].[GetMaxDate]())

	INSERT INTO [Campaign](actionID, campaignTypeID, massmediaID, paymentTypeID, agencyID, modUser, managerDiscount)
	VALUES(@actionID, @campaignTypeID, @massmediaID, @paymentTypeID, @agencyID, @loggedUserId, @managerDiscount)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @campaignID = SCOPE_IDENTITY()

	if @needShow = 1
		EXEC Campaigns @CampaignID = @CampaignID, @actionID = @actionID
END
ELSE IF @actionName = 'DeleteItem' begin
	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 
					Then timeInUseConfirmed - t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.durationU
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed - t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssuesU
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 0
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 0
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 0
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCountU,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCountU,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCountU
	From
		(select i.actualWindowID as windowID, 
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as duration, 
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durationU, 
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as countIssues,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as countIssuesU,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end, 0)) as lastCount,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end, 0)) as firstCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end, 0)) as secondCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end, 0)) as lastCountU
		 from 
			Issue i 
			Inner Join Roller r On r.rollerId = i.rollerId
		where i.campaignID = @campaignID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, @actionID, i.rollerID, tw.windowDateOriginal, tw.massmediaID 
	from Issue i 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
	where i.campaignID = @CampaignID and i.isConfirmed = 1

	if exists(select *
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
		where i.campaignID = @CampaignID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 

	-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
	-- окна с агитацией запоминаем до удаления и снимаем обвязку после
	DECLARE @agitWindows table (windowID int primary key)
	INSERT INTO @agitWindows (windowID)
	SELECT DISTINCT i.actualWindowID
	FROM Issue i INNER JOIN Roller r ON r.rollerID = i.rollerID
	WHERE i.campaignID = @CampaignID AND r.rolActionTypeID = 6 AND i.isConfirmed = 1

	DELETE Issue WHERE CampaignID = @CampaignID
	DELETE FROM [Campaign] WHERE CampaignID = @CampaignID

	DECLARE @agitWindowID int
	DECLARE cur_agit_camp CURSOR LOCAL FOR SELECT windowID FROM @agitWindows

	OPEN cur_agit_camp
	FETCH NEXT FROM cur_agit_camp INTO @agitWindowID
	WHILE @@FETCH_STATUS = 0
	BEGIN
		EXEC AgitationFraming
			@actionName = 'CleanupWindow',
			@windowID = @agitWindowID,
			@loggedUserID = @loggedUserID

		FETCH NEXT FROM cur_agit_camp INTO @agitWindowID
	END
	CLOSE cur_agit_camp
	DEALLOCATE cur_agit_camp
END
ELSE IF @actionName = 'UpdateItem' BEGIN

    -- Read current values before update
    DECLARE @currentAgencyID     SMALLINT;
    DECLARE @currentPaymentTypeID SMALLINT;

    SELECT
        @currentAgencyID      = agencyID,
        @currentPaymentTypeID = paymentTypeID
    FROM dbo.Campaign
    WHERE CampaignID = @CampaignID;

    -- Check agencyID change: block if a linked payment belongs to the old agency
    IF @currentAgencyID <> @agencyID
    BEGIN
        DECLARE @conflictingPaymentID INT;

        SELECT TOP 1 @conflictingPaymentID = pa.paymentID
        FROM dbo.Campaign c
        INNER JOIN dbo.PaymentAction pa ON pa.actionID  = c.actionID
        INNER JOIN dbo.Payment p        ON p.paymentID  = pa.paymentID
        WHERE c.CampaignID = @CampaignID
          AND p.agencyID   = @currentAgencyID
          AND p.isEnabled  = 1;

        IF @conflictingPaymentID IS NOT NULL
            RAISERROR('CannotChangeAgency_PaymentExists', 16, 1);
    END

    -- Check paymentTypeID change: block if a linked payment has the old paymentTypeID
    IF @currentPaymentTypeID <> @paymentTypeID
    BEGIN
        DECLARE @conflictingPaymentID2 INT;

        SELECT TOP 1 @conflictingPaymentID2 = pa.paymentID
        FROM dbo.Campaign c
        INNER JOIN dbo.PaymentAction pa ON pa.actionID      = c.actionID
        INNER JOIN dbo.Payment p        ON p.paymentID      = pa.paymentID
        WHERE c.CampaignID        = @CampaignID
          AND p.paymentTypeID     = @currentPaymentTypeID
          AND p.isEnabled         = 1;

        IF @conflictingPaymentID2 IS NOT NULL
            RAISERROR('CannotChangePaymentType_PaymentExists', 16, 1);
    END

    UPDATE dbo.Campaign
    SET
        paymentTypeID = @paymentTypeID,
        agencyID      = @agencyID,
        modUser       = @loggedUserId,
        actionID      = @actionID
    WHERE CampaignID = @CampaignID;

    IF @needShow = 1
        EXEC Campaigns @CampaignID = @CampaignID, @actionID = @actionID;
END
GO

CREATE OR ALTER PROCEDURE [dbo].[ActionIUD]
(
@actionID int = NULL,
@firmID smallint = NULL,
@userID smallint = NULL,
@newCreatorID smallint = NULL,
@isConfirmed bit = NULL,
@actionName varchar(32),
@loggedUserID smallint
)
WITH EXECUTE AS OWNER
as
SET NOCOUNT on

declare @IsAdmin bit, @isTrafficManager bit

set @IsAdmin = dbo.f_IsAdmin(@loggedUserID)
set @isTrafficManager = [dbo].[f_IsTrafficManager](@loggedUserID)

-- Only admin is allowed to delete ACTIVATED issue which is in the past already
if @actionName = 'DeleteItem' and @IsAdmin <> 1 and @isTrafficManager <> 1
	and exists(select * 
				from TariffWindow tw 
					inner join Issue i on tw.windowId = i.actualWindowID 
					inner join Campaign c on i.campaignID = c.campaignID
				where c.actionID = @actionID and tw.dayOriginal <= dbo.ToShortDate(getdate()) and i.isConfirmed = 1)
begin
	raiserror('PastIssue', 16, 1)
	return
end 

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [Action](firmID, userID, isConfirmed)
	VALUES(@firmID, @userID, @isConfirmed)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @actionID = SCOPE_IDENTITY()

	EXEC Actions1 @actionID = @actionID
END
ELSE IF @actionName = 'DeleteItem' 
	begin
	-- если акция уже находится в журнале удалённых акций, то удалять по настоящему
	If Exists (Select 1 From [Action] WHERE actionID = @actionID And Not deleteDate Is Null)
		Begin

		-- Политическая агитация: выпуски удаляются массово, минуя IssueIUD, поэтому
		-- окна с агитацией запоминаем до удаления и снимаем обвязку после
		Declare @agitWindows table (windowID int primary key)
		Insert Into @agitWindows (windowID)
		Select Distinct i.actualWindowID
		From Issue i
			Inner Join Campaign c On c.campaignID = i.campaignID
			Inner Join Roller r On r.rollerID = i.rollerID
		Where c.actionID = @actionID And r.rolActionTypeID = 6 And i.isConfirmed = 1

		DELETE Issue FROM Campaign c
		WHERE c.campaignID = Issue.campaignID AND c.actionID = @actionID

		DELETE FROM [Action] WHERE actionID = @actionID

		Declare @agitWindowID int
		Declare cur_agit_act Cursor Local For Select windowID From @agitWindows

		Open cur_agit_act
		Fetch Next From cur_agit_act Into @agitWindowID
		While @@FETCH_STATUS = 0
		Begin
			Exec AgitationFraming
				@actionName = 'CleanupWindow',
				@windowID = @agitWindowID,
				@loggedUserID = @loggedUserID

			Fetch Next From cur_agit_act Into @agitWindowID
		End
		Close cur_agit_act
		Deallocate cur_agit_act

		Return

		End
	
	Select @isConfirmed = IsConfirmed From Action Where actionID = @actionID
	If @isConfirmed = 1	Begin
		Exec [ActionDeactivate] @actionID = @actionID, @loggedUserID = @loggedUserID
	End

	Update [Action] Set deleteDate = GETDATE() WHERE actionID = @actionID

	Update 
		TariffWindow
	Set
		timeInUseConfirmed = 
			Case 
				When [maxCapacity] = 0 Then timeInUseConfirmed - t1.duration
				Else timeInUseConfirmed
			End,
		timeInUseUnconfirmed = 
			Case 
				When [maxCapacity] = 0
					Then timeInUseUnconfirmed - t1.durationU
				Else timeInUseUnconfirmed
			End,
		capacityInUseConfirmed = 
			Case 
				When ([maxCapacity] > 0) 
					Then capacityInUseConfirmed - t1.countIssues
				Else capacityInUseConfirmed
			End,
		capacityInUseUnconfirmed = 
			Case  
				When ([maxCapacity] > 0)
					Then capacityInUseUnconfirmed - t1.countIssuesU
				Else capacityInUseUnconfirmed
			end,
		isFirstPositionOccupied = 
			Case 
				When firstCount > 0 Then 0
				Else isFirstPositionOccupied
			End,
		isSecondPositionOccupied = 
			Case 
				When secondCount > 0 Then 0
				Else isSecondPositionOccupied
			End,
		isLastPositionOccupied = 
			Case 
				When lastCount > 0 Then 0
				Else isLastPositionOccupied
			End,
		firstPositionsUnconfirmed = firstPositionsUnconfirmed - firstCountU,
		secondPositionsUnconfirmed = secondPositionsUnconfirmed - secondCountU,
		lastPositionsUnconfirmed = lastPositionsUnconfirmed - lastCountU
	From
		(select i.actualWindowID as windowID, 
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as duration, 
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durationU, 
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as countIssues,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as countIssuesU,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end, 0)) as firstCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end, 0)) as secondCount,
			sum(coalesce(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end, 0)) as lastCount,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end, 0)) as firstCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end, 0)) as secondCountU,
			sum(coalesce(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end, 0)) as lastCountU
		 from 
			Issue i 
			Inner Join Roller r On r.rollerId = i.rollerId
			inner join Campaign c on i.campaignID = c.campaignID
		where c.actionID = @actionID
		group by i.actualWindowID ) as t1
	Where
		TariffWindow.windowId = t1.windowID

	insert into [LogDeletedIssue] ([userId],actionID,rollerId, issueDate, massmediaID) 
	select @loggedUserID, c.actionID, i.rollerID, tw.windowDateOriginal, tw.massmediaID 
	from Issue i 
		inner join TariffWindow tw on i.originalWindowID = tw.windowId
		inner join Campaign c on i.campaignID = c.campaignID 
	where c.actionID = @actionID and i.isConfirmed = 1

	if exists(select *
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
			inner join Campaign c on i.campaignID = c.campaignID 
		where c.actionID = @actionID and i.isConfirmed = 1 and datediff(day,dbo.ToShortDate(getdate()),tw.dayOriginal) <= dbo.f_SysParamsDaysLog())
	begin 
		exec SayAdminThatIssuesDelete @loggedUserID, @actionID
	end 
END
ELSE IF @actionName = 'UpdateItem' 
	BEGIN
	DECLARE @oldFirmId INT, @oldIsConfirmed BIT 
	SELECT @oldFirmId = a.[firmID], @oldIsConfirmed = a.isConfirmed 
	FROM [Action] a 
	WHERE a.[actionID] = @actionID
	
	IF (@oldFirmId <> @firmID)
		DELETE FROM [PaymentAction] WHERE [actionID] = @actionID

	IF (@oldIsConfirmed = 1 
		AND @isConfirmed = 0 AND EXISTS(SELECT * FROM dbo.[PaymentAction] pa WHERE pa.actionID = @actionID))
	BEGIN
		RAISERROR('ActionWithPaymentsCannotDeactivate',16,1)
		RETURN
	END

	UPDATE	
		[Action]
	SET			
		firmID = @firmID,
		userID = IsNull(@newCreatorID, @userID),
		isConfirmed = @isConfirmed
	WHERE		
		actionID = @actionID

	EXEC Actions1 @actionID = @actionID
END
GO

-- Проверка: новые версии применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.HeadCompaniesWithActions')) LIKE N'%datepart(hh, tw.windowDateActual)%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.FirmWithActions1')) LIKE N'%datepart(hh, tw.windowDateActual)%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.Actions1')) LIKE N'%datepart(hh, tw.windowDateActual)%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.IssueIUD')) LIKE N'%inner join Issue i on tw.windowId = i.actualWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsIssueDelete')) LIKE N'%inner join TariffWindow tw on i.actualWindowID = tw.windowId%and tw.dayOriginal <= dbo.ToShortDate(getdate())%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignIUD')) LIKE N'%inner join Issue i on tw.windowId = i.actualWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionIUD')) LIKE N'%inner join Issue i on tw.windowId = i.actualWindowID%'
    PRINT N'ГОТОВО: журналы акций — время по факту; права на удаление и уведомление — по окну выхода (7 процедур).';
ELSE
    RAISERROR(N'24: новая версия применена не ко всем процедурам.', 16, 1);
GO
