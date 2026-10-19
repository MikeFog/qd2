-- Скорость «Журнала использования роликов» (stat_RollerStatistic: десктоп RollerStatisticForm и веб /roller-statistic,
-- docs/tasks/side-defects.md №6). Выдача прежняя: параметры, все три набора, колонки и порядок.
--
-- Было: главный INSERT начинал от всех подтверждённых акций и их кампаний и перебирал ~3 млн выпусков, и только потом
-- отбирал окна периода; станции отсекались ещё позже. ~3 с даже на один день одной станции, без отбора по менеджеру —
-- 3–9 с; план зависел от первого вызова (по прод-логу 15.10 — 20 вызовов по 5–9 с, больше всех по суммарному времени).
-- Стало: сначала выпуски периода на выбранных станциях — от окон периода (окно выхода, actualWindowID, как с Deploy/28)
-- во временную таблицу, с OPTION (RECOMPILE), потом прежние соединения и отборы. Индексы не нужны (Issue по
-- actualWindowID и TariffWindow по станции и дню уже есть).
--
-- Проверено 19.10.2026 (сверка разбора): 23 набора параметров на ArtvisDev, 7 на Artvis, 4 на Tumen — разбивка по дням и
-- по менеджерам, свой/чужой/любой менеджер, права (чужие акции, группа, только свои), фирма, группа компаний, предмет
-- рекламы, скрытые типы оплаты, от одного дня до квартала, 1–42 станции: выдача старой и новой версий совпала (SHA-256).
-- Время: день по всем станциям ~3 с → ~0,3 с, квартал по нескольким станциям 5–9 с → 0,5–0,8 с; редкие вызовы за месяц
-- или квартал по всем станциям с узким отбором (менеджер, фирма) — на 0,1–0,5 с медленнее лучшего прежнего плана
-- (0,94 → 1,35 с): принято ради стабильности, второй путь выполнения по избирательности отбора не делали.
--
-- Порядок: после Deploy/28 (несёт текст процедуры с окном выхода) — без него скрипт отказывается: «Ничего не изменено».
-- В любое время; клиент qd2 и перезапуск веба не нужны. Не перезапускать после 61: 28 (несёт старый текст); если
-- перезапустили — повторить 61. Идемпотентен (CREATE OR ALTER). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 61_roller-statistic-speed.sql

SET NOCOUNT ON;
GO
IF CHARINDEX(N'i.actualWindowID = tw.windowID', ISNULL(OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_RollerStatistic')), N'')) = 0
BEGIN
    RAISERROR(N'61: сначала Deploy/28 (stat_RollerStatistic по окну выхода). Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROC [dbo].[stat_RollerStatistic]
(
@startDate datetime,
@finishDate datetime,
@splitByManager bit = 0,
@splitByDays bit = 0,
@massmediaString varchar(8000),
@userID smallint = NULL,
@isHideWhite BIT = 0,
@isHideBlack BIT = 0,
@showBlack bit = 1,
@showWhite bit = 1,
@loggedUserID smallint,
@firmID int = null,
@headCompanyID int = null,
@advertTypeID int = null
)
AS
SET NOCOUNT ON

Declare @rollerStat TABLE(
rollerID int, 
date datetime, 
userID smallint, 
firmID smallint)


declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
	@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

declare @ugroups table(id int)
insert into @ugroups (id) 
select * from dbo.[fn_GetUserGroups](@loggedUserID)

declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
select x.massmediaID, x.myMassmedia, x.foreignMassmedia 
from dbo.fn_GetMassmediasForUser(@loggedUserID) x
	inner join dbo.fn_CreateTableFromString(@massmediaString) y on x.massmediaID = y.ID

Declare @res1 TABLE(
rollerID int, 
date datetime, 
userID smallint, 
firmID smallint,
massmediaID smallint)

-- Выпуски периода на выбранных станциях берём от окон периода (окно выхода, actualWindowID),
-- а не от всех выпусков всех подтверждённых акций: иначе план перебирал ~3 млн выпусков и только
-- потом отбирал окна периода (docs/tasks/side-defects.md №6). Строки других станций всё равно
-- отбрасывает @massmedias ниже, поэтому станции отсекаются сразу.
-- RECOMPILE: план не должен зависеть от первого вызова (квартал по всем станциям против одного дня).
create table #issues (
	issueID int not null,
	rollerID int not null,
	campaignID int not null,
	dayOriginal smalldatetime not null,
	massmediaID smallint not null)

insert into #issues (issueID, rollerID, campaignID, dayOriginal, massmediaID)
select i.issueID, i.rollerID, i.campaignID, tw.dayOriginal, tw.massmediaID
from TariffWindow tw
	inner join @massmedias mm on mm.massmediaID = tw.massmediaID
	inner join Issue i on i.actualWindowID = tw.windowID
where tw.dayOriginal between @startDate and @finishDate
option (recompile)

Insert Into @res1(rollerID, date, userID, firmID, massmediaID)
Select 
	i.rollerID,
	i.dayOriginal,
	a.userID,
	a.firmID,
	i.massmediaID
FROM 
	#issues i
	INNER JOIN Campaign c ON c.campaignID = i.campaignID
	INNER JOIN [Action] a ON a.actionID = c.actionID
	INNER JOIN Firm f ON f.firmID = a.firmID
	INNER JOIN PaymentType pt ON pt.paymentTypeID = c.paymentTypeID
	INNER JOIN Roller r On r.rollerID = i.rollerID
	LEFT JOIN AdvertType adt on adt.advertTypeID = r.advertTypeID
	inner join 
	(
		select distinct u.userID 
		from [User] u
			left join [GroupMember] gm on u.userID = gm.userID
			left join @ugroups ug on gm.groupID = ug.id	
		where u.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)
	) as x on a.userID = x.userID
where
	a.userID = Coalesce(@userID, a.userID) 
	AND a.[isConfirmed] = 1
	and a.firmID = coalesce(@firmID, a.firmID)
	and f.headCompanyID = coalesce(@headCompanyID, f.headCompanyID)
	and (@advertTypeID Is Null Or r.advertTypeID = @advertTypeID Or adt.parentID = @advertTypeID)
	AND (pt.isHidden = 0 or @isHideWhite = 0) And
		(pt.isHidden = 1 or @isHideBlack = 0) and
	((pt.IsHidden = 1 and @showBlack = 1)  or
	(pt.IsHidden = 0 and @showWhite = 1)) 
group by i.rollerID, i.dayOriginal, a.userID, a.firmID, i.massmediaID, i.issueID

--for optimize
insert into @rollerStat
        ( rollerID, date, userID, firmID )
select r.rollerID, r.date, r.userID, r.firmID from @res1 r 
	inner join @massmedias mm on r.massmediaID = mm.massmediaID
where (r.userID = @loggedUserID and mm.myMassmedia = 1 
		or r.userID <> @loggedUserID and mm.foreignMassmedia = 1)
	
IF @splitByManager = 0
	select
		row_number() over(order by quantity desc) as RowNum,
		rollerID,
		rollerDescription,
		quantity,
		firmDescription,
		firmID,
		dbo.fn_ProductListByRollerId(rollerID) as productList,
		compositionAuthor,
		compositionName,
		duration,
		dbo.fn_Int2Time(duration) as durationString,
		dbo.fn_Int2Time(quantity * duration) as sumDuration,
		advertTypeName,
		isCommon,
		isMute,
		parentID
	FROM (
		SELECT 
			r.rollerID,
			r.name as rollerDescription,
			count(*) as quantity,
			f.name as firmDescription,
			f.firmID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name as advertTypeName,
			r.isCommon,
			r.isMute,
			r.parentID
		FROM 
			@rollerStat rs
			INNER JOIN Roller r ON r.rollerID = rs.rollerID
			INNER JOIN Firm f ON f.firmID = rs.firmID
			LEFT JOIN AdvertType at ON r.advertTypeID = at.advertTypeID
		GROUP BY
			r.rollerID,
			r.name,
			f.name,
			f.firmID,
			r.duration,
			r.compositionAuthor,
			r.compositionName,
			at.name,
			r.isCommon,
			r.isMute,
			r.parentID
	) ss
	ORDER BY 
		quantity desc
ELSE
	select
		row_number() over(order by quantity desc) as RowNum,
		rollerID,
		rollerDescription,
		quantity,
		firmDescription,
		firmID,
		dbo.fn_ProductListByRollerId(rollerID) as productList,
		userName,
		userID,
		compositionAuthor,
		compositionName,
		duration,
		dbo.fn_Int2Time(duration) as durationString,
		dbo.fn_Int2Time(quantity * duration) as sumDuration,
		advertTypeName,
		isCommon,
		isMute,
		parentID
	FROM (
		SELECT 
			r.rollerID,
			r.name as rollerDescription,
			count(*) as quantity,
			f.firmID,
			f.name as firmDescription,
			u.lastName + Space(1) + u.firstName as userName,
			u.userID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name as advertTypeName,
			r.isCommon,
			r.isMute,
			r.parentID
		FROM 
			@rollerStat rs
			INNER JOIN Roller r ON r.rollerID = rs.rollerID
			INNER JOIN [User] u ON rs.userID = u.userID
			INNER JOIN Firm f ON f.firmID = rs.firmID
			LEFT JOIN AdvertType at ON r.advertTypeID = at.advertTypeID
		GROUP BY
			r.rollerID,
			r.name,
			f.name,
			f.firmID,
			u.lastName + Space(1) + u.firstName,
			u.userID,
			r.compositionAuthor,
			r.compositionName,
			r.duration,
			at.name,
			r.isCommon,
			r.isMute,
			r.parentID
	) ss
	ORDER BY 
		quantity desc

-- Add additional data in case when day by day data is required
IF @splitByDays = 1 Begin
	SELECT * FROM @rollerStat
	SELECT DISTINCT date FROM @rollerStat ORDER BY date
END
ELSE BEGIN -- Return 2 fake recordsets
	SELECT 1
	SELECT 1
END
GO

-- Проверка: выпуски периода берутся от окон во временную таблицу.
IF CHARINDEX(N'create table #issues', OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_RollerStatistic'))) > 0
   AND CHARINDEX(N'i.actualWindowID = tw.windowID', OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_RollerStatistic'))) > 0
    PRINT N'ГОТОВО: stat_RollerStatistic — выпуски периода от окон выбранных станций.';
ELSE
    RAISERROR(N'61: новая версия stat_RollerStatistic не применена.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
