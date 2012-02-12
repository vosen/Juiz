using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Appender;
using System.Data.SQLite;

namespace Vosen.MAL
{
    public abstract class Crawler
    {
        protected abstract string LogName { get; }
        protected virtual string DbName { get; set; }
        protected ILog log;

        protected Crawler(bool testing)
        {
            if(testing)
                log = SetupLogger(LogName);
            else
                log = new NullLog();
        }

        private static ILog SetupLogger(string name)
        {
            FileAppender fileAppender = new FileAppender()
            {
                ImmediateFlush = false,
                LockingModel = new FileAppender.ExclusiveLock(),
                AppendToFile = false,
                File = String.Format(name + " " + DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH'-'mm") + ".log"),
                Layout = new log4net.Layout.PatternLayout(@"[%date{yyyy-MM-dd HH:mm:ss}] [%level]: %message%newline%exception")
            };
            fileAppender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(fileAppender);
            return LogManager.GetLogger(typeof(Crawler));
        }

        protected static System.Data.IDbConnection OpenConnection(string path, bool foreignKeys = true)
        {
            var conn = new SQLiteConnection(new SQLiteConnectionStringBuilder() { CacheSize = 16384, Pooling = true, SyncMode = SynchronizationModes.Off, ForeignKeys = foreignKeys, DataSource = path, FailIfMissing = true }.ToString());
            conn.Open();
            return conn;
        }

        protected System.Data.IDbConnection OpenConnection(bool foreignKeys = true)
        {
            return OpenConnection(DbName, foreignKeys);
        }
    }
}
