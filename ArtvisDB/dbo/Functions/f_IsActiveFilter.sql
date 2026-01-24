


CREATE FUNCTION dbo.f_IsActiveFilter
(
@CheckValue bit,
@FilterValue bit
)
RETURNS bit
BEGIN
	RETURN
	case
		when @FilterValue = 1 and @CheckValue = 0 then 0
		else 1
	end
END



