-- Объединённые тарифы: перенос времени выхода окна не должен ломать порядок их окон в эфире.
--
-- Тарифы, объединённые в прайс-листе (TariffUnion, «Объединить с блоком» в тарифе), дают в каждом дне пару
-- окон, которые звучат одним блоком: окно тарифа, затем окно его продолжения. В трафик-менеджменте время
-- выхода окна можно было перенести так, что окно-продолжение выходило раньше (или одновременно): защита от
-- неправильного порядка (09.09.2026, LinkedWindowsWrongOrder) стояла только на объединении окон
-- (windowPrevId/windowNextId), объединённые тарифы не проверялись. См. docs/window-merging.md §3 (#1).
--
--   1. TariffWindowMoveTime — массовый перенос («Перенос времени выхода» в десктопе, «Изменить окна» в вебе).
--      Окна, перенос которых нарушил бы порядок (объединённые окна ИЛИ объединённые тарифы), теперь
--      пропускаются, остальные переносятся. Процедура возвращает число перенесённых окон и список
--      пропущенных — новый клиент показывает его журналом «Перенесено окон: N, не перенесено: M».
--      Раньше нарушение по объединённым окнам отменяло перенос целиком (LinkedWindowsWrongOrder).
--   2. TariffWindowIUD UpdateItem — правка одного окна («Свойства» → «Время выхода реальное»): отказ
--      UnitedTariffWindowsWrongOrder (новое сообщение), если меняется время выхода окна объединённого тарифа
--      и порядок нарушается. Проверка объединённых окон (LinkedWindowsWrongOrder) — без изменений.
--
-- Идемпотентен (CREATE OR ALTER + upsert сообщения), данные не трогает.
-- Клиент: новые Merlin.exe и веб показывают пропущенные окна; старый клиент тоже защищён, но пропуск
-- молчаливый (список процедуры он не читает). После наката перезапустить qd2 и веб — словарь iMessage
-- читается при старте.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 14_united-tariff-windows-order.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;   -- iTranslation.sourceHash - вычисляемая колонка
GO

-- =====================================================================
-- 1. TariffWindowMoveTime
-- =====================================================================
-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 02.02.2009
-- Description:	Шаблонный перенос времени
-- =============================================
CREATE OR ALTER PROCEDURE [dbo].[TariffWindowMoveTime]
(
    @time datetime,
    @newtime datetime,
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
AS
BEGIN
    SET NOCOUNT ON;
    SET DATEFIRST 1; -- Понедельник = 1

    DECLARE @needaddday bit

    IF EXISTS(
        SELECT *
        FROM Pricelist pl
        WHERE pl.PricelistID = @pricelistID
            AND @time < pl.broadcastStart
    )
        SET @needaddday = 1
    ELSE
        SET @needaddday = 0

    -- Окна под перенос + их будущее фактическое время выхода.
    DECLARE @moved TABLE (windowId int PRIMARY KEY, newActual datetime NOT NULL);

    INSERT INTO @moved (windowId, newActual)
    SELECT
        tw.windowId,
        CONVERT(datetime,
            LEFT(CONVERT(varchar, CASE @needaddday WHEN 1 THEN DATEADD(day, 1, tw.dayOriginal) ELSE tw.dayOriginal END, 120), 11)
            + RIGHT(CONVERT(varchar, @newtime, 120), 8),
        120)
    FROM TariffWindow tw
        INNER JOIN Pricelist pl ON tw.massmediaID = pl.massmediaID
            AND pl.pricelistID = @pricelistid
    WHERE tw.dayOriginal BETWEEN @startdate AND @finishdate
        AND tw.windowDateOriginal = CONVERT(datetime,
                LEFT(CONVERT(varchar, CASE @needaddday WHEN 1 THEN DATEADD(day, 1, tw.dayOriginal) ELSE tw.dayOriginal END, 120), 11)
                + RIGHT(CONVERT(varchar, @time, 120), 8),
            120)
        AND (
            (@monday    = 1 AND DATEPART(dw, tw.dayOriginal) = 1) OR
            (@tuesday   = 1 AND DATEPART(dw, tw.dayOriginal) = 2) OR
            (@wednesday = 1 AND DATEPART(dw, tw.dayOriginal) = 3) OR
            (@thursday  = 1 AND DATEPART(dw, tw.dayOriginal) = 4) OR
            (@friday    = 1 AND DATEPART(dw, tw.dayOriginal) = 5) OR
            (@saturday  = 1 AND DATEPART(dw, tw.dayOriginal) = 6) OR
            (@sunday    = 1 AND DATEPART(dw, tw.dayOriginal) = 7)
        );

    -- Соседи переносимых окон по объединению, см. docs/window-merging.md §3 (#1):
    -- цепочка окон (windowPrevId/windowNextId) и объединение тарифов (TariffUnion —
    -- окно тарифа и окно его тарифа-продолжения в тот же день).
    -- Драйвер — @moved (мало строк), соседи — по PK и (tariffId, dayOriginal).
    -- Полусвязи цепочки не ловятся.
    DECLARE @links TABLE (windowId int NOT NULL, neighborId int NOT NULL, isNext bit NOT NULL, isTariffUnion bit NOT NULL);

    INSERT INTO @links (windowId, neighborId, isNext, isTariffUnion)
    SELECT m.windowId, cur.windowPrevId, 0, 0
    FROM @moved m
        INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
    WHERE cur.windowPrevId IS NOT NULL
    UNION ALL
    SELECT m.windowId, cur.windowNextId, 1, 0
    FROM @moved m
        INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
    WHERE cur.windowNextId IS NOT NULL
    UNION ALL
    SELECT m.windowId, p.windowId, 0, 1
    FROM @moved m
        INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
        INNER JOIN TariffUnion tu ON tu.tariffUnionID = cur.tariffId
        INNER JOIN TariffWindow p ON p.tariffId = tu.tariffID AND p.dayOriginal = cur.dayOriginal
    UNION ALL
    SELECT m.windowId, n.windowId, 1, 1
    FROM @moved m
        INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
        INNER JOIN TariffUnion tu ON tu.tariffID = cur.tariffId
        INNER JOIN TariffWindow n ON n.tariffId = tu.tariffUnionID AND n.dayOriginal = cur.dayOriginal;

    -- Окна, перенос которых нарушил бы порядок объединённых окон по будущему факт.
    -- времени (предыдущее строго раньше, следующее строго позже), не переносятся;
    -- остальные переносятся. На окно — одна строка: первый нарушенный сосед.
    DECLARE @skipped TABLE (windowId int PRIMARY KEY, neighborId int NOT NULL, neighborActual datetime NOT NULL,
        isNext bit NOT NULL, isTariffUnion bit NOT NULL);

    INSERT INTO @skipped (windowId, neighborId, neighborActual, isNext, isTariffUnion)
    SELECT windowId, neighborId, neighborActual, isNext, isTariffUnion
    FROM (
        SELECT l.windowId, l.neighborId, l.isNext, l.isTariffUnion,
            COALESCE(mn.newActual, n.windowDateActual) AS neighborActual,
            ROW_NUMBER() OVER (PARTITION BY l.windowId ORDER BY l.isTariffUnion, l.isNext) AS rn
        FROM @links l
            INNER JOIN @moved m ON m.windowId = l.windowId
            INNER JOIN TariffWindow n ON n.windowId = l.neighborId
            LEFT JOIN @moved mn ON mn.windowId = n.windowId
        WHERE (l.isNext = 0 AND COALESCE(mn.newActual, n.windowDateActual) >= m.newActual)
            OR (l.isNext = 1 AND m.newActual >= COALESCE(mn.newActual, n.windowDateActual))
    ) v
    WHERE v.rn = 1;

    DELETE m
    FROM @moved m
        INNER JOIN @skipped s ON s.windowId = m.windowId;

    UPDATE tw
    SET tw.windowDateActual = m.newActual
    FROM TariffWindow tw
        INNER JOIN @moved m ON m.windowId = tw.windowId;

    SELECT movedCount = COUNT(*) FROM @moved;

    -- Не перенесённые окна и нарушенный сосед (время оригинальное — как в сетке
    -- трафика, у соседа и фактическое).
    SELECT
        cur.windowDateOriginal,
        neighborDateOriginal = n.windowDateOriginal,
        s.neighborActual,
        s.isNext,
        s.isTariffUnion
    FROM @skipped s
        INNER JOIN TariffWindow cur ON cur.windowId = s.windowId
        INNER JOIN TariffWindow n ON n.windowId = s.neighborId
    ORDER BY cur.windowDateOriginal;
END
GO

-- =====================================================================
-- 2. TariffWindowIUD
-- =====================================================================
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

-- =====================================================================
-- 3. Сообщение UnitedTariffWindowsWrongOrder (+ испанский перевод, где есть многоязычность веба)
-- =====================================================================
DECLARE @msg NVARCHAR(4000) = N'Перенос не выполнен: окно относится к объединённым тарифам, и после переноса их окна вышли бы в эфир в неправильном порядке — окно, которое должно идти позже, оказалось бы раньше. Переносите окна объединённых тарифов по очереди так, чтобы порядок сохранялся.';
DECLARE @es  NVARCHAR(4000) = N'No se realizó el traslado: la ventana pertenece a tarifas unidas y, después del traslado, sus ventanas saldrían al aire en un orden incorrecto — la ventana que debe ir después quedaría antes. Traslade las ventanas de las tarifas unidas una por una de modo que se mantenga el orden.';

IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'UnitedTariffWindowsWrongOrder')
    UPDATE dbo.iMessage SET [message] = @msg WHERE name = 'UnitedTariffWindowsWrongOrder';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('UnitedTariffWindowsWrongOrder', @msg);

IF OBJECT_ID('dbo.iTranslation') IS NOT NULL
    MERGE dbo.iTranslation AS dst
    USING (SELECT 'es' AS lang, '' AS context, @msg AS [source], @es AS [text]) AS src
       ON dst.lang = src.lang AND dst.context = src.context
      AND dst.sourceHash = CONVERT(binary(32), HASHBYTES('SHA2_256', src.[source]))
    WHEN MATCHED AND dst.[text] <> src.[text] COLLATE Latin1_General_BIN THEN
        UPDATE SET [text] = src.[text]
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (lang, context, [source], [text]) VALUES (src.lang, src.context, src.[source], src.[text]);
GO

-- =====================================================================
-- Проверка
-- =====================================================================
SELECT name, [message] FROM dbo.iMessage WHERE name = 'UnitedTariffWindowsWrongOrder';
SELECT o.name, o.modify_date FROM sys.objects o WHERE o.name IN ('TariffWindowMoveTime', 'TariffWindowIUD') ORDER BY o.name;
GO
