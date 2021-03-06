﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vosen.MAL.Content;
using Dapper;

namespace Vosen.MAL
{
    internal abstract class Scrapper : Crawler
    {
        protected override string LogName { get { return "mal.titlescrapper"; } }

        public Scrapper(bool logging, int concLimit)
            : base(logging, concLimit)
        { }

        protected bool SingleQuery(int id)
        {
            try
            {
                AnimeResult result = Extract.DownloadAnimeNames(id);
                switch(result.Response)
                {
                    case AnimeResponse.Unknown:
                        ProcessUnknownResult(id);
                        return false;
                    case AnimeResponse.InvalidId:
                        ProcessInvalidResult(id);
                        return false;
                    case AnimeResponse.Successs:
                        ProcessSuccess(id, result);
                        return true;
                }
            }
            catch (Exception ex)
            {
                ProcessException(id, ex);
            }
            return false;
        }

        private void ProcessException(int id, Exception ex)
        {
            using (var conn = OpenConnection())
            {
                var trans = conn.BeginTransaction();
                conn.Execute("DELETE FROM \"Anime\" WHERE \"Id\" = :id", new { id = id }, trans);
                conn.Execute("INSERT INTO \"Anime\" (\"Id\") VALUES (:id)", new { id = id }, trans);
                trans.Commit();
            }
            log.Error(String.Format("<{0}> exception when processing", id), ex);
        }

        private void ProcessSuccess(int id, AnimeResult result)
        {
            using (var conn = OpenConnection())
            {
                var trans = conn.BeginTransaction();
                conn.Execute("DELETE FROM \"Anime\" WHERE \"Id\" = :id;", new { id = id }, trans);
                conn.Execute("DELETE FROM \"Anime_Synonyms\" WHERE \"Anime_Id\" = :id;", new { id = id }, trans);
                conn.Execute("INSERT INTO \"Anime\" (\"Id\", \"RomajiName\", \"EnglishName\") VALUES (:id, :romajiName, :englishName)", new { id = id, romajiName = result.RomajiName, englishName = result.EnglishName }, trans);
                conn.Execute("INSERT INTO \"Anime_Synonyms\" (\"Text\", \"Anime_Id\") VALUES (:text, :id)", result.Synonyms.Select(syn => new { text = syn, id = id }), trans);
                trans.Commit();
            }
            log.InfoFormat("<{0}> success", id);
        }

        private void ProcessInvalidResult(int id)
        {
            using (var conn = OpenConnection())
            {
                conn.Execute("DELETE FROM \"Anime\" WHERE \"Id\" = :id", new { id = id });
            }
            log.WarnFormat("<{0}> invalid id", id);
        }

        private void ProcessUnknownResult(int id)
        {
            using (var conn = OpenConnection())
            {
                var trans = conn.BeginTransaction();
                conn.Execute("DELETE FROM \"Anime\" WHERE \"Id\" = :id", new { id = id }, trans);
                conn.Execute("INSERT INTO \"Anime\" (\"Id\") VALUES (:id)", new { id = id }, trans);
                trans.Commit();
            }
            log.WarnFormat("<{0}> result unknown", id);
        }

        public abstract void Run();
    }
}
