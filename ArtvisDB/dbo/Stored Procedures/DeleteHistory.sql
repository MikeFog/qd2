-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 15.01.2009
-- Description:	
-- =============================================
create procedure DeleteHistory 
as 
begin 
	set nocount on;

	declare @lastDate datetime
	
	set @lastDate = dbo.f_SysParamsDateHistorySave()

    delete from LogDeletedIssue
    where date < @lastDate and issueDate < @lastDate
    
    delete from Announcement 
    where dateConfirmed < @lastDate
		or (isConfirmationRequired = 0 and dateCreated < @lastDate)
		
	delete from TransferLog 
	where transferDate < @lastDate and newDate < @lastDate and oldDate < @lastDate
	
	delete from ConfirmationHistory 
	where dateCreated < @lastDate
end
