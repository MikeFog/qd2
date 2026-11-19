-- =============================================
-- Author:		Denis Gladkikh
-- Create date: 01.02.2008
-- Description:	<Description,,>
-- =============================================
CREATE PROCEDURE [dbo].[PackageDiscountPriceListIUD]
(
	@packageDiscountPriceListId INT = NULL,
	@packageDiscountId INT = NULL,
	@startDate DATETIME = NULL,
	@finishDate datetime = null,
	@value decimal(18,2) = NULL,
	@discount decimal(9,4) = NULL,
	@eachVolume TINYINT = NULL,
	@maxPrimePercent TINYINT = NULL, -- максимальная доля прайма кампании, %; пусто: новый — 100, копия — как у исходного, правка — прежнее
	@sourcePackageDiscountPriceListId INT = NULL,
	@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
begin
SET NOCOUNT on
DECLARE 
	@Id int
IF @actionName = 'Clone'
	SELECT @packageDiscountId = packageDiscountID
	FROM PackageDiscountPriceList
	WHERE packageDiscountPriceListID = @sourcePackageDiscountPriceListId
IF @actionName IN('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('StartFinishDateError', 16, 1)
		RETURN
	end
END
	-- При клонировании @packageDiscountPriceListId — исходный прайс-лист, из проверки его исключать нельзя
	if @actionName in ('AddItem', 'UpdateItem', 'Clone') 
		and exists(select * 
	          from PackageDiscountPriceList pdpl 
		           where pdpl.packageDiscountID = @packageDiscountId and 
				(@actionName = 'Clone' or @packageDiscountPriceListId is null or pdpl.packageDiscountPriceListID <> @packageDiscountPriceListId)
				and (pdpl.startDate <= @finishDate
				and pdpl.finishDate >= @startDate))
	begin
		raiserror('PackageDiscountsCross',16,1)
		return 
	end

	IF @actionName = 'AddItem' BEGIN
		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume, maxPrimePercent)
		VALUES(@packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume, ISNULL(@maxPrimePercent, 100))

		if @@rowcount <> 1
		begin
			raiserror('InternalError', 16, 1)
			return 
		end 

		SET @packageDiscountPriceListId = SCOPE_IDENTITY()
		
		EXEC [PackageDiscountPriceLists] @packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'Clone' BEGIN
		-- Копия прайс-листа пакетной скидки на новый период: те же радиостанции и типы кампаний,
		-- значения (сумма, скидка, процент заполнения, процент прайма) берутся из паспорта.
		IF @packageDiscountId IS NULL
		BEGIN
			raiserror('InternalError', 16, 1)
			return 
		END

		-- Прайс-лист и его радиостанции — одним целым
		SET XACT_ABORT ON
		BEGIN TRANSACTION

		INSERT INTO [PackageDiscountPriceList](packageDiscountId, startDate, finishDate, [value], discount, eachVolume, maxPrimePercent)
		SELECT @packageDiscountId, @startDate, @finishDate, @value, @discount, @eachVolume, ISNULL(@maxPrimePercent, src.maxPrimePercent)
		FROM PackageDiscountPriceList src
		WHERE src.packageDiscountPriceListID = @sourcePackageDiscountPriceListId

		SET @packageDiscountPriceListId = SCOPE_IDENTITY()

		INSERT INTO [PackageDiscountMassmedia](packageDiscountPriceListID, massmediaID, isForType1, isForType2, isForType3)
		SELECT @packageDiscountPriceListId, massmediaID, isForType1, isForType2, isForType3
		FROM PackageDiscountMassmedia
		WHERE packageDiscountPriceListID = @sourcePackageDiscountPriceListId

		COMMIT TRANSACTION

		EXEC [PackageDiscountPriceLists] @packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'DeleteItem' BEGIN
		-- Прайс-лист, по которому уже посчитаны акции, удалять нельзя
		IF EXISTS(SELECT * FROM [Action] WHERE packageDiscountPriceListID = @packageDiscountPriceListId) BEGIN
			raiserror('PackageDiscountInUse', 16, 1)
			return
		END

		DELETE FROM PackageDiscountPriceList WHERE packageDiscountPriceListId = @packageDiscountPriceListId
	END
	ELSE IF @actionName = 'UpdateItem' BEGIN

		UPDATE	
			PackageDiscountPriceList
		SET			
			startDate = @startDate,
			finishDate = @finishDate, 
			[value] = @value,
			discount = @discount,
			eachVolume = @eachVolume,
			maxPrimePercent = ISNULL(@maxPrimePercent, maxPrimePercent) -- старый клиент без поля порог не сбрасывает
		WHERE		
			packageDiscountPriceListId = @packageDiscountPriceListId

		EXEC PackageDiscountPriceLists @packageDiscountPriceListId = @packageDiscountPriceListId
	END
END