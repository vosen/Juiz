using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Appender;
using System.Data.SQLite;
using System.Threading.Tasks;
using System.Threading.Tasks.Schedulers;
using Dapper;
using System.Configuration;
using Npgsql;

namespace Vosen.MAL
{
    [System.ComponentModel.DataObject(true)]
    public abstract class Crawler
    {
        protected abstract string LogName { get; }
        protected int ConcurrencyLevel { get; private set; }
        protected ILog log;
        private string connectionString;
        private DbProvider provider;

        protected Crawler(bool logging, int concLimit)
        {
            CreateDBIfNotExists();
            if(logging)
                log = SetupLogger(LogName);
            else
                log = new NullLog();
            if (concLimit > 0)
                ConcurrencyLevel = concLimit;
            else
                ConcurrencyLevel = Environment.ProcessorCount * 2;

        }

        private static ILog SetupLogger(string name)
        {
            FileAppender fileAppender = new FileAppender()
            {
                ImmediateFlush = true,
                LockingModel = new FileAppender.ExclusiveLock(),
                AppendToFile = false,
                File = String.Format(name + " " + DateTime.Now.ToString("yyyy'-'MM'-'dd' 'HH'-'mm") + ".log"),
                Layout = new log4net.Layout.PatternLayout(@"[%date{yyyy-MM-dd HH:mm:ss}] [%level]: %message%newline%exception")
            };
            fileAppender.ActivateOptions();
            log4net.Config.BasicConfigurator.Configure(fileAppender);
            return LogManager.GetLogger(typeof(Crawler));
        }

        private void LoadDbSettings()
        {
            var connStrings = System.Configuration.ConfigurationManager.ConnectionStrings;
            string name = connStrings[0].ProviderName;
            if (name.ToUpperInvariant().Contains("SQLITE"))
                provider = DbProvider.SQLite;
            else if (name.ToUpperInvariant().Contains("POSTGRES"))
                provider = DbProvider.PostgreSQL;
            else
                throw new ArgumentException("Invalid db provider. Supported providers are sqlite and postgresql.");
            if(connStrings[0] == null)
                throw new ArgumentException("ConnectionString can not be empty.");
            connectionString = connStrings[0].ConnectionString;
        }


        private System.Data.IDbConnection OpenSqliteConnection()
        {
            var conn = new SQLiteConnection(connectionString);
            conn.Open();
            return conn;
        }

        private System.Data.IDbConnection OpenPostgresConnection()
        {
            var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            return conn;
        }

        protected int LastInsertId(System.Data.IDbConnection connection)
        {
            if (provider == DbProvider.SQLite)
                return (int)((SQLiteConnection)connection).LastInsertRowId;
            return connection.Execute("SELECT lastval();");
        }

        protected System.Data.IDbConnection OpenConnection()
        {
            if (provider == DbProvider.SQLite)
                return OpenSqliteConnection();
            else if (provider == DbProvider.PostgreSQL)
                return OpenPostgresConnection();
            throw new ArgumentException("Invalid db provider. Supported providers are sqlite and postgresql.");
        }

        protected void CreateDBIfNotExists()
        {
            if (provider == DbProvider.SQLite)
                CreateDBIfNotExistsSQLite();
            else if (provider == DbProvider.PostgreSQL)
                CreateDBIfNotExistsPostgres();
            throw new ArgumentException("Invalid db provider. Supported providers are sqlite and postgresql.");
        }

        private void CreateDBFromParts(string commonPart, string providerPart)
        {
            using (var commonManifest = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(commonPart))
            {
                using (var providerManifest = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(providerPart))
                {
                    using (var commonSreader = new System.IO.StreamReader(commonManifest))
                    {
                        using (var providerSreader = new System.IO.StreamReader(providerManifest))
                        {
                            string commonQuery = commonSreader.ReadToEnd();
                            string providerQuery = providerSreader.ReadToEnd();
                            using (var conn = OpenConnection())
                            {
                                conn.Execute(commonQuery + providerQuery);
                            }
                        }
                    }
                }
            }
        }

        private void CreateDBIfNotExistsPostgres()
        {
            CreateDBFromParts("Vosen.MAL.mal-common.sql", "Vosen.MAL.mal-pg.sql");
        }

        private void CreateDBIfNotExistsSQLite()
        {
            CreateDBFromParts("Vosen.MAL.mal-common.sql", "Vosen.MAL.mal-sqlite.sql");
        }
    }
}
