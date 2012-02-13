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
        protected int blockSize = 256;

        public ContinueMapper(int startIndex, int stopIndex, bool logging, int concLimit, string dbname)
            : base(startIndex, stopIndex, logging, concLimit, dbname) { }

        public override void Run()
        {
            int offset = 0;
            using (var conn = OpenConnection())
            {
                offset = (int)conn.Query<long>("SELECT Max(Id) FROM Users").First() + 1;
            }
            RunFrom(offset);
        }

        private void RunFrom(int offset)
        {
            bool[] block = new bool[blockSize];
            Parallel.For(0, blockSize, new ParallelOptions() { MaxDegreeOfParallelism = ConcurrencyLevel }, (idx) => block[idx] = SingleQuery(offset + idx));
            if (block.Any())
            {
                block = null;
                RunFrom(offset + blockSize);
            }
        }
    }
}
