CREATE PROCEDURE [dbo].[job_DeleteEmptyActions]
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
