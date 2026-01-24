Create PROC [dbo].[TariffWindowRetrieveByDate]
(
@windowDate datetime,
@massmediaID int
)
as
select tw.* 
from 
	TariffWindow tw
where 
	tw.massmediaID = @massmediaID
	and tw.windowDateOriginal = @windowDate
	