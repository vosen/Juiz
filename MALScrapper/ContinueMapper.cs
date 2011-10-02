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
        protected int blockSize = 128;
        protected int threshold = 32;

        public ContinueMapper() : base() { }

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
            Parallel.For(0, blockSize, new ParallelOptions() { MaxDegreeOfParallelism = ConcurrencyLimit }, (idx) =>
            {
                block[idx] = SingleQuery(offset + idx);
            });
            CheckBlock(block, offset);
        }

        private void CheckBlock(bool[] tasks, int oldOffset)
        {
            int invalidCount = tasks.Aggregate(0, (i, task) =>
            {
                if (task)
                    return 0;
                return ++i;
            });
            if (invalidCount >= threshold)
                return;
            RunFrom(oldOffset + blockSize);
        }
    }
}
