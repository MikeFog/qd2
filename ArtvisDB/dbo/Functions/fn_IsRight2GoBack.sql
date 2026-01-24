
CREATE   FUNCTION [dbo].[fn_IsRight2GoBack] 
(
@userId smallint
)  
RETURNS BIT 
AS  
BEGIN	
	-- Заглушка, всегда возвращает false, чтобы не ломать все остальные процедуры
	if (dbo.[f_IsAdmin](@userId) = 1) or (dbo.f_IsTrafficManager(@userId) = 1)
		return 1
	return 0
END

