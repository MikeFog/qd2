CREATE TYPE [dbo].[boolean]
    FROM TINYINT NOT NULL;


GO
GRANT REFERENCES
    ON TYPE::[dbo].[boolean] TO PUBLIC;

