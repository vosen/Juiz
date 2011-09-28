using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Net;
using System.Text.RegularExpressions;
using Dapper;

namespace Vosen.MAL
{
    internal class Mapper
    {
        public int ConcurrencyLimit { get; set; }
        public string DbName { get; set; }

        private int start;
        private int stop;
        private static string addUserCommand = @"INSERT OR REPLACE INTO Users (Id, Name) VALUES  (@id, @name)";

        protected Mapper() 
        {
            DbName = "mal.db";
        }

        public Mapper(int startIndex, int stopIndex)
            :this()
        {
            start = startIndex;
            stop = stopIndex;
        }

        public virtual void Run()
        {
            CreateDB();
            ScanAndFill(Enumerable.Range(start, stop - start + 1));
        }

        protected virtual void ScanAndFill(IEnumerable<int> ids)
        {
            Parallel.ForEach(ids, new ParallelOptions() { MaxDegreeOfParallelism = ConcurrencyLimit }, (idx) =>
            {
                string site;
                using (var client = new WebClient() { Proxy = null })
                {
                    try
                    {
                        site = client.DownloadString("http://www.myanimelist.net/comments.php?id=" + idx);
                    }
                    catch (WebException ex)
                    {
                        using (var conn = OpenConnection())
                        {
                            conn.Execute(addUserCommand, new { id = idx, name = (string)null });
                        }
                        Console.WriteLine("{0}\terror\t{1}\t{2}\t{3}", idx, ex.Response, ex.Status, ex.Message);
                        return;
                    }
                }
                var match = Regex.Match(site, Regex.Escape("http://www.myanimelist.net/profile.php?username=") + @"(?<name>.+?)" + Regex.Escape("\""));
                if (match.Success)
                {
                    var login = match.Groups["name"].Captures[0].Value;
                    using (var conn = OpenConnection())
                    {
                        conn.Execute(addUserCommand, new { id = idx, name = login });
                    }
                    Console.WriteLine("{0}\tsuccess", idx);
                }
                else
                {
                    Console.WriteLine("{0}\tinvalid", idx);
                }
                
            });
        }

        protected SQLiteConnection OpenConnection()
        {
            var conn = new SQLiteConnection(new SQLiteConnectionStringBuilder() { CacheSize = 16384, Pooling = true, SyncMode = SynchronizationModes.Off, ForeignKeys = true, DataSource = DbName }.ToString());
            conn.Open();
            return conn;
        }

        protected void CreateDB()
        {
            var conn = new SQLiteConnection(new SQLiteConnectionStringBuilder() { CacheSize = 16384, Pooling = true, SyncMode = SynchronizationModes.Off, ForeignKeys = true, DataSource = DbName }.ToString());
            if (!System.IO.File.Exists(DbName))
            {
                SQLiteConnection.CreateFile(DbName);
                using (var manifest = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Vosen.MAL.mal.sql"))
                {
                    using (var sreader = new System.IO.StreamReader(manifest))
                    {
                        conn.Open();
                        string query = sreader.ReadToEnd();
                        conn.Execute(query);
                    }
                }
            }
            conn.Dispose();
        }
    }
}
