-- Скорость чтения выпуска модуля по ключу и кампаний (кандидат [SQL-07] в docs/IMPROVEMENTS.md). Обе процедуры общие
-- для десктопа и веба; параметры и выдача прежние.
--
-- 1. ModuleIssueRetrieve: вызов с @moduleIssueId (без @windowID) идёт отдельной веткой без OPTION (RECOMPILE).
--    RECOMPILE поставлен 22.08.2026 (коммит 04e3122) против скана ModuleIssue из-за catch-all (@campaignID is null or ...)
--    и дедлоков на проде. Для выпуска по ключу план и так один - поиск по PK, а компиляция стоила 15-16 мс на каждый
--    вызов при 0 мс выполнения. Читаются те же 25 страниц, что и с RECOMPILE, поэтому дедлоковую правку это не
--    откатывает: транзакции, которые зовут процедуру после записи (ModuleIssueIUD AddItem - постановка модуля в десктопе
--    и вебе; веб «Удалить выпуски» и «Отменить» перечитывают выпуск перед удалением), держат блокировки на ~15 мс
--    меньше на каждый выпуск. Ветки @windowID и по кампании / дню - без изменений, с RECOMPILE.
-- 2. Campaigns, ветка «задана акция или кампания»: отбор по акции поиском по IX_Campaign_actionID_massmediaID, для одной
--    кампании акция берётся из неё поиском по PK. Было cm.actionID = COALESCE(@actionID, cm.actionID) AND cm.campaignID =
--    COALESCE(@campaignID, cm.campaignID) - поиска нет, каждый вызов сканировал Firm, Campaign и Action (5 138 чтений,
--    28-31 мс процессора). Эту ветку зовут обновление объекта кампании и Campaign.GetCampaignById (1-2 вызова на кампанию,
--    ~20 мест десктопа и веба), список кампаний акции. Ветка «ни акции, ни кампании» (по станции) - без изменений.
--    QUOTED_IDENTIFIER у Campaigns - OFF, как на проде (так же его сохранял Deploy/08).
--
-- Проверено 17.10.2026 на ArtvisDev, временными процедурами в одном соединении через SqlClient (как ходят qd2 и веб):
-- 6 004 пары вызовов старой и новой версий, выдача совпала полностью - колонки (имена, типы, порядок), строки и их порядок.
--   ModuleIssueRetrieve - 518 выпусков (случайные, последние, удалённых акций, подтверждённые и макеты) и 3 несуществующих:
--     @moduleIssueId один и в 6 сочетаниях с @campaignID / @issueDate (своими и чужими) и @showUnconfirmed = 0, плюс
--     не менявшиеся ветки (@windowID, по кампании и дню) - 4 238 пар.
--   Campaigns - 746 кампаний (все четыре типа, удалённых акций, без станции, акция 186420) и 328 акций (случайные,
--     удалённые, 10 с наибольшим числом кампаний): @campaignID, @actionID, оба (согласованные и чужие), со всеми
--     параметрами объекта (@massmediaID, @loggedUserID, @languageCode = 'es'), несуществующие, ветка по станции - 1 766 пар.
--   Вызов (SqlClient, тёплый кэш): ModuleIssueRetrieve @moduleIssueId 17-20 -> 0,4-1,0 мс (чтений 25 -> 25);
--     Campaigns @campaignID 30 -> 0,6-1,2 мс (чтений 5 138 -> 28), @actionID 27 -> 0,7-1,3 мс (5 138 -> 44, у акции
--     с 18 кампаниями ~320). План новой версии не зависит от первого вызова: скомпилированная на кампании с 240 выпусками,
--     по окну, без параметров, на акции с 18 кампаниями, на несуществующей кампании или по станции - те же 0,4-1,9 мс;
--     после наката в кэше по одному плану на оператор, без перекомпиляций.
--
-- Порядок: в любое время; клиент qd2 и веб не нужны (контракт тот же). После 08 и 25 (несут прежние тексты обеих процедур;
-- без них скрипт отказывается: «Ничего не изменено»). Идемпотентен (CREATE OR ALTER).
-- Не перезапускать после 52: 08, 25 и ArtvisDB/Scripts campaign-finalprice-with-pack-deploy.sql, web-i18n-sql-deploy.sql
-- (несут старые тексты); если перезапустили - повторить 52.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 52_module-issue-and-campaign-retrieve-speed.sql

SET NOCOUNT ON;
GO
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.ModuleIssueRetrieve')) NOT LIKE N'%i.actualWindowID = @windowID%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.Campaigns')) NOT LIKE N'%@languageCode%'
BEGIN
    RAISERROR(N'52: сначала Deploy/08 и 25. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [dbo].[ModuleIssueRetrieve]
(
@campaignID INT = NULL,
@issueDate datetime = NULL,
@showUnconfirmed BIT = 1,
@moduleIssueId INT = NULL,
@windowID INT = NULL
)
AS
SET NOCOUNT on

if @windowID is not null
	SELECT DISTINCT
		mi.moduleIssueID,
		mi.[campaignID], 
		mi.[moduleID],
		mi.issueDate,
		mi.modulePriceListID,
		mi.positionID,
		mi.rollerID,
		m.name as moduleName,
		r.name as rollerName,
		m.name + ' - ' + r.name as NAME,
		f.[name] AS firmName,
		c.[actionID],
		a.[isConfirmed],
		r.advertTypeName,
		a.deleteDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join vRoller r on r.rollerId = mi.rollerId
		INNER JOIN [Campaign] c ON c.[campaignID] = mi.[campaignID]
		INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		INNER JOIN [Issue] i ON mi.[moduleIssueID] = i.[moduleIssueID] 
	WHERE
		(@campaignID is null or mi.campaignID = @campaignID)
		And (@issueDate is null or mi.issueDate = @issueDate)
		AND (@showUnconfirmed = 1 OR mi.[isConfirmed] = 1)
		AND (@moduleIssueId is null or mi.[moduleIssueID] = @moduleIssueId)
		AND i.actualWindowID = @windowID
	Order By 
		m.name, r.name
	OPTION (RECOMPILE)
else if @moduleIssueId is not null
	-- Один выпуск по ключу (ModuleIssueIUD после записи, веб перечитывает выпуск перед каждым действием).
	-- Без RECOMPILE: план - поиск по PK при любых параметрах, те же страницы, что читает ветка ниже,
	-- а компиляция стоила 15 мс на вызов при 0 мс выполнения (docs/IMPROVEMENTS.md, [SQL-07]).
	SELECT DISTINCT
		mi.moduleIssueID,
		mi.[campaignID],
		mi.[moduleID],
		mi.issueDate,
		mi.modulePriceListID,
		mi.positionID,
		mi.rollerID,
		m.name as moduleName,
		r.name as rollerName,
		m.name + ' - ' + r.name as NAME,
		f.[name] AS firmName,
		c.[actionID],
		a.[isConfirmed],
		ip.[description] as issuePosition,
		r.advertTypeName,
		a.deleteDate
	FROM
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join vRoller r on r.rollerId = mi.rollerId
		INNER JOIN [Campaign] c ON c.[campaignID] = mi.[campaignID]
		INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		Inner Join iIssuePosition ip On ip.positionId = mi.positionId
	WHERE
		mi.[moduleIssueID] = @moduleIssueId
		And (@campaignID is null or mi.campaignID = @campaignID)
		And (@issueDate is null or mi.issueDate = @issueDate)
		AND (@showUnconfirmed = 1 OR mi.[isConfirmed] = 1)
	Order By
		m.name, r.name
else
	SELECT DISTINCT
		mi.moduleIssueID,
		mi.[campaignID], 
		mi.[moduleID],
		mi.issueDate,
		mi.modulePriceListID,
		mi.positionID,
		mi.rollerID,
		m.name as moduleName,
		r.name as rollerName,
		m.name + ' - ' + r.name as NAME,
		f.[name] AS firmName,
		c.[actionID],
		a.[isConfirmed],
		ip.[description] as issuePosition,
		r.advertTypeName,
		a.deleteDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join vRoller r on r.rollerId = mi.rollerId
		INNER JOIN [Campaign] c ON c.[campaignID] = mi.[campaignID]
		INNER JOIN [Action] a ON a.[actionID] = c.[actionID]
		INNER JOIN [Firm] f ON a.[firmID] = f.[firmID]
		Inner Join iIssuePosition ip On ip.positionId = mi.positionId
	WHERE
		(@campaignID is null or mi.campaignID = @campaignID)
		And (@issueDate is null or mi.issueDate = @issueDate)
		AND (@showUnconfirmed = 1 OR mi.[isConfirmed] = 1)
		AND (@moduleIssueId is null or mi.[moduleIssueID] = @moduleIssueId)
	Order By 
		m.name, r.name
	OPTION (RECOMPILE)
GO

-- Campaigns - с QUOTED_IDENTIFIER OFF, как на проде (так же его сохранял Deploy/08).
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
	-- Отбор всегда по акции - поиском по индексу; для одной кампании акцию берём из неё. По COALESCE с колонкой
	-- поиска нет: одна кампания читалась сканом Firm, Action и Campaign, ~30 мс (docs/IMPROVEMENTS.md, [SQL-07]).
	IF @actionID IS NULL
		SELECT @actionID = [actionID] FROM [Campaign] WITH (NOLOCK) WHERE [campaignID] = @campaignID

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
		cm.actionID = @actionID AND
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

SET QUOTED_IDENTIFIER ON;
GO

-- Проверка: новые версии обеих процедур применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.ModuleIssueRetrieve')) LIKE N'%else if @moduleIssueId is not null%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.Campaigns')) LIKE N'%IF @actionID IS NULL%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.Campaigns')) LIKE N'%cm.actionID = @actionID AND%'
    PRINT N'ГОТОВО: ModuleIssueRetrieve (выпуск по ключу без RECOMPILE) и Campaigns (кампания и акция поиском по индексу).';
ELSE
    RAISERROR(N'52: новая версия применена не ко всем процедурам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO