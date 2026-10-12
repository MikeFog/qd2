-- Полная продолжительность не может быть меньше продолжительности — всегда (решение Миши 12.10.2026). Было: «нулевая полная
-- означает "не задана"», и тариф или окно с продолжительностью 1:00 и пустой полной проходили. Теперь пустая полная при
-- ненулевой продолжительности — отказ DurationExceedsTotal (сообщение то же). Окно 0/0 — так заказчик помечает профилактику
-- передатчика — правилу удовлетворяет.
-- 1. TariffIUD, TariffWindowIUD, TariffWindowChangeDuration, GenerateTariffWindowByTemplate: «продолжительность ≤ полной» без
--    исключения для нуля.
-- 2. TariffWindowChangeDurationInDay («Изменить продолжительность по дням» в трафике; меняет только продолжительность):
--    окна профилактики 0/0 пропускаются — массовой правкой профилактика не снимается. «Изменить продолжительность» (задаются
--    обе) по-прежнему применяется ко всем окнам: так профилактику ставят и снимают.
-- 3. TariffIUD «Изменить» с пустой полной (так вызывает ModuleTariffID) оставляет прежнюю полную и не трогает окна. Было:
--    полная тарифа молча становилась равной продолжительности, затем в окна писался NULL — ошибка 515, а тариф оставался
--    изменённым (docs/tasks/side-defects.md №9).
-- Данные не меняются: проверка — только при записи (старые окна с продолжительностью больше полной остаются как есть).
--
-- Ставить после 13, 26 и 35 (несёт их правки этих процедур) — без них скрипт ничего не меняет. Клиент для SQL не нужен;
-- пропуск окон профилактики в вебе («Изменить окна» трафика) — новой сборкой веба. Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 40_duration-total-not-below-duration.sql

SET NOCOUNT ON;
GO
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffIUD')) NOT LIKE N'%duration_total = @oldDurationTotal And duration > @duration_total%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffIUD')) NOT LIKE N'%i.actualWindowID = tw.windowId%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) NOT LIKE N'%UIX_TariffWindow_Tariff_Date%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) NOT LIKE N'%TariffWindowDayChange%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.GenerateTariffWindowByTemplate')) NOT LIKE N'%UIX_TariffWindow_Tariff_Date%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowChangeDuration')) NOT LIKE N'%DurationExceedsTotal%'
   OR OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowChangeDurationInDay')) NOT LIKE N'%DurationExceedsTotal%'
BEGIN
    RAISERROR(N'40: сначала Deploy/13, 26 и 35. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[TariffIUD]
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
@price decimal(18,2) = 0,
@duration smallint = NULL,
@duration_total smallint = NULL,
@comment nvarchar(32) = NULL,
@isForModuleOnly bit = 0,
@actionName varchar(32),
@suffix NVARCHAR(16) = NULL,
@needExt BIT = 0,
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
-- Продолжительность не может быть больше полной (12.10.2026: без исключения «0 = не задана»; 0/0 - так заказчик
-- помечает профилактику передатчика - проходит). При изменении пустые значения - прежние значения тарифа
IF @actionName In ('AddItem', 'Clone') And IsNull(@duration, 0) > IsNull(@duration_total, 0)
	BEGIN
		RAISERROR('DurationExceedsTotal', 16, 1)
		RETURN
	END
IF @actionName = 'UpdateItem' And EXISTS(SELECT * FROM [Tariff] WHERE tariffID = @tariffID
		And IsNull(@duration, duration) > IsNull(@duration_total, duration_total))
	BEGIN
		RAISERROR('DurationExceedsTotal', 16, 1)
		RETURN
	END

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

-- Проверка "на чужой территории": на той же станции в это же время уже есть спонсорский тариф (SponsorTariff живёт в другой таблице/цепочке pricelistID)
IF @actionName In ('AddItem', 'UpdateItem', 'Clone') And
	Exists(
		Select	1
		From	SponsorTariff st
		Inner Join SponsorProgramPricelist spl On spl.pricelistID = st.pricelistID
		Inner Join SponsorProgram sp On sp.sponsorProgramID = spl.sponsorProgramID
		Inner Join Pricelist p On p.pricelistID = @pricelistID
		Where	sp.massmediaID = p.massmediaID and
		spl.startDate <= p.finishDate and spl.finishDate >= p.startDate and
		st.time = @time and
		(
		(st.monday = 1 and @monday = 1) or
		(st.tuesday = 1 and @tuesday = 1) or
		(st.wednesday = 1 and @wednesday = 1) or
		(st.thursday = 1 and @thursday = 1) or
		(st.friday = 1 and @friday = 1) or
		(st.saturday = 1 and @saturday = 1) or
		(st.sunday = 1 and @sunday = 1)
		)
	)
	BEGIN
		RAISERROR('TariffConflictsWithSponsorTariff', 16, 1)
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
		@oldPrice decimal(18,2),
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
			FROM issue i Inner Join TariffWindow tw On i.actualWindowID = tw.windowId
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

	-- новая полная уходит в окна тарифа (ниже), а у окна продолжительность может быть своя, больше тарифной
	IF @duration_total Is Not Null And @duration_total <> @oldDurationTotal
		And EXISTS(SELECT * FROM TariffWindow WHERE tariffId = @tariffID And duration_total = @oldDurationTotal And duration > @duration_total)
		BEGIN
			RAISERROR('DurationExceedsTotal', 16, 1)
			RETURN
		END

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
		-- пустая полная - прежняя (раньше подставлялась продолжительность, а в окна писался NULL - ошибка 515)
		duration_total = ISNULL(@duration_total, duration_total),
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
		and @duration_total Is Not Null

	EXEC Tariffs @TariffID = @TariffID
	END
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
		and @windowDateActual between pl.startDate and pl.finishDate 
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @windowId = SCOPE_IDENTITY()

	SELECT * FROM [TariffWindow] WHERE [windowId] = @windowId
END
GO


CREATE OR ALTER PROCEDURE [dbo].[TariffWindowChangeDuration] 
(
@time datetime, 
@newDuration int,
@newDuration_total int,
@startdate datetime,
@finishdate datetime,
@pricelistid int,
@monday bit = 0,
@tuesday bit = 0,
@wednesday bit = 0,
@thursday bit = 0,
@friday bit = 0,
@saturday bit = 0,
@sunday bit = 0
)
as 
begin 

set nocount on;
SET DATEFIRST 1; -- Устанавливает понедельник как первый день недели

-- Продолжительность не может быть больше полной (12.10.2026: без исключения «0 = не задана»; 0/0 - профилактика
-- передатчика - проходит). Здесь задаются обе, поэтому правка применяется и к окнам профилактики: так её ставят и снимают
if IsNull(@newDuration, 0) > IsNull(@newDuration_total, 0)
begin
	raiserror('DurationExceedsTotal', 16, 1)
	return
end

declare @needaddday bit

if exists(select * 
	from Pricelist pl 
	where pl.PricelistID = @pricelistID
		and @time < pl.broadcastStart)
	set @needaddday = 1
else 
	set @needaddday = 0

update 
	tw 
set 
	tw.duration = @newDuration,
	tw.duration_total = @newDuration_total
from 
	TariffWindow tw 
	inner join Pricelist pl on tw.massmediaID = pl.massmediaID and pl.pricelistID = @pricelistid
where 
	tw.dayOriginal >= @startdate 
	and tw.dayOriginal <= @finishdate
	and tw.windowDateOriginal = convert(datetime, left(convert(varchar, case @needaddday when 1 then dateadd(day, 1, tw.dayOriginal) else tw.dayOriginal end, 120),11) 
				+ right(convert(varchar, @time, 120), 8), 120)
	and (
	(@monday = 1 and DATEPART(weekday, tw.dayOriginal) = 1)
	or (@tuesday = 1 and DATEPART(weekday, tw.dayOriginal) = 2)
	or (@wednesday = 1 and DATEPART(weekday, tw.dayOriginal) = 3)
	or (@thursday = 1 and DATEPART(weekday, tw.dayOriginal) = 4)
	or (@friday = 1 and DATEPART(weekday, tw.dayOriginal) = 5)
	or (@saturday = 1 and DATEPART(weekday, tw.dayOriginal) = 6)
	or (@sunday = 1 and DATEPART(weekday, tw.dayOriginal) = 7)
	)
end

GO


CREATE OR ALTER PROCEDURE [dbo].[TariffWindowChangeDurationInDay] 
(
	@newDuration int,
	@startDate datetime,
	@finishDate datetime,
	@massmediaId int
)
as 
begin 
	set nocount on;

	-- Меняется только продолжительность, полная у каждого окна своя. Окна профилактики передатчика (0/0 - так
	-- заказчик её помечает) пропускаются: массовой правкой профилактика не снимается (решение Миши 12.10.2026)
	-- Продолжительность не может быть больше полной (без исключения «0 = не задана»)
	if exists(select *
		from TariffWindow tw
		where tw.massmediaID = @massmediaId
			And tw.[windowDateOriginal] >= @startDate
			And tw.[windowDateOriginal] <= @finishDate
			And Not (tw.duration = 0 And tw.duration_total = 0)
			And @newDuration > tw.duration_total)
	begin
		raiserror('DurationExceedsTotal', 16, 1)
		return
	end

	update tw
	set tw.duration = @newDuration
	from TariffWindow tw 
	where
		tw.massmediaID = @massmediaId
		And tw.[windowDateOriginal] >= @startDate
		And tw.[windowDateOriginal] <= @finishDate  
		And Not (tw.duration = 0 And tw.duration_total = 0)
end

GO

-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 13.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[GenerateTariffWindowByTemplate]
(
@massmediaID SMALLINT, 
@time datetime,
@duration INT,
@duration_total INT,
@startDate DATETIME,
@finishDate DATETIME,
@monday BIT,
@tuesday BIT,
@wednesday BIT, 
@thursday BIT,
@friday BIT,
@saturday BIT,
@sunday BIT
)
AS
BEGIN
	SET NOCOUNT ON;
	
	SET DATEFIRST 1

	-- Продолжительность не может быть больше полной (12.10.2026: без исключения «0 = не задана»;
	-- 0/0 - профилактика передатчика - проходит)
	if IsNull(@duration, 0) > IsNull(@duration_total, 0)
	begin
		raiserror('DurationExceedsTotal', 16, 1)
		return
	end

	declare @isInsideChain bit
	Declare @currentDate datetime,	@broadcastStart DATETIME, @weekday TINYINT, @actualDate DATETIME 
	Declare @errors Table (
		[windowDate] datetime,
		[errorMessage] NVARCHAR(32)
	)

	SET @time = '1/1/1900 ' + Convert(varchar(8), @time, 108)
	SET @currentDate = @startDate

	While @currentDate <= @finishDate Begin	
		SELECT 
			@broadcastStart = pl.[broadcastStart]
		FROM 
			[Pricelist] pl
		WHERE
			massmediaID = @massmediaID AND
			@currentDate between pl.[startDate] AND pl.finishDate
	
		SET @actualDate = @currentDate + Case When '1/1/1900 ' + Convert(varchar(8), @time, 108) < @broadcastStart Then 1 Else 0 END + Convert(varchar(8), @time, 108)
		Set @weekday = DatePart(dw, @actualDate)
		
		IF (((@time >= @broadcastStart AND (
				(@monday = 1 And @weekday = 1)	Or
				(@tuesday = 1 And @weekday = 2)	Or
				(@wednesday = 1 And @weekday = 3)	Or
				(@thursday = 1 And @weekday = 4)	Or
				(@friday = 1 And @weekday = 5)	Or
				(@saturday = 1 And @weekday = 6)	Or
				(@sunday = 1 And @weekday = 7)
				))
				OR
				(@time < @broadcastStart AND
				((@monday = 1 And @weekday = 7)	Or
				(@tuesday = 1 And @weekday = 1)	Or
				(@wednesday = 1 And @weekday = 2)	Or
				(@thursday = 1 And @weekday = 3)	Or
				(@friday = 1 And @weekday = 4)	Or
				(@saturday = 1 And @weekday = 5)	Or
				(@sunday = 1 And @weekday = 6) )))
				And Not Exists
				(
					Select * From DisabledWindow dw Where dw.massmediaId = @massmediaId And @actualDate between dw.startDate And dw.finishdate
				))
			-- Этап 7 (docs/tasks/window-actual-switch.md): раньше здесь не было BEGIN — условие охватывало только
			-- следующую строку, и окно создавалось в любой день периода, в том числе в неотмеченный и в занятый слот.
			-- Занятая минута — и эфирная (окно станции выходит в эту минуту), и слот (окно по расписанию в эту
			-- минуту) — теперь ошибка в журнале, а не второе окно (Р-7).
			begin
				if exists (select 1 from TariffWindow tw where tw.massmediaID = @massmediaId and tw.windowDateActual = @actualDate)
					or exists (select 1 from TariffWindow tw where tw.massmediaID = @massmediaId and tw.windowDateOriginal = @actualDate)
				begin
					INSERT INTO @errors([windowDate], [errorMessage] ) VALUES(@actualDate, 'UIX_TariffWindow_Tariff_Date')
				end
				else
				begin
					set @isInsideChain = dbo.f_CheckLinkedTariffWindows(@actualDate, @massmediaID)

					IF @isInsideChain = 0
					begin
						INSERT INTO [TariffWindow]([tariffId], [windowDateOriginal], [windowDateActual], [duration], [price], [massmediaID], [maxCapacity], dayActual, dayOriginal, duration_total)
						VALUES (null, @actualDate, @actualDate,	@duration, 0, @massmediaId,	0, @currentDate, @currentDate, @duration_total)
					end
					ELSE
					begin
						INSERT INTO @errors([windowDate], [errorMessage] ) VALUES(@actualDate, 'InsideLinkedWindowError')
					end
				end
			end

		Set @currentDate = DATEADD(DAY, 1, @currentDate)
	End

	SELECT * FROM @errors
END


GO

-- Проверка: новые версии применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffIUD')) LIKE N'%ISNULL(@duration_total, duration_total)%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffIUD')) NOT LIKE N'%@duration_total > 0%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) NOT LIKE N'%@duration_total > 0%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowChangeDuration')) NOT LIKE N'%@newDuration_total > 0%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.GenerateTariffWindowByTemplate')) NOT LIKE N'%@duration_total > 0%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowChangeDurationInDay')) LIKE N'%Not (tw.duration = 0 And tw.duration_total = 0)%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowChangeDurationInDay')) NOT LIKE N'%tw.duration_total > 0%'
    PRINT N'ГОТОВО: полная продолжительность не меньше продолжительности, 0/0 (профилактика) допустимо (5 процедур).';
ELSE
    RAISERROR(N'40: новая версия применена не ко всем процедурам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
