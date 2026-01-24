



Create  FUNCTION dbo.f_IsActiveChildFilter
(
@ParentValue int,
@CheckValue bit,
@FilterValue bit
)
RETURNS bit
BEGIN
	RETURN
	case
		when @FilterValue = 1 and @CheckValue = 0 and @ParentValue Is Null then 0
		else 1
	end
END




