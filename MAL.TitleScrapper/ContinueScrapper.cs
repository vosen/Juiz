using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Threading.Tasks;

namespace Vosen.MAL
{
    class ContinueScrapper : Scrapper
    {
        private int blockSize = 64;

        public ContinueScrapper(bool logging, int concLimit)
            : base(logging, concLimit)
        { }

        public override void Run()
        {
            int maxId;
            using (var conn = OpenConnection())
            {
                maxId = (int)conn.Query<long>("SELECT CAST(COALESCE((SELECT MAX(\"Id\") FROM \"Anime\"), 0) AS BIGINT);").First();
            }
            RunFrom(maxId + 1);
        }

        private void RunFrom(int offset)
        {

            bool[] block = new bool[blockSize];
            Parallel.For(0, blockSize, new ParallelOptions() { MaxDegreeOfParallelism = ConcurrencyLevel }, (idx) => block[idx] = SingleQuery(offset + idx));
            if (block.Any(e => e))
            {
                block = null;
                RunFrom(offset + blockSize);
            }
        }
    }
}
