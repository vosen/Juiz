CREATE TABLE [Users] (
  [Id] INTEGER NOT NULL PRIMARY KEY, 
  [Name] TEXT NOT NULL, 
  -- 0 - not queried / got error, 1 - queried and succeeded
  [Result] BOOLEAN NOT NULL DEFAULT (0));

CREATE UNIQUE INDEX [Name_Unique] ON [Users] ([Name]);


CREATE TABLE [Seen] (
  [Anime_Id] INTEGER NOT NULL, 
  [Score] SMALLINT NOT NULL, 
  [User_Id] INTEGER NOT NULL CONSTRAINT [User_Id] REFERENCES [Users]([Id]), 
  CONSTRAINT [Score_Correct_Range] CHECK(Score >=0 AND Score <=10), 
  CONSTRAINT [] PRIMARY KEY ([Anime_Id], [User_Id]));
