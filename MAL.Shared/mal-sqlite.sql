CREATE UNIQUE INDEX IF NOT EXISTS "Name_Unique" ON "Users" ("Name");
CREATE INDEX IF NOT EXISTS "Synonym_Anime_Id" ON "Anime_Synonyms" ("Anime_Id" );

COMMIT TRANSACTION;