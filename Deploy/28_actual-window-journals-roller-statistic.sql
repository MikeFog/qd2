-- Фактическое окно выпуска, этап 6 (docs/tasks/window-actual-switch.md §5.1, §5.5 п. 1). С удалением
-- broadcastStart не пересекается.
--
-- 1. Журналы акций (подтверждённые, макеты, удалённые), все три уровня дерева — HeadCompaniesWithActions,
--    FirmWithActions1, Actions1: отбор «День выпуска» и «Время рекламного выпуска» ищет выпуск в окне выхода
--    (Issue.actualWindowID), а не в окне постановки. Время окна — фактическое с Deploy/24. Пример на ArtvisDev:
--    выпуск 41707758 перенесён трафиком 14.10.2026 08:45 → 09:45 — акция 186073 находится по 09:45, а не по 08:45.
-- 2. Журнал использования роликов — stat_RollerStatistic и ActionsForRollerStatistic (вместе): выпуск
--    считается в дне окна выхода.
--
-- Только SQL, клиент не нужен (программа и веб). Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 28_actual-window-journals-roller-statistic.sql

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
                        LEFT JOIN TariffWindow tw ON i.actualWindowID = tw.windowId
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
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
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
			inner join TariffWindow tw on i.actualWindowID = tw.windowId
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

CREATE OR ALTER PROC [dbo].[stat_RollerStatistic]
(
@startDate datetime,
@finishDate datetime,
@splitByManager bit = 0,
@splitByDays bit = 0,
@massmediaString varchar(8000),
@userID smallint = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint,
@firmID int = null,
@headCompanyID int = null,
@advertTypeID int = null
)
AS
SET NOCOUNT ON

Declare @rollerStat TABLE(
rollerID int, 
date datetime, 
userID smallint, 
firmID smallint)


declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select x.massmediaID, x.myMassmedia, x.foreignMassmedia 
from dbo.fn_GetMassmediasForUser(@loggedUserID) x
	inner join dbo.fn_CreateTableFromString(@massmediaString) y on x.massmediaID = y.ID

Declare @res1 TABLE(
rollerID int, 
date datetime, 
userID smallint, 
firmID smallint,
massmediaID smallint)

Insert Into @res1(rollerID, date, userID, firmID, massmediaID)
Select 
	i.rollerID,
	tw.dayOriginal,
	a.userID,
	a.firmID,
	tw.massmediaID
FROM 
	Issue i
	inner join TariffWindow tw on i.actualWindowID = tw.windowID
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	INNER JOIN [Action] a ON a.actionID = c.actionID
	INNER JOIN Firm f ON f.firmID = a.firmID
	INNER JOIN PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
	INNER JOIN Roller r On r.rollerID = i.rollerID
	LEFT JOIN AdvertType adt on adt.advertTypeID = r.advertTypeID
	inner join 
	(
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id	
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) as x on a.userID = x.userID
	--inner join @massmedias mm on tw.massmediaID = mm.massmediaID
where
	--(a.userID = @loggedUserID and mm.myMassmedia = 1 
	--	or a.userID <> @loggedUserID and mm.foreignMassmedia = 1) and
	tw.dayOriginal between @startDate and @finishDate and 
	a.userID = Coalesce(@userID, a.userID) 
	AND a.[isConfirmed] = 1
	and a.firmID = coalesce(@firmID, a.firmID)
	and f.headCompanyID = coalesce(@headCompanyID, f.headCompanyID)
	and (@advertTypeID Is Null Or r.advertTypeID = @advertTypeID Or adt.parentID = @advertTypeID)
	AND (pt.isHidden = 0 or @isHideWhite = 0) And
		(pt.isHidden = 1 or @isHideBlack = 0) and
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1)) 
group by i.rollerID, tw.dayOriginal, a.userID, a.firmID, tw.massmediaID, i.issueID

--for optimize
insert into @rollerStat
        ( rollerID, date, userID, firmID )
select r.rollerID, r.date, r.userID, r.firmID from @res1 r 
	inner join @massmedias mm on r.massmediaID = mm.massmediaID
where (r.userID = @loggedUserID and mm.myMassmedia = 1 
		or r.userID <> @loggedUserID and mm.foreignMassmedia = 1)
	
IF @splitByManager = 0
	select
		row_number() over(order by quantity desc) as RowNum,
		rollerID,
		rollerDescription,
		quantity,
		firmDescription,
		firmID,
		dbo.fn_ProductListByRollerId(rollerID) as productList,
		compositionAuthor,
		compositionName,
		duration,
		dbo.fn_Int2Time(duration) as durationString,
		dbo.fn_Int2Time(quantity * duration) as sumDuration,
		advertTypeName,
		isCommon,
		isMute,
		parentID
	FROM (
		SELECT 
			r.rollerID,
			r.name as rollerDescription,
			count(*) as quantity,
			f.name as firmDescription,
			f.firmID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name as advertTypeName,
			r.isCommon,
			r.isMute,
			r.parentID
		FROM 
			@rollerStat rs
			INNER JOIN Roller r ON r.rollerID = rs.rollerID
			INNER JOIN Firm f ON f.firmID = rs.firmID
			LEFT JOIN AdvertType at ON r.advertTypeID = at.advertTypeID
		GROUP BY
			r.rollerID,
			r.name,
			f.name,
			f.firmID,
			r.duration,
			r.compositionAuthor,
			r.compositionName,
			at.name,
			r.isCommon,
			r.isMute,
			r.parentID
	) ss
	ORDER BY 
		quantity desc
ELSE
	select
		row_number() over(order by quantity desc) as RowNum,
		rollerID,
		rollerDescription,
		quantity,
		firmDescription,
		firmID,
		dbo.fn_ProductListByRollerId(rollerID) as productList,
		userName,
		userID,
		compositionAuthor,
		compositionName,
		duration,
		dbo.fn_Int2Time(duration) as durationString,
		dbo.fn_Int2Time(quantity * duration) as sumDuration,
		advertTypeName,
		isCommon,
		isMute,
		parentID
	FROM (
		SELECT 
			r.rollerID,
			r.name as rollerDescription,
			count(*) as quantity,
			f.firmID,
			f.name as firmDescription,
			u.lastName + Space(1) + u.firstName as userName,
			u.userID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name as advertTypeName,
			r.isCommon,
			r.isMute,
			r.parentID
		FROM 
			@rollerStat rs
			INNER JOIN Roller r ON r.rollerID = rs.rollerID
			INNER JOIN [User] u ON rs.userID = u.userID
			INNER JOIN Firm f ON f.firmID = rs.firmID
			LEFT JOIN AdvertType at ON r.advertTypeID = at.advertTypeID
		GROUP BY
			r.rollerID,
			r.name,
			f.name,
			f.firmID,
			u.lastName + Space(1) + u.firstName,
			u.userID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name,
			r.isCommon,
			r.isMute,
			r.parentID
	) ss
	ORDER BY 
		quantity desc

-- Add additional data in case when day by day data is required
IF @splitByDays = 1 Begin
	SELECT * FROM @rollerStat
	SELECT DISTINCT date FROM @rollerStat ORDER BY date
END
ELSE BEGIN -- Return 2 fake recordsets
	SELECT 1
	SELECT 1
END
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
	INNER JOIN TariffWindow tw On tw.windowId = i.actualWindowID
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

-- Проверка: новые версии применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.HeadCompaniesWithActions')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.FirmWithActions1')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.Actions1')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_RollerStatistic')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionsForRollerStatistic')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.HeadCompaniesWithActions')) LIKE N'%actualWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.FirmWithActions1')) LIKE N'%actualWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.Actions1')) LIKE N'%actualWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_RollerStatistic')) LIKE N'%actualWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionsForRollerStatistic')) LIKE N'%actualWindowID%'
    PRINT N'ГОТОВО: журналы акций (3 уровня) и журнал использования роликов — по окну выхода (5 процедур).';
ELSE
    RAISERROR(N'28: новая версия применена не ко всем процедурам.', 16, 1);
GO
