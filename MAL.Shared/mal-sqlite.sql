CREATE TABLE IF NOT EXISTS "Anime_Synonyms" (
  "Id" INTEGER NOT NULL PRIMARY KEY,
  "Text" TEXT NOT NULL,
  "Anime_Id" INTEGER NOT NULL,
  FOREIGN KEY("Anime_Id") REFERENCES "Anime"("Id")
);

CREATE UNIQUE INDEX IF NOT EXISTS "Name_Unique" ON "Users" ("Name");
CREATE INDEX IF NOT EXISTS "Synonym_Anime_Id" ON "Anime_Synonyms" ("Anime_Id" );

COMMIT TRANSACTION;