using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Threading.Tasks;

namespace Vosen.MAL
{
    class RepairScrapper : Scrapper
    {
        public RepairScrapper(bool logging, int concLimit)
            : base(logging, concLimit)
        { }

        public override void Run()
        {
            List<int> ids;
            using (var conn = OpenConnection())
            {
                ids = conn.Query<long>("SELECT CAST(\"Id\" AS BIGINT) FROM \"Anime\" WHERE \"RomajiName\" IS NULL").Select(l => (int)l).ToList();
            }
            Parallel.For(0, ids.Count, new ParallelOptions() { MaxDegreeOfParallelism = ConcurrencyLevel }, (i) => SingleQuery(ids[i]));
        }
    }
}
