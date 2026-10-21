-- Скорость журнала актов («Выписать акт выполненных работ», CampaignsForActJournalRetrieve: десктоп ActJournalForm и веб)
-- и всех, кто считает деньги линейной кампании за период через GetPriceByPeriod (ActionRecalculate, счёт rpt_GenericBill,
-- балансы, stat_*), плюс исправление колонки «Объём продаж» у пакетных кампаний. Параметры и колонки прежние.
--
-- Повод — прод-лог 20.10.2026: журнал актов за 01.01–19.10 по одной фирме — 33 с (за один месяц — 0,1–0,7 с).
-- 1. GetPriceByPeriod, линейная ветка (оба SELECT — без НДС и с НДС): CROSS APPLY + TOP 1 к TariffWindow по windowId
--    вместо inner join с отбором по дню, как в GetIssuesPrice с 30.08.2026 (80155cf). Прямой join строился как Hash
--    Match: на кампанию с 36 выпусками за апрель читались 59 332 окна всех станций за месяц (~15 мс процессора на вызов),
--    а журнал зовёт процедуру на каждую пару «кампания × месяц». windowId — PK, совпадение не более одного: суммы те же.
-- 2. CampaignsForActJournalRetrieve: месяцы до начала акции не заводятся (было только a.finishDate >= месяц) — ~80% строк
--    курсора были пустыми месяцами, цена по ним считалась и потом отсекалась по total > 0.
-- 3. CampaignsForActJournalRetrieve, пакетные кампании (тип 4): количество/длительность выпусков месяца прибавлялись ко
--    ВСЕМ месяцам кампании (UPDATE без r.currentdate) и зависели от порядка курсора — на периоде больше месяца «Объём
--    продаж» был неверным (кампания 387282, «Радио 7», март: 450 шт. при 12 настоящих). Теперь — только к своему месяцу.
--    Колонка только на экране журнала, в акт (OnAirInquireReport) не идёт; суммы не затронуты.
--
-- Проверено 21.10.2026 на ArtvisDev (временными копиями процедур): GetPriceByPeriod — 3 000 вызовов (линейные кампании
-- 2026 года: четыре месяца и весь год, без НДС и с НДС), суммы, тарифная сумма и НДС совпали, 346 с → 6 с. Журнал за
-- 01.01–19.10: агентство 201 + фирма 14567 (вызов из лога), агентства 201, 258, 259, 242 — все колонки, кроме «Объёма
-- продаж», совпали построчно; «Объём продаж» изменился только у пакетной 387282 и равен подсчёту выпусков по месяцам.
-- Время: 1,8 → 0,36 с; 201 — 263 → 13,7 с; 258 — 74 → 4,6 с; 259 — 341 → 32 с; 242 — 19 → 3,2 с.
--
-- Порядок: после Deploy/33 (несёт тексты обеих процедур по окну выхода) — без него скрипт отказывается: «Ничего не
-- изменено». В любое время; клиент qd2 и перезапуск веба не нужны. Не перезапускать после 66: 08, 29, 33 и ArtvisDB/Scripts
-- campaign-finalprice-with-pack-deploy.sql, web-i18n-sql-deploy.sql (несут старые тексты); если перезапустили — повторить 66.
-- Идемпотентен (CREATE OR ALTER). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 66_act-journal-price-by-period-speed.sql

SET NOCOUNT ON;
GO
IF CHARINDEX(N'originalWindowID', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetPriceByPeriod')), N'originalWindowID')) > 0
   OR CHARINDEX(N'coalesce(mi.issueDate, tw.dayOriginal)', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsForActJournalRetrieve')), N'')) = 0
BEGIN
    RAISERROR(N'66: сначала Deploy/33 (GetPriceByPeriod и журнал актов по окну выхода). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE OR ALTER PROC [dbo].[GetPriceByPeriod]
(
@campaignID int, 
@campaignTypeID int,
@startDate datetime, 
@finishDate datetime, 
@price decimal(18,2) OUT,
@massmediaID INT = NULL,
@tariffPrice decimal(18,2) = NULL out,
@showBlack bit = 1,
@taxPrice decimal(18,2) = null out,
@withTax bit = 0,
@rollerIDString VARCHAR(8000) = null
)
As
SET NOCOUNT ON

if EXISTS(SELECT * 
	FROM [Campaign] c 
		INNER JOIN [Action] a ON c.[actionID] = a.[actionID] 
		inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			and (@showBlack = 1 or pt.IsHidden = 0)
		WHERE c.[campaignID] = @campaignID AND a.isSpecial = 1)
	BEGIN
	SELECT @price = price, @tariffPrice = tariffPrice 
	FROM [Campaign] WHERE [campaignID] = @campaignID
		
	RETURN
	end
	
IF (@startDate IS NULL AND @finishDate IS NULL)
begin 
	SELECT @price = 0
	return 
end 

SET @startDate = dbo.ToShortDate(@startDate)
SET @finishDate = dbo.ToShortDate(@finishDate) 
Set	@price = 0

If	@campaignTypeID = 1 
	begin
	-- CROSS APPLY + TOP 1 вместо inner join, как в GetIssuesPrice (Deploy/66): прямой join оптимизатор строит
	-- как Hash Match и вычитывает все окна всех СМИ за период (59 тыс. на месяц), чтобы сматчить их с выпусками
	-- одной кампании. ~15 мс вместо ~0,3 мс на вызов, а журнал актов, ActionRecalculate и отчёты зовут это
	-- в курсоре. windowId — PK TariffWindow, совпадение не более одного: семантика та же.
	if @withTax = 0
		Select	@price = sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice])
		From	
			Issue i
			cross apply
			(
				select top 1 tw.dayOriginal
				from TariffWindow tw
				where tw.windowId = i.actualWindowID and
					tw.dayOriginal between @startDate and @finishDate
			) tw
			inner join Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where	
			i.campaignID = @campaignID	and
			(@rollerIDString is null or rr.ID is not null)
	else 
		Select	@price = sum(i.[tariffPrice] * i.[ratio]), 
			@tariffPrice = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From	
			Issue i
			cross apply
			(
				select top 1 tw.dayOriginal
				from TariffWindow tw
				where tw.windowId = i.actualWindowID and
					tw.dayOriginal between @startDate and @finishDate
			) tw
			inner join Campaign c on i.campaignID = c.campaignID
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and tw.dayOriginal between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where	
			i.campaignID = @campaignID	and
			(@rollerIDString is null or rr.ID is not null)

	End
Else If	@campaignTypeID = 2	
	begin
	
	if @withTax = 0
		Select	
			@price = isnull(Sum(i.[tariffPrice] * i.[ratio]), 0), @tariffPrice = isnull(SUM(i.[tariffPrice]), 0)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
		Where		
			i.campaignID = @campaignID and 
			Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between @startDate and @finishDate
	else 
		Select	
			@price = isnull(Sum(i.[tariffPrice] * i.[ratio]), 0), @tariffPrice = isnull(SUM(i.[tariffPrice]), 0),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ProgramIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between at.startDate and at.finishDate
		Where		
			i.campaignID = @campaignID and 
			Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between @startDate and @finishDate
	End
Else If	@campaignTypeID = 3 begin
	if @withTax = 0
		SELECT @price = 	Sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice])
		From		
			ModuleIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where		
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
	else 
		SELECT @price = 	Sum(i.[tariffPrice] * i.[ratio]), @tariffPrice = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when at.divisor is null then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		From		
			ModuleIssue i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		Where		
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
end
			
ELSE IF @campaignTypeID = 4
BEGIN

	DECLARE @packModulePrice decimal(18,2) 
	DECLARE @tariffPricePM decimal(18,2)

	if @withTax = 0
		SELECT 
			@packModulePrice = SUM(i.[tariffPrice] * i.[ratio]), @tariffPricePM = SUM(i.[tariffPrice])
		FROM [PackModuleIssue] i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
	else  
		SELECT 
			@packModulePrice = SUM(i.[tariffPrice] * i.[ratio]), @tariffPricePM = SUM(i.[tariffPrice]),
			@taxPrice = sum(case when coalesce(at.divisor, 0) < 0.0000001 then 0 else ((i.[tariffPrice] * i.[ratio])/at.divisor) end)
		FROM [PackModuleIssue] i 
			INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
			inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
				and (@showBlack = 1 or pt.IsHidden = 0)
			left join dbo.AgencyTax at on c.agencyID = at.agencyID
				and i.issueDate between at.startDate and at.finishDate
			left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @finishDate and
			(@rollerIDString is null or rr.ID is not null)
		
	IF @massmediaID IS NULL
		BEGIN
			SET	@price = @packModulePrice
			SET @tariffPrice = @tariffPricePM
			RETURN
		END

	CREATE TABLE #tmp(massmediaID SMALLINT, price decimal(18,2))
	INSERT INTO #tmp
	SELECT 
		m.[massmediaID], sum(mpl.[price])
	FROM [PackModuleIssue] i 
		INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
		INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
		INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		INNER JOIN [Campaign] c ON i.[campaignID] = c.[campaignID]
		inner join PaymentType pt On pt.PaymenttypeId = c.paymenttypeId
			and (@showBlack = 1 or pt.IsHidden = 0)
		left join fn_CreateTableFromString(@rollerIDString) rr on i.rollerID = rr.ID
	WHERE 
		i.campaignID = @campaignID	and
		i.issueDate between @startDate and @finishDate  and
		(@rollerIDString is null or rr.ID is not null)
	group by m.massmediaID

	declare @sumPrice decimal(18,2)
	SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1

	DECLARE @sumPriceMM decimal(18,2)
	SELECT @sumPriceMM = sum(t1.price) FROM [#tmp] AS t1 WHERE t1.[massmediaID] = @massmediaID

	SET @price = @packModulePrice * @sumPriceMM / @sumPrice
	SET @tariffPrice = @tariffPricePM * @sumPriceMM / @sumPrice
	drop table #tmp
END
GO

CREATE OR ALTER PROC [dbo].[CampaignsForActJournalRetrieve]
(
@startDate DATETIME = null,
@finishDate DATETIME = null,
@agencyID int = null,
@firmId int = null,
@showBlack bit = 1,
@showWhite bit = 1,
@actionID INT = null,
@loggedUserID smallint,
@languageCode VARCHAR(10) = 'ru' -- язык интерфейса веба (docs/tasks/web-i18n.md); десктоп не передаёт
)
WITH EXECUTE AS OWNER
AS
Set Nocount On
DECLARE @tSec NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' сек.');
DECLARE @tPcs NVARCHAR(200) = dbo.fn_Translate(@languageCode, N' шт.');

If @agencyID Is Null Begin
	RaisError('AgencyShouldBeSelected', 16, 1)
	Return
END

IF @startDate IS NULL 
	SELECT @startDate = dbo.ToShortDate(MIN(c.[startDate])) FROM [Campaign] c
	
IF @finishDate IS NULL 
	SELECT @finishDate = dbo.ToShortDate(MAX(c.finishDate)) FROM [Campaign] c

IF @finishDate < @startDate
BEGIN
	RaisError('WrongDates', 16, 1)
	Return
end

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

Declare @res Table(
	currentdate DATETIME,
	campaignId int,
	typeId smallint,
	total decimal(18,2) NULL,
	massmediaID INT null,
	mistake decimal(18,2) default 0,
	issuesCount INT default 0,
	issuesDuration timeDuration NULL default 0,
	showByDuration BIT NULL default 1
)

DECLARE @tmpDate DATETIME 
SET @tmpDate = @startDate
WHILE @tmpDate <= @finishDate
begin
	Insert Into @res
	Select distinct
		dbo.ToShortDate(CASE 
			WHEN dbo.fn_LastDateOfMonth(@tmpDate) < a.[finishDate] 
				THEN dbo.fn_LastDateOfMonth(@tmpDate) 
				ELSE a.[finishDate]
		end),
		c.campaignID,
		c.campaignTypeID,
		0,
		c.[massmediaID],
		0,
		0,
		0,
		1
	from
		[Action] a 
		inner join Campaign c ON c.[actionID] = a.[actionID]
		inner join PaymentType pt On c.paymentTypeId = pt.paymentTypeId
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	Where	
		(a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) 
		and (
			a.isSpecial = 1 
			or c.campaignTypeID = 4 
			or umm.massmediaID is not null 
			and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1))
		) 
		and	a.isSpecial = 0	AND a.[actionID] = COALESCE(@actionID, a.[actionID])
		and a.firmID = isnull(@firmID, a.firmID)
		and c.agencyId = @agencyId AND
		(pt.isHidden = 0 or @showBlack = 1) And
		(pt.isHidden = 1 or @showWhite = 1) AND
		a.[isConfirmed] = 1 AND
		-- месяцы до начала акции не заводим (Deploy/66): строка всё равно отсеется по total > 0, а цена по ней считается
		dbo.ToShortDate(a.[startDate]) <= dbo.fn_LastDateOfMonth(@tmpDate) AND
		dbo.ToShortDate(a.[finishDate]) >= @tmpDate AND 
		(dbo.fn_LastDateOfMonth(@tmpDate) <= (@finishDate) OR dbo.ToShortDate(a.[finishDate]) <= @finishDate)
				
	SET @tmpDate = DATEADD(month, 1, dbo.fn_FirstDateOfMonth(@tmpDate))
END

Declare	
	@currentDate DATETIME,
	@typeId smallint,
	@total decimal(18,2),
	@campaignID INT,
	@massmediaID smallint,
	@campaignStartDate datetime,
	@campaignFinishDate datetime,
	@campaignFinalPrice decimal(18,2),
	@campaignAdiscount decimal(18,10),
	@mistake decimal(18,2),
	@userID smallint,
	@issuesCount INT,
	@issuesDuration timeDuration,
	@showByDuration BIT

Declare cur_comp2 Cursor local fast_forward
For
SELECT r.currentDate, r.campaignId, r.typeId, c.startDate, c.finishDate, c.finalPrice, a.discount, a.userID From @res r inner join Campaign c on r.campaignId = c.campaignId inner join [Action] a on c.actionID = a.actionID
Open cur_comp2

Fetch Next From cur_comp2 Into @currentDate, @campaignId, @typeId, @campaignStartDate,@campaignFinishDate,@campaignFinalPrice,@campaignAdiscount,@userID
While @@fetch_status = 0 BEGIN
	SET @startDate = dbo.fn_FirstDateOfMonth(@currentDate)

	IF @typeId = 4
	BEGIN
		DELETE FROM @res WHERE [campaignID] = @campaignID AND [currentdate] = @currentDate
		
		Exec GetPriceByPeriod 
			@campaignId, @typeId, @startDate, @currentDate, @total OUT
		
		CREATE TABLE #tmp(massmediaID SMALLINT, price decimal(18,2))
		INSERT INTO #tmp
		SELECT 
			m.[massmediaID], sum(mpl.[price])
		FROM [PackModuleIssue] i 
			INNER JOIN [PackModuleContent] AS pmc ON i.[priceListID] = pmc.[pricelistID]
			INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
			INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
		WHERE 
			i.campaignID = @campaignID	and
			i.issueDate between @startDate and @currentDate 
		group by m.massmediaID
			
			
		declare @sumPrice decimal(18,2)
		SELECT @sumPrice = sum(t1.price) FROM [#tmp] AS t1
			
		INSERT INTO @res ([currentdate],[campaignId],[typeId],[total],[massmediaID])
		select @currentDate, @campaignId, @typeId, @total * sum(t1.price)/ @sumPrice, t1.massmediaID 
		from #tmp as t1
			inner join @massmedias mmu on t1.massmediaID = mmu.massmediaID 
				and ((@userID = @loggedUserID and mmu.myMassmedia = 1) or
					(@userID <> @loggedUserID and mmu.foreignMassmedia = 1))
		group by t1.massmediaID
		
		drop table #tmp 

		update r
		set 
			r.issuesCount = r.issuesCount + x.issuesCount,
			r.issuesDuration = r.issuesDuration + x.issuesDuration,
			r.showByDuration = x.showByDuration
		from 
			@res r
		inner join (
			select 	COUNT(*) as issuesCount, 
				SUM(rol.duration) as issuesDuration, 
				cast(case when SUM(tw.maxCapacity) > 0 then 0 else 1 end as bit) as showByDuration,
				m.massmediaID
			from Issue i 
				inner join TariffWindow tw on i.originalWindowID = tw.windowId
				INNER join Roller rol on rol.rollerID = i.rollerID
				INNER join PackModuleIssue pmi on i.packModuleIssueID = pmi.packModuleIssueID
				INNER JOIN [PackModuleContent] AS pmc ON pmi.[priceListID] = pmc.[pricelistID]
				INNER JOIN [ModulePriceList] AS mpl ON pmc.modulePriceListID = mpl.modulePriceListID
				INNER JOIN [Module] AS m ON mpl.[moduleID] = m.[moduleID]
				left join @massmedias mmu on m.massmediaID = mmu.massmediaID 
					and ((@userID = @loggedUserID and mmu.myMassmedia = 1) or
						(@userID <> @loggedUserID and mmu.foreignMassmedia = 1))
			where i.campaignID = @campaignID and tw.massmediaID = m.massmediaID and pmi.issueDate between @startDate and @currentDate 
			group by m.massmediaID
		) as x on r.massmediaID = x.massmediaID
		-- только строки этого месяца (Deploy/66): без currentdate выпуски месяца прибавлялись ко всем месяцам кампании
		where r.campaignId = @campaignID and r.currentdate = @currentDate
	END
	ELSE
	begin
		Exec GetPriceByPeriod 
			@campaignId, @typeId, @startDate, @currentDate, @total OUT
		
		IF @typeId = 2
		BEGIN
			select 
				@issuesCount = COUNT(*), 
				@issuesDuration = SUM(st.duration), 
				@showByDuration = 0
			From ProgramIssue i 
				inner join SponsorTariff st on i.tariffID = st.tariffID
				inner join SponsorProgramPriceList pl on st.priceListID = pl.priceListID
			Where		
				i.campaignID = @campaignID and 
				Convert(datetime, Convert(varchar(8), i.issueDate, 112), 112) between dbo.ToShortDate(@startDate) and dbo.ToShortDate(@currentDate) 
		END
		ELSE
		BEGIN
			select 
				@issuesCount = COUNT(*), 
				@issuesDuration = SUM(rol.duration), 
				@showByDuration = case when SUM(tw.maxCapacity) > 0 then 0 else 1 end
			from Issue i 
				inner join Roller rol on rol.rollerID = i.rollerID
				inner join TariffWindow tw on i.actualWindowID = tw.windowId
				left join ModuleIssue mi on mi.moduleIssueID = i.moduleIssueID
			where i.campaignID = @campaignID and coalesce(mi.issueDate, tw.dayOriginal) between dbo.ToShortDate(@startDate) and dbo.ToShortDate(@currentDate) 
		END
		
		Update @res
		Set total = @total, showByDuration = @showByDuration, issuesCount = issuesCount + @issuesCount, issuesDuration = issuesDuration + @issuesDuration
		Where currentDate = @currentDate And campaignId = @campaignId
	END
	
	if @campaignFinishDate between @startDate and @currentDate
	begin 
		Exec GetPriceByPeriod @campaignId, @typeId, @campaignStartDate, @campaignFinishDate, @total out
		set @mistake = @campaignFinalPrice - @total
		update @res set mistake = @mistake where campaignId = @campaignID
	end 
	
	Fetch Next From cur_comp2 INTO @currentDate, @campaignId, @typeId, @campaignStartDate,@campaignFinishDate,@campaignFinalPrice,@campaignAdiscount,@userID
End

CLOSE cur_comp2
DEALLOCATE cur_comp2

Select 
	r.currentDate,
	r.currentDate AS currentDate2,
	c.campaignId,
	c.actionId,
	c.startDate,
	c.finishDate,
	cast(null as decimal(18,2)) as total,
	r.total as campaignTotal,
	f.name as firmName,
	f.firmId,
	m.nameWithGroup as massmediaName,
	m.massmediaId,
	pt.name as paymentTypeName,
	u.LastName + ' ' + u.FirstName as userName,
	r.mistake,
	case when r.showByDuration = 1 then dbo.fn_Int2Time(r.issuesDuration) + @tSec else cast(r.issuesCount as nvarchar(10)) + @tPcs end as saleVolume
From 
	@res r
	Inner Join Campaign c On r.CampaignId = c.CampaignId
	Inner Join Action a On a.actionId = c.actionId
	Inner Join Firm f On f.firmId = a.firmId
	Inner Join vMassmedia m On m.massmediaId = r.massmediaID
	Inner Join PaymentType pt On pt.paymentTypeId = c.paymentTypeId
	Inner Join [User] u On u.userId = a.userId
WHERE 
	r.total IS NOT NULL AND r.total > 0
Order by
	r.currentDate asc,
	c.actionId desc
	

select top 1 1
from @res r
	inner join MassMedia mm on r.massmediaID = mm.massmediaID 
	inner join @massmedias mmu on mm.massmediaID = mmu.massmediaID 
where mm.deadline < @finishDate
GO

-- Проверка: новые версии обеих процедур применены.
IF CHARINDEX(N'select top 1 tw.dayOriginal', OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetPriceByPeriod'))) > 0
   AND CHARINDEX(N'inner join TariffWindow tw on i.actualWindowID = tw.windowId', OBJECT_DEFINITION(OBJECT_ID(N'dbo.GetPriceByPeriod'))) = 0
   AND CHARINDEX(N'dbo.ToShortDate(a.[startDate]) <= dbo.fn_LastDateOfMonth(@tmpDate)', OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsForActJournalRetrieve'))) > 0
   AND CHARINDEX(N'r.campaignId = @campaignID and r.currentdate = @currentDate', OBJECT_DEFINITION(OBJECT_ID(N'dbo.CampaignsForActJournalRetrieve'))) > 0
    PRINT N'ГОТОВО: GetPriceByPeriod (окно выпуска по ключу) и CampaignsForActJournalRetrieve (без месяцев до акции, объём пакетных по месяцу).';
ELSE
    RAISERROR(N'66: новая версия применена не ко всем процедурам.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
