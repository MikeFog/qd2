CREATE TYPE [dbo].[timeDuration]
    FROM INT NOT NULL;


GO
GRANT REFERENCES
    ON TYPE::[dbo].[timeDuration] TO PUBLIC;

