-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 17.10.2008
-- Description:	Эфирная справка
-- =============================================
CREATE procedure [dbo].[OnAirInquireReport] 
(
	@startDate datetime, 
	@finishDate datetime, 
	@massmediaId smallint, 
	@campaignID int
)
as 
begin 
	set nocount on;

    select 
		r.[name],
		dbo.fn_Int2Time(r.duration) as durationString,
		convert(varchar, tw.dayActual, 104) as issueDate,
		left(convert(varchar, tw.windowDateActual,108),5) +
		Case i.positionId
			When  -20 Then ' (F)'
			When  -15 Then ' (F)'
			When  -10 Then ' (S)'
			When  -5 Then ' (S)'
			When  10 Then ' (L)'
			When  5 Then ' (L)'
			Else ''
		End
		as issueTime,
		m.painting as dirPainting
	from
		Issue i
		inner join Roller r on i.rollerID = r.rollerID
		inner join TariffWindow tw on i.actualWindowID = tw.windowId
		inner join MassMedia m On m.massmediaID = tw.massmediaID
	where
		tw.dayActual between @startDate and @finishDate
		And m.massmediaID = @massmediaID
		And i.campaignID = @campaignID
	order by
		tw.windowDateActual

	select sp.name,
		dbo.fn_Int2Time(st.duration) as durationString,
		convert(varchar, i.issueDate, 104) as issueDate,
		left(convert(varchar, i.issueDate,108),5) as issueTime
	from dbo.ProgramIssue i 
		inner join dbo.SponsorTariff st on i.tariffID = st.tariffID
		inner join dbo.SponsorProgram sp on i.programID = sp.sponsorProgramID
	where
		i.campaignID = @campaignID
		and dbo.ToShortDate(i.issueDate) between @startDate and @finishDate
	order by
		i.issueDate

end

