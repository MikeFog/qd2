-- Защита: продолжительность не может быть больше полной продолжительности.
--
-- Правило: если полная продолжительность (duration_total) > 0 и duration > duration_total - операция отклоняется
-- ошибкой DurationExceedsTotal. Нулевая полная продолжительность означает «не задана», для неё проверки нет.
--
-- Проверка добавлена в процедуры, которые создают/меняют тарифы и рекламные окна:
--   TariffIUD (AddItem / UpdateItem / Clone), TariffWindowIUD (AddItem / UpdateItem),
--   TariffWindowChangeDuration, TariffWindowChangeDurationInDay, GenerateTariffWindowByTemplate.
-- Копирующие процедуры (клон прайс-листа, генерация окон из тарифов) не менялись: они переносят уже сохранённые значения.
--
-- Уже существующие записи с duration > duration_total скрипт НЕ трогает (на dev: ~160 тарифов, ~37 тыс. окон) -
-- ограничение на таблицах не создаётся; но правка такой записи без исправления продолжительности теперь будет отклонена.
--
-- Скрипт идемпотентен (CREATE OR ALTER, сообщение добавляется один раз), права сохраняются, данные не трогает.
-- Текст сообщения клиент читает из iMessage один раз при старте: после наката qd2 нужно перезапустить
-- (иначе вместо текста будет виден ключ DurationExceedsTotal). Сами проверки в процедурах работают сразу.
--
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -i duration-not-above-total-deploy.sql

SET NOCOUNT ON;
GO

IF NOT EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'DurationExceedsTotal')
	INSERT INTO [dbo].[iMessage] (name, message)
	VALUES ('DurationExceedsTotal', N'Продолжительность не может быть больше полной продолжительности. Операция прервана.');
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
-- Продолжительность не может быть больше полной; нулевая полная продолжительность означает «не задана»
IF @actionName In ('AddItem', 'UpdateItem', 'Clone') And @duration_total > 0 And @duration > @duration_total
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
	-- Продолжительность не может быть больше полной; нулевая полная продолжительность означает «не задана»
	if @duration_total > 0 and @duration > @duration_total
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
	
	if exists(select * 
		from DisabledWindow dw 
		where dw.massmediaID = @massmediaID and 
			((@windowDateActual between dw.startDate and dw.finishDate) or 
				(@windowDateOriginal between dw.startDate and dw.finishDate)))
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
		tw.dayActual = Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), @windowDateActual)), 112), 112)
	from [TariffWindow] tw
		inner join Pricelist pl on tw.massmediaID = pl.massmediaID and @windowDateActual >= pl.startDate and @windowDateActual < pl.finishDate + 1
	WHERE		
		tw.windowId = @windowId
		
	SELECT * FROM [TariffWindow] WHERE [windowId] = @windowId
END
ELSE IF @actionName = 'AddItem'
BEGIN
	declare @isInsideChain bit
	set @isInsideChain = dbo.f_CheckLinkedTariffWindows(@windowDateOriginal, @massmediaID)
	
	IF @isInsideChain = 1
	begin
		raiserror('InsideLinkedWindowError', 16, 1)
		return 
	end 
	
	INSERT 
		INTO [TariffWindow] ([windowDateOriginal], [windowDateActual], [duration], [price], [massmediaID], 
		isDisabled, dayActual, dayOriginal, duration_total) 
	select @windowDateOriginal, @windowDateActual, @duration, @price, @massmediaID, coalesce(@isDisabled, 0)
		,Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart)
		,DATEADD(hh, -DATEPART(hh, pl.broadcastStart), @windowDateActual)), 112), 112)
		,Convert(datetime, Convert(varchar(8), DATEADD(mi, -DATEPART(mi, pl.broadcastStart)
		, DATEADD(hh, -DATEPART(hh, pl.broadcastStart), @windowDateOriginal)), 112), 112)
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

-- Продолжительность не может быть больше полной; нулевая полная продолжительность означает «не задана»
if @newDuration_total > 0 and @newDuration > @newDuration_total
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

	-- Продолжительность не может быть больше полной (нулевая полная продолжительность означает «не задана»)
	if exists(select *
		from TariffWindow tw
		where tw.massmediaID = @massmediaId
			And tw.[windowDateOriginal] >= @startDate
			And tw.[windowDateOriginal] <= @finishDate
			And tw.duration_total > 0 And @newDuration > tw.duration_total)
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

	-- Продолжительность не может быть больше полной; нулевая полная продолжительность означает «не задана»
	if @duration_total > 0 and @duration > @duration_total
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
					Select * From TariffWindow tw Where tw.massmediaID = @massmediaId And tw.windowDateOriginal = @actualDate
				)
				And Not Exists
				(
					Select * From DisabledWindow dw Where dw.massmediaId = @massmediaId And @actualDate between dw.startDate And dw.finishdate
				))

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
	
		Set @currentDate = DATEADD(DAY, 1, @currentDate)
	End

	SELECT * FROM @errors
END
GO

PRINT '--- Проверка ---';
SELECT 'iMessage DurationExceedsTotal' AS [объект],
       CASE WHEN EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'DurationExceedsTotal') THEN 'OK' ELSE 'НЕТ' END AS [состояние]
UNION ALL
SELECT o.name,
       CASE WHEN OBJECT_DEFINITION(OBJECT_ID('dbo.' + o.name)) LIKE '%DurationExceedsTotal%' THEN 'OK' ELSE 'НЕТ' END
FROM (VALUES ('TariffIUD'), ('TariffWindowIUD'), ('TariffWindowChangeDuration'),
             ('TariffWindowChangeDurationInDay'), ('GenerateTariffWindowByTemplate')) o(name);
