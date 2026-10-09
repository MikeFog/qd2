-- Фактическое окно выпуска, шаг 4 (docs/tasks/window-actual-switch.md, этап 3; решения Р-5, Р-9, Р-12, О-8
-- от 09.10.2026 — docs/tasks/window-actual-open-questions.md §9). С удалением broadcastStart не пересекается.
--
-- 1. Дерево журнала акций: дни выхода (IssuesDays), ролики дня (CampaignRollers), выпуски дня (IssuesByDate),
--    «Удалить» у дня/ролика (CampaignsIssueDelete), содержимое модуля и пакета, журнал клонированных выходов
--    (ActionIssues) — по окну выхода. День выпуска: у линейных и спонсорских — день окна выхода, у модулей и
--    пакетов — день модуля/пакета; одно правило во всех процедурах.
-- 2. Веер: состав получаса (RangeSlotIssues) и удаление по шаблону (MasterIssueDelete) — по окну выхода.
--    RangeSlotIssues по-прежнему отдаёт исходное окно для замены ролика (до шага с RollerSubstitute).
--    MasterIssueDelete пропускает кампанию без выпуска в получасе (раньше — ошибка 515 в подтверждённой акции).
-- 3. «Перенос дня» (CampaignTransferDay): выпуск, перенесённый трафиком, переезжает со днём выхода и встаёт на
--    новую дату во время окна выхода (Р-5); цена линейного выпуска пересчитывается по новому окну (Р-9);
--    ненайденное окно больше не подменяется окном предыдущего выпуска.
-- 4. Перенос трафиком (IssueTransfer): отказ, если в окне-приёмнике уже есть ролик того же типа «Локальное СМИ»,
--    «Федеральное СМИ» или «Отбивка политической агитации», и если окно другой станции (Р-12, новое сообщение
--    TransferOtherMassmedia).
--
-- Идемпотентен. Клиент не нужен; после наката перезапустить qd2 и веб (новое сообщение в iMessage читается при
-- старте). Ставить после 26.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 27_actual-window-tree-veer-transfer.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- День выпуска: у линейных и спонсорских — день окна выхода (actualWindowID, куда выпуск перенёс трафик),
-- у модулей и пакетов — день модуля/пакета (правило 07.10.2026, docs/tasks/window-actual-switch.md).
CREATE OR ALTER PROC [dbo].[IssuesDays]
(
@campaignID int,
@massmediaID smallint,
@rollerID int = NULL,
@issueDate DATETIME = NULL
)
AS
SET NOCOUNT ON

SELECT DISTINCT	
	@massmediaID as [massmediaID],
	@campaignID  as [campaignID], 	
	@rollerID  as [rollerID],
	Convert(varchar(10), d.issueDay, 104) as name,
	d.issueDay as issueDate,
	a.[userID] AS userID,
	a.deleteDate
FROM 
	Issue i 
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
	left join ModuleIssue mi on mi.moduleIssueID = i.moduleIssueID
	left join PackModuleIssue pmi on pmi.packModuleIssueID = i.packModuleIssueID
	cross apply (select issueDay = coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal)) d
	inner join Campaign c on i.campaignID = c.campaignID
	inner join [Action] a on c.actionID = a.actionID
WHERE 
	i.[campaignID] = @campaignID AND
	i.rollerID = Coalesce(@rollerID, i.rollerID) and 
	(@issueDate is null or d.issueDay = @issueDate)
ORDER BY
	d.issueDay
GO

-- Created by GitHub Copilot in SSMS - review carefully before executing

/*
Modified by Denis Gladkikh (dgladkikh@fogsoft.ru) - add moduleID and packModuleID for roller subtitude
Performance refactoring by GitHub Copilot:
  - removed redundant SELECT DISTINCT (GROUP BY already ensures uniqueness)
  - replaced vRoller view with direct JOINs to Roller + AdvertType
  - added OPTION (RECOMPILE) to eliminate catch-all parameter sniffing issues
*/
CREATE OR ALTER PROC [dbo].[CampaignRollers]
(
    @campaignID         int,
    @issueDate          datetime = null,
    @moduleIssueID      int = null,
    @packModuleIssueID  int = null,
    @rollerId           int = null
)
AS
SET NOCOUNT ON;

SELECT
    i.campaignID,
    i.rollerID,
    c.massmediaID,
    @issueDate                                                              AS issueDate,
    r.name,
    dbo.fn_Int2Time(r.duration)                                             AS durationString,
    COUNT(i.issueID)                                                        AS [count],
    r.duration,
    r.path,
    CASE WHEN @moduleIssueID     IS NULL THEN NULL ELSE i.moduleIssueID     END AS moduleIssueID,
    CASE WHEN @packModuleIssueID IS NULL THEN NULL ELSE i.packModuleIssueID END AS packModuleIssueID,
    mi.moduleID,
    pmpl.packModuleID,
    r.isMute,
    at.name                                                                 AS advertTypeName,
    mi.modulePricelistID,
    a.deleteDate
FROM
    dbo.Issue i
    INNER JOIN dbo.TariffWindow        tw   ON i.actualWindowID      = tw.windowId   -- окно выхода
    INNER JOIN dbo.Campaign            c    ON c.campaignID          = i.campaignID
    INNER JOIN dbo.Action              a    ON a.actionID            = c.actionID
    INNER JOIN dbo.Roller              r    ON r.rollerID            = i.rollerID
    LEFT  JOIN dbo.AdvertType          at   ON at.advertTypeID       = r.advertTypeID
    LEFT  JOIN dbo.ModuleIssue         mi   ON mi.moduleIssueID      = i.moduleIssueID
    LEFT  JOIN dbo.PackModuleIssue     pmi  ON pmi.packModuleIssueID = i.packModuleIssueID
    LEFT  JOIN dbo.PackModulePriceList pmpl ON pmi.pricelistID       = pmpl.priceListID
                                           AND pmi.issueDate BETWEEN pmpl.startDate AND pmpl.finishDate
WHERE
    i.campaignID = @campaignID
    AND (@moduleIssueID     IS NULL OR i.moduleIssueID     = @moduleIssueID)
    AND (@packModuleIssueID IS NULL OR i.packModuleIssueID = @packModuleIssueID)
    -- день выпуска: модуль/пакет — свой день, остальные — день окна выхода (как IssuesDays)
    AND (@issueDate         IS NULL OR coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal) = @issueDate)
    AND (@rollerId          IS NULL OR i.rollerID          = @rollerId)
GROUP BY
    i.campaignID,
    i.rollerID,
    c.massmediaID,
    r.name,
    r.duration,
    r.path,
    CASE WHEN @moduleIssueID     IS NULL THEN NULL ELSE i.moduleIssueID     END,
    CASE WHEN @packModuleIssueID IS NULL THEN NULL ELSE i.packModuleIssueID END,
    mi.moduleID,
    pmpl.packModuleID,
    r.isMute,
    at.name,
    mi.modulePricelistID,
    a.deleteDate
ORDER BY
    r.name
OPTION (RECOMPILE);
GO

CREATE OR ALTER PROC [dbo].[IssuesByDate]
(
  @massmediaID smallint = NULL,
  @campaignID int = NULL,
  @rollerID int = NULL,
  @issueDate datetime = NULL,
  @issueId int = NULL,
  @moduleIssueID int = NULL,
  @packModuleIssueID int = NULL
)
AS
BEGIN
  SET NOCOUNT ON;

  IF @issueId IS NOT NULL
  BEGIN
    SELECT 
      i.*,
      r.[name],
      r.duration,
      tw.massmediaID,
      tw.tariffId,
      dbo.fn_Int2Time(r.duration) as durationString,
      c.actionID,
      ip.[description] as issuePosition,
      tw.windowDateActual as issueDate,
      tw.windowDateOriginal as issueDateOriginal,
      r.advertTypeName,
      a.deleteDate
    FROM dbo.Issue i
    JOIN dbo.TariffWindow tw ON i.actualWindowID = tw.windowId   -- окно выхода
    JOIN dbo.Campaign c      ON c.campaignID = i.campaignID
    JOIN dbo.[Action] a      ON a.actionID = c.actionID
    JOIN dbo.iIssuePosition ip ON ip.positionId = i.positionId
    JOIN dbo.vRoller r       ON r.rollerID = i.rollerID
    WHERE i.issueID = @issueId;
    RETURN;
  END;

  -- ВАЖНО: если massmediaID не задан, текущая логика и так вернёт пусто.
  -- Если нужно "все massmedia", скажи — сделаем корректно.
  IF @massmediaID IS NULL
    RETURN;

  SELECT 
    i.*,
    r.[name],
    r.duration,
    tw.massmediaID,
    tw.tariffId,
    dbo.fn_Int2Time(r.duration) as durationString,
    c.actionID,
    ip.[description] as issuePosition,
    tw.windowDateActual as issueDate,
    tw.windowDateOriginal as issueDateOriginal,
    r.advertTypeName,
    a.deleteDate
  FROM dbo.TariffWindow tw
  JOIN dbo.Issue i            ON i.actualWindowID = tw.windowId   -- окно выхода
  JOIN dbo.Campaign c         ON c.campaignID = i.campaignID
  JOIN dbo.[Action] a         ON a.actionID = c.actionID
  JOIN dbo.iIssuePosition ip  ON ip.positionId = i.positionId
  JOIN dbo.vRoller r          ON r.rollerID = i.rollerID
  LEFT JOIN dbo.ModuleIssue mi       ON mi.moduleIssueID = i.moduleIssueID
  LEFT JOIN dbo.PackModuleIssue pmi  ON pmi.packModuleIssueID = i.packModuleIssueID
  WHERE
      -- день выпуска: модуль/пакет — свой день, остальные — день окна выхода (как IssuesDays)
      (@issueDate IS NULL OR coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal) = @issueDate)
  AND c.massmediaID = @massmediaID
  AND (@campaignID IS NULL OR c.campaignID = @campaignID)
  AND (@rollerID   IS NULL OR i.rollerID = @rollerID)
  AND (@moduleIssueID IS NULL OR i.moduleIssueID = @moduleIssueID)
  AND (@packModuleIssueID IS NULL OR i.packModuleIssueID = @packModuleIssueID)
  ORDER BY tw.windowDateActual
  OPTION (RECOMPILE);  -- важно при куче optional-параметров
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
		inner join TariffWindow tw on i.actualWindowID = tw.windowId   -- окно выхода
		left join ModuleIssue mi on mi.moduleIssueID = i.moduleIssueID
		left join PackModuleIssue pmi on pmi.packModuleIssueID = i.packModuleIssueID
	where i.campaignID = @campaignID 
		and tw.massmediaID = @massmediaID 
		and i.rollerID = coalesce(@rollerID, i.rollerID)
		-- день выпуска — как в дереве (IssuesDays): модуль/пакет — свой день, остальные — день окна выхода
		and (@issueDate is null or coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal) = Convert(datetime, Convert(varchar(8), @issueDate, 112), 112))

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
	select @loggedUserID, @actionID, i.rollerID, tw.windowDateActual, tw.massmediaID 
	from @issues it 
		inner join Issue i on it.issueID = i.issueID 
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
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

CREATE OR ALTER Proc [dbo].[ModuleIssueContentRetrieve]
(
@moduleIssueId int
)
As
Set Nocount On

SELECT 
	i.*,
	tw.massmediaID,
	r.[name],
	dbo.fn_Int2Time(r.duration) as durationString,
	c.actionID,
	ip.[description] as issuePosition,
	issueDate = tw.windowDateActual,   -- время выхода
	advt.name as advertTypeName
FROM
	Issue i
	inner join Roller r on i.rollerID = r.rollerID 
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	Inner Join iIssuePosition ip On ip.positionId = i.positionId
	inner join TariffWindow tw on tw.windowID = i.actualWindowID   -- окно выхода
	LEFT JOIN AdvertType advt ON advt.advertTypeID = r.advertTypeID
where i.moduleIssueId = @moduleIssueId
ORDER BY 
	i.positionId
GO

CREATE OR ALTER PROC [dbo].[PackModuleIssueContentRetrieve]
(
@packModuleIssueId int = null,
@issueID int = null
)
AS
SET NOCOUNT ON 

SELECT 
	i.[issueID],
	r.[name],
	tw.windowDateActual as [issueDate],   -- время выхода
	dbo.fn_Int2Time(r.duration) as durationString,
	ip.[description] AS issuePosition,
	m.[name] AS massmediaName,
	m.groupName,
	r.advertTypeName
FROM 
	[Issue] i
	inner join vRoller r on i.rollerID = r.rollerID
	INNER JOIN [iIssuePosition] ip ON i.[positionId] = ip.[positionId]
	INNER JOIN [TariffWindow] tw ON tw.[windowId] = i.actualWindowID   -- окно выхода
	INNER JOIN [vMassMedia] m ON tw.[massmediaID] = m.[massmediaID]
WHERE 
	i.[packModuleIssueID] = coalesce(@packModuleIssueId, i.[packModuleIssueID])
	and i.issueID = coalesce(@issueID, i.issueID)
ORDER BY
	m.[name], tw.windowDateActual
GO

CREATE OR ALTER procedure [dbo].[ActionIssues]
(
@actionID int
)
as

SELECT 
	i.*,
	r.[name],
	r.duration,
	tw.massmediaID,
	tw.tariffId,
	dbo.fn_Int2Time(r.duration) as durationString,
	c.actionID,
	ip.[description] as issuePosition,
	tw.windowDateActual as issueDate,   -- время выхода
	-- нужен CampaignPart.IsMarkedAsDeleted: он читает его прямо из параметров
	-- объекта, без него контекстное меню в журнале клонированных выпусков падает
	a.deleteDate
FROM
	Issue i
	inner join Roller r on i.rollerID = r.rollerID
	inner join TariffWindow tw on i.actualWindowID = tw.windowId
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	INNER JOIN [Action] a ON a.actionID = c.actionID
	Inner Join iIssuePosition ip On ip.positionId = i.positionId
WHERE
	c.actionID = @actionID
ORDER BY 
	tw.windowDateActual
GO

-- =============================================
-- Description:	Фактическое содержимое одного или нескольких получасовых слотов веера:
--              выпуски акции по указанным кампаниям. В отличие от «Добавленных выпусков»
--              (пересечение слотов по всем выбранным кампаниям) показывает и
--              частичные слоты — те, где выпуск есть не во всех кампаниях
--              (в сетке они красные).
--              @issueDates — CSV дат в ISO 8601 с "T" (ГГГГ-ММ-ДДTчч:мм:сс), не через
--              дефис-пробел: на сервере с русским @@LANGUAGE 'YYYY-MM-DD HH:MM:SS'
--              парсится как 'YYYY-DD-MM' (день/месяц переставлены) — с "T" формат
--              однозначен независимо от языка сессии.
--              Один вызов — сразу все выделенные окна (было по одному @issueDate на
--              вызов; при выделении по Ctrl+R/Delete десятков окон это давало заметную
--              паузу перед диалогом подтверждения — по круговому запросу на окно).
--              Одна строка на выпуск; requestedIssueDate — какому из @issueDates эта
--              строка соответствует (нужно вызывающему, чтобы разложить обратно по
--              окнам). Группировку «ролик + позиция» собирает C#
--              (TariffWithRangeGrid.GetSlotIssueGroups).
--              originalWindowID/windowDayOriginal — для RollerSubstitute (массовая
--              замена ролика, TariffWithRangeGrid.GetSlotIssueRows): её #days требует
--              именно исходное окно выпуска и его день (twO), пока замена ролика не переведена на окно выхода.
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[RangeSlotIssues]
(
	@actionID int,
	@issueDates varchar(max),
	-- Список кампаний акции (CSV campaignID). NULL/пусто — все линейные кампании акции.
	@campaignIDs varchar(max) = NULL
)
AS
BEGIN
	SET NOCOUNT ON;

	-- Даты слотов разбираются один раз во временную таблицу. Раньше STRING_SPLIT стоял
	-- прямо в соединении по диапазону времени, и сервер разбирал всю строку дат заново на
	-- каждый выпуск акции (выпуски × слоты): неделя веера у акции на 11 тыс. выпусков
	-- считалась 20–45 с и всё это время держала S-блокировку на всю таблицу Issue —
	-- чужие IssueIUD/RollerSubstitute ждали (прод, 29.09.2026).
	CREATE TABLE #requested (issueDate datetime NOT NULL, INDEX IX_requested CLUSTERED (issueDate));
	INSERT INTO #requested (issueDate)
	SELECT CONVERT(datetime, value, 126)
	FROM STRING_SPLIT(@issueDates, ',');

	SELECT
		req.issueDate AS requestedIssueDate,
		i.issueID,
		i.campaignID,
		i.rollerID,
		r.[name] AS rollerName,
		r.duration,
		dbo.fn_Int2Time(r.duration) AS durationString,
		i.positionId,
		twO.windowId AS originalWindowID,
		twO.dayOriginal AS windowDayOriginal
	FROM #requested req
		-- Слот определяется тем же окном, что и в MasterIssueDelete: выпуск ищется по окну выхода
		-- (actualWindowID), получас — по фактическому времени этого окна (правило 07.10.2026).
		INNER JOIN dbo.TariffWindow tw
			ON tw.windowDateActual BETWEEN req.issueDate AND DATEADD(second, -1, DATEADD(minute, 30, req.issueDate))
		INNER JOIN dbo.Issue i ON i.actualWindowID = tw.windowId
		INNER JOIN dbo.TariffWindow twO ON twO.windowId = i.originalWindowID
		INNER JOIN dbo.Campaign c ON c.campaignID = i.campaignID
		INNER JOIN dbo.Roller r ON r.rollerID = i.rollerID
	WHERE c.actionID = @actionID
		AND c.campaignTypeID = 1
		AND (@campaignIDs IS NULL
			OR c.campaignID IN (SELECT CONVERT(int, value) FROM STRING_SPLIT(@campaignIDs, ',')))
	ORDER BY req.issueDate, i.rollerID, i.positionId, i.campaignID;
END
GO

-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 18.05.2009
-- Description:	Delete Master Issue
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[MasterIssueDelete] 
(
	@issueDate datetime, 
	@actionID int,
	@positionID int,
	@rollerID int,
	@grantorID smallint = null,
	@loggedUserId smallint,
	-- Список кампаний акции (CSV campaignID), с которыми работает веер. NULL/пусто —
	-- все линейные кампании акции (прежнее поведение для вызовов без выбора).
	@campaignIDs varchar(max) = NULL
)
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;

	-- Симметрично AddRangeIssues: удаление выпусков веера касается только линейных кампаний
	-- (campaignTypeID = 1). Выпуски модульных/спонсорских кампаний живут в своих таблицах
	-- (ModuleIssue/ProgramIssue) и этой процедурой не трогаются.
	declare cur_massmedias cursor local fast_forward for
	select c.massmediaID, c.campaignID from dbo.Campaign c
	where c.actionID = @actionID and c.campaignTypeID = 1
		and (@campaignIDs is null
			or c.campaignID in (select convert(int, value) from string_split(@campaignIDs, ',')))

	declare @massmediaID smallint, @campaignID int, @issueID int
	
	open cur_massmedias
	fetch next from cur_massmedias into @massmediaID, @campaignID
	
	while @@fetch_status = 0
	begin 
		select @issueID = null
		
		select top 1 @issueID = i.issueID
		from Issue i 
			inner join TariffWindow tw on i.actualWindowID = tw.windowId   -- окно выхода
		where tw.massmediaID = @massmediaID and i.campaignID = @campaignID and i.positionID = @positionID and i.rollerID = @rollerID
			and tw.windowDateActual between @issueDate and dateadd(second, -1, dateadd(minute, 30, @issueDate))
		order by case when (tw.duration - tw.timeInUseConfirmed) > 0 then 0 else 1 end, tw.windowDateActual
	
		-- Выпуска этой кампании в получасе нет — кампанию пропускаем. Раньше IssueIUD вызывался с
		-- @issueID = NULL: в подтверждённой акции это ошибка 515 (LogDeletedIssue.issueDate NOT NULL),
		-- и удаление по остальным станциям обрывалось.
		if @issueID is not null
		begin
		exec dbo.IssueIUD
			@rollerID = @rollerID,
			@campaignID = @campaignID,
			@positionId = @positionId, 
			@loggedUserId = @loggedUserId,
			@massmediaID = @massmediaID,
			@actionName = 'DeleteItem',
			@grantorID = @grantorID,
			@issueID = @issueID

	
		if @@error <> 0 
			return 
		end
	
		fetch next from cur_massmedias into @massmediaID, @campaignID
	end 
END
GO

CREATE OR ALTER PROC [dbo].[CampaignTransferDay]
(
@campaignID int,
@massmediaID SMALLINT = null,
@oldDate datetime,
@newDate datetime,
@loggedUserId smallint,
@rollerId smallint = null
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

IF @oldDate = @newDate
	return

Declare	
	@issueID int, @issueTimeString datetime,  
	@rollerDuration int,
	@RightToGoBack bit, @isAdmin bit, @rightForMinus bit, @isTrafficManager bit,
	@campaignFinishDate datetime, @campaignTypeID tinyint,
	@isConfirmed bit, @timeBonus int, @issuesDuration int,
	@res smallint, @msgError varchar(64), @positionId smallint,
	@startDate datetime,
	@windowId int,
	@deadLine DATETIME,
	@modulePriceListID INT,
	@moduleID INT,
	@priceListID INT,
	@packModulePriceListID INT,
	@packModuleID INT,
	@rolActionTypeID TINYINT,
	@issueDateOriginal DATETIME,
	@actionID int,
	@tomorrow datetime,
	@managerDiscount decimal(18,10),
	@campaignStartDate datetime,
	@newPrice decimal(18,2), @newWindowPrice decimal(18,2),
	@extraFirst int, @extraSecond int, @extraLast int

SELECT 
	@campaignTypeID	= c.campaignTypeID,
	@timeBonus = c.timeBonus,
	@issuesDuration = c.issuesDuration,
	--@deadLine = m.deadLine,
	@actionID = a.[actionID],
	@isConfirmed = a.isConfirmed,
	@managerDiscount = c.managerDiscount,
	@campaignStartDate = case when c.startDate > @newDate then @newDate else c.startDate end,
	@campaignFinishDate = case when c.finishDate < @newDate then @newDate else c.finishDate end
FROM 
	Campaign c
	INNER JOIN Action a ON c.actionID = a.actionID
	--LEFT Join MassMedia m On m.massmediaId = c.massmediaID
WHERE 
	c.campaignID = @campaignID

Select
	@deadLine = max(mm.deadLine)
From
	Issue i
	Inner Join TariffWindow tw on i.originalWindowID = tw.windowId
	Inner Join MassMedia mm on mm.massmediaID = tw.massmediaID
Where 
	i.campaignID = @campaignID

if dbo.[fn_IsAcceptRatioForUser](@loggedUserId, @managerDiscount, @campaignStartDate, @campaignFinishDate) = 0
begin 
	 raiserror('CannotTransferDayBecauseOfManagerDiscount', 16, 1)
	 return
end

Exec hlp_GetMainUserCredentials
	@loggedUserId = @loggedUserId,
	@rightToGoBack = @rightToGoBack out,
	@isAdmin = @isAdmin out,
	@isTrafficManager = @isTrafficManager out,
	@rightForMinus = @rightForMinus OUT

SELECT 
	@oldDate = dbo.ToShortDate(@oldDate),
	@newDate = dbo.ToShortDate(@newDate),
	@tomorrow = dateadd(day, 1, Convert(datetime, Convert(varchar(8),getdate(), 112), 112))

if @isAdmin = 0 And @isTrafficManager = 0 And @isConfirmed = 1 And (@oldDate < @tomorrow Or @oldDate<= IsNull(@deadLine, Convert(datetime, '19000101',112)))
begin
	raiserror('TransferErrorFromThePast', 16, 1)
	return 
end

-- Политическая агитация: перенос дня двигает выпуски прямым UPDATE, минуя
-- IssueIUD/IssueTransfer. Копим пары "откуда -> куда" по подтверждённой агитации
-- и двигаем обвязку после того, как весь день перенесён (промежуточные состояния
-- цикла обвязку не дёргают)
Declare @agitMoves table (oldWindowID int, newWindowID int)
Declare @agitOldWindowID int

-- static: выборка фиксируется при открытии. С динамическим курсором (fast_forward) перенесённая строка
-- Issue сдвигалась по кластерному ключу (originalWindowID) и у модулей/пакетов (отбор по дню модуля,
-- он меняется только после цикла) выбиралась второй раз.
Declare	curIssues cursor local static read_only
For
Select	
	i.issueID,
	-- Время слота на новой дате (Р-5, 09.10.2026): у линейных и спонсорских — окно выхода (выпуск, перенесённый
	-- трафиком, «живёт» там), у модульных и пакетных — окно модуля (исходное): их слот задаёт модуль.
	Convert(varchar, case when @campaignTypeID in (1, 2) then twA.windowDateOriginal else tw.windowDateOriginal end, 108) as issueTimeString,
	i.positionId,
	r.duration,
	mpl.[priceListID],
	mpl.[moduleID],
	case when @campaignTypeID in (1, 2) then coalesce(tA.[pricelistID], t.[pricelistID]) else t.[pricelistID] end,
	pmi.[pricelistID],
	pmpl.[packModuleID],
	r.[rolActionTypeID]
From	
	Issue i
	inner join TariffWindow tw on i.originalWindowID = tw.windowId
	inner join TariffWindow twA on i.actualWindowID = twA.windowId   -- окно выхода
	INNER JOIN [Roller] r ON i.[rollerID] = r.[rollerID]
	LEFT JOIN dbo.[ModuleIssue] mi ON mi.moduleIssueID = i.[moduleIssueID]
	LEFT JOIN dbo.[ModulePriceList] mpl ON mpl.[modulePriceListID] = mi.[modulePriceListID]
	LEFT JOIN [Tariff] t ON t.[tariffID] = tw.[tariffId]
	LEFT JOIN [Tariff] tA ON tA.[tariffID] = twA.[tariffId]
	LEFT JOIN [PackModuleIssue] pmi ON pmi.[packModuleIssueID] = i.[packModuleIssueID]
	LEFT JOIN [PackModulePriceList] pmpl ON pmi.[pricelistID] = pmpl.[priceListID]
Where	
	i.campaignID = @campaignID and
	i.rollerId = Coalesce(@rollerId, i.rollerId) and
	-- день выпуска — как в дереве (IssuesDays): модуль/пакет — свой день, остальные — день окна выхода
	coalesce(mi.issueDate, pmi.issueDate, twA.dayOriginal) = @oldDate

Open	curIssues
Fetch Next from curIssues 
Into @issueID, @issueTimeString, @positionId, @rollerDuration, @modulePriceListID, @moduleID, @priceListID, @packModulePriceListID, 
	@packModuleID, @rolActionTypeID

WHILE @@fetch_status = 0 
	BEGIN
	-- Окно ищется заново для каждого выпуска: без сброса ненайденное окно молча заменялось окном
	-- предыдущего выпуска цикла.
	SET @windowId = NULL
	SET @issueDateOriginal = @newDate + @issueTimeString
	
	IF @campaignTypeID = 1 OR @campaignTypeID = 2
		Begin
		Select @windowId = tw.windowId From TariffWindow tw 
			INNER JOIN [Tariff] t ON tw.[tariffId] = t.[tariffID] AND t.[pricelistID] = @priceListID
		Where tw.windowDateOriginal = @issueDateOriginal
		End
	ELSE IF @campaignTypeID = 3
		Select @windowId = tw.windowId From TariffWindow tw 
			Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
			Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID And mpl.pricelistId = @modulePriceListID AND mpl.moduleId = @moduleId
		Where tw.windowDateOriginal = @issueDateOriginal
	ELSE IF @campaignTypeID = 4
		Begin
		Select 
			@windowId = tw.windowId, @deadLine = mm.[deadLine] 			
		From 
			TariffWindow tw 
			INNER JOIN [Tariff] t ON tw.[tariffId] = t.[tariffID] AND t.[pricelistID] = @priceListID
			Inner Join ModuleTariff mt On mt.tariffId = tw.tariffId
			Inner Join ModulePriceList mpl On mpl.modulePriceListID = mt.modulePriceListID  
			INNER JOIN [PackModuleContent] pmc ON mpl.modulePriceListID = pmc.modulePriceListID AND pmc.[pricelistID] = @packModulePriceListID
			INNER JOIN [PackModulePriceList] pmpl ON pmc.[pricelistID] = pmpl.[priceListID] AND pmpl.[packModuleID] = @packModuleID
			INNER JOIN [Pricelist] pl ON t.[pricelistID] = pl.[pricelistID]
			INNER JOIN [MassMedia] mm ON mm.[massmediaID] = pl.[massmediaID]
		Where 
			tw.windowDateOriginal = @issueDateOriginal

		End
	
	If Not @windowId Is Null begin
		set @issuesDuration = @issuesDuration - @rollerDuration

		Exec @res = hlp_IssueVerify	
			@issueID, 
			'AddItem',
			@massmediaID,  
			@deadLine,
			@windowId,
			@issueDateOriginal, 
			@rollerDuration,
			@rightToGoBack,	
			@isAdmin, 
			@isTrafficManager, 
			@rightForMinus, 
			@campaignFinishDate,
			1,  -- так как проверяем конкретный выпуск, то всё будем проверять по правилам линейной кампании
			@isConfirmed, 
			@positionId, 
			@timeBonus,
			@issuesDuration, 
			NULL,
			@rolActionTypeID,
			@msgError out

		IF @res = 1 BEGIN
			RAISERROR(@msgError, 16, 1)
			close curIssues
			deallocate curIssues
			RETURN 
		END

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

		If @rolActionTypeID = 6
		Begin
			Set @agitOldWindowID = Null
			Select @agitOldWindowID = i.actualWindowID
			From Issue i Where i.issueID = @issueID And i.isConfirmed = 1

			If Not @agitOldWindowID Is Null
				Insert Into @agitMoves (oldWindowID, newWindowID) Values (@agitOldWindowID, @windowId)
		End

		-- Р-9 (09.10.2026, согласовано с заказчиком): цена выпуска — по новому окну, как при постановке
		-- (IssueIUD AddItem). Только у линейных: у модульных и пакетных цена в выпуске не хранится (0), у
		-- спонсорских деньги считаются по программам (ProgramIssue), а не по выпускам роликов.
		If @campaignTypeID = 1
		Begin
			Select
				@newWindowPrice = tw.price,
				@extraFirst = IsNull(p.extraChargeFirstRoller, 0),
				@extraSecond = IsNull(p.extraChargeSecondRoller, 0),
				@extraLast = IsNull(p.extraChargeLastRoller, 0)
			From TariffWindow tw
				Left Join Tariff t On t.tariffID = tw.tariffId
				Left Join Pricelist p On p.pricelistID = t.pricelistID
			Where tw.windowId = @windowId
			Set @newPrice = dbo.fn_GetIssuePrice(@rollerDuration, @newWindowPrice, 1, @positionId, @extraFirst, @extraSecond, @extraLast)
		End

		Update Issue Set actualWindowId = @windowId, [originalWindowID] = @windowId,
			tariffPrice = case when @campaignTypeID = 1 then @newPrice else tariffPrice end
		Where issueID = @issueID
		
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
	ELSE BEGIN
		raiserror('TransferError', 16, 1)
		close curIssues
		deallocate curIssues
		return
	END
	
	Fetch Next from curIssues 
	Into @issueID, @issueTimeString, @positionId, @rollerDuration, @modulePriceListID, @moduleID, @priceListID, @packModulePriceListID, @packModuleID, @rolActionTypeID
end

IF @campaignTypeID = 3
	UPDATE [ModuleIssue] SET [issueDate] = @newDate WHERE [campaignID] = @campaignID AND [issueDate] = @oldDate
		
IF @campaignTypeID = 4
	UPDATE [PackModuleIssue] SET [issueDate] = @newDate WHERE [campaignID] = @campaignID AND [issueDate] = @oldDate

close curIssues
deallocate curIssues

-- Политическая агитация: день перенесён - двигаем обвязку. Сначала снимаем со
-- старых окон (CleanupWindow сам проверит, не осталось ли там подтверждённой
-- агитации других акций), затем создаём в новых
If Exists (Select 1 From @agitMoves)
Begin
	Declare @agitWindowID int
	Declare curAgitOld cursor local fast_forward For
		Select Distinct oldWindowID From @agitMoves
		Where oldWindowID Not In (Select newWindowID From @agitMoves)

	Open curAgitOld
	Fetch Next From curAgitOld Into @agitWindowID
	While @@fetch_status = 0
	Begin
		Exec AgitationFraming
			@actionName = 'CleanupWindow',
			@windowID = @agitWindowID,
			@loggedUserID = @loggedUserID

		Fetch Next From curAgitOld Into @agitWindowID
	End
	Close curAgitOld
	Deallocate curAgitOld

	Declare curAgitNew cursor local fast_forward For
		Select Distinct newWindowID From @agitMoves

	Open curAgitNew
	Fetch Next From curAgitNew Into @agitWindowID
	While @@fetch_status = 0
	Begin
		Exec AgitationFraming
			@actionName = 'InsertForWindow',
			@windowID = @agitWindowID,
			@loggedUserID = @loggedUserID

		Fetch Next From curAgitNew Into @agitWindowID
	End
	Close curAgitNew
	Deallocate curAgitNew
End
GO

CREATE OR ALTER PROC [dbo].[IssueTransfer]
(
@issueID int,
@campaignID int,
@newWindowId int,
@newDate datetime,
@newPosition smallint = null,
@loggedUserID smallint,
@massmediaID smallint,
@isConfirmed bit
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON

declare 
	@isAdmin bit,	
	@IsTrafficManager bit,	
	@rightForMinus bit, 
	@rightToGoBack bit

-- Р-12 (09.10.2026): окно-приёмник — той же станции, что и окно, где выпуск стоит сейчас (у пакетной
-- кампании Campaign.massmediaID пуст: пакет на нескольких станциях). Интерфейс переносит только внутри
-- сетки одной станции; здесь — страховка от ошибки вызывающего кода.
if not exists (select 1
		from Issue i
			inner join TariffWindow twCur on twCur.windowId = i.actualWindowID
			inner join TariffWindow tw on tw.windowId = @newWindowId and tw.massmediaID = twCur.massmediaID
		where i.issueID = @issueID)
begin
	raiserror('TransferOtherMassmedia', 16, 1)
	return
end

-- Р-12: в окне один «Локальное СМИ»/«Локальное СМИ (агитация)», один «Федеральное СМИ»/«Федеральное
-- СМИ (агитация)» и одна «Отбивка политической агитации» — как при постановке (hlp_IssueVerify),
-- по окну выхода. Раньше перенос трафиком это не проверял.
declare @transferRolType tinyint, @transferTypeError varchar(50)
select @transferRolType = r.rolActionTypeID
from Issue i inner join Roller r on r.rollerID = i.rollerID
where i.issueID = @issueID

if @transferRolType in (4, 44, 5, 55, 7) and exists (select 1
		from Issue i2
			inner join Roller r2 on r2.rollerID = i2.rollerID
		where i2.actualWindowID = @newWindowId
			and i2.issueID <> @issueID
			and ((@transferRolType in (4, 44) and r2.rolActionTypeID in (4, 44))
				or (@transferRolType in (5, 55) and r2.rolActionTypeID in (5, 55))
				or (@transferRolType = 7 and r2.rolActionTypeID = 7)))
begin
	set @transferTypeError = case when @transferRolType in (4, 44) then 'RolType4AlreadyExistInWindow'
		when @transferRolType in (5, 55) then 'RolType5AlreadyExistInWindow'
		else 'RolType7AlreadyExistInWindow' end
	raiserror(@transferTypeError, 16, 1)
	return
end

-- Проверка на возможность размещения
if exists(select * 
		from DisabledWindow dw 
			inner join TariffWindow tw on tw.massmediaID = dw.massmediaID
				and tw.windowDateActual between dw.startDate And dw.finishDate
			where tw.windowID = @newWindowId)
begin
	raiserror('DisabledWindowTransfer',16,1)
	return
end 

declare @msgError varchar(50)
EXEC hlp_GetMainUserCredentials

	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus OUT

select top 1 @msgError = 
	case
		when mm.deadLine is not null and tw.dayActual <= mm.deadLine and @isAdmin = 0 and @IsTrafficManager = 0 then 'DeadLineViolationTransfer' 
		when tw.isDisabled = 1 then 'DisabledInsertRoller'
		when r.rolActionTypeID in (1, 8, 9) and tw.maxCapacity > 0 then 'DisabledInsertSimpleRoller'
		when (( tw.isFirstPositionOccupied = 1 And @newPosition = -20) 
				or (tw.isSecondPositionOccupied = 1	And @newPosition = -10)
				or (tw.isLastPositionOccupied = 1	And @newPosition = 10)) then 'FirstLastIssueErrorTransfer'
		when @rightForMinus = 0 AND i.isConfirmed = 1 and (tw.[maxCapacity] > 0 AND (tw.[maxCapacity] - (tw.[capacityInUseConfirmed] + 1)) < 0) then 'WindowMaxCapacityOverflowTransfer'
		when @rightForMinus = 0 AND i.isConfirmed = 1 and tw.[timeInUseConfirmed] + r.duration > tw.duration then 'WindowOverflowTransfer'
		else null 
	end 
from 
	Issue i 
	inner join Roller r on i.rollerID = r.rollerID
	inner join [TariffWindow] tw on tw.windowId = @newWindowId
	inner join MassMedia mm on tw.massmediaID = mm.massmediaID
where i.issueID = @issueID 
order by 1 desc

if @msgError is not null 
begin 
	raiserror(@msgError,16,1)
	return
end 

-- Политическая агитация: запоминаем окно и тип ролика до переноса, чтобы после
-- переноса снять обвязку со старого окна и создать в новом (см. хвост процедуры)
Declare @agitOldWindowID int, @agitRolActionTypeID tinyint, @agitIsConfirmed bit
Select
	@agitOldWindowID = i.actualWindowID,
	@agitRolActionTypeID = r.rolActionTypeID,
	@agitIsConfirmed = i.isConfirmed
From Issue i Inner Join Roller r On r.rollerID = i.rollerID
Where i.issueID = @issueID

Declare
	@oldDate datetime

Select
	@oldDate = tw.windowDateActual
From
	Issue i 
	inner join TariffWindow tw on i.actualWindowID = tw.windowID
Where 
	i.issueId = @issueId

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

UPDATE Issue
SET	actualWindowID = @newWindowId,
	positionId = @newPosition
WHERE issueID = @issueID

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

declare @actionID int
select  @actionID = c.actionID from Campaign c where c.campaignID = @campaignID

-- Insert record to transfer log journal
-- Write Log
INSERT INTO [TransferLog]([userID], [oldDate], [newDate], [actionID], [issueID])
Values(@loggedUserID, @oldDate, @newDate, @actionID, @issueID)

-- Политическая агитация: перенос подтверждённого ролика типа 6 переносит и обвязку -
-- снимаем её со старого окна (если там больше нет агитации) и создаём в новом
If @agitRolActionTypeID = 6 And @agitIsConfirmed = 1
Begin
	Exec AgitationFraming
		@actionName = 'CleanupWindow',
		@windowID = @agitOldWindowID,
		@loggedUserID = @loggedUserID

	Exec AgitationFraming
		@actionName = 'InsertForWindow',
		@windowID = @newWindowId,
		@loggedUserID = @loggedUserID
End
GO

-- Сообщение TransferOtherMassmedia (+ испанский перевод, где есть многоязычность веба)
DECLARE @msg NVARCHAR(4000) = N'Невозможно перенести рекламный выпуск в окно другой радиостанции. Операция прервана.';
DECLARE @es  NVARCHAR(4000) = N'No se puede trasladar la emisión publicitaria a una ventana de otra emisora. Operación cancelada.';

IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'TransferOtherMassmedia')
    UPDATE dbo.iMessage SET [message] = @msg WHERE name = 'TransferOtherMassmedia';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('TransferOtherMassmedia', @msg);

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg AS [source], @es AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO

-- Проверка: новые версии применены, сообщение есть.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.IssuesDays')) LIKE N'%cross apply (select issueDay = coalesce(mi.issueDate, pmi.issueDate, tw.dayOriginal)) d%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignRollers')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.IssuesByDate')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsIssueDelete')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ModuleIssueContentRetrieve')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.PackModuleIssueContentRetrieve')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.ActionIssues')) NOT LIKE N'%originalWindowID%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.RangeSlotIssues')) LIKE N'%INNER JOIN dbo.Issue i ON i.actualWindowID = tw.windowId%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.MasterIssueDelete')) LIKE N'%if @issueID is not null%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignTransferDay')) LIKE N'%SET @windowId = NULL%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignTransferDay')) LIKE N'%tariffPrice = case when @campaignTypeID = 1 then @newPrice%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.IssueTransfer')) LIKE N'%TransferOtherMassmedia%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.IssueTransfer')) LIKE N'%@transferTypeError%'
   AND EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'TransferOtherMassmedia')
    PRINT N'ГОТОВО: дерево журнала, веер, «Перенос дня» и перенос трафиком — по окну выхода (11 процедур + сообщение).';
ELSE
    RAISERROR(N'27: новая версия применена не ко всем процедурам.', 16, 1);
GO
