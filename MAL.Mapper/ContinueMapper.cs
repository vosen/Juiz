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
            Task<bool>[] block = new Task<bool>[blockSize];
            for (int i = 0; i < block.Length; i++)
            {
                int idx = i;
                block[idx] = TaskFactory.StartNew(() => SingleQuery(offset + idx));
            }
            Task.WaitAll(block);
            if (block.Any(task => task.Result))
                RunFrom(offset + blockSize);
        }
    }
}
