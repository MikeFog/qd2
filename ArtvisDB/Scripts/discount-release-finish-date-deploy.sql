/*
    ПРОД-ДЕПЛОЙ: явная дата окончания у наборов скидок радиостанции («Дата принятия скидок», сущность 22).

    БЫЛО   у набора только дата принятия; DiscountRelease.finishDate заполнялась процедурой
           автоматически = дата принятия СЛЕДУЮЩЕГО набора (в период не входит), у последнего — NULL.
    СТАЛО  обе даты задаются явно, как у прайс-листов; finishDate — последний день периода
           ВКЛЮЧИТЕЛЬНО, NOT NULL; периоды одной радиостанции не пересекаются.

    ЧТО ДЕЛАЕТ
      1. Данные (один раз, пока колонка допускает NULL), в одной транзакции:
         - набор, за которым есть следующий: finishDate = начало следующего − 1 день;
         - последний набор с NULL: finishDate = 31.12.2026 (если начинается позже — 31.12 года начала);
         - последний набор с уже заполненной датой: finishDate − 1 день (как сейчас и считается);
         - finishDate NOT NULL; проверка «нет перевёрнутых и пересекающихся периодов», иначе откат.
      2. dbo.DiscountReleaseIUD — @finishDate; AddItem/UpdateItem/Clone проверяют порядок дат
         (StartFinishDateError) и пересечение (PLPeriodIntersection); соседние наборы больше
         не подгоняются, DeleteItem только удаляет.
      3. dbo.hlp_CompanyDiscountCalculate — дата окончания входит в период.
      4. dbo.DiscountReleases — фильтр «скрыть прошедшие» по включительной дате.
      5. Метаданные сущности 22: поле «Дата окончания» в паспорте, колонка в журналах (iEntityAttribute).
      Клиент: новый Merlin.exe (клон предзаполняет окончание 31.12). Старый клиент работает, но
      «Создать копию» в нём упрётся в пересечение с исходным набором.
      После наката клиентов qd2 перезапустить (метаданные читаются при старте).

    ИДЕМПОТЕНТНОСТЬ  повторный запуск безопасен: перенос данных пропускается, если колонка уже NOT NULL.
    ОТКАТ            процедуры — из предыдущего коммита; ALTER COLUMN finishDate DATETIME NULL;
                     данные назад не переводятся (включительная дата отличается ровно на 1 день).
*/

-- USE [Artvis];
-- GO

SET NOCOUNT ON;
-- sqlcmd по умолчанию создаёт процедуры с QUOTED_IDENTIFIER OFF — задаём явно, как у процедур на проде
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.DiscountRelease') IS NULL OR OBJECT_ID('dbo.DiscountReleaseIUD') IS NULL
   OR OBJECT_ID('dbo.hlp_CompanyDiscountCalculate') IS NULL
BEGIN
    RAISERROR('НЕ ТА БАЗА: нет DiscountRelease / DiscountReleaseIUD / hlp_CompanyDiscountCalculate. Деплой прерван.', 16, 1);
    SET NOEXEC ON;
END
GO
-- 1. Данные
IF COLUMNPROPERTY(OBJECT_ID('dbo.DiscountRelease'), 'finishDate', 'AllowsNull') = 1
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    UPDATE dr SET finishDate = CASE
            WHEN nx.nextStart IS NOT NULL THEN DATEADD(DAY, -1, nx.nextStart)
            WHEN dr.finishDate IS NOT NULL THEN DATEADD(DAY, -1, dr.finishDate)
            ELSE DATEFROMPARTS(CASE WHEN YEAR(dr.startDate) > 2026 THEN YEAR(dr.startDate) ELSE 2026 END, 12, 31)
        END
    FROM dbo.DiscountRelease dr
    OUTER APPLY (SELECT MIN(n.startDate) AS nextStart
                 FROM dbo.DiscountRelease n
                 WHERE n.massmediaID = dr.massmediaID AND n.startDate > dr.startDate) nx;

    PRINT 'DiscountRelease: даты окончания выставлены — ' + CAST(@@ROWCOUNT AS varchar(10));

    ALTER TABLE dbo.DiscountRelease ALTER COLUMN finishDate DATETIME NOT NULL;

    IF EXISTS(SELECT * FROM dbo.DiscountRelease WHERE startDate > finishDate)
       OR EXISTS(SELECT * FROM dbo.DiscountRelease a
                 JOIN dbo.DiscountRelease b ON b.massmediaID = a.massmediaID
                     AND b.discountReleaseID <> a.discountReleaseID
                     AND b.startDate <= a.finishDate AND b.finishDate >= a.startDate)
    BEGIN
        ROLLBACK TRANSACTION;
        RAISERROR('После переноса есть перевёрнутые или пересекающиеся периоды — всё откатено. Деплой прерван.', 16, 1);
        SET NOEXEC ON;
    END
    ELSE
        COMMIT TRANSACTION;
END
ELSE
    PRINT 'DiscountRelease.finishDate уже NOT NULL — перенос данных пропущен.';
GO
-- 2–4. Процедуры
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

CREATE OR ALTER PROCEDURE [dbo].[hlp_CompanyDiscountCalculate]
(
@massMediaID smallint,
@campaignTypeID tinyint,
@startDate datetime,
@tariffPrice decimal(18,2),
@discountValue decimal(9,4) output
)
as
SET NOCOUNT on
select @discountValue = NULL

Select	
	@discountValue = dv.discount
From		
	DiscountRelease dr 
	Inner Join DiscountValue dv On dv.discountReleaseId = dr.discountReleaseId
Where	
	dr.[massmediaID] = @massMediaID and
	@startDate >= dr.startDate AND 
	@startDate < DATEADD(DAY, 1, dr.finishDate) and
	dv.summa <= @tariffPrice 
	AND 
	(
	(dr.[isForType1] = 1 And @campaignTypeID = 1)
	Or 	(dr.[isForType2] = 1 And @campaignTypeID = 2)
	Or 	(dr.[isForType3] = 1 And @campaignTypeID = 3)
	)
Set	@DiscountValue = IsNull(@DiscountValue, 1)
GO

CREATE OR ALTER PROCEDURE [dbo].[DiscountReleases]
(
@massmediaID smallint = NULL,
@discountReleaseID smallint = NULL,
@hideDiscountsInThePast bit = 0
)
as
set nocount on
SELECT 
	dr.*,
	'Скидки от ' + Convert(varchar(10), dr.[startDate], 104) + CASE WHEN dr.[finishDate] IS NOT NULL THEN ' до ' + Convert(varchar(10), dr.[finishDate], 104) ELSE '' END as name
FROM 
	[DiscountRelease] dr
WHERE
	dr.[massmediaID] = Coalesce(@massmediaID, dr.[massmediaID])
	AND dr.[discountReleaseID] = Coalesce(@discountReleaseID, dr.[discountReleaseID])
	And (@hideDiscountsInThePast = 0 or dr.finishDate >= CAST(GETDATE() AS date))
ORDER BY
	dr.startDate DESC
GO
-- 5. Метаданные сущности 22
UPDATE dbo.iEntity
SET passport = REPLACE(CAST(passport AS nvarchar(max)),
        N'<field caption="Дата принятия:" name="startDate"/>',
        N'<field caption="Дата принятия:" name="startDate"/>' + CHAR(13) + CHAR(10) + CHAR(9) + CHAR(9)
            + N'<field caption="Дата окончания:" name="finishDate"/>')
WHERE entityID = 22 AND CAST(passport AS nvarchar(max)) NOT LIKE N'%"finishDate"%';

IF NOT EXISTS(SELECT * FROM dbo.iEntity WHERE entityID = 22 AND CAST(passport AS nvarchar(max)) LIKE N'%"finishDate"%')
    RAISERROR('Паспорт сущности 22 не изменился: не найдено поле startDate в ожидаемом виде. Поправить вручную.', 16, 1);

IF NOT EXISTS(SELECT * FROM dbo.iEntityAttribute WHERE entityID = 22 AND name = 'finishDate')
    INSERT INTO dbo.iEntityAttribute(entityID, alias, name, ordinal_position, selector, dataType)
    VALUES (22, N'Дата окончания', 'finishDate', 2, 0, NULL);
GO
SET NOEXEC OFF;
GO
-- Контроль
SELECT COUNT(*) AS releases, SUM(CASE WHEN finishDate = '20261231' THEN 1 ELSE 0 END) AS closedAt31Dec
FROM dbo.DiscountRelease;
SELECT name, alias, ordinal_position FROM dbo.iEntityAttribute WHERE entityID = 22 ORDER BY ordinal_position;
GO
