using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Text.RegularExpressions;
using Dapper;
using log4net;
using log4net.Appender;
using Vosen.MAL.Content;
using Vosen.MAL;
using System.Data.Common;
using System.Threading.Tasks.Schedulers;

namespace Vosen.MAL
{
    abstract class Mapper : Crawler
    {
        protected override string LogName { get { return "mal.mapper"; } }

        protected int start;
        protected int stop;

        protected Mapper(int startIndex, int stopIndex, bool logging, int concLimit, string dbname)
            :base(logging, concLimit)
        {
            start = startIndex;
            stop = stopIndex;
            DbName = dbname;
        }

        public abstract void Run();

        protected bool SingleQuery(int idx)
        {
            try
            {
                NameResult result = Extract.DownloadName(idx);
                switch (result.Response)
                {
                    case NameResponse.Unknown:
                        ProcessUnknownResult(idx);
                        return false;
                    case NameResponse.Success:
                        ProcessSuccessResult(idx, result.Name);
                        return true;
                    case NameResponse.InvalidId:
                        ProcessInvalidResult(idx);
                        return false;
                }
            }
            catch(DbException ex)
            {
                ProcessSQLException(idx, ex);
            }
            catch (Exception ex)
            {
                ProcessNonSQLException(idx, ex);
            }
            return false;
        }

        private void ProcessNonSQLException(int idx, Exception ex)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute(@"INSERT INTO Users (Id, Name) VALUES  (@id, NULL)", new { id = idx });
            }
            log.Error(String.Format("<{0}> exception when processing", idx), ex);
        }

        private void ProcessSQLException(int idx, DbException ex)
        {
            log.Error(String.Format("<{0}> exception when processing", idx), ex);
        }

        private void ProcessInvalidResult(int idx)
        {
            using (var conn = OpenConnection(false))
            {
                conn.Execute(@"DELETE FROM Users WHERE Id = @id", new { id =idx });
            }
            log.InfoFormat("<{0}> invalid id", idx);
        }

        private void ProcessSuccessResult(int idx, string login)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute(@"INSERT INTO Users (Id, Name) VALUES  (@id, @name)", new { id = idx, name = login });
            }
            log.InfoFormat("<{0}> success", idx);
        }

        private void ProcessUnknownResult(int idx)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute(@"INSERT INTO Users (Id, Name) VALUES  (@id, NULL)", new { id = idx });
            }
            log.WarnFormat("<{0}> unknown result", idx);
        }

        protected void CreateDB()
        {
            if (!System.IO.File.Exists(DbName))
            {
                System.IO.File.Create(DbName);
                using (var manifest = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Vosen.MAL.mal.sql"))
                {
                    using (var sreader = new System.IO.StreamReader(manifest))
                    {
                        using (var conn = OpenConnection(DbName))
                        {
                            string query = sreader.ReadToEnd();
                            conn.Execute(query);
                        }
                    }
                }
            }
        }
    }
}
