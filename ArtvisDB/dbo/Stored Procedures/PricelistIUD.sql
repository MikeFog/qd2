CREATE           PROCEDURE [dbo].[PricelistIUD]
(
@pricelistID smallint OUT,
@massmediaID smallint = NULL, -- в случае Clone тут будет ID радиостанции куда надо клонировать выбранный прайслист
@startDate datetime = NULL,
@finishDate datetime = NULL,
@broadcastStart smalldatetime = NULL,
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
		INSERT INTO [Tariff]([pricelistID], [time], [monday], [tuesday], [wednesday], [thursday], [friday], [saturday], [sunday], [price], [duration], [comment], [isForModuleOnly], [maxCapacity], needExt, needInJingle, needOutJingle, suffix, duration_total)
		SELECT @PricelistID, [time], [monday], [tuesday], [wednesday], [thursday], [friday], [saturday], [sunday], [price], [duration], [comment], [isForModuleOnly], [maxCapacity], needExt, needInJingle, needOutJingle, suffix, duration_total
		FROM [Tariff] Where PricelistID = @oldPricelistId

		INSERT INTO TariffUnion (tariffID, tariffUnionID)
		SELECT tn1.tariffID, tn2.tariffID
		FROM TariffUnion tu
			JOIN Tariff to1 ON to1.tariffID=tu.tariffID
			JOIN Tariff tn1 ON tn1.pricelistID=@PricelistID
								AND tn1.time=to1.time 
								AND tn1.monday=to1.monday
								AND tn1.tuesday=to1.tuesday
								AND tn1.wednesday=to1.wednesday
								AND tn1.thursday=to1.thursday
								AND tn1.friday=to1.friday
								AND tn1.saturday=to1.saturday
								AND tn1.sunday=to1.sunday
			JOIN Tariff to2 ON to2.tariffID=tu.tariffUnionID
			JOIN Tariff tn2 ON tn2.pricelistID=@PricelistID
								AND tn2.time=to2.time 
								AND tn2.monday=to2.monday
								AND tn2.tuesday=to2.tuesday
								AND tn2.wednesday=to2.wednesday
								AND tn2.thursday=to2.thursday
								AND tn2.friday=to2.friday
								AND tn2.saturday=to2.saturday
								AND tn2.sunday=to2.sunday
		WHERE to1.pricelistID=@oldPricelistId 
				AND to2.pricelistID=@oldPricelistId
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
