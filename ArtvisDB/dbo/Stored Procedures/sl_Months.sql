-- Для эфирной справки
CREATE    procedure [dbo].[sl_Months](@campaignId int)
as
SET NOCOUNT on

declare @monthes table (massmediaID smallint, finishDate datetime, MonthDate int , MonthYear int )

insert into @monthes
SELECT distinct tw.massmediaID, c.finishDate, MonthDate = month(tw.dayActual), MonthYear = YEAR(tw.dayActual)  FROM 
		Campaign c
		inner join Issue i
			on i.campaignId = c.campaignId
		inner join TariffWindow tw on i.actualWindowID = tw.windowID
	WHERE c.campaignId = @campaignId 

if exists(select * from dbo.Campaign c where c.campaignID = @campaignId and c.campaignTypeID = 2)
begin 
	insert into @monthes
	select distinct c.massmediaID, c.finishDate, MonthDate = month(i.issueDate), MonthYear = YEAR(i.issueDate)  FROM 
		Campaign c
		inner join dbo.ProgramIssue i
			on i.campaignId = c.campaignId
		left join @monthes m on m.massmediaID = c.massmediaID and c.finishDate = m.finishDate and m.MonthDate = month(i.issueDate) and m.MonthYear = YEAR(i.issueDate)
	where c.campaignId = @campaignId and m.MonthYear is null
end

select distinct mon.MonthDate, mon.MonthYear from 
(
	select MonthDate, MonthYear, count(mm.massmediaID) mmCount 
	from @monthes as months
		inner join MassMedia mm on months.massmediaID = mm.massmediaID
	where 
		mm.[deadLine] IS NOT NULL 
				AND (months.finishDate <= mm.deadLine OR (MonthYear < YEAR(mm.[deadLine]) 
					OR (MonthYear = YEAR(mm.[deadLine]) 
						AND MonthDate < month(mm.[deadLine])) 
					OR (MonthYear = YEAR(mm.[deadLine]) 
						AND MonthDate = month(mm.[deadLine]) 
						AND mm.[deadLine] = dbo.fn_LastDateOfMonth(mm.[deadLine])) ))
	group by MonthDate, MonthYear
) as mon
inner join 
(
	select MonthDate, MonthYear, count(massmediaID) as mmCount from @monthes group by MonthDate, MonthYear
) as mon2 on mon.MonthDate = mon2.MonthDate and mon.MonthYear = mon2.MonthYear and mon.mmCount = mon2.mmCount 
ORDER BY mon.MonthYear, mon.MonthDate

