﻿BEGIN TRANSACTION;
CREATE TABLE "Users" (
  "Id" INTEGER NOT NULL PRIMARY KEY,
  "Name" TEXT NULL, 
  -- 0 - not queried / got error, 1 - queried and succeeded
  "Result" BOOLEAN NOT NULL DEFAULT '0'
);
CREATE UNIQUE INDEX "Name_Unique" ON "Users" ("Name");

CREATE TABLE "Seen" (
  "Anime_Id" INTEGER NOT NULL, 
  "Score" SMALLINT NOT NULL, 
  "User_Id" INTEGER NOT NULL,
  CONSTRAINT "Score_Correct_Range" CHECK( "Score" >=0 AND "Score" <=10), 
  CONSTRAINT "pk_Seen" PRIMARY KEY ("Anime_Id", "User_Id"),
  FOREIGN KEY("User_Id") REFERENCES "Users"("Id") ON DELETE NO ACTION
);

CREATE TABLE "Anime" (
  "Id" INTEGER NOT NULL PRIMARY KEY,
  "EnglishName" TEXT NULL,
  "RomajiName" TEXT NULL
);
