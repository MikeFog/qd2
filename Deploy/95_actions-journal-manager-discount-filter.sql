-- Журнал акций, отбор «Менеджерская скидка»: ошибка 8114 «Error converting data type void type to decimal» (17.11.2026,
-- разбор прод-лога 06–17.11.2026: aezhova 09.11 13:32, шесть попыток, дерево журнала не открывалось).
-- Причина — ошибка SQL Server: в HeadCompaniesWithActions параметр @managerDiscount decimal(8,2) стоит прямо в запросе
-- с OPTION (RECOMPILE). Если первый вызов после сброса кэша процедуры пришёл без @managerDiscount (обычный журнал без
-- этого отбора), все следующие вызовы из программы с этим параметром падают, пока план не сбросят (sp_recompile на проде
-- 17.11.2026 помог до следующего сброса кэша). Воспроизведено на проде (16.0.1000 RTM) и ArtvisDev (16.0.1220).
-- Теперь параметр копируется в локальную переменную того же типа и запрос использует её — так ошибки нет.
-- Параметры, тип параметра и выдача не меняются. Остальные уровни дерева (FirmWithActions1, Actions1 — float) не затронуты.
--
-- После 24 и 28 (несут более старый текст процедуры). Не перезапускать после 95: 24, 28 и
-- ArtvisDB/Scripts/actions-createdate-end-boundary-deploy.sql — вернут ошибку; если перезапустили — повторить 95.
-- В любое время, на Artvis и Tumen. Клиент и веб не нужны. Идемпотентен (CREATE OR ALTER).
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 95_actions-journal-manager-discount-filter.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF OBJECT_ID(N'dbo.HeadCompaniesWithActions') IS NULL
BEGIN
    RAISERROR(N'95: нет dbo.HeadCompaniesWithActions — не та база. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
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

	-- Не убирать: обход ошибки SQL Server (Deploy/95). Decimal-параметр прямо в запросе с OPTION (RECOMPILE):
	-- если первый вызов после сброса кэша пришёл без @managerDiscount, все следующие вызовы из программы
	-- с этим параметром падают с 8114 «Error converting data type void type to decimal» до сброса плана.
	-- Ошибка воспроизведена на 16.0.1000 RTM и 16.0.1220; через локальную переменную её нет.
	declare @managerDiscountValue decimal(8,2) = @managerDiscount

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
        and (@managerDiscountValue is null or (c.managerDiscount - @managerDiscountValue) < -0.005)
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

PRINT N'ГОТОВО';
GO
