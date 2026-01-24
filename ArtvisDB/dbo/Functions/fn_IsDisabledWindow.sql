




Create      FUNCTION dbo.fn_IsDisabledWindow 
(
@MassMediaId smallint, 
@IssueDate datetime
)  
RETURNS BIT 
AS  
BEGIN 
If Exists (
	Select	*
	From	
		DisabledWindow
	Where	
		massMediaID = @MassMediaId and
		@IssueDate between startDate And finishDate
	)
	return 1

return 0
END




