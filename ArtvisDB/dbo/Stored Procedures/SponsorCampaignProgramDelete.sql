
/*
Mdified: Denis Gladkikh (dgladkikh@fogsoft.ru) 17.09.2008 - Add broadcast start logic to sponsor price list
*/
CREATE PROCEDURE [dbo].[SponsorCampaignProgramDelete]
(
	@campaignID AS INT,
	@programID AS SMALLINT = NULL,
	@loggedUserID AS SMALLINT,
	@actionName AS VARCHAR(32),
	@issueDate AS DATETIME = NULL
)
AS
begin
	SET NOCOUNT ON
	if @actionName <> 'DeleteItem' 
		return 
		
	if @issueDate is not null 
		set @issueDate = dbo.ToShortDate(@issueDate)		
		
	declare @issues table (issueID int primary key)
	insert into @issues 
	select i.issueID
		from ProgramIssue i
			inner join SponsorTariff st on i.tariffID = st.tariffID
			inner join SponsorProgramPricelist pl on st.pricelistID = pl.pricelistID
		where i.campaignID = @campaignID 
			and i.programID = coalesce(@programID, i.programID)
			and (@issueDate is null or dbo.ToShortDate(DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate))) = @issueDate)
	
	if dbo.f_IsAdmin(@loggedUserID) <> 1 
		and exists(select * from @issues it 
					inner join ProgramIssue i on it.issueID = i.issueID 
					inner join SponsorTariff st on i.tariffID = st.tariffID
					inner join SponsorProgramPricelist pl on st.pricelistID = pl.pricelistID
					where dbo.ToShortDate(DATEADD(mi, -DATEPART(mi, pl.broadcastStart), DATEADD(hh, -DATEPART(hh, pl.broadcastStart), i.issueDate))) < Convert(datetime, Convert(varchar(8), dateadd(day, 1, getdate()), 112), 112))
	begin 
		raiserror('PastIssue', 16, 1)
		return
	end
	
	if exists(select c.campaignID, sum(pl.bonus) from @issues it
				inner join ProgramIssue i on it.issueID = i.issueID 
				inner join Campaign c on i.campaignID = c.campaignID
				inner join SponsorTariff st on i.tariffID = st.tariffID
				inner join [SponsorProgramPricelist] pl ON st.[pricelistID] = pl.[pricelistID]
			  group by c.campaignID, c.issuesDuration, c.timeBonus
			  having c.issuesDuration > c.timeBonus - sum(pl.bonus))
	begin 
		raiserror('ProgramIssueDeleteBonusError', 16, 1)
		return 
	end 
	
	delete from i from ProgramIssue i inner join @issues it on i.issueID = it.issueID
		
end 





