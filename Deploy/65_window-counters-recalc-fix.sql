-- Остатки окон (TariffWindow): правка «Пересчитать остатки» (О-7) и разовый ремонт завышенных неподтверждённых счётчиков.
-- Решение Миши 21.10.2026 (docs/tasks/side-defects.md, «Найдено попутно», счётчики занятости окна; разбор 19.10.2026).
--
-- 1. job_DeleteEmptyActions — кнопка «Пересчитать остатки» в Protector (FrmMain.RecalculateWindows) и хвост ручного
--    job_DeleteHistory. Пересчёт теперь по правилу всех писателей (ArtvisDB/Scripts/window-counters-check.sql):
--    выпуски акций из журнала удалённых не считаются (раньше считались — после ActionRestore они попадали в счётчики
--    дважды), окно, из которого ушли все подтверждённые выпуски, обнуляется (раньше INNER JOIN его пропускал).
--    Одна выборка по Issue и один проход по TariffWindow вместо двух. Удаление пустых акций и DeleteHistory — как было.
--    Ошибка или отмена откатывает всё (XACT_ABORT, TRY/CATCH; прежний хвост с @@error не работал). На время пересчёта —
--    исключительная блокировка TariffWindow: писатели меняют счётчики приращениями, и выпуск, поставленный или снятый
--    между выборкой и записью окна, был бы затёрт. Пересчитываются ВСЕ окна, включая прошлые (это верные значения).
--    На ArtvisDev (2 млн окон) одно только чтение — 25–37 с, всё это время qd2 ждёт: запускать в нерабочее время.
--    Кнопка «Пересчитать остатки» старого Protector.exe упирается в таймаут клиента 30 с и откатывается; нужен новый
--    Protector.exe (таймаут 600 с, транзакцию ведёт процедура) или запуск из SSMS / sqlcmd:
--      sqlcmd -S <сервер> -d <база> -E -Q "exec dbo.job_DeleteEmptyActions"
--    ВНИМАНИЕ: процедура, как и раньше, удаляет без журнала акции без дат (вместе с кампаниями) и обычные акции без
--    кампаний — в том числе пустые акции веб-мастера (открытый дефект, docs/tasks/open-work.md, «job_DeleteEmptyActions:
--    ... удаление акций мастера»). Перед запуском посмотреть, что уйдёт:
--      select actionID from [Action] a where (a.startDate is null and a.finishDate is null)
--        or (a.isSpecial = 0 and not exists (select 1 from Campaign c where c.actionID = a.actionID))
-- 2. Ремонт: неподтверждённые счётчики (timeInUseUnconfirmed, capacityInUseUnconfirmed, first/second/lastPositionsUnconfirmed)
--    окон С СЕГОДНЯШНЕГО ДНЯ приводятся к правилу. Подтверждённые не трогаются (они верны во всех окнах), прошлые окна
--    не трогаются (решение Миши). Было завышено только вверх: Artvis — 78 755 окон 07.07–28.10.2025 и с 12.01.2026, Tumen —
--    1 600 окон 20.04–31.05.2026 (на прошлые дни ремонт не идёт). Заметно было только в остатках «с макетами».
--
-- Запускать в НЕРАБОЧЕЕ время: ремонт держит всю TariffWindow до конца транзакции (на ArtvisDev — около 6 с).
-- Независим от других скриптов; клиент qd2 и перезапуск веба не нужны. Одна транзакция ремонта, идемпотентен
-- (CREATE OR ALTER; повторный ремонт ничего не меняет). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 65_window-counters-recalc-fix.sql

SET NOCOUNT ON;
GO
SET XACT_ABORT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- ===== 1. job_DeleteEmptyActions =====
CREATE OR ALTER PROCEDURE [dbo].[job_DeleteEmptyActions]
WITH EXECUTE AS OWNER
AS
BEGIN
	SET NOCOUNT ON;
	-- Ошибка или отмена (таймаут клиента) откатывает всё: прежний хвост «if @@error = 0» не срабатывал никогда
	-- (@@error сбрасывался предыдущим if), и пересчёт мог зафиксироваться наполовину.
	SET XACT_ABORT ON;

	declare @isTransactionStart bit = 0
	if @@trancount = 0
	begin
		begin tran
		set @isTransactionStart = 1
	end

	begin try
	delete from [Action] where startDate is null and finishDate is null

	delete from a
		from [Action] a
			left join Campaign c on a.actionID = c.actionID
		where a.isSpecial = 0 and c.campaignID is null

	exec DeleteHistory

	-- Пересчёт счётчиков занятости окон («Пересчитать остатки» в Protector) по правилу всех писателей
	-- (ArtvisDB/Scripts/window-counters-check.sql): считаются все строки Issue окна выхода (actualWindowID), кроме
	-- выпусков акций из журнала удалённых (Action.deleteDate IS NOT NULL — ActionIUD снимает их со счётчиков,
	-- ActionRestore возвращает); окно без ёмкости (maxCapacity = 0) считает секунды, с ёмкостью — штуки; позиции:
	-- подтверждённые — флаг, неподтверждённые — счётчик. Окно, из которого ушли все выпуски, обнуляется.
	-- До Deploy/65 (О-7, docs/tasks/window-actual-open-questions.md) выпуски удалённых акций считались, а подтверждённые
	-- счётчики опустевших окон не обнулялись.
	-- Писатели меняют счётчики приращениями: выпуск, снятый или поставленный между выборкой по Issue и записью окна,
	-- был бы затёрт старым значением. Поэтому на время пересчёта — исключительная блокировка всей TariffWindow
	-- (qd2 ждёт; запускать в нерабочее время).
	declare @lock int
	select top (1) @lock = 1 from TariffWindow with (tablockx, holdlock)

	;with agg as (
		select i.actualWindowID as windowId,
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as durC,
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durU,
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as cntC,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as cntU,
			sum(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end) as firstC,
			sum(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end) as secondC,
			sum(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end) as lastC,
			sum(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end) as firstU,
			sum(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end) as secondU,
			sum(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end) as lastU
		from Issue i
			inner join Roller r on r.rollerID = i.rollerID
			inner join Campaign c on c.campaignID = i.campaignID
			inner join [Action] a on a.actionID = c.actionID
		where a.deleteDate is null
		group by i.actualWindowID
	)
	update tw set
		timeInUseConfirmed = e.tc,
		timeInUseUnconfirmed = e.tu,
		capacityInUseConfirmed = e.cc,
		capacityInUseUnconfirmed = e.cu,
		isFirstPositionOccupied = e.f,
		isSecondPositionOccupied = e.s,
		isLastPositionOccupied = e.l,
		firstPositionsUnconfirmed = e.fu,
		secondPositionsUnconfirmed = e.su,
		lastPositionsUnconfirmed = e.lu
	from TariffWindow tw
		left join agg g on g.windowId = tw.windowId
		cross apply (select
			case when tw.maxCapacity = 0 then isnull(g.durC, 0) else 0 end as tc,
			case when tw.maxCapacity = 0 then isnull(g.durU, 0) else 0 end as tu,
			case when tw.maxCapacity > 0 then isnull(g.cntC, 0) else 0 end as cc,
			case when tw.maxCapacity > 0 then isnull(g.cntU, 0) else 0 end as cu,
			case when isnull(g.firstC, 0) > 0 then 1 else 0 end as f,
			case when isnull(g.secondC, 0) > 0 then 1 else 0 end as s,
			case when isnull(g.lastC, 0) > 0 then 1 else 0 end as l,
			isnull(g.firstU, 0) as fu,
			isnull(g.secondU, 0) as su,
			isnull(g.lastU, 0) as lu) e
	where tw.timeInUseConfirmed <> e.tc or tw.timeInUseUnconfirmed <> e.tu
		or tw.capacityInUseConfirmed <> e.cc or tw.capacityInUseUnconfirmed <> e.cu
		or tw.isFirstPositionOccupied <> e.f or tw.isSecondPositionOccupied <> e.s or tw.isLastPositionOccupied <> e.l
		or tw.firstPositionsUnconfirmed <> e.fu or tw.secondPositionsUnconfirmed <> e.su or tw.lastPositionsUnconfirmed <> e.lu

	if @isTransactionStart = 1
		commit tran
	end try
	begin catch
		if @isTransactionStart = 1 and @@trancount > 0
			rollback tran;
		throw;
	end catch
END
GO

-- ===== 2. Ремонт неподтверждённых счётчиков окон с сегодняшнего дня =====
DECLARE @today date = CAST(GETDATE() AS date);
DECLARE @fixed int;

BEGIN TRANSACTION;
-- Как в процедуре: без исключительной блокировки выпуск, поставленный или снятый во время ремонта, затёрся бы.
DECLARE @lock int;
SELECT TOP (1) @lock = 1 FROM TariffWindow WITH (TABLOCKX, HOLDLOCK);
	;with agg as (
		select i.actualWindowID as windowId,
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as durC,
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durU,
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as cntC,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as cntU,
			sum(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end) as firstC,
			sum(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end) as secondC,
			sum(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end) as lastC,
			sum(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end) as firstU,
			sum(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end) as secondU,
			sum(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end) as lastU
		from TariffWindow w
			inner join Issue i on i.actualWindowID = w.windowId
			inner join Roller r on r.rollerID = i.rollerID
			inner join Campaign c on c.campaignID = i.campaignID
			inner join [Action] a on a.actionID = c.actionID
		where w.dayActual >= @today and a.deleteDate is null
		group by i.actualWindowID
	)
	update tw set
		timeInUseUnconfirmed = e.tu,
		capacityInUseUnconfirmed = e.cu,
		firstPositionsUnconfirmed = e.fu,
		secondPositionsUnconfirmed = e.su,
		lastPositionsUnconfirmed = e.lu
	from TariffWindow tw
		left join agg g on g.windowId = tw.windowId
		cross apply (select
			case when tw.maxCapacity = 0 then isnull(g.durC, 0) else 0 end as tc,
			case when tw.maxCapacity = 0 then isnull(g.durU, 0) else 0 end as tu,
			case when tw.maxCapacity > 0 then isnull(g.cntC, 0) else 0 end as cc,
			case when tw.maxCapacity > 0 then isnull(g.cntU, 0) else 0 end as cu,
			case when isnull(g.firstC, 0) > 0 then 1 else 0 end as f,
			case when isnull(g.secondC, 0) > 0 then 1 else 0 end as s,
			case when isnull(g.lastC, 0) > 0 then 1 else 0 end as l,
			isnull(g.firstU, 0) as fu,
			isnull(g.secondU, 0) as su,
			isnull(g.lastU, 0) as lu) e
	where tw.dayActual >= @today
		and (tw.timeInUseUnconfirmed <> e.tu or tw.capacityInUseUnconfirmed <> e.cu
			or tw.firstPositionsUnconfirmed <> e.fu or tw.secondPositionsUnconfirmed <> e.su or tw.lastPositionsUnconfirmed <> e.lu);
SET @fixed = @@ROWCOUNT;
COMMIT TRANSACTION;
PRINT CONCAT(N'65: исправлено окон (неподтверждённые, с ', CONVERT(varchar(10), @today, 104), N'): ', @fixed);
GO

-- ===== Проверка =====
DECLARE @today date = CAST(GETDATE() AS date);
DECLARE @badU int, @badC int;
	;with agg as (
		select i.actualWindowID as windowId,
			sum(case when i.isConfirmed = 1 then r.duration else 0 end) as durC,
			sum(case when i.isConfirmed = 0 then r.duration else 0 end) as durU,
			sum(case when i.isConfirmed = 1 then 1 else 0 end) as cntC,
			sum(case when i.isConfirmed = 0 then 1 else 0 end) as cntU,
			sum(case when i.isConfirmed = 1 and i.positionId = -20 then 1 else 0 end) as firstC,
			sum(case when i.isConfirmed = 1 and i.positionId = -10 then 1 else 0 end) as secondC,
			sum(case when i.isConfirmed = 1 and i.positionId = 10 then 1 else 0 end) as lastC,
			sum(case when i.isConfirmed = 0 and i.positionId = -20 then 1 else 0 end) as firstU,
			sum(case when i.isConfirmed = 0 and i.positionId = -10 then 1 else 0 end) as secondU,
			sum(case when i.isConfirmed = 0 and i.positionId = 10 then 1 else 0 end) as lastU
		from TariffWindow w
			inner join Issue i on i.actualWindowID = w.windowId
			inner join Roller r on r.rollerID = i.rollerID
			inner join Campaign c on c.campaignID = i.campaignID
			inner join [Action] a on a.actionID = c.actionID
		where w.dayActual >= @today and a.deleteDate is null
		group by i.actualWindowID
	)
	select
		@badU = sum(case when tw.timeInUseUnconfirmed <> e.tu or tw.capacityInUseUnconfirmed <> e.cu
			or tw.firstPositionsUnconfirmed <> e.fu or tw.secondPositionsUnconfirmed <> e.su or tw.lastPositionsUnconfirmed <> e.lu then 1 else 0 end),
		@badC = sum(case when tw.timeInUseConfirmed <> e.tc or tw.capacityInUseConfirmed <> e.cc
			or tw.isFirstPositionOccupied <> e.f or tw.isSecondPositionOccupied <> e.s or tw.isLastPositionOccupied <> e.l then 1 else 0 end)
	from TariffWindow tw
		left join agg g on g.windowId = tw.windowId
		cross apply (select
			case when tw.maxCapacity = 0 then isnull(g.durC, 0) else 0 end as tc,
			case when tw.maxCapacity = 0 then isnull(g.durU, 0) else 0 end as tu,
			case when tw.maxCapacity > 0 then isnull(g.cntC, 0) else 0 end as cc,
			case when tw.maxCapacity > 0 then isnull(g.cntU, 0) else 0 end as cu,
			case when isnull(g.firstC, 0) > 0 then 1 else 0 end as f,
			case when isnull(g.secondC, 0) > 0 then 1 else 0 end as s,
			case when isnull(g.lastC, 0) > 0 then 1 else 0 end as l,
			isnull(g.firstU, 0) as fu,
			isnull(g.secondU, 0) as su,
			isnull(g.lastU, 0) as lu) e
	where tw.dayActual >= @today;

IF CHARINDEX(N'a.deleteDate is null', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.job_DeleteEmptyActions')), N'')) = 0
    RAISERROR(N'65: job_DeleteEmptyActions не обновлена.', 16, 1);
ELSE IF ISNULL(@badU, 0) <> 0
    RAISERROR(N'65: остались окна с неверными неподтверждёнными счётчиками: %d.', 16, 1, @badU);
ELSE
    PRINT CONCAT(N'ГОТОВО: «Пересчитать остатки» исправлен, неподтверждённые счётчики окон с сегодняшнего дня верны',
        CASE WHEN ISNULL(@badC, 0) <> 0 THEN CONCAT(N'; подтверждённые расходятся в ', @badC, N' окнах — их ремонт не входил в скрипт, проверить window-counters-check.sql') ELSE N'' END, N'.');
GO
