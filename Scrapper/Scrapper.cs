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
using System.Globalization;

namespace Vosen.MAL
{
    class Scrapper
    {
        private static Regex trimWhitespace = new Regex(@"\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex captureRating = new Regex(@"http://myanimelist\.net/anime/(?<id>[0-9]+?)/", RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
            return OpenConnection(DbName);
        }

        private static SQLiteConnection OpenConnection(string path)
        {
            var conn = new SQLiteConnection(new SQLiteConnectionStringBuilder() { CacheSize = 32768, Pooling = true, SyncMode = SynchronizationModes.Off, ForeignKeys = true, DataSource = path }.ToString());
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
                        MarkAsQueried(name, false);
                        Console.WriteLine("{0}\terror\t{1}\t{2}", name, ex.Status, ex.Message);
                        return;
                    }
                }

                // Check for MAL fuckups
                if (site.Contains("There was a MySQL Error."))
                {
                    MarkAsQueried(name, false);
                    Console.WriteLine("{0}\tsuccess", name);
                    return;
                }

                // Check if user exists
                if (site.Contains("Invalid Username Supplied"))
                {
                    // remove the user
                    using (var conn = OpenConnection())
                    {
                        conn.Execute("DELETE FROM Users WHERE Name = @nick", new { nick = name });
                    }
                    Console.WriteLine("{0}\tsuccess", name);
                    return;
                }

                // Check for private profile
                if(site.Contains("This list has been made private by the owner."))
                {
                    MarkAsQueried(name, true);
                    Console.WriteLine("{0}\tsuccess", name);
                    return;
                }


                var doc = new HtmlDocument();
                doc.LoadHtml(site);
                var tableNode = doc.GetElementbyId("list_surround");
                var mainIndices = FindTitleRatingIndices(tableNode);
                // check for people who don't put ratings on their profiles
                if (mainIndices == null)
                {
                    MarkAsQueried(name, false);
                    Console.WriteLine("{0}\tsuccess", name);
                    return;
                }
                var ratings = tableNode.ChildNodes
                    .Where(n => n.Name == "table" && !n.Attributes.Contains("class"))
                    .Select(n => ExtractPayload(n, mainIndices.Item1, mainIndices.Item2))
                    .Where(t => t != null)
                    .Select(t => ParseRatings(t.Item1, t.Item2))
                    .Where(t => t != null).ToList();

                long user_id = 0;
                using (var conn = OpenConnection())
                {
                    user_id = conn.Query<long>(@"SELECT Id FROM USERS WHERE Name = @nick LIMIT 1;", new { nick = name }).First();
                    conn.Execute(@"INSERT INTO Seen (Anime_Id, Score, User_Id) VALUES (@anime, @score, @user)", ratings.Select(t => new { anime = t.Item1, score = t.Item2, user = user_id }));
                }
                Console.WriteLine("{0}\tsuccess", name);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0}\terror\t\t{1}\t{2}", name, ex.Message, ex.StackTrace);
            }
        }

        protected  static bool IsHeadCell(HtmlNode node)
        {
            if (node.Name != "td")
                return false;
            var headerAttrib = node.Attributes["class"];
            if (headerAttrib == null)
                return false;
            return headerAttrib.Value == "table_header";
        }

        protected void MarkAsQueried(string name, bool success)
        {
            using (var conn = OpenConnection())
            {
                // add empty watchlist
                if(success)
                    conn.Execute(@"UPDATE Users SET Result = 1 WHERE Name = @nick;", new { nick = name });
                else
                    conn.Execute(@"UPDATE Users SET Result = 0 WHERE Name = @nick;", new { nick = name });
            }
        }

        protected static IList<HtmlNode> ExtractHeadCells(HtmlNode node)
        {
            if (node.Name != "table")
                return null;
            var row = node.ChildNodes.FirstOrDefault(n => n.Name == "tr");
            if (row == null)
                return null;
            var headNodes = row.ChildNodes.Where(IsHeadCell).ToList();
            if (headNodes.Count == 0)
                return null;
            return headNodes;
        }

        protected static Tuple<int, int> FindTitleRatingIndices(HtmlNode outerNode)
        {
            var headNodes = outerNode.ChildNodes.Select(ExtractHeadCells).FirstOrDefault(e => e != null);
            if (headNodes == null)
                return null;
            int title = -1;
            int rating = -1;
            // we've got <td> nodes containing titles
            for (int i = 0; i < headNodes.Count; i++)
            {
                // strip whitespace
                string innerText = trimWhitespace.Replace(headNodes[i].InnerText, " ");
                if (innerText == "Anime Title")
                {
                    title = i;
                }
                else
                {
                    // look for a <strong> child
                    var strongNode = headNodes[i].ChildNodes.FirstOrDefault(n => n.Name == "strong");
                    if (strongNode != null && strongNode.InnerText == "Score")
                        rating = i;
                }
                if (title != -1 && rating != -1)
                    return Tuple.Create(title, rating);
            }
            return null;
        }

        protected static Tuple<HtmlNode, HtmlNode> ExtractPayload(HtmlNode tableNode, int titleIndex, int ratingIndex)
        {
            var row = tableNode.Element("tr");
            if (row == null)
                return null;
            var cells = row.Elements("td").ToList();
            if(cells.Count < 2)
                return null;
            var linkNode = cells[titleIndex].ChildNodes.FirstOrDefault(n => n.Name == "a");
            if (linkNode == null)
                return null;
            var linkNodeClass = linkNode.Attributes["class"];
            if (linkNodeClass == null || linkNodeClass.Value != "animetitle")
                return null;
            return Tuple.Create(linkNode, cells[ratingIndex]);

        }

        protected static Tuple<int, int> ParseRatings(HtmlNode animeLink, HtmlNode ratingCell)
        {
            int rating;
            if(ratingCell.InnerText != null && Int32.TryParse(ratingCell.InnerText, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite, NumberFormatInfo.InvariantInfo, out rating))
            {
                int id = Int32.Parse(captureRating.Match(animeLink.Attributes["href"].Value).Groups["id"].Captures[0].Value);
                return Tuple.Create(id, rating);
            }
            return null;
        }

        public static void CleanDB(string path)
        {
            string dbname = path ?? "mal.db";
            using (var db = OpenConnection(dbname))
            {
                db.Execute(@"UPDATE [Users] SET [Result] = NULL;
                             DELETE FROM Seen;");
            }
        }

    }
}
