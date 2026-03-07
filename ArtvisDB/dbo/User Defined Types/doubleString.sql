CREATE TYPE [dbo].[doubleString]
    FROM VARCHAR (256) NULL;


GO
GRANT REFERENCES
    ON TYPE::[dbo].[doubleString] TO PUBLIC;

