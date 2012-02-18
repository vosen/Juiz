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