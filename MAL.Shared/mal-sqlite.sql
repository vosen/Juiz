CREATE TABLE "Anime_Synonyms" (
  "Id" INTEGER NOT NULL PRIMARY KEY,
  "Text" TEXT NOT NULL,
  "Anime_Id" INTEGER NOT NULL,
  FOREIGN KEY("Anime_Id") REFERENCES "Anime"("Id")
);
CREATE INDEX "Synonym_Anime_Id" ON "Synonyms" ("Anime_Id" );

COMMIT TRANSACTION;