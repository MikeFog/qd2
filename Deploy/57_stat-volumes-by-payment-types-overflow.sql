-- «Отчет по типам оплаты» (stat_VolumesByPaymentTypes, сущность 222 «Статистика:: Отчет по типам оплаты», пункт
-- «Бухгалтерия → Специальные отчёты → Отчет по типам оплат» в десктопе и вебе) падал с ошибкой 8115 «Ошибка
-- арифметического переполнения при преобразовании numeric к типу данных numeric», как только в периоде есть акция, у
-- которой «С оплатой» или «Без оплаты» от 1 млн ₽: без разбивки по типам оплаты суммы приводились к decimal(8,2)
-- (предел 999 999,99). На копии прода Artvis падали август и сентябрь 2026, 2025 год и январь–сентябрь 2026 (акция
-- 184174 — 3 499 200 ₽). С разбивкой — decimal(10, 2) (предел 99 999 999,99, пока не достигнут). Теперь обе ветки —
-- decimal(18,2), как суммы, из которых отчёт складывается (Campaign.finalPrice, GetPriceByPeriod, #res). Параметры,
-- колонки и их порядок прежние; значения не меняются (scale 2 → 2). docs/tasks/stat-volumes-by-payment-types-overflow.md
--
-- Проверено 19.10.2026: выдача старой и новой версий побайтно совпала везде, где старая не падала (Artvis — 8 месяцев
-- 2025–2026, неделя, 2025 год и январь–сентябрь 2026 в обеих ветках; ArtvisDev; Tumen — 2025 и январь–сентябрь 2026);
-- где старая падала, новая отдаёт строки, итоги без разбивки равны итогам с разбивкой по каждой акции.
--
-- Порядок: в любое время; клиент qd2 и перезапуск веба не нужны. Нужна версия процедуры после
-- ArtvisDB/Scripts/campaign-finalprice-with-pack-deploy.sql (c.finalPrice без «* a.discount», на проде с 25.09.2026) —
-- без неё скрипт отказывается: «Ничего не изменено». Не перезапускать после 57 campaign-finalprice-with-pack-deploy.sql
-- (несёт старый текст); если перезапустили — повторить 57. Идемпотентен (CREATE OR ALTER). Ожидается «ГОТОВО».
--   sqlcmd -S <сервер> -d <база> -E -f 65001 -I -b -i 57_stat-volumes-by-payment-types-overflow.sql

SET NOCOUNT ON;
GO
IF OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_VolumesByPaymentTypes')) IS NULL
   OR CHARINDEX(N'c.finalPrice * a.discount', OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_VolumesByPaymentTypes'))) > 0
BEGIN
    RAISERROR(N'57: сначала ArtvisDB/Scripts/campaign-finalprice-with-pack-deploy.sql. Ничего не изменено.', 16, 1);
    SET NOEXEC ON;
END
GO
SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 26.01.2009
-- Description:	
-- =============================================
CREATE OR ALTER procedure [dbo].[stat_VolumesByPaymentTypes] 
(
	@managerID smallint = NULL, 
	@firmID int = null,
	@startDate datetime = null,
	@finishDate datetime = null,
	@agencyID smallint = null,
	@groupByPaymentType bit = 0,
	@loggedUserID smallint 
)
WITH EXECUTE AS OWNER
as 
begin 
	set nocount on;

    declare @massmedias table(massmediaID smallint primary key, myMassmedia bit, foreignMassmedia bit)
	insert into @massmedias (massmediaID, myMassmedia, foreignMassmedia) 
	select * from dbo.fn_GetMassmediasForUser(@loggedUserID)

	declare @isRightToViewForeignActions bit, @isRightToViewGroupActions bit

	select @isRightToViewForeignActions = dbo.fn_IsRightToViewForeignActions(@loggedUserID),
		@isRightToViewGroupActions = dbo.fn_IsRightToViewGroupActions(@loggedUserID)

	declare @ugroups table(id int)
	insert into @ugroups (id) 
	select * from dbo.[fn_GetUserGroups](@loggedUserID)
	
	create table #res(actionID int, userID smallint, firmID int, price decimal(18,2), paymentTypeID smallint, isHidden bit)
	
--	insert into [#res] (
--		actionID,
--		userID,
--		firmID,
--		price,
--		paymentTypeID,
--		isHidden
--	) 
	declare cur_companies cursor local fast_forward
	for
	select distinct a.actionID, 
		a.userID, 
		a.firmID,
		c.finalPrice,
		pt.paymentTypeID,
		pt.isHidden,
		c.startDate,
		c.finishDate,
		c.campaignID,
		c.campaignTypeID
	from Campaign c
		inner join [Action] a  on c.actionID = a.actionID
		inner join paymentType pt on c.paymentTypeID = pt.paymentTypeID
		left join @massmedias umm on c.massmediaID = umm.massmediaID
		left join GroupMember gm on a.userID = gm.userID
		left join @ugroups ug on gm.groupID = ug.id
	where (a.userID = @loggedUserID or @isRightToViewForeignActions = 1 or (@isRightToViewGroupActions = 1 and ug.id is not null)) and
		(a.isSpecial = 1 or (c.campaignTypeID <> 4 and umm.massmediaID is not null and ((a.userID = @loggedUserID and umm.myMassmedia = 1) or (a.userID <> @loggedUserID and umm.foreignMassmedia = 1) )) 
				or (c.campaignTypeID = 4 and not exists(select * 
															from PackModuleIssue pmi 
																inner join PackModuleContent pmc on pmi.pricelistID = pmc.pricelistID
																inner join Module m on pmc.moduleID = m.moduleID
																left join @massmedias ummm on m.massmediaID = ummm.massmediaID
															where pmi.campaignID = c.campaignID and (ummm.massmediaID is null or 
																(a.userID = @loggedUserID and ummm.myMassmedia = 0) or
																 (a.userID <> @loggedUserID and ummm.foreignMassmedia = 0) )))) and	
		(@finishDate is null or c.startDate <= @finishDate)
		and (@startDate is null or c.finishDate >= @startDate)
		and a.[isConfirmed] = 1
		and c.agencyID = coalesce(@agencyID, c.agencyID)
		and a.userID = coalesce(@managerID, a.userID)
		and a.firmID = coalesce(@firmID, a.firmID)
	
	declare @actionID int, @userID smallint, 
		@campaignPrice decimal(18,2), @paymentTypeID smallint, @isHidden bit,
		@campaignstartdate datetime, @campaignfinishDate datetime, @campaignID int,
		@campaignTypeID  tinyint
	
	open cur_companies
	fetch next from cur_companies into 
		@actionID, @userID, @firmID, @campaignPrice, @paymentTypeID, @isHidden, @campaignstartdate,@campaignfinishDate,  @campaignID, @campaignTypeID
		
	while @@fetch_status = 0
	begin
		if @finishDate < @campaignfinishDate or @startDate > @campaignstartdate
			exec GetPriceByPeriod @campaignId, @CampaignTypeID, @startDate, @finishDate, @campaignPrice out

		if	@campaignPrice > 0 
			insert into [#res] (actionID,userID,firmID,price,paymentTypeID,isHidden) 
			values(@actionID, @userID, @firmID, @campaignPrice, @paymentTypeID, @isHidden)	
	
		fetch next from cur_companies into 
			@actionID, @userID, @firmID, @campaignPrice, @paymentTypeID, @isHidden, @campaignstartdate,@campaignfinishDate,  @campaignID, @campaignTypeID
	end
	
	if @groupByPaymentType = 0
	begin 
		select r.actionID, -- primary key
			r.actionID as 'Акция',
			coalesce(u.lastName,space(0)) + space(1) + coalesce(u.firstName,space(0)) + space(1) + coalesce(u.secondName,space(0)) as 'Менеджер',
			f.name as 'Фирма',
			Cast(r.not_hidden as decimal(18,2)) as 'С оплатой',
			Cast(r.hidden as decimal(18,2)) 'Без оплаты'
		from (
				select r.actionID,
					r.firmID,
					r.userID,
					sum(case when r.isHidden = 1 then r.price else 0 end) as hidden,
					sum(case when r.isHidden = 0 then r.price else 0 end) as not_hidden
				from #res r
				group by r.actionID,r.firmID,r.userID
			) r
			inner join [User] u on r.userID = u.userID
			inner join Firm f on r.firmID = f.firmID
		order by r.actionID
	end 
	else 
	begin 
		declare @sql nvarchar(max)
		declare @sqlselect nvarchar(max)
		declare @sqlsubselect nvarchar(max)
	
		declare cur_paymentType cursor fast_forward local
		for 
		select pt.paymentTypeID, pt.name
		from PaymentType pt
		where pt.isActive = 1
		order by pt.name
		
		declare @name nvarchar(64), @index smallint
		
		select @index = 1, @sqlsubselect = '', @sqlselect = ''
		
		open cur_paymentType
		
		fetch next from cur_paymentType into @paymentTypeID, @name
		
		while @@fetch_status = 0
		begin 
			set @sqlsubselect = @sqlsubselect + ', sum(case when r.paymentTypeID = ' + cast(@paymentTypeID as varchar) + ' then r.price else 0 end) as pt' + cast(@index as varchar)
			set @sqlselect = @sqlselect + ', cast(r.pt' + cast(@index as varchar) + ' as decimal(18, 2))  as ''' + replace(@name, '''', '''''') + ''''

			set @index = @index + 1		
			fetch next from cur_paymentType into @paymentTypeID, @name
		end 
		
		close cur_paymentType
		deallocate cur_paymentType
			
		set @sql = 'select r.actionID, r.actionID as ''Акция'',
					coalesce(u.lastName,space(0)) + space(1) + coalesce(u.firstName,space(0)) + space(1) + coalesce(u.secondName,space(0)) as ''Менеджер'',
					f.name as ''Фирма'' '
		set @sql = @sql + @sqlselect		
		set @sql = @sql + '
			from (
					select r.actionID,
						r.firmID,
						r.userID'
		set @sql = @sql + @sqlsubselect		
		set @sql = @sql + '
					from #res r
					group by r.actionID,r.firmID,r.userID
				) r
				inner join [User] u on r.userID = u.userID
				inner join Firm f on r.firmID = f.firmID
			order by r.actionID	'
		
		exec sp_executeSQL @sql
	end
	
	drop table #res
end
GO

-- Проверка: обе ветки отдают суммы decimal(18,2).
IF CHARINDEX(N'decimal(8,2)', OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_VolumesByPaymentTypes'))) = 0
   AND CHARINDEX(N'decimal(10, 2)', OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_VolumesByPaymentTypes'))) = 0
   AND CHARINDEX(N'Cast(r.not_hidden as decimal(18,2))', OBJECT_DEFINITION(OBJECT_ID(N'dbo.stat_VolumesByPaymentTypes'))) > 0
    PRINT N'ГОТОВО: stat_VolumesByPaymentTypes — суммы decimal(18,2) в обеих ветках.';
ELSE
    RAISERROR(N'57: новая версия stat_VolumesByPaymentTypes не применена.', 16, 1);
GO

-- Снять режим «не выполнять», если его включила защита в начале (иначе окно SSMS остаётся в NOEXEC).
SET NOEXEC OFF;
GO
