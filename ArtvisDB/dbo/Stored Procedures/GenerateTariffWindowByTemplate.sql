-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 13.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[GenerateTariffWindowByTemplate]
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

