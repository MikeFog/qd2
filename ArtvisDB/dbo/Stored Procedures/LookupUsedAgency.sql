create procedure LookupUsedAgency
as
begin

	select a.agencyID as [id], LTRIM(RTRIM(a.name)) as [name] from 
	(	
		select distinct c.agencyID from dbo.Campaign c
		union 
		select distinct o.agencyID from dbo.StudioOrder o
	) x 
	inner join dbo.Agency a on x.agencyID = a.agencyID
	order by [name]
	
end
