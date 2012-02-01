using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;
using System.Net;
using Dapper;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Globalization;
using MALContent;
using log4net.Repository.Hierarchy;
using log4net.Appender;
using log4net;

namespace Vosen.MAL
{
    class Scrapper
    {
        private static Regex trimWhitespace = new Regex(@"\s+", RegexOptions.CultureInvariant | RegexOptions.Compiled);
        private static Regex captureRating = new Regex(@"http://myanimelist\.net/anime/(?<id>[0-9]+?)/", RegexOptions.CultureInvariant | RegexOptions.Compiled);

        public int ConcurrencyLimit { get; set; }
        public string DbName { get; set; }
        private ILog log;

        public Scrapper(bool logging=true)
        {
            DbName = "mal.db";
            if (logging)
                log = SetupLogger();
            else
                log = new NullLog();
            log.Info("Scrapping started");
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

        private static ILog SetupLogger()
        {
            FileAppender fileAppender = new FileAppender()
            {
                LockingModel = new FileAppender.ExclusiveLock(),
                AppendToFile = false,
                File = String.Format("mal.scrapper " + DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH'-'mm") + ".log"),
                Layout = new log4net.Layout.PatternLayout(@"[%date{yyyy-MM-dd HH:mm:ss}] [%level]: %message%newline%exception")
            };
            fileAppender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(fileAppender);
            return LogManager.GetLogger(typeof(Scrapper));
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
                ExtractionResult result = Extract.DownloadRatedAnime(name);
                switch (result.Response)
                {
                    case ExtractionResultType.Unknown:
                        ProcessUnknownResult(name);
                        break;
                    case ExtractionResultType.Successs:
                        ProcessSuccess(name, result.Ratings);
                        break;
                    case ExtractionResultType.MySQLError:
                        ProcessMySQLError(name);
                        break;
                    case ExtractionResultType.InvalidUsername:
                        ProcessInvalidUser(name);
                        break;
                    case ExtractionResultType.ListIsPrivate:
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
            log.Error(String.Format("<0> exception when processing",name), ex);
        }

        private void ProcessPrivate(string name)
        {
            using (var conn = OpenConnection())
                conn.Execute(@"UPDATE Users SET Result = 1 WHERE Name = @nick;", new { nick = name });
            log.InfoFormat("<{0}> list is private", name);
        }

        private void ProcessInvalidUser(string name)
        {
            using (var conn = OpenConnection())
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
                    conn.Execute(@"UPDATE [Users] SET [Result] = 0;", transaction:dbtrans);
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
