-- =============================================
-- Author:		Denis Gladkikh (dgladkikh@fogsoft.ru)
-- Create date: 15.01.2009
-- Description:	Получить настройку о последней дате хранения истории
-- =============================================
CREATE FUNCTION f_SysParamsDateHistorySave
(
)
returns datetime 
AS
BEGIN
	declare @days smallint 
    select top 1 @days = cast([value] as int) from iInternalVariable where [name] = 'DaysHistorySave'
	return dbo.ToShortDate(dateadd(day, -coalesce(@days, 365), getdate()))
END
