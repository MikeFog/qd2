


CREATE    PROCEDURE [dbo].[DiscountReleaseIUD]
(
@discountReleaseID smallint = NULL,
@massmediaID smallint = NULL,
@startDate datetime = NULL,
@finishDate datetime = NULL,
@isForType1 bit = 0,
@isForType2 bit = 0,
@isForType3 bit = 0,
@sourceDiscountReleaseID smallint = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
set nocount on

-- Набор скидок действует с startDate по finishDate включительно, обе даты задаются явно
-- (как у прайс-листов). Соседние наборы не подгоняются, периоды одной радиостанции
-- пересекаться не могут.
IF @actionName = 'Clone'
	SELECT @massmediaID = massmediaID FROM DiscountRelease WHERE discountReleaseID = @sourceDiscountReleaseID
ELSE IF @actionName = 'UpdateItem'
	SELECT @massmediaID = massmediaID FROM DiscountRelease WHERE discountReleaseID = @discountReleaseID

IF @actionName IN ('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @massmediaID IS NULL OR @startDate IS NULL OR @finishDate IS NULL BEGIN
		raiserror('InternalError', 16, 1)
		return
	END

	SET @startDate = CAST(@startDate AS date)
	SET @finishDate = CAST(@finishDate AS date)

	IF @startDate > @finishDate BEGIN
		raiserror('StartFinishDateError', 16, 1)
		return
	END

	IF EXISTS(
		SELECT * FROM DiscountRelease
		WHERE
			massmediaID = @massmediaID AND
			startDate <= @finishDate AND
			finishDate >= @startDate AND
			(@actionName <> 'UpdateItem' OR discountReleaseID <> @discountReleaseID)
		) BEGIN
		raiserror('PLPeriodIntersection', 16, 1)
		return
	END
END

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [DiscountRelease](massmediaID, startDate, finishDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @finishDate, @isForType1, @isForType2, @isForType3)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return
	end

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'Clone' BEGIN
	-- Копия набора скидок радиостанции на новый период:
	-- те же суммы и проценты (DiscountValue), даты и флаги типов кампаний берутся из паспорта.
	SET XACT_ABORT ON
	BEGIN TRANSACTION

	INSERT INTO [DiscountRelease](massmediaID, startDate, finishDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @finishDate, @isForType1, @isForType2, @isForType3)

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	INSERT INTO [DiscountValue](discountReleaseID, summa, discount)
	SELECT @DiscountReleaseID, summa, discount
	FROM DiscountValue
	WHERE discountReleaseID = @sourceDiscountReleaseID

	COMMIT TRANSACTION

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	-- Набор, по которому уже посчитаны кампании, удалять нельзя
	IF EXISTS(SELECT * FROM Campaign WHERE discountReleaseID = @discountReleaseID) BEGIN
		raiserror('DiscountReleaseInUse', 16, 1)
		return
	END

	DELETE FROM [DiscountRelease] WHERE DiscountReleaseID = @DiscountReleaseID
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE
		[DiscountRelease]
	SET
		startDate = @startDate,
		finishDate = @finishDate,
		isForType1 = @isForType1,
		isForType2 = @isForType2,
		isForType3 = @isForType3
	WHERE
		discountReleaseID = @discountReleaseID

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID

END
GO
GRANT EXECUTE
    ON OBJECT::[dbo].[DiscountReleaseIUD] TO PUBLIC
    AS [dbo];

