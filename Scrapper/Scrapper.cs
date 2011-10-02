﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Net;
using Dapper;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Vosen.MAL
{
    class Scrapper
    {
        public int ConcurrencyLimit { get; set; }
        public string DbName { get; set; }

        public Scrapper()
        {
            DbName = "mal.db";
        }

        public void Run()
        {
            ScrapAndFill();
        }

        protected SQLiteConnection OpenConnection()
        {
            var conn = new SQLiteConnection(new SQLiteConnectionStringBuilder() { CacheSize = 16384, Pooling = true, SyncMode = SynchronizationModes.Off, ForeignKeys = true, DataSource = "mal.db" }.ToString());
            conn.Open();
            return conn;
        }

        protected void ScrapAndFill()
        {
            IEnumerable<string> ids;
            using (var conn = OpenConnection())
            {
                ids = conn.Query<string>(@"SELECT Name FROM Users WHERE Watchlist_Id IS NULL");
            }
            Parallel.ForEach(ids, new ParallelOptions() { MaxDegreeOfParallelism = ConcurrencyLimit }, SingleQuery);
        }

        protected void SingleQuery(string name)
        {
            try
            {
                string site;
                using (var client = new WebClient() { Proxy = null })
                {
                    try
                    {
                        site = client.DownloadString("http://myanimelist.net/animelist/" + name + "&status=7");
                    }
                    catch (WebException ex)
                    {
                        using (var conn = OpenConnection())
                        {
                            conn.Execute(@"UPDATE Users SET Watchlist_Id = NULL WHERE Name = @nick)", new { nick = name });
                        }
                        Console.WriteLine("{0}\terror\t{1}\t{2}", name, ex.Status, ex.Message);
                        return;
                    }
                }
                var doc = new HtmlDocument();
                doc.LoadHtml(site);
                var tableNode = doc.GetElementbyId("list_surround");
                var ratings = tableNode.ChildNodes
                    .Where(n => n.Name == "table" && !n.Attributes.Contains("class"))
                    .Select(ExtractPayload)
                    .Where(t => t != null)
                    .Select(t => ParseRatings(t.Item1, t.Item2))
                    .Where(t => t != null).ToList();

                long watchlist_id = 0;
                using (var conn = OpenConnection())
                {
                    watchlist_id = conn.Query<long>(@"INSERT INTO Watchlist VALUES (NULL);
                                               UPDATE Users SET Watchlist_Id = last_insert_rowid() WHERE Name = @nick;
                                               SELECT last_insert_rowid();", new { nick = name }).First();
                    conn.Execute(@"INSERT INTO Seen VALUES (@watchlist, @anime, @score)", ratings.Select(t => new { watchlist = watchlist_id, anime = t.Item1, score = t.Item2 }));
                }
                Console.WriteLine("{0}\tsuccess", name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}\terror\t\t{1}\t{2}", name, ex.Message, ex.StackTrace);
            }
        }

        protected static Tuple<HtmlNode, HtmlNode> ExtractPayload(HtmlNode tableNode)
        {
            var row = tableNode.Element("tr");
            if (row == null)
                return null;
            var cells = row.Elements("td").ToList();
            if(cells.Count < 2)
                return null;
            var linkNode = cells.Select(n => n.Element("a")).First(n => n != null);
            if (linkNode == null)
                return null;
            var linkNodeClass = linkNode.Attributes["class"];
            if (linkNodeClass == null || linkNodeClass.Value != "animetitle")
                return null;
            var scoreNode = linkNode.ParentNode.NextSibling;
            while (scoreNode.Name != "td")
            {
                scoreNode = scoreNode.NextSibling;
                if (scoreNode == null)
                    return null;
            }
            return Tuple.Create(linkNode, scoreNode);

        }

        protected static Tuple<int, int> ParseRatings(HtmlNode animeLink, HtmlNode ratingCell)
        {
            int id = Int32.Parse(Regex.Match(animeLink.Attributes["href"].Value, Regex.Escape(@"http://myanimelist.net/anime/") + @"(?<id>[0-9]+?)").Groups["id"].Captures[0].Value);
            string rating = ratingCell.InnerText;
            if (rating == "-")
                return null;
            return Tuple.Create(id, Int32.Parse(rating));
        }

    }
}
