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
        protected const int blockSize = 32;
        protected const int threshold = 32;

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
            Task<bool>[] block = new Task<bool>[blockSize];
            for(int i = 0; i < blockSize; i++)
            {
                int copy = i + offset;
                block[i] = Task.Factory.StartNew(() => SingleQuery(copy));
            }
            Task.Factory.ContinueWhenAll(block, (b) => CheckBlock(b, offset)).Wait();
        }

        private void CheckBlock(Task<bool>[] tasks, int oldOffset)
        {
            int invalidCount = tasks.Aggregate(0, (i, task) =>
            {
                if (task.Result)
                    return 0;
                return ++i;
            });
            if (invalidCount >= threshold)
                return;
            RunFrom(oldOffset + blockSize);
        }
    }
}
