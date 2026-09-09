/*
    ПРОД-ДЕПЛОЙ: запрет переноса объединённых окон в неправильный порядок.
    Справочник и разбор слабых мест — docs/window-merging.md §3.

    ЗАЧЕМ
      Объединённые окна трафика (windowPrevId/windowNextId) — это непрерывный
      кусок эфира из нескольких окон подряд. «Голова» цепочки в эфире идёт раньше
      «хвоста». Перенос времени выхода (windowDateActual) раньше ничего про
      объединение не знал, и окно-«голову» можно было передвинуть на время позже
      окна-«хвоста» — или наоборот. Порядок в эфире ломался, а это молча портило:
        - DJin-выгрузку: пара выходила двумя отдельными блоками в обратном
          порядке (хвост без заголовка блока и входного джингла, суммирование
          длительности не находило партнёра);
        - обвязку политагитации: локальный (44) и федеральный (55) идентификаторы
          СМИ звучали в обратном порядке.

    ЧТО ДЕЛАЕТ ЭТОТ СКРИПТ (идемпотентно)
      1. TariffWindowMoveTime (шаблонный перенос из сетки трафика) — перед
         переносом проверяет, что порядок каждой затронутой цепочки по БУДУЩЕМУ
         фактическому времени сохранится; иначе RAISERROR('LinkedWindowsWrongOrder').
      2. TariffWindowIUD @actionName='UpdateItem' (правка одного окна через паспорт
         «Свойства» → «Время выхода реальное»; там же пишутся связи при
         объединении/отмене объединения) — та же проверка порядка. Срабатывает
         только при реальном изменении времени ИЛИ при установке связи; правки
         isDisabled/price/продолжительности объединение не задевают.
      3. iMessage: текст кода ошибки LinkedWindowsWrongOrder.

    ЧЕГО НЕ ДЕЛАЕТ
      - НЕ запрещает менять продолжительность объединённого окна (обсуждается с
        заказчиком).
      - НЕ запрещает объединять окна, если тариф уже «тариф-продолжение»
        (обсуждается с заказчиком).
      Не переделывает механизм. Существующие рассинхроны в данных не лечит.

    ОТКАТ
      Прежние версии процедур — в git до этого коммита. Строку iMessage можно
      оставить (безвредна) либо удалить по name.
*/

SET NOCOUNT ON;
GO

-- Код ошибки продолжительности из ранней версии этого скрипта больше не нужен.
IF EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'CannotChangeDurationOfLinkedWindow')
    DELETE FROM [dbo].[iMessage] WHERE name = 'CannotChangeDurationOfLinkedWindow';
GO

-- =====================================================================
-- 1. TariffWindowMoveTime
-- =====================================================================
GO
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

    -- Порядок цепочки по будущему факт. времени: голова строго раньше хвоста.
    -- Драйвер — @moved (мало строк), соседи — по PK. Полусвязи не ловятся.
    IF EXISTS (
        SELECT 1
        FROM @moved m
            INNER JOIN TariffWindow cur ON cur.windowId = m.windowId
            LEFT JOIN TariffWindow p  ON p.windowId = cur.windowPrevId
            LEFT JOIN @moved mp       ON mp.windowId = p.windowId
            LEFT JOIN TariffWindow n  ON n.windowId = cur.windowNextId
            LEFT JOIN @moved mn       ON mn.windowId = n.windowId
        WHERE
            (p.windowId IS NOT NULL
                AND COALESCE(mp.newActual, p.windowDateActual) >= m.newActual)
            OR
            (n.windowId IS NOT NULL
                AND m.newActual >= COALESCE(mn.newActual, n.windowDateActual))
    )
    BEGIN
        RAISERROR('LinkedWindowsWrongOrder', 16, 1);
        RETURN;
    END

    UPDATE tw
    SET tw.windowDateActual = m.newActual
    FROM TariffWindow tw
        INNER JOIN @moved m ON m.windowId = tw.windowId;
END
GO

-- =====================================================================
-- 2. TariffWindowIUD — правка одного окна через паспорт («Свойства»)
--    Добавлена одна проверка в ветку UpdateItem; остальное без изменений.
-- =====================================================================
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
-- 3. Текст сообщения (upsert — при повторном запуске текст обновляется)
-- =====================================================================
DECLARE @msgWrongOrder nvarchar(4000) = N'Перенос не выполнен: после него объединённые рекламные окна вышли бы в эфир в неправильном порядке — окно, которое должно идти позже, оказалось бы раньше. Отмените объединение окон, выполните перенос и объедините их заново.';

IF EXISTS (SELECT 1 FROM [dbo].[iMessage] WHERE name = 'LinkedWindowsWrongOrder')
	UPDATE [dbo].[iMessage] SET [message] = @msgWrongOrder WHERE name = 'LinkedWindowsWrongOrder';
ELSE
	INSERT INTO [dbo].[iMessage] (name, [message]) VALUES ('LinkedWindowsWrongOrder', @msgWrongOrder);
GO

-- =====================================================================
-- Проверка
-- =====================================================================
SELECT name, [message] FROM [dbo].[iMessage] WHERE name = 'LinkedWindowsWrongOrder';

SELECT o.name, o.modify_date
FROM sys.objects o
WHERE o.name IN ('TariffWindowMoveTime', 'TariffWindowIUD')
ORDER BY o.name;
GO
