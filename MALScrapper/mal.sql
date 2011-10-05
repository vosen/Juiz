CREATE TABLE [Seen] (
  [Watchlist_Id] INTEGER NOT NULL CONSTRAINT [Seen_Watchlist] REFERENCES [Watchlist]([Id]), 
  [Anime_Id] INTEGER NOT NULL, 
  [Score] TINYINT NOT NULL);


CREATE TABLE [Watchlist] (
  [Id] INTEGER NOT NULL PRIMARY KEY);


CREATE TABLE [Users] (
  [Id] INTEGER NOT NULL PRIMARY KEY, 
  [Name] TEXT, 
  [Watchlist_Id] INTEGER);

  CREATE UNIQUE INDEX [Name_Unique] ON [Users] ([Name]);