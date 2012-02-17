CREATE TABLE IF NOT EXISTS "Anime_Synonyms" (
  "Id" SERIAL,
  "Text" TEXT NOT NULL,
  "Anime_Id" INTEGER NOT NULL,
  FOREIGN KEY("Anime_Id") REFERENCES "Anime"("Id")
);

do $$
begin
  CREATE UNIQUE INDEX "Name_Unique" ON "Users" ("Name");
exception when SQLSTATE '42P07' then
end;
$$ language 'plpgsql';

do $$
begin
  CREATE INDEX "Synonym_Anime_Id" ON "Anime_Synonyms" ("Anime_Id" );
exception when SQLSTATE '42P07' then
end;
$$ language 'plpgsql';


COMMIT TRANSACTION;