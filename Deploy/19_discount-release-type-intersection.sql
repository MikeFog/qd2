-- Наборы скидок радиостанции (сущность 22): наборы для РАЗНЫХ типов кампаний одной станции могут действовать
-- одновременно. DiscountReleaseIUD отказывал с PLPeriodIntersection при любом пересечении периодов одной станции,
-- хотя скидка (hlp_CompanyDiscountCalculate) всегда выбиралась с учётом типа кампании. Так устроены скидки в Тюмени:
-- на 6 станциях с 01.01.2026 параллельно набор для линейных (тип 1) и набор для модульных (тип 3) — на них
-- 06.10.2026 упал перенос данных discount-release-finish-date-deploy.sql (исправлен тем же коммитом).
-- Теперь пересекаться не могут только периоды наборов одной станции с общим типом (isForType1/2/3).
-- Тело = master (с проверкой DiscountReleaseInUse при удалении).
--
-- Предусловие: накачены discount-release-finish-date-deploy.sql и discount-applied-pricelist-id-deploy.sql
-- (на Artvis — с 29.09.2026). Идемпотентен (CREATE OR ALTER), данные не трогает, клиент не нужен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 19_discount-release-type-intersection.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[DiscountReleaseIUD]
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
-- (как у прайс-листов). Соседние наборы не подгоняются, периоды наборов одной радиостанции
-- с общим типом кампаний пересекаться не могут (наборы для разных типов действуют параллельно).
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
			((isForType1 = 1 AND @isForType1 = 1) OR (isForType2 = 1 AND @isForType2 = 1) OR (isForType3 = 1 AND @isForType3 = 1)) AND
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
GO
