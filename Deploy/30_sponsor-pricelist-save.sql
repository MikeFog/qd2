-- Спонсорский прайс-лист снова сохраняется (docs/broadcast-start.md [SQL-BS-02]).
-- sponsorPLIUD писала в broadcastStart параметр @broadcastStart, которого программа не передаёт (у сущности 12
-- такого атрибута нет): NULL в колонку NOT NULL — «Не удалось вставить значение NULL в столбец broadcastStart».
-- Падали все три действия: «Изменить», «Добавить» и «Клонировать». Теперь колонка не пишется: при создании и
-- клонировании — значение по умолчанию (00:00), при изменении — остаётся прежним. Параметр оставлен (контракт).
--
-- Только SQL, клиент не нужен (программа и веб). Независим от остальных; идемпотентен.
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 30_sponsor-pricelist-save.sql

SET NOCOUNT ON;
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
Parameters:
@basePricelistID
Tariff table for new pricelist will be populated base on tariffs from @basePricelistID
Modified by: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008
*/
CREATE OR ALTER PROCEDURE [dbo].[sponsorPLIUD]
(
@pricelistID smallint = NULL,
@sponsorProgramID smallint = NULL,
@startDate datetime = NULL,
@finishDate datetime = NULL,
@broadcastStart smalldatetime = null, -- не используется: колонку не пишем (broadcastStart, docs/broadcast-start.md [SQL-BS-02])
@bonus smallint = NULL,
@isStandAlone BIT = 0,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
IF @actionName IN('AddItem', 'UpdateItem', 'Clone') BEGIN
	IF @startDate > @finishDate BEGIN
		RAISERROR('StartFinishDateError', 16, 1)
		RETURN
		END

	IF EXISTS(
		SELECT * 
			FROM SponsorProgramPricelist	
		WHERE 
			(@startDate between startDate and finishDate Or 
			@finishDate between startDate and finishDate) and
			(IsNull(@pricelistID, 0) <> pricelistID Or @actionName <> 'UpdateItem') and
			sponsorProgramID = @sponsorProgramID
		) BEGIN
		RAISERROR('PLPeriodIntersection', 16, 1)
		RETURN
		END
	END

IF @actionName In ('AddItem', 'Clone') BEGIN
	Declare @basePricelistID smallint
	SET @basePricelistID = @pricelistID

	INSERT INTO [SponsorProgramPricelist] WITH (TABLOCKX) 
	(sponsorProgramID, startDate, finishDate, bonus, isStandAlone)
	VALUES(@sponsorProgramID, @startDate, @finishDate, @bonus, @isStandAlone)

	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @pricelistID = SCOPE_IDENTITY()
	
	IF @actionName = 'Clone' Begin
		-- Clone tariff list
		INSERT INTO [SponsorTariff]([pricelistID], [time], [monday], [tuesday], [wednesday], [thursday], [friday], [saturday], [sunday], [price], [duration], [comment], [isAlive], [path], [suffix], [needExt], [needInJingle], [needOutJingle])
		SELECT @pricelistID, [time], [monday], [tuesday], [wednesday], [thursday], [friday], [saturday], [sunday], [price], [duration], [comment], [isAlive], [path], [suffix], [needExt], [needInJingle], [needOutJingle]
		FROM [SponsorTariff]
		WHERE pricelistID = @basePricelistID

		End

	EXEC SponsorPricelists @pricelistID = @pricelistID
	END
ELSE IF @actionName = 'DeleteItem'
	DELETE FROM [SponsorProgramPricelist] WHERE pricelistID = @pricelistID
ELSE IF @actionName = 'UpdateItem' BEGIN
	Declare @oldStartDate datetime, @oldFinishDate datetime
	SELECT @oldStartDate = startDate, @oldFinishDate = finishDate FROM [SponsorProgramPricelist] WHERE pricelistID = @pricelistID

	-- если поменяли даты, то они не должны уходить за границы созданных рекламных кампаний
	if (@oldStartDate <> @startDate or @oldFinishDate <> @finishDate)
		and exists(select * from [dbo].[ProgramIssue] pi INNER JOIN [SponsorTariff] t On t.tariffID = pi.tariffID
					where t.pricelistID = @pricelistID
						and (pi.issueDate < @startDate or pi.issueDate >= DateAdd(DAY, 1, @finishDate)) )
	begin 
		raiserror('SponsorPricelistInUse', 16, 1)
		return
	end 

	UPDATE	
		[SponsorProgramPricelist]
	SET			
		sponsorProgramID = @sponsorProgramID, 
		startDate = @startDate, 
		finishDate = @finishDate, 
		bonus = @bonus,
		isStandAlone = @isStandAlone
	WHERE		
		pricelistID = @pricelistID

	EXEC SponsorPricelists @pricelistID = @pricelistID
	END
GO

-- Проверка: новая версия применена (broadcastStart не пишется).
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.sponsorPLIUD')) NOT LIKE N'%broadcastStart = @broadcastStart%'
   AND OBJECT_DEFINITION(OBJECT_ID(N'dbo.sponsorPLIUD')) NOT LIKE N'%isStandAlone, @broadcastStart)%'
    PRINT N'ГОТОВО: спонсорский прайс-лист сохраняется (sponsorPLIUD).';
ELSE
    RAISERROR(N'30: новая версия sponsorPLIUD не применена.', 16, 1);
GO
