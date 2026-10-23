-- Выпуск спонсорской программы (ProgramIssueIUD AddItem; десктоп — сетка программ, шаблон по программе, клон акции;
-- веб — вкладка «Спонсорство» этапа 7, docs/tasks/web-action-forms.md, решение В-34; дефекты Д-25, Д-26 в
-- docs/action-forms.md). Две правки, только в AddItem:
-- 1. Дата выпуска проверяется по сроку прайс-листа тарифа (Д-25): раньше тариф проверялся только по программе и дню
--    недели, и выпуск можно было поставить в день, когда прайс-лист тарифа уже кончился или ещё не начался (так ставит
--    клон акции на последнем дне прайс-листа). Срок — дни startDate…finishDate включительно (конец — finishDate + 1
--    день, как в sponsorPLIUD). Отказ — прежний текст ProgramNotExists («В данную дату программы не существует.»,
--    перевод на испанский уже есть).
-- 2. Цена выпуска берётся из тарифа (SponsorTariff.price), а не от клиента (Д-26; как IssueIUD с 03.10.2026): в давно
--    открытой форме она могла устареть. Параметр @tariffPrice оставлен в контракте и не используется — клиенты не
--    меняются. Цену использованного тарифа менять нельзя (SponsorTariffInUse), поэтому у выпусков одного тарифа цена
--    по-прежнему одна.
-- DeleteItem и UpdateItem, параметры и ответ процедуры прежние. Данные на ArtvisDev (копия прода): из 2 858 выпусков
-- программ цена расходится с тарифом у 0, вне срока прайс-листа — 0; правка существующих записей не касается.
--
-- Порядок: после Deploy/29 (ProgramIssueIUD без broadcastStart) — без него скрипт отказывается: «Ничего не изменено». В
-- любое время, Artvis и Tumen; клиент qd2 и перезапуск веба не нужны. Накатить раньше веб-сборки этапа 7
-- («Спонсорство»). Клон акции: новый Merlin.exe ищет прайс-лист программы по дню и пропускает выпуск вне срока строкой
-- в таблице ошибок; текущий клиент на последнем дне прайс-листа брал прайс-лист следующего года и после 68 прервётся
-- ошибкой. На деле клон акции со спонсорской кампанией падает и без 68 — на роликах, сразу после программ (Д-31 в
-- docs/action-forms.md, с 2025 года). Не перезапускать после 68: 29 (несёт старый текст ProgramIssueIUD); если
-- перезапустили — повторить 68. Идемпотентен (CREATE OR ALTER). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 68_program-issue-price-and-pricelist-term.sql

SET NOCOUNT ON;
GO
IF CHARINDEX(N'broadcastStart', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssueIUD')), N'broadcastStart')) > 0
   OR CHARINDEX(N'ProgramNotExists', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssueIUD')), N'')) = 0
BEGIN
    RAISERROR(N'68: сначала Deploy/29 (ProgramIssueIUD без broadcastStart). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE [dbo].[ProgramIssueIUD]
(
@issueID int = NULL OUT,
@campaignID int = NULL,
@programID smallint = NULL,
@tariffID int = NULL,
@issueDate datetime = NULL,
@tariffPrice decimal(18,2) = NULL,
@bonus smallint = NULL,
@loggedUserID smallint,
@isConfirmed bit = null,
@advertTypeID smallint = null,
@actionName varchar(32)
)
WITH EXECUTE AS OWNER
AS
SET NOCOUNT ON
set datefirst 1
DECLARE	
	@massmediaID smallint,
	@RightToGoBack bit,
	@IsAdmin bit,
	@IsTrafficManager bit,
	@RightForMinus bit, 
	@finishDate datetime,
	@timeBonus int,
	@issuesDuration int

SELECT 
	@massmediaID = massmediaID,
	@finishDate = finishDate,
	@timeBonus = timeBonus,
	@issuesDuration = issuesDuration	
FROM 
	Campaign 
WHERE 
	campaignID = @campaignID

EXEC hlp_GetMainUserCredentials
	@loggedUserId, @rightToGoBack out, @isAdmin out, @IsTrafficManager out, @rightForMinus out

IF @actionName = 'AddItem' 
	BEGIN
	-- Verify data against few rules
	-- 1. Disabled window
	IF dbo.fn_IsDisabledWindow(@massmediaID, @issueDate) = 1 BEGIN
		RAISERROR('DisabledWindowInsertProgram', 16, 1)
		RETURN
		END

	-- 2. impossible to add issues with date less than today 
	If	dbo.ToShortDate(@issueDate) <= dbo.ToShortDate(getdate()) And @IsAdmin <> 1 And @IsTrafficManager <> 1  BEGIN
		RAISERROR('IncorrectProgramIssueDate', 16, 1)
		RETURN
		END

	-- 3. if company has already finished, refuse changes ------
	if	@finishDate < dbo.ToShortDate(getdate()) And @IsAdmin <> 1 And @IsTrafficManager <> 1  BEGIN
		RAISERROR('CampaignAlreadyFinished', 16, 1)
		RETURN
		END

	declare @datepart tinyint 
	set @datepart = datepart(dw, @issueDate)

	-- Тариф этой программы, отмеченный день недели и день в сроке прайс-листа тарифа (Д-25). Срок — дни
	-- startDate…finishDate включительно, выпуск идёт во время тарифа: конец — finishDate + 1 день, как в sponsorPLIUD
	-- (SponsorPricelistInUse); BETWEEN отказал бы выпускам последнего дня.
	if not exists(select *
		from SponsorTariff t
			inner join SponsorProgramPricelist sppl on t.pricelistID = sppl.pricelistID
		where t.tariffID = @tariffID and sppl.sponsorProgramID = @programID
			and @issueDate >= sppl.startDate and @issueDate < dateadd(day, 1, sppl.finishDate)
			and ((t.monday = 1 and @datepart = 1)
				or (t.tuesday = 1 and @datepart = 2)
				or (t.wednesday = 1 and @datepart = 3)
				or (t.thursday = 1 and @datepart = 4)
				or (t.friday = 1 and @datepart = 5)
				or (t.saturday = 1 and @datepart = 6)
				or (t.sunday = 1 and @datepart = 7)))
	begin 
		raiserror('ProgramNotExists',16,1)
		return
	end 

	if exists(select * from ProgramIssue where programID = @programID and tariffID = @tariffID and @issueDate = issueDate and isConfirmed = 1)
	begin 
		raiserror('UIX_ProgramIssue_program_issueDate',16,1)
		return
	end

	-- Цена выпуска — из тарифа, а не от клиента (Д-26; как IssueIUD с 03.10.2026): в давно открытой форме она могла
	-- устареть. Параметр @tariffPrice оставлен в контракте и не используется.
	select @tariffPrice = t.price from SponsorTariff t where t.tariffID = @tariffID

	INSERT INTO [ProgramIssue](campaignID, programID, tariffID, issueDate, [tariffPrice], isConfirmed, advertTypeID)
	select @campaignID, @programID, @tariffID, @issueDate, @tariffPrice, @isConfirmed, @advertTypeID
	
	if @@rowcount <> 1
	begin
		raiserror('InternalError', 16, 1)
		return 
	end 

	SET @issueID = SCOPE_IDENTITY()
		
	EXEC dbo.[ProgramIssues] @issueID= @issueID
END
ELSE IF @actionName = 'DeleteItem' BEGIN
	-- Only admin is allowed to delete issue which is in the past already
	If	@issueDate < GetDate() And @IsAdmin <> 1 BEGIN
		RAISERROR('PastIssue', 16, 1)
		RETURN
	END	
	
	if @bonus is null 
		select @bonus = pl.bonus
			FROM ProgramIssue i 
				inner join SponsorTariff st on i.tariffID = st.tariffID
				INNER JOIN [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
			where i.issueID = @issueID

	-- Should be balance between time bonus and roller issues duration
	If	@issuesDuration > @timeBonus - @bonus BEGIN
		RAISERROR('ProgramIssueDeleteBonusError', 16, 1)
		RETURN
	END		
	
	DELETE FROM [ProgramIssue] WHERE issueID = @issueID
	END
ELSE IF @actionName = 'UpdateItem'
	Begin
	UPDATE	
		[ProgramIssue]
	SET			 
		issueDate = @issueDate,
		advertTypeID = @advertTypeID
	WHERE		
		issueID = @issueID

	EXEC dbo.[ProgramIssues] @issueID= @issueID
	End
GO

IF CHARINDEX(N'select @tariffPrice = t.price from SponsorTariff t where t.tariffID = @tariffID', OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssueIUD'))) > 0
   AND CHARINDEX(N'@issueDate < dateadd(day, 1, sppl.finishDate)', OBJECT_DEFINITION(OBJECT_ID(N'dbo.ProgramIssueIUD'))) > 0
    PRINT N'ГОТОВО: ProgramIssueIUD — цена из тарифа, дата выпуска в сроке прайс-листа программы.';
ELSE
    RAISERROR(N'68: новая версия ProgramIssueIUD не применена.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
