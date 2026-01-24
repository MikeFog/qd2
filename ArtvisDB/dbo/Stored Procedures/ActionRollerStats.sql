-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 26.09.2008
-- Description:	Get Rollers Statistic for action
-- =============================================
CREATE procedure [dbo].[ActionRollerStats] 
(
	@actionID int
)
as 
begin 
	set nocount on;

	declare @res table(rollerID int, massmediaID smallint, [count] int, price money)

    declare cur_campaigns cursor local fast_forward
    for	
		select campaignID, campaignTypeID, massmediaID from Campaign where actionID = @actionID
		
	open cur_campaigns
	
	declare @campaignID int, @campaignTypeID tinyint, @massmediaID smallint
	
	fetch next from cur_campaigns into @campaignID, @campaignTypeID, @massmediaID
	
	while @@fetch_status = 0
	begin 
		if @campaignTypeID = 1
		begin 
			insert into @res (rollerID,	massmediaID,[count],price) 
			select i.rollerID, @massmediaID, count(*), sum(i.[tariffPrice] * i.[ratio])  
			from Issue i 
			where i.campaignID = @campaignID 
			group by i.rollerID
		end 
		else if @campaignTypeID = 2
		begin 
			insert into @res (rollerID,	massmediaID,[count],price) 
			select i.rollerID, @massmediaID, count(*), 0 
			from Issue i 
			where i.campaignID = @campaignID 
			group by i.rollerID
		end 
		else if @campaignTypeID = 3
		begin 
			insert into @res (rollerID,massmediaID,[count],	price) 
			select mi.rollerID, @massmediaID, count(issues.[count]), sum(mi.tariffPrice * mi.ratio) 
			from ModuleIssue mi 
				inner join (select i.moduleIssueID, count(*) as [count] 
							from Issue i 
							where i.campaignID = @campaignID 
							group by i.moduleIssueID) as issues 
						on issues.moduleIssueID = mi.moduleIssueID
			where mi.campaignID = @campaignID
			group by mi.rollerID
		end 
		else if @campaignTypeID = 4
		begin 
			declare @helper table (packModuleIssueID int, massmediaID smallint, [count] int)
			-- this is for work quickly
			insert into @helper (packModuleIssueID, massmediaID,[count])
			select i.packModuleIssueID, tw.massmediaID, count(*) as [count] 
			from Issue i 
				inner join TariffWindow tw on i.originalWindowID = tw.windowID 
			where i.campaignID = @campaignID 
			group by i.packModuleIssueID, tw.massmediaID
		
			insert into @res (rollerID,massmediaID,[count],	price) 
			select pmIssue.rollerID, m.massmediaID, sum(issues.[count]), sum(pmIssue.price * cast(mpl.price as float) / cast(mmPriceSum.price as float))
			from 
				(select pmi.packModuleIssueID, pmi.issueDate, pmi.pricelistID, pmi.rollerID, pmi.tariffPrice * pmi.ratio as price  
					from PackModuleIssue pmi 
					where pmi.campaignID = @campaignID) as pmIssue 
				inner join PackModulePriceList pmpl on pmIssue.pricelistID = pmpl.priceListID
				inner join PackModuleContent pmc on pmc.pricelistID = pmpl.priceListID
				inner join ModulePriceList mpl on pmc.modulePriceListID = mpl.modulePriceListID
				inner join Module m on mpl.moduleID = m.moduleID
				inner join @helper as issues 
						on issues.packModuleIssueID = pmIssue.packModuleIssueID	 and issues.massmediaID = m.massmediaID
				
				inner join (select pmi.packModuleIssueID, sum(mpl.price) as price from PackModuleIssue pmi
								inner join PackModulePriceList pmpl on pmi.pricelistID = pmpl.priceListID
								inner join PackModuleContent pmc on pmc.pricelistID = pmpl.priceListID
								inner join ModulePriceList mpl on pmc.modulePriceListID = mpl.modulePriceListID
								inner join Module m on mpl.moduleID = m.moduleID
								where pmi.campaignID = @campaignID
								Group by pmi.packModuleIssueID) as mmPriceSum on mmPriceSum.packModuleIssueID = pmIssue.packModuleIssueID
			group by pmIssue.rollerID, m.massmediaID
		end 
	
		fetch next from cur_campaigns into @campaignID, @campaignTypeID, @massmediaID
	end 
	
	close cur_campaigns
	deallocate cur_campaigns
	
	select 
		mm.massmediaID, 
		rol.rollerID, 
		mm.[name] as massmedia, 
		mm.groupName,
		rol.[name] as roller, 
		sum(r.[count]) as [count], 
		sum(r.price) as [price]  
	from 
		@res r 
		inner join Roller rol on rol.rollerID = r.rollerID 
		left join vMassmedia mm on r.massmediaID = mm.massmediaID
	group by 
		mm.massmediaID, mm.[name], rol.rollerID, rol.[name], mm.groupName
	order by 
		mm.[name], rol.[name]
end

