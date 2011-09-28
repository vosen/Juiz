using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SQLite;

namespace Vosen.MAL
{
    class Scrapper
    {
        public int ConcurrencyLimit { get; set; }

        public Scrapper()
        {}

        public void Run()
        {
            using (var conn = OpenConnection())
            {

            }
        }

        protected SQLiteConnection OpenConnection()
        {
            var conn = new SQLiteConnection(new SQLiteConnectionStringBuilder() { CacheSize = 16384, Pooling = true, SyncMode = SynchronizationModes.Off, ForeignKeys = true, DataSource = "mal.db" }.ToString());
            conn.Open();
            return conn;
        }

        protected void ScrapAndFill(int id, string name)
        {
            using (var conn = OpenConnection())
            {

            }
        }
    }
}
