/*
    ПРОД-ДЕПЛОЙ: граница фильтра "по дате создания акции" (@createDateEnd).
    Затронуты 3 catch-all процедуры журнала акций:
        dbo.Actions1
        dbo.FirmWithActions1
        dbo.HeadCompaniesWithActions

    ПОВОД
      В фильтре журнала пользователь задаёт дату конца интервала создания
      (напр. 01.05). Из клиента параметр приходит как '2025-05-01 00:00:00'.
      Условие было  a.createDate <= @createDateEnd  — и все акции, созданные
      01.05 в рабочее время (11:42 и т.п.), из выборки выпадали: их время
      больше полуночи.

    ПРАВКА (одинаковая во всех трёх процедурах)
      1. В начале тела, сразу после SET NOCOUNT:
             SET @createDateEnd = DATEADD(DAY, 1, CAST(@createDateEnd AS date));
         Граница сдвигается на начало следующих суток. NULL остаётся NULL —
         guard (@createDateEnd IS NULL OR ...) продолжает работать.
      2. В условиях сравнения  <=  заменено на строгое  < :
             (@createDateEnd IS NULL OR a.createDate < @createDateEnd)
         Колонка a.createDate функцией не оборачивается — предикат SARGable,
         план и использование индексов не меняются.
      Парная нижняя граница @createDateBegin (>=) не трогалась.

    ИДЕМПОТЕНТНОСТЬ  повторный запуск перезаливает то же тело (CREATE OR ALTER).
    ОТКАТ
      git show master:"ArtvisDB/dbo/Stored Procedures/Actions1.sql"
      git show master:"ArtvisDB/dbo/Stored Procedures/FirmWithActions1.sql"
      git show master:"ArtvisDB/dbo/Stored Procedures/HeadCompaniesWithActions.sql"
      (взять версию до этого коммита) и залить как CREATE OR ALTER.

    ЗАПУСК
      sqlcmd -S <прод-сервер> -d <прод-БД> -E -b -I -i actions-createdate-end-boundary-deploy.sql
      либо открыть в SSMS на нужной БД и выполнить целиком.
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO

/* -- Преполёт ------------------------------------------------------------ */
IF OBJECT_ID('dbo.Actions1') IS NULL
   OR OBJECT_ID('dbo.FirmWithActions1') IS NULL
   OR OBJECT_ID('dbo.HeadCompaniesWithActions') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: нет dbo.Actions1 / FirmWithActions1 / HeadCompaniesWithActions. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
PRINT 'БД     : ' + DB_NAME();
PRINT 'Сервер : ' + CONVERT(sysname, SERVERPROPERTY('ServerName'));
PRINT 'Actions1 до                : ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.Actions1')) LIKE '%DATEADD(DAY, 1, CAST(@createDateEnd%' THEN 'уже с фиксом' ELSE 'старая' END;
PRINT 'FirmWithActions1 до        : ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.FirmWithActions1')) LIKE '%DATEADD(DAY, 1, CAST(@createDateEnd%' THEN 'уже с фиксом' ELSE 'старая' END;
PRINT 'HeadCompaniesWithActions до: ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.HeadCompaniesWithActions')) LIKE '%DATEADD(DAY, 1, CAST(@createDateEnd%' THEN 'уже с фиксом' ELSE 'старая' END;
GO

/* -- ALTER dbo.Actions1 ---------------------------------------------------- */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
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
@headCompanyID int = null
)
AS
SET NOCOUNT on
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
			'Акция №' + LTRIM(a.[actionID]) + ' (' + LTRIM(f.name) + ')'  as name,
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
			'Акция №' + LTRIM(a.[actionID]) as name,
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
			'Акция №' + LTRIM(a.[actionID]) as name,
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

/* -- ALTER dbo.FirmWithActions1 ---------------------------------------------------- */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
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
			and (@issueDate is null or ((datepart(hh, tw.windowDateOriginal) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateOriginal) = datepart(minute, @issueDate))) )
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
																and a1.finishDate >= @withoutActionsSince
																and (@startOfInterval is null or a1.startDate < @startOfInterval)))
			and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			and f.headCompanyId = COALESCE(@headCompanyId, f.headCompanyId)
		order by f.[name]
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
							and a1.finishDate >= @withoutActionsSince
							and (@startOfInterval is null or a1.startDate < @startOfInterval)
						)
					)
				and (@managerDiscount is null or (c.managerDiscount - @managerDiscount) < -0.005)
			)
		AND f.headCompanyId = COALESCE(@headCompanyId, f.headCompanyId) 
		order by f.[name]

GO

/* -- ALTER dbo.HeadCompaniesWithActions ---------------------------------------------------- */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
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
                      AND (@issueDate is null or ((datepart(hh, tw.windowDateOriginal) = datepart(hh, @issueDate)) and (datepart(minute, tw.windowDateOriginal) = datepart(minute, @issueDate))) )
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

/* -- Постпроверка ------------------------------------------------------- */
PRINT 'Actions1 после                : ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.Actions1')) LIKE '%DATEADD(DAY, 1, CAST(@createDateEnd%' THEN 'OK, с фиксом' ELSE 'НЕ ОБНОВИЛАСЬ' END;
PRINT 'FirmWithActions1 после        : ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.FirmWithActions1')) LIKE '%DATEADD(DAY, 1, CAST(@createDateEnd%' THEN 'OK, с фиксом' ELSE 'НЕ ОБНОВИЛАСЬ' END;
PRINT 'HeadCompaniesWithActions после: ' + CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.HeadCompaniesWithActions')) LIKE '%DATEADD(DAY, 1, CAST(@createDateEnd%' THEN 'OK, с фиксом' ELSE 'НЕ ОБНОВИЛАСЬ' END;
GO
SET NOEXEC OFF;
GO
