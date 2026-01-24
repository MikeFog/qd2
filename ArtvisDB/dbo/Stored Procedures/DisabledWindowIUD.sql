




CREATE      PROCEDURE [dbo].[DisabledWindowIUD]
(
@disabledWindowID int OUT,
@massmediaID smallint = NULL,
@startDate smalldatetime = NULL,
@finishDate smalldatetime = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
set nocount on

if @actionName in ('AddItem','UpdateItem')
begin 
	if @startDate >= @finishDate 
	Begin
		RAISERROR('StartFinishDateErrorDisabledWindow', 16, 1)
		RETURN	
	End
	
	IF EXISTS(SELECT * 	FROM [Issue] i  inner join TariffWindow tw on i.actualWindowID = tw.windowId
		WHERE tw.[massmediaID] = @massmediaID AND tw.windowDateActual BETWEEN @startDate AND @finishDate)
	BEGIN
		RAISERROR('CannotAddDisabledWindow', 16, 1)
		RETURN
	END 
	
	DELETE 
		FROM [TariffWindow] 
		WHERE [massmediaID] = @massmediaID AND ([windowDateActual] BETWEEN @startDate AND @finishDate) 
end 

IF @actionName = 'AddItem' 
BEGIN
	INSERT INTO [DisabledWindow](massmediaID, startDate, finishDate)
	VALUES(@massmediaID, @startDate, @finishDate)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @disabledWindowID = SCOPE_IDENTITY()

	EXEC disabledWindows @disabledWindowID = @disabledWindowID

END
ELSE IF @actionName = 'DeleteItem'
BEGIN 
	SELECT @massmediaID = dw.[massmediaID]
		, @startDate = dw.[startDate]
		, @finishdate = dw.[finishdate] 
	FROM [DisabledWindow] dw 
	WHERE [disabledWindowID] = @disabledWindowID

	DELETE FROM [DisabledWindow] WHERE DisabledWindowID = @disabledWindowID
	
	DECLARE cur_PriceLists CURSOR local fast_forward
	FOR 
		SELECT pl.[pricelistID] FROM [Pricelist] pl 
		WHERE pl.[startDate] <= @finishDate AND DATEADD(DAY, 1, pl.[finishDate]) >= @startDate AND pl.[massmediaID] = @massmediaID
		
	OPEN cur_PriceLists

	DECLARE @plID INT, @sDate DATETIME, @fDate DATETIME
	SELECT @sDate = dbo.ToShortDate(@startDate), @fDate = dbo.ToShortDate(DATEADD(DAY, 1, @finishDate))
	FETCH NEXT FROM cur_PriceLists INTO @plID
	WHILE @@FETCH_STATUS = 0
	BEGIN 
		EXEC [GenerateTariffWindows]
			@pricelistId = @plID, --  smallint
			@startDate = @sDate, --  datetime
			@finishDate = @fDate --  datetime
		FETCH NEXT FROM cur_PriceLists INTO @plID
	END 
	CLOSE cur_PriceLists
	DEALLOCATE cur_PriceLists
END 
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[DisabledWindow]
	SET			
		massmediaID = @massmediaID, 
		startDate = @startDate, 
		finishDate = @finishDate
	WHERE		
		disabledWindowID = @disabledWindowID

	EXEC disabledWindows @disabledWindowID = @disabledWindowID

	END

