using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Dapper;
using System.Collections;

namespace Vosen.MAL
{
    internal class FillingMapper : Mapper
    {
        public FillingMapper(int startIndex, int stopIndex, bool logging, int concLimit, string dbname)
            : base(startIndex, stopIndex, logging, concLimit, dbname) { }

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
                rangeEnd = stop - start + 1;
            }
            else
            {
                rangeStart = ids.Min();
                rangeEnd = ids.Max() - rangeStart;
            }
            int end = ids.Max();
            var range = Enumerable.Range(rangeStart, rangeEnd).Except(ids);
            // we've got a list of indices, now time to run.
            ScanAndFill(range);
        }
    }
}
