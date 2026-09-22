


CREATE    PROCEDURE [dbo].[DiscountReleaseIUD]
(
@discountReleaseID smallint = NULL,
@massmediaID smallint = NULL,
@startDate datetime = NULL,
@isForType1 bit = 0,
@isForType2 bit = 0,
@isForType3 bit = 0,
@sourceDiscountReleaseID smallint = NULL,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
as
set nocount on
DECLARE 
	@Id int,
	@date datetime

IF @actionName = 'AddItem' BEGIN
	INSERT INTO [DiscountRelease](massmediaID, startDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @isForType1, @isForType2, @isForType3)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	-- Set finish date for previous discount release
	SELECT TOP 1 
		@Id = discountReleaseID		
	FROM	
		DiscountRelease
	WHERE	
		massmediaID = @massmediaID AND
		startDate	< @startDate
	ORDER BY 
		startDate DESC

	IF @Id IS NOT NULL
		UPDATE DiscountRelease SET finishDate = @startDate WHERE discountReleaseID = @Id
	SET @id = NULL 
	-- May be this discount release has finishDate 
	SELECT TOP 1 
		@Id = discountReleaseID,
		@date = startDate
	FROM	
		DiscountRelease
	WHERE	
		massmediaID = @massmediaID AND
		startDate	> @startDate
	ORDER BY 
		startDate 

	IF @Id IS NOT NULL
		UPDATE DiscountRelease SET finishDate = @startDate WHERE discountReleaseID = @DiscountReleaseID


	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'Clone' BEGIN
	-- Копия набора скидок радиостанции на новую дату принятия:
	-- те же суммы и проценты (DiscountValue), флаги типов кампаний берутся из паспорта.
	SELECT @massmediaID = massmediaID FROM DiscountRelease WHERE discountReleaseID = @sourceDiscountReleaseID

	IF @massmediaID IS NULL
	BEGIN
		raiserror('InternalError', 16, 1)
		return
	END

	IF EXISTS(SELECT * FROM DiscountRelease WHERE massmediaID = @massmediaID AND startDate = @startDate)
	BEGIN
		raiserror('DiscountReleaseStartDateExists', 16, 1)
		return
	END

	-- Конец нового периода — начало следующего набора (если он уже есть)
	SELECT TOP 1 @date = startDate
	FROM DiscountRelease
	WHERE massmediaID = @massmediaID AND startDate > @startDate
	ORDER BY startDate

	-- Набор, его суммы и конец предыдущего периода — одним целым
	SET XACT_ABORT ON
	BEGIN TRANSACTION

	INSERT INTO [DiscountRelease](massmediaID, startDate, finishDate, isForType1, isForType2, isForType3)
	VALUES(@massmediaID, @startDate, @date, @isForType1, @isForType2, @isForType3)

	SET @DiscountReleaseID = SCOPE_IDENTITY()

	-- Предыдущий набор заканчивается там, где начинается новый
	UPDATE DiscountRelease SET finishDate = @startDate
	WHERE discountReleaseID = (SELECT TOP 1 discountReleaseID
	                           FROM DiscountRelease
	                           WHERE massmediaID = @massmediaID AND startDate < @startDate
	                           ORDER BY startDate DESC)

	INSERT INTO [DiscountValue](discountReleaseID, summa, discount)
	SELECT @DiscountReleaseID, summa, discount
	FROM DiscountValue
	WHERE discountReleaseID = @sourceDiscountReleaseID

	COMMIT TRANSACTION

	EXEC DiscountReleases @discountReleaseID = @discountReleaseID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	SELECT @date = finishDate	FROM DiscountRelease
	WHERE	DiscountReleaseID = @DiscountReleaseID

	DELETE FROM [DiscountRelease] WHERE DiscountReleaseID = @DiscountReleaseID

	UPDATE DiscountRelease SET finishDate = @date 
	WHERE	massmediaID = @massmediaID AND finishDate = @startDate	
	
END
ELSE IF @actionName = 'UpdateItem' BEGIN
	UPDATE	
		[DiscountRelease]
	SET			
		startDate = @startDate,
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

