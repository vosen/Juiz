using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Collections;
using System.Threading.Tasks;

namespace Vosen.MAL
{
    internal class FillingMapper : Mapper
    {
        public FillingMapper(int startIndex, int stopIndex, bool logging, int concLimit)
            : base(startIndex, stopIndex, logging, concLimit) { }

        public override void Run()
        {
            IEnumerable<int> ids;
            using (var conn = OpenConnection())
            {
                ids = conn.Query<long>("SELECT Id FROM Users").Select( l => (int)l);
            }
            int rangeStart, rangeEnd;
            if(start == -1 || stop == -1)
            {
                rangeStart = ids.Min();
                rangeEnd = ids.Max();
            }
            else
            {
                rangeStart = Math.Max(start, ids.Min());
                rangeEnd = Math.Min(stop, ids.Max());
            }
            int end = ids.Max();
            var range = Enumerable.Range(rangeStart, rangeEnd).Except(ids).ToList();
            // we've got a list of indices, now time to run.
            Parallel.For(0, range.Count, new ParallelOptions() { MaxDegreeOfParallelism = ConcurrencyLevel }, i => SingleQuery(range[i]));
        }
    }
}
