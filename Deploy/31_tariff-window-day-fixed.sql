-- Фактическое окно и время, этап 0 (docs/tasks/window-actual-switch.md §5.5 п. 3; решение Р-2 от 07.10.2026).
-- TariffWindowIUD:
-- 1. День окна менять нельзя — только время выхода в пределах дня. «Изменить» (паспорт окна в программе и веб-версии,
--    объединение окон) отказывает, если время выхода попадает на другой день; «Добавить» («Создать новое окно…») —
--    если время выхода и время по расписанию в разных днях. Новое сообщение TariffWindowDayChange (+ перевод es).
--    dayOriginal = dayActual становится гарантией: места «день окна» по правилу не переводятся. На ArtvisDev у всех
--    2 011 204 окон день времени выхода и день по расписанию совпадают — правку ни одного окна запрет не блокирует.
-- 2. Закрытый период (профилактика, DisabledWindow) проверяется по времени выхода, а не ещё и по времени по
--    расписанию. На ArtvisDev окон, которые закрытый период задевает только по времени по расписанию, нет.
--
-- Ставить после 29 (несёт его правку TariffWindowIUD). Клиент не нужен; после наката перезапустить qd2 и веб
-- (новое сообщение в iMessage читается при старте). Идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 31_tariff-window-day-fixed.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
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

-- Сообщение TariffWindowDayChange (+ испанский перевод, где есть многоязычность веба)
DECLARE @msg NVARCHAR(4000) = N'Рекламное окно нельзя перенести на другой день: время выхода можно менять только в пределах дня окна. Операция прервана.';
DECLARE @es  NVARCHAR(4000) = N'La ventana publicitaria no se puede trasladar a otro día: la hora de emisión solo puede cambiarse dentro del día de la ventana. Operación cancelada.';

IF EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'TariffWindowDayChange')
    UPDATE dbo.iMessage SET [message] = @msg WHERE name = 'TariffWindowDayChange';
ELSE
    INSERT INTO dbo.iMessage (name, [message]) VALUES ('TariffWindowDayChange', @msg);

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

-- Проверка: новая версия применена, сообщение есть.
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) LIKE N'%TariffWindowDayChange%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) NOT LIKE N'%@windowDateOriginal between dw.startDate%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.TariffWindowIUD')) NOT LIKE N'%DATEPART(%broadcastStart%'
   AND EXISTS (SELECT 1 FROM dbo.iMessage WHERE name = 'TariffWindowDayChange')
    PRINT N'ГОТОВО: день окна менять нельзя, закрытый период — по времени выхода (TariffWindowIUD + сообщение).';
ELSE
    RAISERROR(N'31: новая версия TariffWindowIUD или сообщение не применены.', 16, 1);
GO
