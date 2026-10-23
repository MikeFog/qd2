-- Дерево «день → выпуски» для выпусков спонсорских программ (CampaignDaysTreePassport, ветка @campaignTypeID = 100;
-- десктоп — CampaignDaysForm «Удалить выпуски спонсорских программ / … программы» и «Назначить предмет рекламы выпускам
-- спонсорских программ»; веб — те же окна, этап 7.4 docs/tasks/web-action-forms.md). У выпуска программы issueDate — день и
-- время тарифа, а строки дней строились distinct по полной дате: если в день стоят программы в разное время, день
-- показывался дважды (на ArtvisDev — 350 из 2 511 дней кампаний), под каждой копией — все выпуски дня с одинаковыми
-- подписями без времени, и отмеченное в окне могло разойтись с удаляемым. Теперь день — одна строка (дата без времени),
-- в подписи выпуска — время выхода, как у выпусков роликов («13:02 Online - Туризм»). Остальные ветки, параметры и
-- колонки выдачи прежние.
--
-- Порядок: после Deploy/29 (несёт текст процедуры без broadcastStart) — без него скрипт отказывается: «Ничего не
-- изменено». В любое время, Artvis и Tumen; клиент qd2 и перезапуск веба не нужны. Не перезапускать после 69: 29 (несёт
-- старый текст CampaignDaysTreePassport); если перезапустили — повторить 69. Идемпотентен (CREATE OR ALTER). Ожидается
-- «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 69_program-issues-tree-one-day.sql

SET NOCOUNT ON;
GO
IF CHARINDEX(N'@campaignTypeID = 100', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignDaysTreePassport')), N'')) = 0
   OR CHARINDEX(N'broadcastStart', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignDaysTreePassport')), N'broadcastStart')) > 0
BEGIN
    RAISERROR(N'69: сначала Deploy/29 (CampaignDaysTreePassport без broadcastStart). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
Получает данные для показа дней и выпусков в виде дерева
*/
CREATE OR ALTER PROC [dbo].[CampaignDaysTreePassport]
(
@campaignID int,
@campaignTypeID tinyint,
@objectID int = NULL, 
@positionID int = NULL
)
AS
SET NOCOUNT ON

-- It must be original day
declare @days table (id varchar(20), parentID varchar(20), issueDate datetime, [image] varchar(50), [name] nvarchar(128))

if (@campaignTypeID in (1,2))
	begin 
	insert into @days(id, issueDate,[image], [name])
	select distinct
		convert(varchar, tw.dayActual, 104),
		tw.dayActual,
		'Day.png',
		convert(varchar, tw.dayActual, 104)
	from
		Issue i
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
	where
		i.[campaignID] = @campaignID
		And i.rollerID = IsNull(@objectID, i.rollerID)
		And i.positionId = IsNull(@positionID, i.positionId)
	order by
		tw.dayActual

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		i.issueID,
		convert(varchar, tw.dayActual, 104),
		'Issue.png',
		CONVERT(varchar(5), tw.windowDateActual, 108) + ' ' + r.name +
		Case i.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		tw.dayActual
	from
		Issue i
		Inner Join Roller r On i.rollerID = r.rollerID
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join Tariff t on tw.tariffId = t.tariffID
		inner join Pricelist pl on t.pricelistID = pl.pricelistID
	where
		i.[campaignID] = @campaignID
		And i.rollerID = IsNull(@objectID, i.rollerID)
		And i.positionId = IsNull(@positionID, i.positionId)
	order by
		tw.windowDateActual
end 
else if @campaignTypeID = 3
	Begin
	insert into @days(id, issueDate,[image], [name])
	select distinct
		convert(varchar, mi.issueDate, 104),
		mi.issueDate,
		'Day.png',
		convert(varchar, mi.issueDate, 104)
	from
		[ModuleIssue] mi
	where
		mi.[campaignID] = @campaignID
		And mi.moduleID = IsNull(@objectID, mi.moduleID)
		And mi.positionId = IsNull(@positionID, mi.positionId)
	order by
		mi.issueDate

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		mi.moduleIssueID,
		convert(varchar, mi.issueDate, 104),
		'Module.png',
		m.name + ' - ' + r.name +
		Case mi.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		mi.issueDate
	FROM 
		[ModuleIssue] mi
		INNER JOIN Module m ON m.moduleID = mi.moduleID
		Inner Join Roller r on r.rollerId = mi.rollerId
	where 
		mi.[campaignID] = @campaignID
		And mi.moduleID = IsNull(@objectID, mi.moduleID)
		And mi.positionId = IsNull(@positionID, mi.positionId)
	End
else if @campaignTypeID = 4
	Begin
	insert into @days(id, issueDate,[image], [name])
	select distinct
		convert(varchar, pmi.issueDate, 104),
		pmi.issueDate,
		'Day.png',
		convert(varchar, pmi.issueDate, 104)
	from
		[PackModuleIssue] pmi
		INNER JOIN [PackModulePriceList] pl ON pmi.[pricelistID] = pl.[priceListID]
	where
		pmi.[campaignID] = @campaignID
		And pl.packModuleID = IsNull(@objectID, pl.packModuleID)
		And pmi.positionId = IsNull(@positionID, pmi.positionId)
	order by
		pmi.issueDate

	insert into @days(id, parentID, [image], [name], issueDate)
	select
		pmi.[packModuleIssueID],
		convert(varchar, pmi.issueDate, 104),
		'PackModule.png',
		pm.name + ' - ' + r.name+
		Case pmi.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End,
		pmi.issueDate
	FROM 
		[PackModuleIssue] pmi
		INNER JOIN [PackModulePriceList] pl ON pmi.[pricelistID] = pl.[priceListID]
		INNER JOIN [PackModule] pm ON pl.[packModuleID] = pm.[packModuleID]
		INNER JOIN [Roller] r ON pmi.[rollerID] = r.[rollerID]
	where 
		pmi.[campaignID] = @campaignID
		And pl.packModuleID = IsNull(@objectID, pl.packModuleID)
		And pmi.positionId = IsNull(@positionID, pmi.positionId)
	End
else if @campaignTypeID = 100 -- такого типа нет, это для выпусков программ спонсорской кампании
	Begin
	-- День — по дате без времени: у выпуска программы issueDate — день и время тарифа, и distinct по полной дате давал по
	-- строке дня на каждое время (дерево показывало день дважды, с теми же выпусками в каждой копии).
	insert into @days(id, issueDate,[image], [name])
	select distinct
		convert(varchar, mi.issueDate, 104),
		dbo.ToShortDate(mi.issueDate),
		'Day.png',
		convert(varchar, mi.issueDate, 104)
	from
		[ProgramIssue] mi
	where
		mi.[campaignID] = @campaignID
		And mi.programID = IsNull(@objectID, mi.programID)
	order by
		2

	insert into @days(id, parentID, [image], [name], issueDate)
	SELECT
		pi.[issueID],
		Convert(varchar, pi.issueDate, 104),
		'SponsorProgram.png',
		-- Время выхода, как у выпусков роликов: программы дня в разное время различимы.
		CONVERT(varchar(5), pi.issueDate, 108) + ' ' + sp.name + COALESCE(' - ' + adv.name, ''),
		pi.[issueDate]
	FROM 
		[ProgramIssue] pi
		INNER JOIN SponsorProgram sp ON sp.sponsorProgramID = pi.programID
		LEFT JOIN AdvertType adv On adv.advertTypeID = pi.advertTypeID
	where 
		pi.[campaignID] = @campaignID
		And pi.programID = IsNull(@objectID, pi.programID)
	End

select * from @days order by issueDate

select positionId as Id, description as name from iIssuePosition Where positionId In(-20, -10, 0, 10)
GO

IF CHARINDEX(N'dbo.ToShortDate(mi.issueDate)', OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignDaysTreePassport'))) > 0
   AND CHARINDEX(N'CONVERT(varchar(5), pi.issueDate, 108)', OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignDaysTreePassport'))) > 0
    PRINT N'ГОТОВО: CampaignDaysTreePassport — день выпусков программ одной строкой, время в подписи выпуска.';
ELSE
    RAISERROR(N'69: новая версия CampaignDaysTreePassport не применена.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
