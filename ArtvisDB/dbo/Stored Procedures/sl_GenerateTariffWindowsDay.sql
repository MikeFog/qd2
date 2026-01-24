
CREATE        Proc [dbo].[sl_GenerateTariffWindowsDay]
(
@massmediaId smallint,
@pricelistId smallint,
@date datetime,
@broadcastStart datetime
)
as
SET NOCOUNT ON
SET DATEFIRST 1

Declare 
	@weekday int
Set @weekday = DatePart(dw, @date)

INSERT INTO [TariffWindow]([tariffId], [windowDateOriginal], [windowDateActual], [duration], [price], [massmediaID], [maxCapacity], dayActual, dayOriginal, duration_total)
Select
	tariffId,
	(
	@date + 
	Case 
		When '1/1/1900 ' + Convert(varchar(8), t.[time], 108) < @broadcastStart Then 1
		Else 0
	End
	) + Convert(varchar(8), t.[time], 108),
	(
	@date + 
	Case 
		When '1/1/1900 ' + Convert(varchar(8), t.[time], 108) < @broadcastStart Then 1
		Else 0
	End
	) + Convert(varchar(8), t.[time], 108),
	duration,
	price, 
	@massmediaId,
	[maxCapacity],
	@date,
	@date,
	duration_total
From
	Tariff t
Where
	t.pricelistId = @pricelistId AND (
		(t.[time] >= @broadcastStart AND
		(
		(t.monday = 1 And @weekday = 1)	Or
		(t.tuesday = 1 And @weekday = 2)	Or
		(t.wednesday = 1 And @weekday = 3)	Or
		(t.thursday = 1 And @weekday = 4)	Or
		(t.friday = 1 And @weekday = 5)	Or
		(t.saturday = 1 And @weekday = 6)	Or
		(t.sunday = 1 And @weekday = 7)
		))
		OR
		(t.[time] < @broadcastStart AND
		((t.monday = 1 And @weekday = 7)	Or
		(t.tuesday = 1 And @weekday = 1)	Or
		(t.wednesday = 1 And @weekday = 2)	Or
		(t.thursday = 1 And @weekday = 3)	Or
		(t.friday = 1 And @weekday = 4)	Or
		(t.saturday = 1 And @weekday = 5)	Or
		(t.sunday = 1 And @weekday = 6) ))
		)
	And Not Exists
	(
	Select * From TariffWindow tw Where tw.massmediaID = @massmediaId And 
	tw.windowDateOriginal = 
		@date + 
		Case 
			When '1/1/1900 ' + Convert(varchar(8), t.[time], 108) < @broadcastStart Then 1
			Else 0
		End
		+ Convert(varchar(8), t.[time], 108)
	)
	And Not Exists
	(
	Select * From DisabledWindow dw Where dw.massmediaId = @massmediaId And 
		@date + 
		Case 
			When '1/1/1900 ' + Convert(varchar(8), t.[time], 108) < @broadcastStart Then 1
			Else 0
		End
		 + Convert(varchar(8), t.[time], 108) between dw.startDate And dw.finishdate
	)

