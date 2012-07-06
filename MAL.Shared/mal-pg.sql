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

CREATE OR REPLACE RULE replace_user AS
	ON INSERT TO "Users"
	WHERE
		EXISTS(SELECT 1 FROM "Users" WHERE "Id" = NEW."Id")
	DO INSTEAD
		(UPDATE "Users" SET "Name"=NEW."Name" WHERE "Id" = NEW."Id");


COMMIT TRANSACTION;