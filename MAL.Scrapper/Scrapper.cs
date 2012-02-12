using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using Dapper;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using Vosen.MAL.Content;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using log4net;

namespace Vosen.MAL
{
    class Scrapper : Crawler
    {
        private static Regex trimWhitespace = new Regex(@"\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex captureRating = new Regex(@"http://myanimelist\.net/anime/(?<id>[0-9]+?)/", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        override protected string LogName { get { return "mal.scrapper"; } }
        private int ConcurrencyLimit { get; set; }

        public Scrapper(bool logging, int concLimit, string dbName)
            :base(logging)
        {
            ConcurrencyLimit = concLimit;
            DbName = dbName;
        }

        public void Run()
        {
            ScrapAndFill();
        }

        protected void ScrapAndFill()
        {
            IEnumerable<string> ids;
            using (var conn = OpenConnection())
            {
                ids = conn.Query<string>(@"SELECT Name FROM Users WHERE Result = 0");
            }
            Parallel.ForEach(ids, new ParallelOptions() { MaxDegreeOfParallelism = ConcurrencyLimit }, SingleQuery);
        }

        protected void SingleQuery(string name)
        {
            try
            {
                AnimelistResult result = Extract.DownloadRatedAnime(name);
                switch (result.Response)
                {
                    case AnimelistResponse.Unknown:
                        ProcessUnknownResult(name);
                        break;
                    case AnimelistResponse.Successs:
                        ProcessSuccess(name, result.Ratings);
                        break;
                    case AnimelistResponse.MySQLError:
                        ProcessMySQLError(name);
                        break;
                    case AnimelistResponse.InvalidUsername:
                        ProcessInvalidUser(name);
                        break;
                    case AnimelistResponse.ListIsPrivate:
                        ProcessPrivate(name);
                        break;
                }
            }
            catch (Exception ex)
            {
                ProcessException(name, ex);
            }
        }

        private void ProcessException(string name, Exception ex)
        {
            log.Error(String.Format("<{0}> exception when processing", name), ex);
        }

        private void ProcessPrivate(string name)
        {
            using (var conn = OpenConnection())
                conn.Execute(@"UPDATE Users SET Result = 1 WHERE Name = @nick;", new { nick = name });
            log.InfoFormat("<{0}> list is private", name);
        }

        private void ProcessInvalidUser(string name)
        {
            using (var conn = OpenConnection(false))
                conn.Execute("DELETE FROM Users WHERE Name = @nick", new { nick = name });
            log.InfoFormat("<{0}> invalid user", name);
        }

        private void ProcessMySQLError(string name)
        {
            using (var conn = OpenConnection())
                conn.Execute(@"UPDATE Users SET Result = 1 WHERE Name = @nick;", new { nick = name });
            log.InfoFormat("<{0}> MySQL error", name);
        }

        private void ProcessSuccess(string name, IList<AnimeRating> ratings)
        {
            using (var conn = OpenConnection())
            {
                using (var dbtrans = conn.BeginTransaction())
                {
                    long user_id = conn.Query<long>(@"SELECT Id FROM USERS WHERE Name = @nick LIMIT 1;", new { nick = name }, transaction:dbtrans).First();
                    conn.Execute(@"INSERT INTO Seen (Anime_Id, Score, User_Id) VALUES (@anime, @score, @user);", ratings.Select(t => new { anime = t.AnimeId, score = t.Rating, user = user_id}), transaction:dbtrans);
                    conn.Execute(@"UPDATE Users SET Result = 1 WHERE Name = @nick;", new { nick = name }, transaction:dbtrans);
                    dbtrans.Commit();
                }
            }
            log.InfoFormat("<{0}> success", name);
        }

        private void ProcessUnknownResult(string name)
        {
            log.WarnFormat("<{0}> result unknown", name);
        }

        public static void CleanDB(string path)
        {
            string dbname = path ?? "mal.db";
            using (var db = OpenConnection(dbname))
            {
                db.Execute(@"UPDATE [Users] SET [Result] = 0;
                             DELETE FROM Seen;");
            }
        }
    }
}
