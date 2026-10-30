-- broadcastStart: удаление закрыто на шаге 3 (решение Миши 29.10.2026, docs/broadcast-start.md) — шаги 4–5 не делаем,
-- поле остаётся со значением 00:00. Два живых дефекта, которые шаг 4 закрыл бы попутно, чинятся точечно, и удаляется
-- резервная копия шага 1.
--
--   1) [SQL-BS-03] TariffWindowIUD AddItem: прайс-лист искался «between startDate and finishDate», а finishDate хранится
--      с 00:00 — «Создать новое окно…» в последний день прайс-листа после полуночи давало InternalError. Теперь
--      «>= startDate and < finishDate + 1», как у проверки BadTariffWindowDay в той же процедуре.
--   2) [SQL-BS-01] stat_PackModuleLoading (загрузка пакетов, вариант «все дни»): тариф ровно в начало эфирного дня
--      (00:00) не проходил ни в одну из веток «< broadcastStart» / «> broadcastStart». Теперь «>=».
--      В stat_ModuleLoading то же условие — в закомментированном блоке, там не правится.
--   3) DROP TABLE bak_SponsorProgramPricelist_broadcastStart — копия спонсорских 03:00 до шага 1 (40 строк), нужна была
--      только для отката, пока решение не окончательное.
--
-- Проверено на ArtvisDev в откатываемых транзакциях: окно 31.12.2026 10:17 на станции 132 — до правки InternalError,
-- после — создано; 15.12.2026 и 01.01.2027 (прайс-лист 2027 есть) — создаются, как и раньше.
--
-- Когда: в любое время, на Artvis и Tumen; клиент и веб не нужны. Идемпотентен (CREATE OR ALTER, DROP IF EXISTS).
-- Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 88_broadcast-start-close.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[TariffWindowIUD]
(
@windowId int = NULL,
@windowDateActual datetime = NULL,
@windowDateOriginal datetime = NULL,
@duration int = NULL,
@duration_total int = NULL,
@price decimal(18,2) = NULL,
@massmediaID INT = NULL,
@isDisabled bit = null,
@windowPrevId int = null,
@windowNextId int = null,
@actionName varchar(32)
)
as
SET NOCOUNT ON

if @actionName in ('UpdateItem', 'AddItem')
begin
	-- Продолжительность не может быть больше полной (12.10.2026: без исключения «0 = не задана»;
	-- 0/0 - профилактика передатчика - проходит)
	if IsNull(@duration, 0) > IsNull(@duration_total, 0)
	begin
		raiserror('DurationExceedsTotal', 16,1)
		return
	end

	if (not exists (select * from Pricelist pl 
					where pl.massmediaID = @massmediaID 
						and @windowDateActual >= pl.startDate and @windowDateActual < finishDate + 1)
		or
		not exists (select * from Pricelist pl 
					where pl.massmediaID = @massmediaID 
						and @windowDateOriginal >= pl.startDate and @windowDateOriginal < pl.finishDate + 1))
	begin 
		raiserror('BadTariffWindowDay', 16,1)
		return 
	end
	
	-- Закрытый период (профилактика) — по времени выхода; время по расписанию — только ключ слота
	-- (docs/tasks/window-actual-switch.md, правило 07.10.2026)
	if exists(select *
		from DisabledWindow dw
		where dw.massmediaID = @massmediaID and
			@windowDateActual between dw.startDate and dw.finishDate)
	begin
		raiserror('CannotAddWindow_Disabled', 16,1)
		return 
	end
end 

IF @actionName = 'DeleteItem'
begin 
	if exists(select * 
			from Issue 
			where actualWindowID = @windowId or originalWindowID = @windowId)
	begin 
		raiserror('FK_Issue_TariffWindow', 16,1)
		return 
	end 

	if exists(select * From [TariffWindow] WHERE windowId = @windowId And tariffId Is Not Null)
	begin 
		raiserror('TariffWindowDeleteAttempt', 16,1)
		return 
	end 
	
	DELETE FROM [TariffWindow] WHERE windowId = @windowId
end
ELSE IF @actionName = 'UpdateItem'
begin
	-- День окна менять нельзя, только время выхода в пределах дня (Р-2, docs/tasks/window-actual-open-questions.md):
	-- день окна — ключ слота, dayOriginal = dayActual у всех окон.
	if exists (select 1 from [TariffWindow]
			where windowId = @windowId and dayOriginal <> dbo.ToShortDate(@windowDateActual))
	begin
		raiserror('TariffWindowDayChange', 16, 1)
		return
	end

	-- Объединённое окно (windowPrevId/windowNextId): перенос времени выхода не
	-- должен ломать порядок окон в эфире (окно-«хвост» не может выйти раньше
	-- окна-«головы»). См. docs/window-merging.md §3 (#1). Проверяем, только если
	-- реально меняется время выхода ИЛИ впервые ставится связь (объединение);
	-- правки isDisabled / price / продолжительности сюда не попадают.
	if (@windowPrevId is not null or @windowNextId is not null)
	begin
		declare @oldActual datetime, @oldPrevId int, @oldNextId int
		select @oldActual = windowDateActual, @oldPrevId = windowPrevId, @oldNextId = windowNextId
		from [TariffWindow] where windowId = @windowId

		if @windowDateActual <> @oldActual
			or isnull(@windowPrevId, 0) <> isnull(@oldPrevId, 0)
			or isnull(@windowNextId, 0) <> isnull(@oldNextId, 0)
		begin
			if @windowPrevId is not null
				and exists (select 1 from [TariffWindow]
					where windowId = @windowPrevId and windowDateActual >= @windowDateActual)
			begin
				raiserror('LinkedWindowsWrongOrder', 16, 1)
				return
			end

			if @windowNextId is not null
				and exists (select 1 from [TariffWindow]
					where windowId = @windowNextId and windowDateActual <= @windowDateActual)
			begin
				raiserror('LinkedWindowsWrongOrder', 16, 1)
				return
			end
		end
	end

	-- Окно объединённого тарифа (TariffUnion): окно тарифа, с которым объединён этот,
	-- в тот же день должно выйти раньше, окно тарифа-продолжения — позже.
	declare @tariffId int, @dayOriginal datetime, @actualBefore datetime
	select @tariffId = tariffId, @dayOriginal = dayOriginal, @actualBefore = windowDateActual
	from [TariffWindow] where windowId = @windowId

	if @tariffId is not null and @windowDateActual <> @actualBefore
		and (exists (select 1
				from TariffUnion tu
					inner join [TariffWindow] p on p.tariffId = tu.tariffID and p.dayOriginal = @dayOriginal
				where tu.tariffUnionID = @tariffId and p.windowDateActual >= @windowDateActual)
			or exists (select 1
				from TariffUnion tu
					inner join [TariffWindow] n on n.tariffId = tu.tariffUnionID and n.dayOriginal = @dayOriginal
				where tu.tariffID = @tariffId and n.windowDateActual <= @windowDateActual))
	begin
		raiserror('UnitedTariffWindowsWrongOrder', 16, 1)
		return
	end

	UPDATE	
		tw
	SET			
		tw.windowDateActual = @windowDateActual, 
		tw.duration = @duration, 
		tw.duration_total = @duration_total,
		tw.price = @price,
		tw.windowPrevId = @windowPrevId,
		tw.windowNextId = @windowNextId,
		tw.isDisabled = coalesce(@isDisabled, 0),
		tw.dayActual = Convert(datetime, Convert(varchar(8), @windowDateActual, 112), 112)
	from [TariffWindow] tw
		inner join Pricelist pl on tw.massmediaID = pl.massmediaID and @windowDateActual >= pl.startDate and @windowDateActual < pl.finishDate + 1
	WHERE		
		tw.windowId = @windowId
		
	SELECT * FROM [TariffWindow] WHERE [windowId] = @windowId
END
ELSE IF @actionName = 'AddItem'
BEGIN
	-- Время выхода — в тот же день, что и время по расписанию (Р-2)
	if dbo.ToShortDate(@windowDateActual) <> dbo.ToShortDate(@windowDateOriginal)
	begin
		raiserror('TariffWindowDayChange', 16, 1)
		return
	end

	-- Р-7 (этап 7, docs/tasks/window-actual-switch.md): новое окно — не в минуту, где уже выходит окно станции, и не
	-- в слот существующего окна (по расписанию). Раньше «Создать новое окно…» создавало второе окно в ту же минуту
	-- или дубль слота
	if exists (select 1 from TariffWindow tw where tw.massmediaID = @massmediaID
			and tw.windowDateActual in (@windowDateActual, @windowDateOriginal))
		or exists (select 1 from TariffWindow tw where tw.massmediaID = @massmediaID
			and tw.windowDateOriginal in (@windowDateActual, @windowDateOriginal))
	begin
		raiserror('UIX_TariffWindow_Tariff_Date', 16, 1)
		return
	end

	declare @isInsideChain bit
	-- склейки — по времени выхода (Р-8)
	set @isInsideChain = dbo.f_CheckLinkedTariffWindows(@windowDateActual, @massmediaID)
	
	IF @isInsideChain = 1
	begin
		raiserror('InsideLinkedWindowError', 16, 1)
		return 
	end 
	
	INSERT 
		INTO [TariffWindow] ([windowDateOriginal], [windowDateActual], [duration], [price], [massmediaID], 
		isDisabled, dayActual, dayOriginal, duration_total) 
	select @windowDateOriginal, @windowDateActual, @duration, @price, @massmediaID, coalesce(@isDisabled, 0)
		,Convert(datetime, Convert(varchar(8), @windowDateActual, 112), 112)
		,Convert(datetime, Convert(varchar(8), @windowDateOriginal, 112), 112)
		, @duration_total
	from 
		Pricelist pl 
	where 
		@massmediaID = pl.massmediaID 
		-- finishDate хранится с 00:00: «between» не находил прайс-лист в его последний день после полуночи
		-- (InternalError у «Создать новое окно…»), условие то же, что у проверки BadTariffWindowDay выше (SQL-BS-03)
		and @windowDateActual >= pl.startDate and @windowDateActual < pl.finishDate + 1
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @windowId = SCOPE_IDENTITY()

	SELECT * FROM [TariffWindow] WHERE [windowId] = @windowId
END
GO

CREATE OR ALTER PROCEDURE [dbo].[stat_PackModuleLoading]
(
@startDay datetime = null, 
@finishDay datetime = null,
@packModuleID int = null,
@FirmID int = null,
@UserID int = null,
@ShowBusyOnly bit = 1,
@actionID int = null,
@loggedUserID smallint,
@headCompanyId smallint = null
)
AS
BEGIN
	SET NOCOUNT ON;
	
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

	SET DATEFIRST 1

	if @StartDay is not null
		set @StartDay = dbo.ToShortDate(@StartDay)
		
	if @FinishDay is not null
		set @FinishDay = dateadd(ss, -1, dateadd(day, 1, dbo.ToShortDate(@FinishDay)))

	if @UserID is not null 
		set	@ShowBusyOnly = 1
		
	if @ShowBusyOnly = 0
	begin 
		if @StartDay is null or @FinishDay is null 
		select @StartDay = case when @StartDay is null then dbo.ToShortDate(min(pl.startDate)) else @StartDay end,
			@finishDay = case when @finishDay is null then dbo.ToShortDate(max(pl.finishDate)) else @finishDay end
		from PackModulePriceList pl 
		
		declare @date datetime, @dateweek tinyint
		set @date = @StartDay
		
		declare @res table(issueDate datetime, packModulePriceListID int)
		
		while (@date <= @FinishDay)
		begin 
			set @dateweek = datepart(dw, @date)
		
			insert into @res (issueDate,packModulePriceListID) 
			select distinct @date, pmpl.pricelistID
			from 
				ModuleTariff mt
				inner join Tariff t on mt.tariffID = t.tariffID
				inner join TariffWindow tw on t.tariffID = tw.tariffId
				inner join ModulePriceList mpl on mt.modulePriceListID = mpl.modulePriceListID
				inner join Pricelist pl on mpl.priceListID = pl.pricelistID
				inner join PackModuleContent pmc on pmc.modulePriceListID = mpl.modulePriceListID
				inner join PackModulePriceList pmpl on pmc.pricelistID = pmpl.priceListID
			where pmpl.startDate <= @FinishDay and pmpl.finishDate >= @StartDay
			 and (t.[time] < pl.broadcastStart and ((t.monday = 1 and @dateweek = 7)
					or (t.tuesday = 1 and @dateweek = 1)
					or (t.wednesday = 1 and @dateweek = 2)
					or (t.thursday = 1 and @dateweek = 3)
					or (t.friday = 1 and @dateweek = 4)
					or (t.saturday = 1 and @dateweek = 5)
					or (t.sunday = 1 and @dateweek = 6))
				 -- >=, а не >: тариф ровно в начало эфирного дня (00:00) не попадал ни в одну ветку (SQL-BS-01)
				 or (t.[time] >= pl.broadcastStart and ((t.monday = 1 and @dateweek = 1)
					or (t.tuesday = 1 and @dateweek = 2)
					or (t.wednesday = 1 and @dateweek = 3)
					or (t.thursday = 1 and @dateweek = 4)
					or (t.friday = 1 and @dateweek = 5)
					or (t.saturday = 1 and @dateweek = 6)
					or (t.sunday = 1 and @dateweek = 7))))
							
			set @date = dateadd(day, 1, @date)
		end 
		
		select 
			row_number() over(order by pmi.issueDate) as RowNum,
			pm.[name] as pmname, 
			r.issueDate as issueDate, 
			pmi.tariffPrice * pmi.ratio as price,
			f.name as fname,
			hc.name as headCompanyName,
			case when u.userID is null then '' else isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') end as manager,
			a.actionID 
		from @res r
			inner join PackModulePriceList pmpl on r.packModulePriceListID = pmpl.priceListID
			inner join PackModule pm on pmpl.packModuleID = pm.packModuleID
			left join PackModuleIssue pmi on r.issueDate = pmi.issueDate and pmi.pricelistID = pmpl.priceListID
			left join Campaign c on pmi.campaignID = c.campaignID
			left join [Action] a on c.actionID = a.actionID
			left join [User] u on a.userID = u.userID
			left join Firm f on a.firmID = f.firmID
			left join HeadCompany hc on hc.headCompanyID = f.headCompanyID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
		where (c.actionID is null or c.actionID = coalesce(@actionID, c.actionID))
			and (a.firmID is null or a.firmID = coalesce(@firmID, a.firmID))
			and (hc.headCompanyID is null or hc.headCompanyID = coalesce(@headCompanyID, hc.headCompanyID))
			and (a.userID is null or a.userID = coalesce(@userID, a.userID))
			and pm.packModuleID = coalesce(@packModuleID, pm.packModuleID)
			and (@StartDay is null or @StartDay <= r.issueDate)
			and (@finishDay is null or @FinishDay >= r.issueDate)
			and (pmi.isConfirmed is null or pmi.isConfirmed = 1)
			and not exists(select * 
						from PackModuleContent pmc1
							inner join Module m1 on pmc1.moduleID = m1.moduleID
							left join @massmedias ummm on m1.massmediaID = ummm.massmediaID
						where  pmpl.pricelistID = pmc1.pricelistID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0)))
		order by pmi.issueDate
	end 
	else 
	begin
		select 
			row_number() over(order by pmi.issueDate) as RowNum,
			pm.[name] as pmname, 
			pmi.issueDate as issueDate, 
			pmi.tariffPrice * pmi.ratio as price,
			f.name as fname,
			hc.name as headCompanyName,
			isnull(u.lastname, '') + space(1) + isnull(left(u.firstname, 1), '') + '.' + isnull(left(u.secondname, 1), '') as manager,
			a.actionID as actionID
		from 
			PackModuleIssue pmi  
			inner join Campaign c on pmi.campaignID = c.campaignID
			inner join [Action] a on c.actionID = a.actionID
			inner join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
			inner join PackModule pm on pmpl.packModuleID = pm.packModuleID
			inner join [User] u on a.userID = u.userID
			inner join Firm f on a.firmID = f.firmID
			inner join HeadCompany hc on hc.headCompanyID = f.headCompanyID
			inner join 
			(
				select distinct u.userID 
				from [User] u
					left join [GroupMember] gm on u.userID = gm.userID
					left join @ugroups ug on gm.groupID = ug.id
				where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
			) as x on a.userID = x.userID
		where c.actionID = coalesce(@actionID, c.actionID)
			and a.firmID = coalesce(@firmID, a.firmID)
			and hc.headCompanyID = coalesce(@headCompanyID, hc.headCompanyID)
			and a.userID = coalesce(@userID, a.userID)
			and pm.packModuleID = coalesce(@packModuleID, pm.packModuleID)
			and (@StartDay is null or @StartDay <= pmi.issueDate)
			and (@finishDay is null or @FinishDay >= pmi.issueDate)
			and pmi.isConfirmed = 1
			and not exists(select * 
						from PackModuleContent pmc1
							inner join Module m1 on pmc1.moduleID = m1.moduleID
							left join @massmedias ummm on m1.massmediaID = ummm.massmediaID
						where  pmpl.pricelistID = pmc1.pricelistID and (ummm.massmediaID is null or 
															(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
															 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0)))
		order by pmi.issueDate
	end
END
GO

DROP TABLE IF EXISTS dbo.bak_SponsorProgramPricelist_broadcastStart;
GO

IF OBJECT_DEFINITION(OBJECT_ID('dbo.TariffWindowIUD')) LIKE '%(SQL-BS-03)%'
	AND OBJECT_DEFINITION(OBJECT_ID('dbo.stat_PackModuleLoading')) LIKE '%(SQL-BS-01)%'
	AND OBJECT_ID('dbo.bak_SponsorProgramPricelist_broadcastStart') IS NULL
	PRINT N'ГОТОВО: broadcastStart закрыт на шаге 3, SQL-BS-01 и SQL-BS-03 исправлены, резервная копия удалена.';
ELSE
	RAISERROR(N'88: не всё применено.', 16, 1);
GO
