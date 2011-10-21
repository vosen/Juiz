CREATE TABLE [Watchlist] (
  [Id] INTEGER NOT NULL PRIMARY KEY);


CREATE TABLE [Seen] (
  [Watchlist_Id] INTEGER NOT NULL CONSTRAINT [Seen_Watchlist] REFERENCES [Watchlist]([Id]) ON DELETE CASCADE, 
  [Anime_Id] INTEGER NOT NULL, 
  [Score] SMALLINT NOT NULL, 
  CONSTRAINT [Score_Correct_Rage] CHECK(Score >=0 AND Score <=10));


CREATE TABLE [Users] (
  [Id] INTEGER NOT NULL PRIMARY KEY, 
  [Name] TEXT, 
  [Watchlist_Id] INTEGER);

CREATE UNIQUE INDEX [Name_Unique] ON [Users] ([Name]);


