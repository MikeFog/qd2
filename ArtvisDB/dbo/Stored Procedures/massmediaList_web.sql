CREATE   PROC [dbo].[massmediaList_web]
(
    @showActive bit = 1,
    @massmediaGroupID int = NULL,
    @search nvarchar(100) = NULL,
    @skip int = 0,
    @take int = 50,
    @sort nvarchar(50) = N'name',      -- name | groupName | isActive
    @dir  nvarchar(4)  = N'asc'        -- asc | desc
)
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH src AS
    (
        SELECT
            mm.massmediaID,
            mm.name,
            mm.isActive,
            v.groupName,
            v.nameWithGroup,
			v.prefix
        FROM dbo.MassMedia mm
        LEFT JOIN dbo.vMassmedia v ON v.massmediaID = mm.massmediaID
        WHERE
            (@showActive = 0 OR mm.isActive = 1)
            AND (@massmediaGroupID IS NULL OR mm.massmediaGroupID = @massmediaGroupID)
            AND (
                @search IS NULL OR @search = N'' OR
                mm.name LIKE N'%' + @search + N'%' OR
                v.groupName LIKE N'%' + @search + N'%'
            )
    )
    SELECT
        massmediaID, name, groupName, nameWithGroup, isActive, prefix,
        totalCount = COUNT(*) OVER()
    FROM src
    ORDER BY
        CASE WHEN @sort = N'name'      AND @dir = N'asc'  THEN name      END ASC,
        CASE WHEN @sort = N'name'      AND @dir = N'desc' THEN name      END DESC,
        CASE WHEN @sort = N'groupName' AND @dir = N'asc'  THEN groupName END ASC,
        CASE WHEN @sort = N'groupName' AND @dir = N'desc' THEN groupName END DESC,
        CASE WHEN @sort = N'isActive'  AND @dir = N'asc'  THEN CAST(isActive as int) END ASC,
        CASE WHEN @sort = N'isActive'  AND @dir = N'desc' THEN CAST(isActive as int) END DESC,
        massmediaID
    OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY;
END
