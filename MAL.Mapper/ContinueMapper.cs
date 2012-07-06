using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Collections;
using System.Threading.Tasks;

namespace Vosen.MAL
{
    internal class ContinueMapper : Mapper
    {
        protected int blockSize = 1024;

        public ContinueMapper(int startIndex, int stopIndex, bool logging, int concLimit)
            : base(startIndex, stopIndex, logging, concLimit) { }

        public override void Run()
        {
            int offset = 0;
            using (var conn = OpenConnection())
            {
                offset = (int)conn.Query<int>("SELECT CAST(COALESCE((SELECT Max(\"Id\") FROM \"Users\"), 0) AS BIGINT)").First() + 1;
            }
            RunFrom(offset);
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
