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

