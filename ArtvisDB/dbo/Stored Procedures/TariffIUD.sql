CREATE PROCEDURE [dbo].[TariffIUD]
(
@tariffID int = NULL,
@pricelistID smallint = NULL,
@time smalldatetime = NULL,
@monday tinyint = NULL,
@tuesday tinyint = NULL,
@wednesday tinyint = NULL,
@thursday tinyint = NULL,
@friday tinyint = NULL,
@saturday tinyint = NULL,
@sunday tinyint = NULL,
@price money = 0,
@duration smallint = NULL,
@duration_total smallint = NULL,
@comment nvarchar(32) = NULL,
@isForModuleOnly bit = 0,
@actionName varchar(32),
@suffix NVARCHAR(16) = NULL,
@needExt BIT = 1,
@maxCapacity SMALLINT,
@needInJingle bit = 1,
@needOutJingle bit = 1,
@isUnionEnable bit = 0,
@tariffUnionID int = NULL,
@blockTypeId smallint = NULL,
@notEarly bit = 0,
@notLater bit = 0,
@openBlock bit = 0,
@openPhonogram bit = 0
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
-- Check, may be tariff with such attributes has been already created
IF @actionName In ('AddItem', 'UpdateItem', 'Clone') And
	Exists(
		Select	* From Tariff
		Where	tariffID <> IsNull(@tariffID, 0) and
		pricelistId = @pricelistID and
		time = @time and 
		(
		(monday = @monday and @monday = 1) or
		(tuesday = @tuesday and @tuesday = 1) or
		(wednesday = @Wednesday and @wednesday = 1) or
		(thursday = @thursday and @thursday = 1) or
		(friday = @friday and @friday = 1) or
		(saturday = @saturday and @saturday = 1) or
		(sunday = @sunday and @sunday = 1) 
		)
	)
	BEGIN
		RAISERROR('TariffAlreadyExists', 16, 1)
		RETURN
	END	

-- проверим, что новый тариф не "разрывает" цепочку. Ищем тариф, который "перед" этим в какой либо из дней, и который начало цепочки 
IF @actionName In ('AddItem', 'UpdateItem', 'Clone')
	Begin
		Declare @time2 smalldatetime, @broadcastStart smalldatetime, @hour int
		Select @broadcastStart = broadcastStart From [dbo].[Pricelist] Where [pricelistID] = @pricelistID
		Set @hour = DATEPART(hour, @time)
		Set @time2 = dbo.fn_GetTariffTimesWithBroadcast(@time, @broadcastStart)

		-- Это для случая, если создаётся или изменяется тариф, который не является частью цепочки. Тут мы проверяем, что он не "влез" между 2-мя тарифами, объединёнными в цепочку
		IF @tariffUnionID Is Null And Exists (
			SELECT 1 
			FROM 
				TariffUnion
			WHERE
				[tariffID] IN (
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @monday = 1 And ((monday = 1 And @hour >= DATEPART(hour, time)) Or (sunday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @tuesday = 1 And ((tuesday = 1 And @hour >= DATEPART(hour, time)) Or (monday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @wednesday = 1 And ((wednesday = 1 And @hour >= DATEPART(hour, time)) Or (tuesday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @thursday = 1 And ((thursday = 1 And @hour >= DATEPART(hour, time)) Or (wednesday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @friday = 1 And ((friday = 1 And @hour >= DATEPART(hour, time)) Or (thursday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @saturday = 1 And ((saturday = 1 And @hour >= DATEPART(hour, time)) Or (friday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				UNION
				SELECT TOP 1 tariffID FROM Tariff WHERE pricelistID = @pricelistID And dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) < @time2 
					And @sunday = 1 And ((sunday = 1 And @hour >= DATEPART(hour, time)) Or (saturday = 1 And @hour < DATEPART(hour, time))) 
				ORDER BY dbo.fn_GetTariffTimesWithBroadcast(time, @broadcastStart) DESC
				)
				AND [tariffUnionID] <> ISNULL(@tariffID, 0)
			)
		BEGIN
			RAISERROR('TariffChainDamage', 16, 1)
			RETURN
		END
	END 
	
/*
if @actionName in ('AddItem', 'UpdateItem', 'Clone') and @isUnionEnable = 1 
	and @tariffUnionID is not null 
	and exists(select * from Tariff t where t.tariffID = @tariffUnionID and t.[time] < @time)
begin 
	RAISERROR('TariffUnionError', 16, 1)
	RETURN
end 
*/
	
IF (@actionName IN ('AddItem', 'Clone')) BEGIN
	INSERT INTO [Tariff](pricelistID, time, monday, tuesday, wednesday, thursday, friday, saturday, sunday, 
		price, duration, comment, isForModuleOnly, suffix, needExt, [maxCapacity], [needInJingle], [needOutJingle], 
		[blockTypeID], [notEarly], [notLater], [openBlock], [openPhonogram], duration_total)
	VALUES(@pricelistID, @time, @monday, @tuesday, @wednesday, @thursday, @friday, @saturday, @sunday, 
		@price, @duration, @comment, @isForModuleOnly, @suffix, @needExt, @maxCapacity, @needInJingle, @needOutJingle,
		@blockTypeID, @notEarly, @notLater, @openBlock, @openPhonogram, @duration_total)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @tariffID = SCOPE_IDENTITY()
	
	if @isUnionEnable = 1 and @tariffUnionID is not null 
	begin 
		if exists(select * from TariffUnion tu where tu.tariffUnionID = @tariffUnionID)
		begin 
			RAISERROR('TariffUnionErrorAlreadyUsed', 16, 1)
			RETURN
		end 
		
		insert into TariffUnion (tariffID, tariffUnionID) 
		values (@tariffID, @tariffUnionID) 
	end 
	
	EXEC Tariffs @TariffID = @TariffID
	END
ELSE IF @actionName = 'DeleteItem'
begin 
	delete from TariffUnion where tariffID = @tariffID or tariffUnionID = @tariffID
	DELETE FROM [Tariff] WHERE TariffID = @TariffID
end 
ELSE IF @actionName = 'UpdateItem' BEGIN
	-- Check if Tariff already in use and attributes can't be changed
	Declare	@oldIsForModuleOnly bit,
		@oldTime datetime, 
		@oldPrice money,
		@oldMonday bit,
		@oldTuesday bit,
		@oldWednesday bit,
		@oldThursday bit,
		@oldFriday bit,
		@oldSaturday bit,
		@oldSunday BIT,
		@oldMaxCap SMALLINT,
		@oldDuration int,
		@oldDurationTotal int

	SELECT 
		@oldIsForModuleOnly = isForModuleOnly,
		@oldTime = time, 
		@oldPrice = price,
		@oldMonday = monday,
		@oldTuesday = tuesday,
		@oldWednesday = wednesday,
		@oldThursday = thursday,
		@oldFriday = friday,
		@oldSaturday = saturday,
		@oldSunday = sunday,
		@oldMaxCap = maxCapacity,
		@oldDuration = duration,
		@oldDurationTotal = duration_total
	FROM
		[Tariff]
	WHERE		
		TariffID = @TariffID		
	
	IF (@oldIsForModuleOnly <> @IsForModuleOnly And @IsForModuleOnly = 1)
		And EXISTS(
			SELECT * 
			FROM issue i Inner Join TariffWindow tw On i.originalWindowID = tw.windowId
			WHERE tw.tariffID = @tariffID And i.moduleIssueID Is Null
		)
		BEGIN
		RAISERROR('TariffForModuleOnlyError', 16, 1)
		RETURN
	END

	if ((@oldMaxCap = 0 and @maxCapacity > 0) or (@oldMaxCap > 0 and @maxCapacity = 0))
		and exists(select * from ModuleTariff mt where mt.tariffID = @tariffID)
	begin 
		RAISERROR('TariffForModuleCannotChangeType', 16, 1)
		RETURN
	end 

	IF (@oldMaxCap <> @maxCapacity 
		or @oldTime <> @time Or @oldPrice <> @price Or @oldMonday <> @monday Or
		@oldTuesday <> @tuesday	Or @oldWednesday <> @wednesday Or
		@oldThursday <> @thursday Or @oldFriday <> @friday Or 
		@oldSaturday <> @saturday Or @oldSunday <> @sunday Or
		@oldDuration <> @duration
		) And
		EXISTS(SELECT * FROM tariffWindow WHERE tariffID = @tariffID)
		Begin
			RAISERROR('TariffInUse', 16, 1)
			RETURN	
		End

	UPDATE	
		[Tariff]
	SET			
		[time] = ISNULL(@time, TIME), 
		monday = ISNULL(@monday, [monday]), 
		tuesday = ISNULL(@tuesday, tuesday),
		wednesday = ISNULL(@wednesday, wednesday),
		thursday = ISNULL(@thursday, thursday),
		friday = ISNULL(@friday, friday),
		saturday = ISNULL(@saturday, saturday),
		sunday = ISNULL(@sunday, sunday),
		price = ISNULL(@price, price),
		duration = ISNULL(@duration, duration),
		duration_total = ISNULL(@duration_total, duration),
		comment = ISNULL(@comment, comment),
		isForModuleOnly = ISNULL(@isForModuleOnly,isForModuleOnly),
		suffix = ISNULL(@suffix,suffix),
		needExt = ISNULL(@needExt,needExt),
		[maxCapacity] = ISNULL(@maxCapacity,maxCapacity),
		needInJingle = ISNULL(@needInJingle, needInJingle),
		needOutJingle = ISNULL(@needOutJingle, needOutJingle),
		[blockTypeID] = @blockTypeID,
		[notEarly] = @notEarly,
		[notLater] = @notLater,
		[openBlock] = @openBlock,
		[openPhonogram] = @openPhonogram
	WHERE		
		TariffID = @TariffID
	
	if exists(select * from TariffUnion tu where tu.tariffUnionID = @tariffUnionID and tu.tariffID <> @tariffID)
		begin 
			RAISERROR('TariffUnionErrorAlreadyUsed', 16, 1)
			RETURN
		end 

	-- если обновилась информация о тарифе, который входит в существующую цепочку, надо проверить, что с цепочкой всё в порядке
	declare @startID int, @secondID int, @nextID int
	Select @startID = tariffID, @secondID = tariffUnionID From TariffUnion Where tariffID = @TariffID Or tariffUnionID = @TariffID
	If @startId Is Not Null
	Begin
		Set @nextID = dbo.fn_FindTariffIDForChain(@startID, @pricelistID)
		If IsNull(@nextID, 0) != @secondID
		begin 
			RAISERROR('TariffChainWrongUpdate', 16, 1)
			RETURN
		end 
	End
		
	delete from TariffUnion where tariffID = @tariffID
	
	if @isUnionEnable = 1 and @tariffUnionID is not null 
		insert into TariffUnion (tariffID, tariffUnionID) 
		values (@tariffID, @tariffUnionID) 

	-- если поменяласть полная продолжительность, надо обновить TariffWindow
	Update TariffWindow set duration_total = @duration_total Where tariffId = @tariffID and duration_total = @oldDurationTotal

	EXEC Tariffs @TariffID = @TariffID
	END
