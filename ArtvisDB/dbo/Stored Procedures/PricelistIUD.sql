CREATE           PROCEDURE [dbo].[PricelistIUD]
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
		-- TariffUnion: время выхода у клона может отличаться от исходного тарифа,
		-- поэтому сопоставлять по (time + дни недели), как раньше, уже нельзя).
		DECLARE @tariffMap TABLE (oldTariffID int PRIMARY KEY, newTariffID int NOT NULL)

		-- Клонируем тарифы. Тариф-клон берёт цену, длительность и время выхода
		-- из ПОСЛЕДНЕГО (по windowDateOriginal) сгенерированного рекламного окна
		-- исходного тарифа — то есть из фактического состояния тарифа на момент
		-- окончания прайс-листа, каким бы оно ни было (правки по дороге, откаты
		-- и т.п. игнорируются). isDisabled не учитывается — отключённое окно
		-- всё равно выходит в эфир и несёт актуальные цену/время/длительность.
		-- Если окон у тарифа нет вовсе — значения берутся из самого тарифа.
		MERGE INTO [Tariff] AS tgt
		USING (
			SELECT
				t.tariffID AS oldTariffID,
				COALESCE(CONVERT(smalldatetime, CONVERT(varchar(8), lw.windowDateActual, 108)), t.[time]) AS [time],
				t.[monday], t.[tuesday], t.[wednesday], t.[thursday], t.[friday], t.[saturday], t.[sunday],
				COALESCE(lw.price, t.[price]) AS [price],
				COALESCE(lw.duration, t.[duration]) AS [duration],
				t.[comment], t.[isForModuleOnly], t.[maxCapacity],
				t.needExt, t.needInJingle, t.needOutJingle, t.suffix,
				COALESCE(lw.duration_total, t.duration_total) AS duration_total
			FROM [Tariff] t
				OUTER APPLY (
					SELECT TOP 1 tw.windowDateActual, tw.price, tw.duration, tw.duration_total
					FROM [TariffWindow] tw
					WHERE tw.tariffId = t.tariffID
					ORDER BY tw.windowDateOriginal DESC
				) lw
			WHERE t.pricelistID = @oldPricelistId
		) AS src
		ON 1 = 0
		WHEN NOT MATCHED THEN
			INSERT ([pricelistID], [time], [monday], [tuesday], [wednesday], [thursday], [friday], [saturday], [sunday], [price], [duration], [comment], [isForModuleOnly], [maxCapacity], needExt, needInJingle, needOutJingle, suffix, duration_total)
			VALUES (@PricelistID, src.[time], src.[monday], src.[tuesday], src.[wednesday], src.[thursday], src.[friday], src.[saturday], src.[sunday], src.[price], src.[duration], src.[comment], src.[isForModuleOnly], src.[maxCapacity], src.needExt, src.needInJingle, src.needOutJingle, src.suffix, src.duration_total)
		OUTPUT src.oldTariffID, inserted.tariffID INTO @tariffMap (oldTariffID, newTariffID);

		INSERT INTO TariffUnion (tariffID, tariffUnionID)
		SELECT m1.newTariffID, m2.newTariffID
		FROM TariffUnion tu
			JOIN @tariffMap m1 ON m1.oldTariffID = tu.tariffID
			JOIN @tariffMap m2 ON m2.oldTariffID = tu.tariffUnionID
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
