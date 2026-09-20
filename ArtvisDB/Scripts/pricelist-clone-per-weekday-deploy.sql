/*
    ПРОД-ДЕПЛОЙ: dbo.PricelistIUD — клонирование прайс-листа по дням недели.
    Ветка feature/pricelist-clone-per-weekday. Разбор: project_pricelist_clone_window_overrides (v3).

    ЧТО ДЕЛАЕТ
      @actionName = 'Clone': состояние каждого тарифа (время выхода, цена, длительность,
      duration_total) берётся из ПОСЛЕДНЕГО окна КАЖДОГО дня недели (по dayOriginal).
      Дни с одинаковым состоянием образуют один тариф-клон, различающиеся — отдельные
      клоны с непересекающимися днями (до 7 на тариф). Тарифы одной цепочки TariffUnion
      делятся совместно (иначе нарушается инвариант «одинаковые наборы дней»),
      продолжения связываются 1:1. Дни без окон в последние 4 недели тарифа — значения
      самого тарифа. Контракт процедуры (параметры, результат) не менялся, C# не трогали.

    ЧТО ЗАЛИВАЕТСЯ
      1. CREATE OR ALTER PROCEDURE dbo.PricelistIUD.
      2. DROP INDEX IF EXISTS IX_TariffWindow_TariffID_LastWindow ON dbo.TariffWindow —
         индекс нужен был только предыдущей версии клона (v2, fb94efd); новая версия
         ищет окна по IX_TariffWindow_TariffID_DayOriginal. Если индекса на проде нет —
         шаг ничего не делает. Порядок важен: сначала процедура, потом индекс.

    ТАБЛИЦЫ И ДАННЫЕ НЕ МЕНЯЮТСЯ. Права на процедуру сохраняются (CREATE OR ALTER).

    ИДЕМПОТЕНТНОСТЬ  повторный запуск безопасен.

    ОТКАТ
      Залить PricelistIUD из master (версия fb94efd) через CREATE OR ALTER и, если нужен
      быстрый поиск последнего окна, пересоздать индекс:
        CREATE NONCLUSTERED INDEX [IX_TariffWindow_TariffID_LastWindow]
            ON [dbo].[TariffWindow]([tariffId] ASC, [windowDateOriginal] DESC)
            INCLUDE([price], [duration], [duration_total], [windowDateActual]);

    ПРОВЕРКА ПОСЛЕ ДЕПЛОЯ  pricelist-clone-per-weekday-check.sql (клон в транзакции с ROLLBACK).
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
GO
/* -- Преполёт: та ли база ---------------------------------------------- */
IF OBJECT_ID('dbo.PricelistIUD') IS NULL OR OBJECT_ID('dbo.Tariff') IS NULL
   OR OBJECT_ID('dbo.TariffWindow') IS NULL OR OBJECT_ID('dbo.TariffUnion') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: нет dbo.PricelistIUD / Tariff / TariffWindow / TariffUnion. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
PRINT 'БД     : ' + DB_NAME();
DECLARE @msg nvarchar(200) = 'Индекс IX_TariffWindow_TariffID_LastWindow до: ' + CASE
    WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.TariffWindow') AND name = 'IX_TariffWindow_TariffID_LastWindow')
    THEN 'есть (будет удалён)' ELSE 'нет' END;
PRINT @msg;
GO
/* -- CREATE OR ALTER dbo.PricelistIUD ---------------------------------------- */
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO
CREATE OR ALTER PROCEDURE [dbo].[PricelistIUD]
(
@pricelistID smallint OUT,
@massmediaID smallint = NULL, -- в случае Clone тут будет ID радиостанции куда надо клонировать выбранный прайслист
@startDate datetime = NULL,
@finishDate datetime = NULL,
@broadcastStart smalldatetime = '19000101',
@extraChargeFirstRoller tinyint = NULL,
@extraChargeSecondRoller tinyint = NULL,
@extraChargeLastRoller tinyint = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
IF @actionName IN('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('StartFinishDateError', 16, 1)
		RETURN
		end

--	IF (@actionName <> 'UpdateItem')
--	BEGIN 
	IF EXISTS(
		SELECT * FROM Pricelist	
		WHERE 
			(@startDate between startDate and finishDate Or 
			@finishDate between startDate and finishDate) and
			massmediaID = @massmediaID and 
			( @actionName IN('AddItem', 'Clone') or @pricelistID <> pricelistID)
		) BEGIN
		RAISERROR('PLPeriodIntersection', 16, 1)
		RETURN
		END
	END
--	END

IF @actionName In ('AddItem', 'Clone') BEGIN
	INSERT INTO [Pricelist](massmediaID, startDate, finishDate, broadcastStart, [extraChargeFirstRoller], [extraChargeSecondRoller], [extraChargeLastRoller])
	VALUES(@massmediaID, @startDate, @finishDate, @broadcastStart, @extraChargeFirstRoller, @extraChargeSecondRoller, @extraChargeLastRoller)

	Declare @oldPricelistId smallint
	Set @oldPricelistId = @PricelistID

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @PricelistID = SCOPE_IDENTITY()

	If @actionName = 'Clone' Begin
		-- Соответствие исходный тариф -> тариф-клон (нужно для клонирования
		-- TariffUnion). Один исходный тариф может дать несколько клонов (см. ниже),
		-- mask — набор дней недели клона: бит 0 = понедельник ... бит 6 = воскресенье.
		DECLARE @tariffMap TABLE (oldTariffID int NOT NULL, newTariffID int PRIMARY KEY, mask int NOT NULL)

		-- Состояние каждого тарифа по каждому дню недели, на который он действует:
		-- время выхода, цена, длительность из ПОСЛЕДНЕГО сгенерированного окна ЭТОГО
		-- дня недели (день недели — по dayOriginal, то есть по эфирным суткам).
		-- Смотрим на 4 недели назад от последнего окна тарифа: этого хватает, чтобы
		-- нашлось окно на каждый день недели, а поиск остаётся индексным.
		-- isDisabled не учитывается — отключённое окно всё равно выходит в эфир.
		-- Если окон на этот день нет вовсе — значения берутся из самого тарифа.
		-- rootID — начало цепочки TariffUnion (тариф вне цепочки — сам себе корень).
		DECLARE @tariffDay TABLE (
			oldTariffID int NOT NULL, dow tinyint NOT NULL, rootID int NOT NULL,
			[time] smalldatetime NOT NULL, price decimal(18, 2) NOT NULL,
			duration [dbo].[timeDuration] NOT NULL, duration_total [dbo].[timeDuration] NOT NULL,
			PRIMARY KEY (oldTariffID, dow))

		;WITH chain AS (
			SELECT t.tariffID, t.tariffID AS rootID
			FROM [Tariff] t
			WHERE t.pricelistID = @oldPricelistId
				AND NOT EXISTS (SELECT 1 FROM TariffUnion u WHERE u.tariffUnionID = t.tariffID)
			UNION ALL
			SELECT u.tariffUnionID, c.rootID
			FROM chain c
				JOIN TariffUnion u ON u.tariffID = c.tariffID
		)
		INSERT INTO @tariffDay (oldTariffID, dow, rootID, [time], price, duration, duration_total)
		SELECT t.tariffID, d.dow, c.rootID,
			COALESCE(CONVERT(smalldatetime, CONVERT(varchar(8), lw.windowDateActual, 108)), t.[time]),
			COALESCE(lw.price, t.[price]),
			COALESCE(lw.duration, t.[duration]),
			COALESCE(lw.duration_total, t.duration_total)
		FROM [Tariff] t
			JOIN chain c ON c.tariffID = t.tariffID
			CROSS APPLY (VALUES (0, t.[monday]), (1, t.[tuesday]), (2, t.[wednesday]), (3, t.[thursday]),
				(4, t.[friday]), (5, t.[saturday]), (6, t.[sunday])) d(dow, isOn)
			LEFT JOIN (
				SELECT w.tariffId, w.dow, w.windowDateActual, w.price, w.duration, w.duration_total
				FROM (
					SELECT tw.tariffId, DATEDIFF(DAY, '19000101', tw.dayOriginal) % 7 AS dow, -- 01.01.1900 — понедельник
						tw.windowDateActual, tw.price, tw.duration, tw.duration_total,
						ROW_NUMBER() OVER (PARTITION BY tw.tariffId, DATEDIFF(DAY, '19000101', tw.dayOriginal) % 7
							ORDER BY tw.dayOriginal DESC, tw.windowDateOriginal DESC) AS rn
					FROM [Tariff] t2
						CROSS APPLY (SELECT MAX(x.dayOriginal) AS lastDay FROM [TariffWindow] x WHERE x.tariffId = t2.tariffID) ld
						JOIN [TariffWindow] tw ON tw.tariffId = t2.tariffID AND tw.dayOriginal >= DATEADD(DAY, -27, ld.lastDay)
					WHERE t2.pricelistID = @oldPricelistId
				) w
				WHERE w.rn = 1
			) lw ON lw.tariffId = t.tariffID AND lw.dow = d.dow
		WHERE t.pricelistID = @oldPricelistId AND d.isOn = 1

		-- Клонируем тарифы. Дни недели одного тарифа, у которых состояние совпало,
		-- образуют ОДИН тариф-клон; если состояние различается — тариф делится на
		-- несколько клонов с непересекающимися наборами дней.
		-- Тарифы, связанные через TariffUnion, должны иметь одинаковые наборы дней
		-- (инвариант цепочки), поэтому день различается для всей цепочки сразу:
		-- сигнатура дня (sig) склеена из состояний всех тарифов цепочки. Так, если
		-- разделился тариф, разделяется и его продолжение — и клоны сопоставляются 1:1.
		MERGE INTO [Tariff] AS tgt
		USING (
			SELECT
				g.oldTariffID, g.[time], g.mask, g.[price], g.[duration], g.duration_total,
				t.[comment], t.[isForModuleOnly], t.[maxCapacity],
				t.needExt, t.needInJingle, t.needOutJingle, t.suffix
			FROM (
				SELECT s.oldTariffID, s.sig,
					MAX(s.[time]) AS [time], MAX(s.price) AS price,
					MAX(s.duration) AS duration, MAX(s.duration_total) AS duration_total,
					SUM(POWER(2, s.dow)) AS mask
				FROM (
					SELECT td.*,
						(SELECT CONCAT(x.oldTariffID, '/', CONVERT(varchar(8), x.[time], 108), '/', x.price, '/', x.duration, '/', x.duration_total, ';')
						 FROM @tariffDay x
						 WHERE x.rootID = td.rootID AND x.dow = td.dow
						 ORDER BY x.oldTariffID
						 FOR XML PATH('')) AS sig
					FROM @tariffDay td
				) s
				GROUP BY s.oldTariffID, s.sig
			) g
				JOIN [Tariff] t ON t.tariffID = g.oldTariffID
		) AS src
		ON 1 = 0
		WHEN NOT MATCHED THEN
			INSERT ([pricelistID], [time], [monday], [tuesday], [wednesday], [thursday], [friday], [saturday], [sunday], [price], [duration], [comment], [isForModuleOnly], [maxCapacity], needExt, needInJingle, needOutJingle, suffix, duration_total)
			VALUES (@PricelistID, src.[time],
				CASE WHEN src.mask & 1 <> 0 THEN 1 ELSE 0 END, CASE WHEN src.mask & 2 <> 0 THEN 1 ELSE 0 END,
				CASE WHEN src.mask & 4 <> 0 THEN 1 ELSE 0 END, CASE WHEN src.mask & 8 <> 0 THEN 1 ELSE 0 END,
				CASE WHEN src.mask & 16 <> 0 THEN 1 ELSE 0 END, CASE WHEN src.mask & 32 <> 0 THEN 1 ELSE 0 END,
				CASE WHEN src.mask & 64 <> 0 THEN 1 ELSE 0 END,
				src.[price], src.[duration], src.[comment], src.[isForModuleOnly], src.[maxCapacity], src.needExt, src.needInJingle, src.needOutJingle, src.suffix, src.duration_total)
		OUTPUT src.oldTariffID, inserted.tariffID, src.mask INTO @tariffMap (oldTariffID, newTariffID, mask);

		-- Продолжения переносим между клонами с одинаковым набором дней.
		INSERT INTO TariffUnion (tariffID, tariffUnionID)
		SELECT m1.newTariffID, m2.newTariffID
		FROM TariffUnion tu
			JOIN @tariffMap m1 ON m1.oldTariffID = tu.tariffID
			JOIN @tariffMap m2 ON m2.oldTariffID = tu.tariffUnionID AND m2.mask = m1.mask
	End
	
	Exec Pricelists @pricelistID = @pricelistID
END
ELSE IF @actionName = 'DeleteItem' 
	DELETE FROM [Pricelist] WHERE PricelistID = @PricelistID
ELSE IF @actionName = 'UpdateItem' BEGIN
	DECLARE @tariffWindowStart DATETIME,
		@tariffWindowEnd DATETIME,
		@broadcastOld SMALLDATETIME

	SELECT @broadcastOld = [broadcastStart] FROM [Pricelist] WHERE [pricelistID] = @pricelistID

	IF EXISTS(SELECT * 
		FROM [Tariff] t 
			INNER JOIN [TariffWindow] tw ON tw.[tariffId] = t.tariffID
		WHERE t.pricelistID = @pricelistID
			AND ((t.TIME < @broadcastOld AND t.TIME >= @broadcastStart) 
				OR (t.TIME > @broadcastOld AND t.TIME < @broadcastStart)))
	BEGIN
		RAISERROR ('UpdatePriceListBroadcastFaild', 16, 1)
		RETURN
	END

	SELECT @tariffWindowStart = MIN([TariffWindow].dayOriginal), @tariffWindowEnd = MAX([TariffWindow].dayOriginal) 
		FROM 
			[TariffWindow]
		INNER JOIN [Tariff] ON [TariffWindow].[tariffId] = [Tariff].[tariffID]
			AND tariff.[pricelistID] = @pricelistID 
		
	IF ((@startDate BETWEEN @tariffWindowStart AND @tariffWindowEnd AND @startDate <> @tariffWindowStart) OR 
			(DATEADD(DAY, 1, @finishDate) BETWEEN @tariffWindowStart AND @tariffWindowEnd AND @finishDate <> @tariffWindowEnd))
	BEGIN
		RAISERROR ('UpdateTariffWindowFaild', 16, 1)
		RETURN
	END
	
	if exists(select * from ModulePriceList mpl where mpl.priceListID = @pricelistID and (mpl.startDate < @startDate or mpl.finishDate > @finishDate))
	begin 
		RAISERROR ('CannoChangePriceListDatesUsesInModule', 16, 1)
		RETURN
	end 
	
	UPDATE	
		[Pricelist]
	SET			
		startDate = @startDate, 
		finishDate = @finishDate,
		broadcastStart = @broadcastStart,
		extraChargeFirstRoller = @extraChargeFirstRoller,
		extraChargeSecondRoller = @extraChargeSecondRoller,
		extraChargeLastRoller = @extraChargeLastRoller
	WHERE		
		pricelistID = @pricelistID

	Exec Pricelists @pricelistID = @pricelistID
END
GO
/* -- Индекс предыдущей версии клона больше не нужен ------------------------- */
DROP INDEX IF EXISTS [IX_TariffWindow_TariffID_LastWindow] ON [dbo].[TariffWindow];
GO

/* -- Проверка ----------------------------------------------------------- */
DECLARE @msg nvarchar(200) = 'PricelistIUD после: ' + CASE
    WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.PricelistIUD')) LIKE '%@tariffDay%'
    THEN 'OK — новая версия (по дням недели)' ELSE 'ОШИБКА — старая версия' END;
PRINT @msg;
SET @msg = 'Индекс IX_TariffWindow_TariffID_LastWindow после: ' + CASE
    WHEN EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.TariffWindow') AND name = 'IX_TariffWindow_TariffID_LastWindow')
    THEN 'ОШИБКА — остался' ELSE 'OK — удалён' END;
PRINT @msg;
GO
SET NOEXEC OFF;
GO
