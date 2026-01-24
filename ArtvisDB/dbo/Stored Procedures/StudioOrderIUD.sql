CREATE                PROCEDURE [dbo].[StudioOrderIUD]
(
@studioOrderID int = NULL,
@actionID int = NULL,
@studioID smallint = NULL,
@agencyID smallint = NULL,
@rolstyleID smallint = NULL,
@paymentTypeID smallint = NULL,
@rollerID int = NULL,
@rollerDuration int = NULL,
@isComplete bit = NULL,
@ratio float = 1,
@tariffID int = NULL,
@grantorID smallint = NULL,
@loggedUserId smallint = NULL,
@actionName varchar(32),
@name varchar(64) = NULL
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
DECLARE  @price money, @finalPrice money, @finishDate datetime 


IF @actionName = 'AddItem' OR @actionName = 'UpdateItem'
BEGIN
	IF EXISTS(SELECT * FROM [Roller] r LEFT JOIN [StudioOrder] so ON r.[rollerID] = so.[rollerID] 
		WHERE r.name = @name AND (so.[studioOrderID] IS NULL OR so.[studioOrderID] <> @studioOrderID)  and r.isMute = 0)
	BEGIN
		RAISERROR('RollerName_Unique', 16, 1)
		RETURN
	END
END

IF @actionName = 'AddItem' BEGIN
	SET	@tariffID = dbo.f_GetStudioTariffId(@studioId, @rolstyleID, getdate())
	SET @price = dbo.f_OrderPrice(@rollerDuration, @tariffID)
	SET @finalPrice = @price * @ratio

	INSERT INTO [StudioOrder](actionID, studioID, agencyID, rolstyleID, paymentTypeID, [name], price, rollerID, rollerDuration, ratio, tariffID, userID)
	VALUES(@actionID, @studioID, @agencyID, @rolstyleID, @paymentTypeID, @name, @price, @rollerID, @rollerDuration, @ratio, @tariffID, @loggedUserId)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @StudioOrderID = SCOPE_IDENTITY()
	
	Exec StudioOrders @StudioOrderID = @StudioOrderID, @loggedUserId = @loggedUserId

	-- Update Action properties
	Exec StudioOrderActionIUD 
		@actionName = 'UpdatePrice', @priceChange = @price, 
		@finalPriceChange = @finalPrice, @actionID = @actionID

	END
ELSE IF @actionName = 'DeleteItem' Begin
	SELECT 
		@price = -price,
		@finalPrice = -finalPrice
	FROM [StudioOrder] WHERE StudioOrderID = @StudioOrderID
	
	DELETE FROM [StudioOrder] WHERE StudioOrderID = @StudioOrderID

	IF Not Exists (SELECT * FROM StudioOrder WHERE actionID = @actionID And finishDate Is Null)	
		SELECT @finishDate = Max(finishDate) FROM StudioOrder WHERE actionID = @actionID

	-- Update Action properties
	Exec StudioOrderActionIUD 
		@actionName = 'UpdatePrice', @priceChange = @price, 
		@finalPriceChange = @finalPrice, @actionID = @actionID,
		@finishDate = @finishDate	
	
	End
ELSE IF @actionName = 'UpdateItem' Begin
	DECLARE @priceCurrent money, @finalPriceCurrent money, @oldIsReady bit
	SELECT 
		@priceCurrent = price, 
		@finalPriceCurrent = finalPrice,
		@oldIsReady = isComplete
	FROM StudioOrder 
	WHERE studioOrderID = @studioOrderID
	
	IF (@isComplete = 1 AND @oldIsReady = 0)
	BEGIN
		DECLARE @firmID INT, @rolTypeID INT
		
		SET @firmID = (SELECT firmID FROM StudioOrderAction WHERE actionID = @actionID)
		
		DECLARE @coalesceRollerDuration INT
		SET @coalesceRollerDuration = coalesce(@rollerDuration, 0)

		DECLARE @todayDate DATETIME
		SET @todayDate = GETDATE()

		if @name is null 
		begin 
			raiserror('StudioOrderMustHaveName', 16, 1)
			return
		end 

		EXEC [RollerIUD]
			@name = @name, --  nvarchar(64)
			@duration = @coalesceRollerDuration, --  int
			@firmID = @firmID, --  smallint
			@rolTypeID = @rolTypeID, --  smallint
			@rolStyleID = @rolStyleID, --  smallint
			@path = NULL, --  nvarchar(1024)
			@isEnabled = 0, --  tinyint
			@actionName = 'AddItem', --  varchar(32)
			@createDate = @todayDate,
			@studioOrderID = @studioOrderID,
			@rolActionTypeID = 1,
			@isCommon = 0,
			@rollerID = @rollerID out 
	
		UPDATE StudioOrder SET rollerID = @rollerID WHERE [studioOrderID] = @studioOrderID
	END
	ELSE
	BEGIN
		IF @oldIsReady = 1 And @isComplete = 0 
		BEGIN
			IF EXISTS(SELECT * FROM [PaymentStudioOrderAction] WHERE [actionID] = @actionID)
				BEGIN
					RAISERROR('OrderActionAlreadyPayment', 16, 1)
					RETURN
				END
			IF EXISTS(SELECT * 
				FROM [Issue] i INNER JOIN dbo.[StudioOrder] so 
				ON so.rollerID = i.rollerID AND so.studioOrderID = @studioOrderID)
				BEGIN
					RAISERROR('OrderActionRollerAlreadyUsed', 16, 1)
					RETURN
				END
			
			DELETE FROM Roller WHERE rollerID IN (SELECT rollerID FROM dbo.[StudioOrder] WHERE [studioOrderID] = @studioOrderID)
		END
	END
	
	if @oldIsReady = 1 And @isComplete = 1 and exists(select * 
															from StudioOrder 
															where studioOrderID = @studioOrderID 
																and agencyID = @agencyID 
																and rolstyleID = @rolstyleID 
																and rollerID = @rollerID 
																and [name] = @name 
																and isComplete = @isComplete
																and ratio = @ratio)
	begin 
		-- 19/10/2010 - Removed, because artvis asked us to remove this check
		--IF EXISTS(SELECT * 
		--		FROM [Issue] i INNER JOIN dbo.[StudioOrder] so 
		--		ON so.rollerID = i.rollerID AND so.studioOrderID = @studioOrderID)
		--		BEGIN
		--			RAISERROR('OrderActionRollerAlreadyUsed', 16, 1)
		--			RETURN
		--		end
	
		update StudioOrder set paymentTypeID = @paymentTypeID where studioOrderID = @studioOrderID
		Exec StudioOrders @StudioOrderID = @StudioOrderID, @loggedUserId = @loggedUserId
		return 
	end 
	
	-- If Order was already marked as ready all changes should be rejected
	-- First order should be marked as not ready
	IF @oldIsReady = 1 And @isComplete = 1 BEGIN
		RAISERROR('OrderChangesRejected', 16, 1)
		RETURN	
	END

	declare @today datetime 
	declare @billDate datetime 
	
	select @today = dbo.ToShortDate(getdate())
	
	select @billDate = so.billDate from StudioOrderBill so inner join dbo.StudioOrder o on so.actionID = o.actionID where o.studioOrderID = @studioOrderID
	
	if @oldIsReady = 0 and @isComplete = 1 
		and @billDate is not null
		and coalesce((select avg(at.divisor) from dbo.AgencyTax at where at.agencyID = @agencyID and @today between at.startDate and at.finishDate ), 0)
		 <> coalesce((select avg(at.divisor) from dbo.AgencyTax at where at.agencyID = @agencyID and @billDate between at.startDate and at.finishDate ), 0)
	begin 
		insert into Announcement ( dateConfirmed, fromUserID, toUserID, subject) 
		select null, @loggedUserId, so.userID, 'Вероятно налог для агенства ''' + ag.name + ''' изменился для акции по производству роликов №' + cast(so.actionID as varchar(20)) + ', фирма: ''' + f.name + '''. Распечатайте и проверьте документы.'
		from dbo.StudioOrder so 
			inner join dbo.StudioOrderAction a on so.actionID = a.actionID
			inner join dbo.Firm f on a.firmID = f.firmID
			inner join dbo.Agency ag on so.agencyID = ag.agencyID
		where so.studioOrderID = @StudioOrderID
	end

	UPDATE	
		[StudioOrder]
	SET			
		agencyID = @agencyID, 
		rolstyleID = @rolstyleID, 
		paymentTypeID = @paymentTypeID, 
		rollerID = @rollerID, 
		[name] = @name,
		rollerDuration = @rollerDuration, 
		isComplete = @isComplete, 
		finishDate = 
			CASE @isComplete
				WHEN 1 THEN @today
				ELSE Null
			END,
		ratio = @ratio
	WHERE		
		studioOrderID = @studioOrderID

	Exec StudioOrders @StudioOrderID = @StudioOrderID, @loggedUserId = @loggedUserId

	IF Not Exists (SELECT * FROM StudioOrder WHERE actionID = @actionID And finishDate Is Null)	
		SELECT @finishDate = Max(finishDate) FROM StudioOrder WHERE actionID = @actionID
		
	-- если все ролики выполнены - обновляем статус всего заказа.
	SET @price = dbo.f_OrderPrice(@rollerDuration, @tariffID)
	SET @finalPrice = @price * @ratio

	-- Update Action properties
	SET @price = @priceCurrent - @price
	SET @finalPrice = @finalPrice - @finalPriceCurrent

	Exec StudioOrderActionIUD 
		@actionName = 'UpdatePrice', @priceChange = @price, 
		@finalPriceChange = @finalPrice, 
		@actionID = @actionID, @finishDate = @finishDate
		
	if @finishDate is null 
		select @finishDate = so.createDate from StudioOrder so where so.studioOrderID = @studioOrderID
		
	IF dbo.fn_IsAcceptRatioForUser(@loggedUserId, @ratio, @finishDate, @finishDate) = 0 
		and dbo.fn_IsAcceptRatioForUser(@grantorID, @ratio, @finishDate, @finishDate) = 0
		begin
			RAISERROR('MaxRatioExcess', 16, 1)
			RETURN
		end

	end

-- If grantor was specified add record to the history table 
IF Not @grantorID Is Null And @actionName IN ('UpdateItem', 'AddItem') BEGIN
	DECLARE @msg nvarchar(4000)
	SET @msg = 'Установлена скидка ' + RTRIM(@ratio) + ', Акция №' + RTRIM(@actionID) + ', Заказ: ' + @name
	Exec ConfirmationHistoryID @confirmationTypeID = 1, @userID = @loggedUserId,
		@grantorID = @grantorID, @description = @msg, 
		@actionName = 'AddItem'
	END
