CREATE TYPE [dbo].[doubleString]
    FROM NVARCHAR (256) NULL;


GO
GRANT REFERENCES
    ON TYPE::[dbo].[doubleString] TO PUBLIC;

