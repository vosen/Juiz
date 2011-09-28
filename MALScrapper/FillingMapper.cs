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
        public FillingMapper() : base() {}

        public override void Run()
        {
            IEnumerable<int> ids;
            using (var conn = OpenConnection())
            {
                ids = conn.Query<long>("SELECT Id FROM Users").Select( l => (int)l);
            }
            int start = ids.Min();
            int end = ids.Max();
            var range = Enumerable.Range(start, end - start).Except(ids);
            // we've got a list of indices, now time to run.
            ScanAndFill(range);
        }
    }
}
