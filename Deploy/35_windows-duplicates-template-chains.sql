-- Фактическое окно и время, этап 7 «Окна» — только ошибки (docs/tasks/window-actual-switch.md §5.1; решения Р-7, Р-8
-- от 11.10.2026; отображение — вопросы заказчику, docs/window-original-actual-customer.md §10):
-- 1. TariffWindowIUD «Добавить» («Создать новое окно…»): отказ UIX_TariffWindow_Tariff_Date, если у станции уже есть окно,
--    которое выходит в эту минуту или стоит в этом слоте по расписанию (раньше — второе окно в ту же минуту или дубль слота).
--    Проверка цепочки — по времени выхода. Клиент qd2 (SpecialTariffWindow.cs) больше не ищет «такое же» окно (найденное
--    давало техническую ошибку) и не прибавляет время второй раз при повторном «ОК» — его ставить ПОСЛЕ этого скрипта.
-- 2. GenerateTariffWindowByTemplate («Создать новое окно по шаблону…», «Продлить окно»): тело дня под BEGIN…END (раньше
--    окна создавались в неотмеченные дни и в занятые слоты, писалась ложная InsideLinkedWindowError); занятая минута —
--    эфирная или слот — строка «Окно … уже существует» в журнале ошибок вместо второго окна.
-- 3. Склейки по времени выхода (Р-8): f_CheckLinkedTariffWindows — время внутри эфирного промежутка объединённой пары
--    (открытый интервал); CheckLinkedWindows — объединение рвётся, если между окнами в эфире выходит чужое окно,
--    уведомление во времени выхода. На ArtvisDev на 11.10.2026 это не рвёт ни одной из 416 склеек.
--
-- Ставить после 31 (несёт его TariffWindowIUD) — без него скрипт ничего не меняет. Клиент qd2 — после скрипта.
-- Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 35_windows-duplicates-template-chains.sql

SET NOCOUNT ON;
GO
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) NOT LIKE N'%TariffWindowDayChange%'
BEGIN
    RAISERROR(N'35: сначала Deploy/31. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER FUNCTION [dbo].[f_CheckLinkedTariffWindows]
(
@windowDate datetime,
@massmediaId int
)
RETURNS bit
AS
BEGIN

-- Этап 7 (docs/tasks/window-actual-switch.md, Р-8): время @windowDate попадает внутрь эфирного промежутка
-- объединённой пары окон станции — голова A выходит раньше, хвост B позже (интервал открытый: та же минута, что у
-- головы или хвоста, — не вторжение). Раньше смотрели по времени по расписанию: «последнее окно до @windowDate
-- имеет продолжение». Поиск — в пределах суток до @windowDate (связи внутри дня); условие по dayActual держит план на
-- индексе (massmediaID, dayActual) при любом первом вызове — без него план мог читать всю историю станции.
IF EXISTS (
	select 1
	from TariffWindow a
		inner join TariffWindow b on b.windowId = a.windowNextId
	where a.massmediaID = @massmediaId
		and a.dayActual >= dateadd(day, -1, convert(date, @windowDate))
		and a.dayActual <= convert(date, @windowDate)
		and a.windowNextId is not null
		and a.windowDateActual < @windowDate
		and a.windowDateActual >= dateadd(day, -1, @windowDate)
		and b.windowDateActual > @windowDate)
	RETURN 1

RETURN 0

END
GO

CREATE OR ALTER procedure [dbo].[CheckLinkedWindows]
(
@startDate DATETIME, 
@finishDate DATETIME,
@massmediaId int,
@loggedUserId int
)
WITH EXECUTE AS OWNER
AS

Declare @windowId int, @windowNextId int, @windowDate1 datetime, @windowDate2 datetime

-- Этап 7 (docs/tasks/window-actual-switch.md, Р-8): объединение окон A → B рвётся, если между ними в эфире выходит
-- чужое окно станции (A.выход < C.выход < B.выход, интервал открытый). Раньше смотрели по времени по расписанию:
-- следующее после головы окно не было хвостом. Уведомление — во времени выхода.
DECLARE cur_windows CURSOR LOCAL STATIC FOR
Select a.windowId, a.windowNextId, a.windowDateActual, b.windowDateActual
From TariffWindow a
	inner join TariffWindow b on b.windowId = a.windowNextId
Where a.massmediaID = @massmediaId And a.windowDateActual >= @startDate And a.windowDateActual < @finishDate
	And a.dayActual >= convert(date, @startDate) And a.dayActual <= @finishDate And a.windowNextId is not null
	And exists (
		select 1 from TariffWindow c
		where c.massmediaID = @massmediaId
			and c.windowDateActual > a.windowDateActual
			and c.windowDateActual < b.windowDateActual
			and c.windowId <> a.windowId and c.windowId <> b.windowId)

Open cur_windows
Fetch Next From  cur_windows Into @windowId, @windowNextId, @windowDate1, @windowDate2
While @@FETCH_STATUS = 0
	Begin
	-- удаляем связь между окнами и отправляем уведомление траффик-менеджеру
	Update [dbo].[TariffWindow] Set windowNextId = Null Where windowId = @windowId
	Update [dbo].[TariffWindow] Set [windowPrevId] = Null Where windowId = @windowNextId

	Exec SayTrafficThatWindowLinkDeleted @loggedUserId, @massmediaId, @windowDate1, @windowDate2
	Fetch Next From  cur_windows Into @windowId, @windowNextId, @windowDate1, @windowDate2
	End

Close cur_windows
Deallocate cur_windows
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

-- Сообщение UIX_TariffWindow_Tariff_Date (есть в старых базах; на случай, если нет) + испанский перевод
DECLARE @msg NVARCHAR(4000) = N'Окно на этой радиостанции в данное время уже существует.';
DECLARE @es  NVARCHAR(4000) = N'Ya existe una ventana en esta emisora a esta hora.';
IF NOT EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'UIX_TariffWindow_Tariff_Date')
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('UIX_TariffWindow_Tariff_Date', @msg);
SELECT @msg = [message] FROM dbo.iMessage WHERE name = 'UIX_TariffWindow_Tariff_Date';
IF OBJECT_ID('dbo.iTranslation') IS NOT NULL AND @msg = N'Окно на этой радиостанции в данное время уже существует.'
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg AS [source], @es AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO

-- Проверка: новые версии применены.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) LIKE N'%UIX_TariffWindow_Tariff_Date%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) LIKE N'%f_CheckLinkedTariffWindows(@windowDateActual%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.GenerateTariffWindowByTemplate')) LIKE N'%UIX_TariffWindow_Tariff_Date%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.f_CheckLinkedTariffWindows')) LIKE N'%b.windowDateActual > @windowDate%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.CheckLinkedWindows')) LIKE N'%c.windowDateActual < b.windowDateActual%'
   AND EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'UIX_TariffWindow_Tariff_Date')
    PRINT N'ГОТОВО: окна — дубли, шаблон, склейки по времени выхода (4 объекта).';
ELSE
    RAISERROR(N'35: новая версия применена не ко всем объектам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
